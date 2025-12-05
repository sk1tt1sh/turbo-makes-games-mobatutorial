using UnityEngine;

/// <summary>
/// Global wind system that controls wind direction and strength for all foliage shaders.
/// Automatically pushes wind values to shader globals every frame.
/// </summary>
public class GlobalWindManager : MonoBehaviour
{
    [Header("Wind Settings")]
    [Tooltip("Current wind direction in world space (will be normalized)")]
    public Vector3 windDirection = new Vector3(1, 0, 0);
    
    [Tooltip("Base wind strength multiplier")]
    [Range(0f, 2f)]
    public float windStrength = 1.0f;
    
    [Header("Wind Variation")]
    [Tooltip("Enable smooth wind direction changes over time")]
    public bool enableWindShifting = true;
    
    [Tooltip("How quickly wind direction shifts (lower = slower)")]
    [Range(0.01f, 1f)]
    public float windShiftSpeed = 0.1f;
    
    [Tooltip("How much the wind can deviate from base direction (degrees)")]
    [Range(0f, 90f)]
    public float windShiftAmount = 30f;
    
    [Header("Wind Gusts")]
    [Tooltip("Enable random wind gusts")]
    public bool enableGusts = true;
    
    [Tooltip("How often gusts occur (seconds)")]
    [Range(3f, 20f)]
    public float gustFrequency = 8f;
    
    [Tooltip("How much gusts increase wind strength")]
    [Range(0f, 1f)]
    public float gustStrength = 0.4f;
    
    [Tooltip("How long gusts last (seconds)")]
    [Range(0.5f, 3f)]
    public float gustDuration = 1.5f;
    
    // Shader property IDs (cached for performance)
    private static readonly int GlobalWindDirection = Shader.PropertyToID("_GlobalWindDirection");
    private static readonly int GlobalWindStrength = Shader.PropertyToID("_GlobalWindStrength");
    
    // Internal state
    private Vector3 baseWindDirection;
    private float currentGustMultiplier = 1.0f;
    private float nextGustTime;
    private float gustEndTime;
    
    void Start()
    {
        // Normalize and cache base direction
        baseWindDirection = windDirection.normalized;
        
        // Initialize gust timing
        if (enableGusts)
        {
            nextGustTime = Time.time + Random.Range(gustFrequency * 0.5f, gustFrequency * 1.5f);
        }
    }
    
    void Update()
    {
        Vector3 currentDirection = baseWindDirection;
        
        // === WIND DIRECTION SHIFTING ===
        if (enableWindShifting)
        {
            // Use Perlin noise for smooth, organic direction changes
            float noiseX = Mathf.PerlinNoise(Time.time * windShiftSpeed, 0f) * 2f - 1f;
            float noiseZ = Mathf.PerlinNoise(0f, Time.time * windShiftSpeed) * 2f - 1f;
            
            // Convert base direction to angle, add noise-based offset
            float baseAngle = Mathf.Atan2(baseWindDirection.z, baseWindDirection.x);
            float noiseOffset = (noiseX * windShiftAmount) * Mathf.Deg2Rad;
            float currentAngle = baseAngle + noiseOffset;
            
            // Reconstruct direction vector
            currentDirection = new Vector3(
                Mathf.Cos(currentAngle),
                0f,
                Mathf.Sin(currentAngle)
            );
        }
        
        // === WIND GUSTS ===
        if (enableGusts)
        {
            float currentTime = Time.time;
            
            // Start new gust?
            if (currentTime >= nextGustTime && currentTime < gustEndTime)
            {
                // We're in a gust - ramp up
                float gustProgress = (currentTime - nextGustTime) / (gustDuration * 0.3f);
                currentGustMultiplier = Mathf.Lerp(1.0f, 1.0f + gustStrength, Mathf.Min(gustProgress, 1f));
            }
            else if (currentTime >= gustEndTime)
            {
                // Gust ending - ramp down
                float fadeProgress = (currentTime - gustEndTime) / (gustDuration * 0.3f);
                currentGustMultiplier = Mathf.Lerp(1.0f + gustStrength, 1.0f, Mathf.Min(fadeProgress, 1f));
                
                // Schedule next gust
                if (fadeProgress >= 1f)
                {
                    nextGustTime = currentTime + Random.Range(gustFrequency * 0.5f, gustFrequency * 1.5f);
                    gustEndTime = nextGustTime + gustDuration;
                    currentGustMultiplier = 1.0f;
                }
            }
        }
        else
        {
            currentGustMultiplier = 1.0f;
        }
        
        // === PUSH TO SHADER GLOBALS ===
        float finalStrength = windStrength * currentGustMultiplier;
        
        Shader.SetGlobalVector(GlobalWindDirection, currentDirection);
        Shader.SetGlobalFloat(GlobalWindStrength, finalStrength);
    }
    
    // Optional: Visualize wind direction in editor
    void OnDrawGizmos()
    {
        if (!Application.isPlaying)
        {
            baseWindDirection = windDirection.normalized;
        }
        
        // Draw wind direction arrow
        Gizmos.color = Color.cyan;
        Vector3 origin = transform.position;
        Vector3 direction = Application.isPlaying ? 
            Shader.GetGlobalVector(GlobalWindDirection) : 
            baseWindDirection;
        
        Gizmos.DrawRay(origin, direction * 5f);
        Gizmos.DrawSphere(origin + direction * 5f, 0.3f);
        
        // Draw wind strength indicator
        Gizmos.color = Color.yellow;
        float strength = Application.isPlaying ? 
            Shader.GetGlobalFloat(GlobalWindStrength) : 
            windStrength;
        Gizmos.DrawWireSphere(origin, strength * 2f);
    }
    
    // Public API for weather systems, etc.
    public void SetWindDirection(Vector3 direction)
    {
        baseWindDirection = direction.normalized;
        windDirection = baseWindDirection;
    }
    
    public void SetWindStrength(float strength)
    {
        windStrength = Mathf.Clamp(strength, 0f, 2f);
    }
    
    public Vector3 GetCurrentWindDirection()
    {
        return Shader.GetGlobalVector(GlobalWindDirection);
    }
    
    public float GetCurrentWindStrength()
    {
        return Shader.GetGlobalFloat(GlobalWindStrength);
    }
}
