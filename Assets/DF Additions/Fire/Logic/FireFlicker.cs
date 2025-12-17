using UnityEngine;

[RequireComponent(typeof(Light))]
public class FireLightFlicker : MonoBehaviour
{
    [Header("Intensity Flicker")]
    [SerializeField] private float minIntensity = 1.5f;
    [SerializeField] private float maxIntensity = 2.5f;
    [SerializeField] private float flickerSpeed = 8f;
    
    [Header("Color Variation")]
    [SerializeField] private bool varyColor = true;
    [SerializeField] private Color colorA = new Color(1f, 0.6f, 0.2f);
    [SerializeField] private Color colorB = new Color(1f, 0.5f, 0.1f);
    [SerializeField] private float colorSpeed = 3f;
    
    [Header("Position Jitter")]
    [SerializeField] private bool enableJitter = false;
    [SerializeField] private float jitterAmount = 0.1f;
    [SerializeField] private float jitterSpeed = 10f;
    
    private Light fireLight;
    private Vector3 originalPosition;
    private float baseIntensity;
    private float randomOffset;
    
    void Start()
    {
        fireLight = GetComponent<Light>();
        baseIntensity = fireLight.intensity;
        originalPosition = transform.localPosition;
        
        // Random offset so multiple fires don't sync
        randomOffset = Random.Range(0f, 100f);
        
        // Set initial values
        minIntensity = baseIntensity * 0.8f;
        maxIntensity = baseIntensity * 1.2f;
    }
    
    void Update()
    {
        float time = Time.time + randomOffset;
        
        // Flicker intensity using Perlin noise
        float flicker1 = Mathf.PerlinNoise(time * flickerSpeed, 0f);
        float flicker2 = Mathf.PerlinNoise(time * flickerSpeed * 0.5f, 50f);
        float combinedFlicker = (flicker1 * 0.7f + flicker2 * 0.3f);
        
        fireLight.intensity = Mathf.Lerp(minIntensity, maxIntensity, combinedFlicker);
        
        // Color variation
        if(varyColor)
        {
            float colorFlicker = Mathf.PerlinNoise(time * colorSpeed, 100f);
            fireLight.color = Color.Lerp(colorA, colorB, colorFlicker);
        }
        
        // Position jitter
        if(enableJitter)
        {
            float jitterX = (Mathf.PerlinNoise(time * jitterSpeed, 0f) - 0.5f) * jitterAmount;
            float jitterY = (Mathf.PerlinNoise(time * jitterSpeed, 50f) - 0.5f) * jitterAmount;
            float jitterZ = (Mathf.PerlinNoise(time * jitterSpeed, 100f) - 0.5f) * jitterAmount;
            
            transform.localPosition = originalPosition + new Vector3(jitterX, jitterY, jitterZ);
        }
    }
}
