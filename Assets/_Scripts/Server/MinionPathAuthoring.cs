using Unity.Entities;
using UnityEngine;

public class MinionPathAuthoring : MonoBehaviour {
  public Vector3[] TopLanePath;
  public Vector3[] MidLanePath;
  public Vector3[] BotLanePath;


  public class MinionPathAuthoringBaker : Baker<MinionPathAuthoring> {
    public override void Bake(MinionPathAuthoring authoring) {
      Entity entity = GetEntity(TransformUsageFlags.None);//Data only entity does not need transform
      Entity topLane = CreateAdditionalEntity(TransformUsageFlags.None,false, "TopLane");
      Entity midLane = CreateAdditionalEntity(TransformUsageFlags.None,false, "MidLane");
      Entity botLane = CreateAdditionalEntity(TransformUsageFlags.None,false, "BotLane");

      var topLanePath = AddBuffer<MinionPathPosition>(topLane);
      foreach(var pos in authoring.TopLanePath) {
        topLanePath.Add(new MinionPathPosition { Value = pos });
      }
      
      
      var midLanePath = AddBuffer<MinionPathPosition>(midLane);
      foreach(var pos in authoring.MidLanePath) {
        midLanePath.Add(new MinionPathPosition { Value = pos });
      }
      
      var botLanePath = AddBuffer<MinionPathPosition>(botLane);
      foreach(var pos in authoring.BotLanePath) {
        botLanePath.Add(new MinionPathPosition { Value = pos });
      }

      AddComponent(entity, new MinionPathContainers {
        BotLane = botLane,
        MidLane = midLane,
        TopLane = topLane
      });
    }
  }
}