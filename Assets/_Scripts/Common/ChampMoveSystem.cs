// ChampMoveSystem.cs (fixed: always ground-snap, even when idle)
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;

[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
public partial class ChampMoveSystem : SystemBase {
  protected override void OnCreate() {
    RequireForUpdate<GamePlayingTag>();
  }

  protected override void OnUpdate() {
    float deltaTime = SystemAPI.Time.DeltaTime;

    const float MOVEMENT_THRESHOLD = 0.1f;
    const float GROUND_SNAP_OFFSET = 0.5f;   // how high above hit.point the character should stand
    const float RAY_START_HEIGHT = 25f;    // start well above terrain
    const float RAY_DISTANCE = 200f;   // long enough for tall terrain / bad Y states

    // Optional: restrict to a ground layer if you have it set up
    // int groundMask = LayerMask.GetMask("Default", "Terrain", "Ground");
    // For now: everything
    int groundMask = ~0;

    foreach(var (transform, movePosition, moveSpeed) in
             SystemAPI.Query<
                 RefRW<LocalTransform>,
                 RefRO<ChampMoveTargetPosition>,
                 RefRO<CharacterMoveSpeed>>()
             .WithNone<ChampDashingTag>()
             .WithAll<Simulate>()) {
      float3 currentPos = transform.ValueRO.Position;
      float3 targetPos = movePosition.ValueRO.Value;

      // --- XZ movement (only if not already at target) ---
      float2 curXZ = new float2(currentPos.x, currentPos.z);
      float2 tarXZ = new float2(targetPos.x, targetPos.z);
      float distanceXZ = math.distance(curXZ, tarXZ);

      float3 newPos = currentPos;
      float3 directionXZ = float3.zero;

      bool shouldMove = distanceXZ >= MOVEMENT_THRESHOLD;

      if(shouldMove) {
        directionXZ = math.normalize(new float3(targetPos.x - currentPos.x, 0f, targetPos.z - currentPos.z));

        // guard against NaNs
        if(math.lengthsq(directionXZ) > 0.0001f) {
          float3 step = directionXZ * moveSpeed.ValueRO.Value * deltaTime;
          newPos += step;

          // Rotate to face movement direction
          transform.ValueRW.Rotation = quaternion.LookRotationSafe(directionXZ, math.up());
        }
      }

      // --- ALWAYS ground-snap (even when idle) ---
      // Start ray above the *new* XZ position, not based on current Y (which might already be wrong).
      Vector3 rayStart = new Vector3(newPos.x, newPos.y + RAY_START_HEIGHT, newPos.z);

      if(Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, RAY_DISTANCE, groundMask, QueryTriggerInteraction.Ignore)) {
        //newPos.y = hit.point.y + GROUND_SNAP_OFFSET;
        Debug.DrawRay(rayStart, Vector3.down, Color.red, 0.5f);
      }
      else {
        // If we didn't find ground, do NOT force falling while idle; keep current Y.
        // (If you want gravity, do it in a dedicated gravity/physics system.)
        Debug.DrawRay(rayStart, Vector3.down, Color.green, 0.5f);
        //newPos.y = currentPos.y;
      }

      transform.ValueRW.Position.x = newPos.x;
      transform.ValueRW.Position.z = newPos.z;
    }
  }
}