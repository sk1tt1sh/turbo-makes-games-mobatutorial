using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(ParticleSystem))]
public class FireParticleBuilder : MonoBehaviour
{
    [Header("Fire Type")]
    [SerializeField] private FireType fireType = FireType.Campfire;
    
    [Header("Materials")]
    [SerializeField] private Material fireMaterial;
    [SerializeField] private Material crackleLayerMaterial;
    [SerializeField] private Material emberMaterial;
    [SerializeField] private Material smokeMaterial;
    
    [Header("Optional Light")]
    [SerializeField] private bool createLight = true;
    [SerializeField] private Color lightColor = new Color(1f, 0.55f, 0.2f);
    
    private ParticleSystem mainPS;
    private Light fireLight;
    
    public enum FireType
    {
        Candle,
        Torch,
        Campfire,
        Bonfire,
        MagicFire
    }
    
    [ContextMenu("Build Fire")]
    public void BuildFire()
    {
        mainPS = GetComponent<ParticleSystem>();
        
        // Configure based on fire type
        switch(fireType)
        {
            case FireType.Candle:
                ConfigureCandle();
                break;
            case FireType.Torch:
                ConfigureTorch();
                break;
            case FireType.Campfire:
                ConfigureCampfire();
                break;
            case FireType.Bonfire:
                ConfigureBonfire();
                break;
            case FireType.MagicFire:
                ConfigureMagicFire();
                break;
        }
        
        // Apply material if provided
        if(fireMaterial != null)
        {
            var renderer = mainPS.GetComponent<ParticleSystemRenderer>();
            renderer.material = fireMaterial;
        }
        
        // Create light if needed
        if(createLight && fireLight == null)
        {
            CreateFireLight();
        }
        
        // Always create all effects
        CreateEmbers();
        CreateSmoke();
        CreateCrackleLayer();
        
        Debug.Log($"Fire particle system built: {fireType}");
    }
    
    private void ConfigureCandle()
    {
        var main = mainPS.main;
        main.duration = 5f;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(1f, 1.5f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.3f, 0.5f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.3f, 0.5f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, 360f * Mathf.Deg2Rad);
        main.gravityModifier = -0.3f;
        main.maxParticles = 15;
        
        var emission = mainPS.emission;
        emission.rateOverTime = 8f;
        
        var shape = mainPS.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.1f;
        
        SetupColorOverLifetime();
        SetupSizeOverLifetime();
        SetupRenderer();
    }
    
    private void ConfigureTorch()
    {
        var main = mainPS.main;
        main.duration = 5f;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(1.2f, 1.8f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 0.8f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.5f, 0.8f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, 360f * Mathf.Deg2Rad);
        main.gravityModifier = -0.5f;
        main.maxParticles = 20;
        
        var emission = mainPS.emission;
        emission.rateOverTime = 12f;
        
        var shape = mainPS.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.15f;
        
        SetupColorOverLifetime();
        SetupSizeOverLifetime();
        SetupRenderer();
    }
    
    private void ConfigureCampfire()
    {
        var main = mainPS.main;
        main.duration = 5f;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(1.0f, 1.5f); // Shorter lifetime
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.3f, 0.6f); // Slower rise
        main.startSize = new ParticleSystem.MinMaxCurve(0.8f, 1.2f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, 360f * Mathf.Deg2Rad);
        main.gravityModifier = -0.3f; // Less rising force
        main.maxParticles = 30;
        
        var emission = mainPS.emission;
        emission.rateOverTime = 15f;
        
        var shape = mainPS.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.3f;
        
