# EntitiesNavMeshBuilder

https://youtu.be/9Z5d9XStRzQ

<a href="https://youtu.be/9Z5d9XStRzQ" target="_blank">
 <img src="https://github.com/bustedbunny/EntitiesNavMeshBuilder/assets/30902981/f3bd9dea-e9eb-474c-ae6b-0f4c86ddc98b" alt="Previw Video" />
</a>

## Usage

- If you're using an hybrid approach, add `NavMeshAuthoring` component to your `GameObjects`.
- If not, the native ECS approach is to add `NavMeshPart` component to your entities as well as `MeshNavMeshPart` or `ColliderNavMeshPart`.

ECS Example:
```c#
if (state.EntityManager.HasComponent<PhysicsCollider>(entity)) {
    ecb.AddComponent(entity, new NavMeshPart() { 
        // area = set a different area than 0 if you want
    });
    ecb.AddComponent(entity, new ColliderNavMeshPart());
} else if (state.EntityManager.HasComponent<MaterialMeshInfo>(entity)) {
    ecb.AddComponent(entity, new NavMeshPart() { 
        // area = set a different area than 0 if you want
    });
    ecb.AddComponent(entity, new MeshNavMeshPart());
}
```

## F.A.Q

1. I've added those components to the entity and nav mesh seems to ignore it.
- This can happen when instancing static prefabs. Untoggle the static check.