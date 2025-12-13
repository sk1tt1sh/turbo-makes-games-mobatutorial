// ChampMoveInputSystem.cs
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using UnityEngine;

/// <summary>
/// WASD camera-relative movement input for DOTS characters.
/// Replaces Troy's click-to-move system with MMO-style WASD controls.
/// </summary>
[UpdateInGroup(typeof(GhostInputSystemGroup))]
public partial class ChampMoveInputSystem : SystemBase
{
    protected override void OnCreate()
    {
        RequireForUpdate<OwnerChampTag>();
    }

    protected override void OnUpdate()
    {
        // Only process input for the local player's champion
        if (!SystemAPI.TryGetSingletonEntity<OwnerChampTag>(out Entity champEntity))
            return;

        // Get camera for camera-relative movement
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogWarning("No main camera found for WASD input!");
            return;
        }

        // Read WASD input
        float forward = 0f;
        float strafe = 0f;

        if (Input.GetKey(KeyCode.W)) forward += 1f;
        if (Input.GetKey(KeyCode.S)) forward -= 1f;
        if (Input.GetKey(KeyCode.A)) strafe -= 1f;
        if (Input.GetKey(KeyCode.D)) strafe += 1f;

        // Get current position (needed both for movement and for STOP)
        var currentTransform = EntityManager.GetComponentData<Unity.Transforms.LocalTransform>(champEntity);

        // If no input, STOP by setting target to current position
        if (Mathf.Abs(forward) < 0.01f && Mathf.Abs(strafe) < 0.01f)
        {
            EntityManager.SetComponentData(champEntity, new ChampMoveTargetPosition
            {
                Value = currentTransform.Position
            });
            return;
        }

        // Calculate camera-relative movement direction
        Vector3 cameraForward = mainCamera.transform.forward;
        cameraForward.y = 0f;
        cameraForward.Normalize();

        Vector3 cameraRight = mainCamera.transform.right;
        cameraRight.y = 0f;
        cameraRight.Normalize();

        Vector3 moveDirection = (cameraForward * forward + cameraRight * strafe).normalized;

        // Set target position a short distance ahead in movement direction (tighter feel than 10 units)
        float lookAhead = 1.0f; // try 0.5f–2.0f to taste
        float3 targetPosition = currentTransform.Position + (float3)(moveDirection * lookAhead);

        // Update the move target component
        EntityManager.SetComponentData(champEntity, new ChampMoveTargetPosition
        {
            Value = targetPosition
        });

        // Clear any auto-attack target when moving (optional - comment out if you don't want this)
        if (SystemAPI.HasComponent<ChampTargetGhost>(champEntity))
        {
            EntityManager.SetComponentData(champEntity, new ChampTargetGhost { TargetId = 0 });
        }
    }
}
