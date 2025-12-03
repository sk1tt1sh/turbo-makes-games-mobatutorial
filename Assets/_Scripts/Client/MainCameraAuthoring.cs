using Unity.Entities;
using UnityEngine;

public class MainCameraAuthoring : MonoBehaviour {

}

public class MainCameraBaker : Baker<MainCameraAuthoring> {
  public override void Bake(MainCameraAuthoring authoring) {
    Entity entity = GetEntity(TransformUsageFlags.Dynamic);
    AddComponentObject(entity, new MainCamera());
    AddComponent<MainCameraTag>(entity);
  }
}

