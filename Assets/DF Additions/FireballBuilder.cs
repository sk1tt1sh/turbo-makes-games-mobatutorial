using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Builds a roaring, splashy fireball particle system.
/// Attach this to a GameObject, assign your fire material, then click "Build Fireball" in Inspector.
/// </summary>
public class FireballBuilder : MonoBehaviour
{
    [Header("Required")]
    [Tooltip("Assign your fire material (with RealisticFire shader)")]
    public Material fireMaterial;
    
    [Header("Fireball Settings")]
    [Range(0.5f, 5f)]
    public float size = 2f;
    [Range(0.5f, 3f)]
    public float intensity = 1.5f;
    [Range(0f, 1f)]
    public float chaos = 0.7f;
    
    [Header("Colors")]
    public Gradient fireGradient;
    
    void Reset()
    {
        // Set default gradient
        fireGradient = new Gradient();
        GradientColorKey[] colorKeys = new GradientColorKey[4];
        colorKeys[0] = new GradientColorKey(new Color(1f, 1f, 0.8f), 0f);    // White core
        colorKeys[1] = new GradientColorKey(new Color(1f, 0.7f, 0f), 0.3f);  // Yellow-orange
        colorKeys[2] = new GradientColorKey(new Color(1f, 0.2f, 0f), 0.7f);  // Red-orange
        colorKeys[3] = new GradientColorKey(new Color(0.3f, 0f, 0f), 1f);    // Dark red
        
        GradientAlphaKey[] alphaKeys = new GradientAlphaKey[3];
        alphaKeys[0] = new GradientAlphaKey(0f, 0f);      // Fade in
        alphaKeys[1] = new GradientAlphaKey(1f, 0.1f);    // Full opacity
        alphaKeys[2] = new GradientAlphaKey(0f, 1f);      // Fade out
        
        fireGradient.SetKeys(colorKeys, alphaKeys);
    }
    
    [ContextMenu("Build Fireball")]
    public void BuildFireball()
    {
        if (fireMaterial == null)
        {
            Debug.LogError("Please assign a fire material first!");
            return;
        }
        
        // Create main fireball
        ParticleSystem mainFire = GetOrCreateParticleSystem("MainFire");
        ConfigureMainFire(mainFire);
        
        // Create core glow
        ParticleSystem coreGlow = GetOrCreateParticleSystem("CoreGlow");
        ConfigureCoreGlow(coreGlow);
        
        // Create sparks
        ParticleSystem sparks = GetOrCreateParticleSystem("Sparks");
        ConfigureSparks(sparks);
        
        // Create smoke trails
        ParticleSystem smoke = GetOrCreateParticleSystem("SmokeTrails");
        ConfigureSmoke(smoke);
        
        Debug.Log("Fireball particle system built successfully!");
    }
    
    ParticleSystem GetOrCreateParticleSystem(string childName)
    {
        Transform child = transform.Find(childName);
        GameObject obj;
        
        if (child == null)
        {
            obj = new GameObject(childName);
            obj.transform.SetParent(transform);
            obj.transform.localPosition = Vector3.zero;
            obj.transform.localRotation = Quaternion.identity;
        }
        else
        {
            obj = child.gameObject;
        }
        
        ParticleSystem ps = obj.GetComponent<ParticleSystem>();
        if (ps == null)
        {
            ps = obj.AddComponent<ParticleSystem>();
        }
        
        return ps;
    }
    
    void ConfigureMainFire(ParticleSystem ps)
    {
        var main = ps.main;
        main.duration = 1.0f;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.8f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 2f);
        main.startSize = new ParticleSystem.MinMaxCurve(size * 0.8f, size * 1.2f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, 360f * Mathf.Deg2Rad);
        main.startColor = Color.white;
        main.gravityModifier = 0f;
        main.maxParticles = 50;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        
        // Emission
        var emission = ps.emission;
        emission.rateOverTime = 40f * intensity;
        
