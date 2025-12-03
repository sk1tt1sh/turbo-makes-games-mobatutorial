using Unity.Entities;
using UnityEngine;

class MobaTeamAuthoring : MonoBehaviour {
  public TeamType MobaTeam;

  class MobaTeamAuthoringBaker : Baker<MobaTeamAuthoring> {
    public override void Bake(MobaTeamAuthoring authoring) {
      Entity entity = GetEntity(TransformUsageFlags.Dynamic);
      AddComponent(entity, new MobaTeam { Value = authoring.MobaTeam });
    }
  }
}