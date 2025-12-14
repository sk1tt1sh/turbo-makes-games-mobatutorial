using Unity.Collections;
using Unity.Entities;
using UnityEngine.AI;
using UnityEngine.Serialization;

namespace EntitiesNavMeshBuilder.Data
{
    public struct NavMeshCollection : IComponentData
    {
        public NativeList<NavMeshBuildSource> sources;
        public NativeReference<NavMeshCollectionMetadata> metadata;
    }
}