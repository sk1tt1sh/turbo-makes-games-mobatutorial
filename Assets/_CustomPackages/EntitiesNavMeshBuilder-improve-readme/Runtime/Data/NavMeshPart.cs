using Unity.Entities;

namespace EntitiesNavMeshBuilder.Data
{
    public struct NavMeshPart : IComponentData {
        public int area;
    }

    public struct MeshNavMeshPart : IComponentData { }

    public struct ColliderNavMeshPart : IComponentData { }
}