        SetupColorOverLifetime();
        SetupSizeOverLifetime();
        SetupRenderer();
    }
    
    private void ConfigureBonfire()
    {
        var main = mainPS.main;
        main.duration = 5f;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(2f, 3f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.8f, 1.5f);
        main.startSize = new ParticleSystem.MinMaxCurve(1.5f, 2.5f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, 360f * Mathf.Deg2Rad);
        main.gravityModifier = -0.6f;
        main.maxParticles = 50;
        
        var emission = mainPS.emission;
        emission.rateOverTime = 25f;
        
        var shape = mainPS.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.5f;
        
        SetupColorOverLifetime();
        SetupSizeOverLifetime();
        SetupRenderer();
    }
    
    private void ConfigureMagicFire()
    {
        var main = mainPS.main;
        main.duration = 5f;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(1.5f, 2.5f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.6f, 1.2f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.8f, 1.5f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, 360f * Mathf.Deg2Rad);
        main.gravityModifier = -0.7f;
        main.maxParticles = 35;
        
        var emission = mainPS.emission;
        emission.rateOverTime = 18f;
        
        var shape = mainPS.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.25f;
        
        SetupColorOverLifetime();
        SetupSizeOverLifetime();
        SetupRenderer();
    }
    
    private void SetupColorOverLifetime()
    {
        var col = mainPS.colorOverLifetime;
        col.enabled = true;
        
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] 
            { 
                new GradientColorKey(new Color(1f, 1f, 0.86f), 0f),      // White
                new GradientColorKey(new Color(1f, 0.86f, 0.39f), 0.3f), // Yellow
                new GradientColorKey(new Color(1f, 0.47f, 0.12f), 0.6f), // Orange
                new GradientColorKey(new Color(0.24f, 0.04f, 0f), 1f)    // Dark
            },
            new GradientAlphaKey[] 
            { 
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(1f, 0.5f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        
        col.color = gradient;
    }
    
    private void SetupSizeOverLifetime()
    {
        var sol = mainPS.sizeOverLifetime;
        sol.enabled = true;
        
        AnimationCurve curve = new AnimationCurve();
        curve.AddKey(0f, 0.3f);
        curve.AddKey(0.3f, 1f);
        curve.AddKey(1f, 0.5f);
        
        sol.size = new ParticleSystem.MinMaxCurve(1f, curve);
    }
    
    private void SetupRenderer()
    {
        var renderer = mainPS.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.alignment = ParticleSystemRenderSpace.View;
        renderer.sortMode = ParticleSystemSortMode.Distance;
        
        // If no material set, use default
        if(fireMaterial != null)
        {
            renderer.material = fireMaterial;
        }
    }
    
    private void CreateFireLight()
    {
        // Check if light already exists
        fireLight = GetComponentInChildren<Light>();
        
        GameObject lightObj;
        if(fireLight == null)
        {
            lightObj = new GameObject("FireLight");
            lightObj.transform.SetParent(transform);
            lightObj.transform.localPosition = Vector3.up * 0.3f; // Slightly above fire base
            
            fireLight = lightObj.AddComponent<Light>();
        }
        else
        {
            lightObj = fireLight.gameObject;
        }
        
        fireLight.type = LightType.Point;
        fireLight.color = lightColor;
        fireLight.renderMode = LightRenderMode.ForcePixel; // Better quality
        fireLight.cullingMask = -1; // Everything - lights all layers
        
        // Size based on fire type
        switch(fireType)
        {
            case FireType.Candle:
                fireLight.range = 3f;
                fireLight.intensity = 1.5f;
                break;
            case FireType.Torch:
                fireLight.range = 5f;
                fireLight.intensity = 2f;
                break;
            case FireType.Campfire:
                fireLight.range = 12f; // Much bigger range
                fireLight.intensity = 5f; // Much brighter
                break;
            case FireType.Bonfire:
                fireLight.range = 12f;
                fireLight.intensity = 4f;
                break;
            case FireType.MagicFire:
                fireLight.range = 9f;
                fireLight.intensity = 3.5f;
                fireLight.color = new Color(0.5f, 0.8f, 1f); // Blue tint
                break;
        }
        
        // Enable shadows for more atmosphere (optional)
        // fireLight.shadows = LightShadows.Soft;
        
        // Add flicker script if it exists in project
        if(!lightObj.GetComponent<FireLightFlicker>())
        {
            // Try to add it, but don't fail if script doesn't exist
            var flickerType = System.Type.GetType("FireLightFlicker");
            if(flickerType != null)
            {
                lightObj.AddComponent(flickerType);
                Debug.Log("Added FireLightFlicker component for animated lighting.");
            }
            else
            {
                Debug.Log("FireLight created. Add FireLightFlicker.cs to your project for animated flickering.");
            }
        }
    }
    
    private void CreateEmbers()
    {
        // Check if embers already exist
        Transform emberTransform = transform.Find("Embers");
        if(emberTransform != null)
            return;
            
        GameObject emberObj = new GameObject("Embers");
        emberObj.transform.SetParent(transform);
        emberObj.transform.localPosition = Vector3.zero;
        
        ParticleSystem emberPS = emberObj.AddComponent<ParticleSystem>();
        
        var main = emberPS.main;
        main.duration = 5f;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(2f, 4f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.1f, 0.3f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.05f);
        main.startColor = new Color(1f, 0.4f, 0f);
        main.gravityModifier = -0.3f;
        main.maxParticles = 20;
        
        var emission = emberPS.emission;
        emission.rateOverTime = 3f;
        
        var shape = emberPS.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.3f;
        
        var col = emberPS.colorOverLifetime;
        col.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] 
            { 
                new GradientColorKey(new Color(1f, 0.6f, 0.2f), 0f),
                new GradientColorKey(new Color(1f, 0.2f, 0f), 0.5f),
                new GradientColorKey(new Color(0.2f, 0f, 0f), 1f)
            },
            new GradientAlphaKey[] 
            { 
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        col.color = gradient;
        
        var renderer = emberPS.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        
        // Use assigned material or default particle material
        if(emberMaterial != null)
        {
            renderer.material = emberMaterial;
        }
        else
        {
            renderer.material = new Material(Shader.Find("Particles/Standard Unlit"));
            Debug.LogWarning("No ember material assigned. Using default particle material.");
        }
    }
    
    private void CreateSmoke()
    {
        // Check if smoke already exists
        Transform smokeTransform = transform.Find("Smoke");
        if(smokeTransform != null)
            return;
            
        GameObject smokeObj = new GameObject("Smoke");
        smokeObj.transform.SetParent(transform);
        smokeObj.transform.localPosition = Vector3.up * 1.5f; // Spawn well above fire tips
        
        ParticleSystem smokePS = smokeObj.AddComponent<ParticleSystem>();
        
        var main = smokePS.main;
        main.duration = 5f;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(5f, 8f); // Very long life
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.05f, 0.1f); // Super slow
        main.startSize = new ParticleSystem.MinMaxCurve(0.5f, 0.8f); // Start bigger
        main.startColor = new Color(0.35f, 0.35f, 0.35f, 0.15f); // More subtle opacity
        main.gravityModifier = -0.02f; // Barely rises
        main.maxParticles = 40;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        
        var emission = smokePS.emission;
        emission.rateOverTime = 3f; // Very slow emission
        
        var shape = smokePS.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.15f; // Very tight spawn area
        shape.radiusThickness = 1f;
        
        // Color fades to transparent
        var col = smokePS.colorOverLifetime;
        col.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] 
            { 
                new GradientColorKey(new Color(0.4f, 0.4f, 0.4f), 0f),
                new GradientColorKey(new Color(0.3f, 0.3f, 0.3f), 0.3f),
                new GradientColorKey(new Color(0.2f, 0.2f, 0.2f), 1f)
            },
            new GradientAlphaKey[] 
            { 
                new GradientAlphaKey(0f, 0f), // Start invisible
                new GradientAlphaKey(0.3f, 0.15f), // Fade in
                new GradientAlphaKey(0.25f, 0.5f), // Stay visible
                new GradientAlphaKey(0.2f, 0.75f), // Still visible
                new GradientAlphaKey(0f, 1f) // Fade out at very end
            }
        );
        col.color = gradient;
        
        // Size stays relatively constant (scales from start size)
        var sol = smokePS.sizeOverLifetime;
        sol.enabled = true;
        AnimationCurve curve = new AnimationCurve();
        curve.AddKey(0f, 1.0f); // Start at 100% of start size
        curve.AddKey(0.5f, 1.1f); // Slight growth
        curve.AddKey(1f, 1.3f); // Modest growth at end (30% bigger)
        sol.size = new ParticleSystem.MinMaxCurve(1f, curve); // Multiplier curve
        
        // Add noise for meandering movement (replaces velocity over lifetime to avoid errors)
        var noise = smokePS.noise;
        noise.enabled = true;
        noise.strength = 0.15f; // Increased for more lateral movement
        noise.frequency = 0.5f;
        noise.scrollSpeed = 0.2f;
        noise.damping = false;
        noise.octaveCount = 2;
        noise.octaveMultiplier = 0.5f;
        noise.octaveScale = 2f;
        
        var renderer = smokePS.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sortMode = ParticleSystemSortMode.Distance;
        renderer.sortingFudge = 50;
        
        // Use assigned material or default
        if(smokeMaterial != null)
        {
            renderer.material = smokeMaterial;
        }
        else
        {
            renderer.material = new Material(Shader.Find("Particles/Standard Unlit"));
            Debug.LogWarning("No smoke material assigned. Using default particle material.");
        }
    }
    
    private void CreateCrackleLayer()
    {
        // Check if crackle already exists
        Transform crackleTransform = transform.Find("CrackleLayer");
        if(crackleTransform != null)
            return;
            
        GameObject crackleObj = new GameObject("CrackleLayer");
        crackleObj.transform.SetParent(transform);
        crackleObj.transform.localPosition = Vector3.zero;
        
        ParticleSystem cracklePS = crackleObj.AddComponent<ParticleSystem>();
        
        var main = cracklePS.main;
        main.duration = 5f;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.8f); // Short lived
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.1f, 0.3f); // Barely moves
        main.startSize = new ParticleSystem.MinMaxCurve(0.4f, 0.8f); // Medium size
        main.startRotation = 0f; // No rotation - keep vertical
        main.gravityModifier = -0.1f; // Slight upward drift
        main.maxParticles = 50;
        
        var emission = cracklePS.emission;
        emission.rateOverTime = 40f; // Lots of particles for base coverage
        
        var shape = cracklePS.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.25f;
        shape.radiusThickness = 0.5f; // Ring spawn
        
        // Bright to dark quickly (crackle effect)
        var col = cracklePS.colorOverLifetime;
        col.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] 
            { 
                new GradientColorKey(new Color(1f, 0.9f, 0.5f), 0f), // Bright yellow
                new GradientColorKey(new Color(1f, 0.5f, 0.1f), 0.3f), // Orange
                new GradientColorKey(new Color(0.5f, 0.1f, 0f), 1f) // Dark red
            },
            new GradientAlphaKey[] 
            { 
                new GradientAlphaKey(0.8f, 0f),
                new GradientAlphaKey(0.4f, 0.5f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        col.color = gradient;
        
        // Shrink quickly
        var sol = cracklePS.sizeOverLifetime;
        sol.enabled = true;
        AnimationCurve curve = new AnimationCurve();
        curve.AddKey(0f, 1f);
        curve.AddKey(0.5f, 0.7f);
        curve.AddKey(1f, 0.2f);
        sol.size = new ParticleSystem.MinMaxCurve(1f, curve);
        
        var renderer = cracklePS.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sortingFudge = 200; // In front of main fire
        
        // Use assigned crackle material or fire material or default
        if(crackleLayerMaterial != null)
        {
            renderer.material = crackleLayerMaterial;
        }
        else if(fireMaterial != null)
        {
            renderer.material = fireMaterial;
            Debug.Log("Using fire material for crackle layer. Assign separate crackle material for visual variety.");
        }
        else
        {
            renderer.material = new Material(Shader.Find("Particles/Standard Unlit"));
            Debug.LogWarning("No crackle material assigned. Using default.");
        }
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(FireParticleBuilder))]
public class FireParticleBuilderEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        EditorGUILayout.Space(10);
        
        FireParticleBuilder builder = (FireParticleBuilder)target;
        
        if(GUILayout.Button("🔥 Build Fire 🔥", GUILayout.Height(40)))
        {
            builder.BuildFire();
        }
        
        EditorGUILayout.Space(5);
        EditorGUILayout.HelpBox("Click 'Build Fire' to automatically configure the particle system!", MessageType.Info);
    }
}
#endif