        // Shape - Sphere
        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = size * 0.3f;
        shape.radiusThickness = 0.5f; // Emit from volume, not just surface
        
        // Velocity Over Lifetime - Swirling outward
        var velocity = ps.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.Local;
        velocity.radial = new ParticleSystem.MinMaxCurve(1f, 3f);
        velocity.orbitalX = new ParticleSystem.MinMaxCurve(-30f * chaos, 30f * chaos);
        velocity.orbitalY = new ParticleSystem.MinMaxCurve(-30f * chaos, 30f * chaos);
        velocity.orbitalZ = new ParticleSystem.MinMaxCurve(-30f * chaos, 30f * chaos);
        
        // Size Over Lifetime - Expand then shrink
        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0f, 0.2f);      // Start small
        sizeCurve.AddKey(0.3f, 1.2f);    // Expand quickly
        sizeCurve.AddKey(1f, 0.3f);      // Shrink at end
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);
        
        // Rotation Over Lifetime - Chaotic spin
        var rotationOverLifetime = ps.rotationOverLifetime;
        rotationOverLifetime.enabled = true;
        rotationOverLifetime.z = new ParticleSystem.MinMaxCurve(-180f * chaos, 180f * chaos);
        
        // Color Over Lifetime
        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(fireGradient);
        
        // Noise - Turbulence
        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = new ParticleSystem.MinMaxCurve(1f * chaos, 2f * chaos);
        noise.frequency = 2f;
        noise.scrollSpeed = 0.5f;
        noise.damping = false;
        noise.octaveCount = 2;
        
        // Renderer
        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.material = fireMaterial;
        renderer.sortingOrder = 0;
    }
    
    void ConfigureCoreGlow(ParticleSystem ps)
    {
        var main = ps.main;
        main.duration = 1.0f;
        main.loop = true;
        main.startLifetime = 0.3f;
        main.startSpeed = 0f;
        main.startSize = size * 1.5f;
        main.startColor = new Color(1f, 0.8f, 0.3f, 0.6f);
        main.maxParticles = 10;
        
        // Emission
        var emission = ps.emission;
        emission.rateOverTime = 20f;
        
        // Shape - Point
        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = size * 0.1f;
        
        // Size Over Lifetime - Pulse
        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve pulseCurve = new AnimationCurve();
        pulseCurve.AddKey(0f, 0.8f);
        pulseCurve.AddKey(0.5f, 1.2f);
        pulseCurve.AddKey(1f, 0.8f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, pulseCurve);
        
        // Color Over Lifetime - Bright core fade
        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient glowGradient = new Gradient();
        GradientColorKey[] colorKeys = new GradientColorKey[2];
        colorKeys[0] = new GradientColorKey(new Color(1f, 1f, 0.9f), 0f);
        colorKeys[1] = new GradientColorKey(new Color(1f, 0.5f, 0f), 1f);
        GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];
        alphaKeys[0] = new GradientAlphaKey(0.8f, 0f);
        alphaKeys[1] = new GradientAlphaKey(0f, 1f);
        glowGradient.SetKeys(colorKeys, alphaKeys);
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(glowGradient);
        
        // Renderer
        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.material = fireMaterial;
        renderer.sortingOrder = -1; // Behind main fire
    }
    
    void ConfigureSparks(ParticleSystem ps)
    {
        var main = ps.main;
        main.duration = 1.0f;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.2f, 0.6f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(3f, 8f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.15f);
        main.startColor = new Color(1f, 0.8f, 0.3f, 1f);
        main.gravityModifier = 0.5f;
        main.maxParticles = 100;
        
        // Emission - Bursts
        var emission = ps.emission;
        emission.rateOverTime = 0f;
        // FIXED: Correct argument order (time, minCount, maxCount, cycleCount, repeatInterval)
        emission.SetBursts(new ParticleSystem.Burst[] {
            new ParticleSystem.Burst(0f, 15, 25, 3, 0.1f) // Burst 15-25 sparks, 3 cycles, every 0.1s
        });
        
        // Shape - Sphere surface
        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = size * 0.5f;
        shape.radiusThickness = 1f; // Surface only
        
        // Size Over Lifetime - Shrink
        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sparkSizeCurve = new AnimationCurve();
        sparkSizeCurve.AddKey(0f, 1f);
        sparkSizeCurve.AddKey(1f, 0f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sparkSizeCurve);
        
        // Color Over Lifetime - Fade to dark
        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient sparkGradient = new Gradient();
        GradientColorKey[] colorKeys = new GradientColorKey[2];
        colorKeys[0] = new GradientColorKey(new Color(1f, 0.9f, 0.5f), 0f);
        colorKeys[1] = new GradientColorKey(new Color(0.3f, 0.1f, 0f), 1f);
        GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];
        alphaKeys[0] = new GradientAlphaKey(1f, 0f);
        alphaKeys[1] = new GradientAlphaKey(0f, 1f);
        sparkGradient.SetKeys(colorKeys, alphaKeys);
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(sparkGradient);
        
        // Renderer - Use default material (bright particles)
        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.material = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
        renderer.material.SetColor("_BaseColor", new Color(1f, 0.8f, 0.3f, 1f));
    }
    
    void ConfigureSmoke(ParticleSystem ps)
    {
        var main = ps.main;
        main.duration = 1.0f;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.2f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 1.5f);
        main.startSize = new ParticleSystem.MinMaxCurve(size * 0.5f, size * 1f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, 360f * Mathf.Deg2Rad);
        main.startColor = new Color(0.2f, 0.05f, 0f, 0.3f);
        main.gravityModifier = -0.2f; // Slight upward drift
        main.maxParticles = 30;
        
        // Emission
        var emission = ps.emission;
        emission.rateOverTime = 15f;
        
        // Shape - Sphere
        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = size * 0.4f;
        
        // Velocity Over Lifetime - Slow expansion
        var velocity = ps.velocityOverLifetime;
        velocity.enabled = true;
        velocity.radial = new ParticleSystem.MinMaxCurve(0.5f, 1.5f);
        
        // Size Over Lifetime - Expand
        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve smokeSizeCurve = new AnimationCurve();
        smokeSizeCurve.AddKey(0f, 0.5f);
        smokeSizeCurve.AddKey(1f, 2f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, smokeSizeCurve);
        
        // Rotation Over Lifetime
        var rotationOverLifetime = ps.rotationOverLifetime;
        rotationOverLifetime.enabled = true;
        rotationOverLifetime.z = new ParticleSystem.MinMaxCurve(-45f, 45f);
        
        // Color Over Lifetime - Fade to transparent
        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient smokeGradient = new Gradient();
        GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];
        alphaKeys[0] = new GradientAlphaKey(0.4f, 0f);
        alphaKeys[1] = new GradientAlphaKey(0f, 1f);
        smokeGradient.alphaKeys = alphaKeys;
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(smokeGradient);
        
        // Renderer
        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.material = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
        renderer.material.SetColor("_BaseColor", new Color(0.2f, 0.05f, 0f, 0.5f));
        renderer.sortingOrder = -2; // Behind everything
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(FireballBuilder))]
public class FireballBuilderEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        FireballBuilder builder = (FireballBuilder)target;
        
        GUILayout.Space(10);
        
        if (GUILayout.Button("Build Fireball", GUILayout.Height(40)))
        {
            builder.BuildFireball();
        }
        
        GUILayout.Space(5);
        
        EditorGUILayout.HelpBox(
            "1. Assign your fire material\n" +
            "2. Adjust size, intensity, and chaos\n" +
            "3. Click 'Build Fireball'\n" +
            "4. Play to see the effect!",
            MessageType.Info
        );
    }
}
#endif
