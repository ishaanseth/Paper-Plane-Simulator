using UnityEngine;

public class PaperWingController : MonoBehaviour
{
    [Header("Wing References")]
    public Cloth leftWingCloth;
    public Cloth rightWingCloth;
    public Rigidbody planeRigidbody;
    
    [Header("Cloth Response")]
    [Range(0f, 2f)] public float baseStiffness = 0.8f;
    [Range(0f, 2f)] public float baseBending = 0.7f;
    [Range(0f, 2f)] public float baseDamping = 0.85f;
    
    [Header("Dynamic Adjustments")]
    public float speedStiffnessEffect = 0.2f;
    public float verticalVelocityBendEffect = 0.3f;
    public float turbulenceAmount = 0.5f;
    [Range(0f, 1f)] public float worldVelocityInfluence = 0.5f;
    
    // Original cloth settings
    private float originalStretchStiffness;
    private float originalBendStiffness;
    private float originalDamping;
    private float originalWorldVelocityScale;
    
    // Virtual particle weights cache
    private ClothSkinningCoefficient[] leftWingCoefficients;
    private ClothSkinningCoefficient[] rightWingCoefficients;
    
    void Start()
    {
        // Get rigidbody if not assigned
        if (planeRigidbody == null)
            planeRigidbody = GetComponentInParent<Rigidbody>();
            
        // Find cloth components if not assigned
        if (leftWingCloth == null)
            leftWingCloth = transform.Find("LeftWing.002")?.GetComponent<Cloth>();
            
        if (rightWingCloth == null)
            rightWingCloth = transform.Find("RightWing.002")?.GetComponent<Cloth>();
            
        if (leftWingCloth != null)
        {
            // Store original cloth settings
            originalStretchStiffness = leftWingCloth.stretchingStiffness;
            originalBendStiffness = leftWingCloth.bendingStiffness;
            originalDamping = leftWingCloth.damping;
            originalWorldVelocityScale = leftWingCloth.worldVelocityScale;
            
            // Cache coefficients
            leftWingCoefficients = leftWingCloth.coefficients;
        }
        
        if (rightWingCloth != null)
        {
            rightWingCoefficients = rightWingCloth.coefficients;
        }
        
        // Initial cloth configuration
        ConfigureClothForFlight(leftWingCloth);
        ConfigureClothForFlight(rightWingCloth);
    }
    
    void ConfigureClothForFlight(Cloth cloth)
    {
        if (cloth == null) return;
        
        // Enable tethers for structural integrity
        cloth.useTethers = true;
        
        // Initial stiffness settings
        cloth.stretchingStiffness = baseStiffness;
        cloth.bendingStiffness = baseBending;
        cloth.damping = baseDamping;
        cloth.worldVelocityScale = worldVelocityInfluence;
        
        // Don't use gravity directly - we'll simulate it based on velocity and orientation
        cloth.useGravity = false;
    }
    
    void FixedUpdate()
    {
        if (planeRigidbody == null) return;
        
        // Get velocity in local space
        Vector3 localVelocity = transform.InverseTransformDirection(planeRigidbody.velocity);
        float speed = planeRigidbody.velocity.magnitude;
        
        // Calculate dynamic cloth properties based on flight conditions
        float dynamicStiffness = Mathf.Clamp(baseStiffness + (speed * speedStiffnessEffect * 0.1f), 0, 1);
        float dynamicBending = Mathf.Clamp(baseBending - (Mathf.Abs(localVelocity.y) * verticalVelocityBendEffect * 0.1f), 0, 1);
        
        // Apply different forces for diving vs climbing
        Vector3 externalAcceleration = Vector3.zero;
        
        // When diving (negative y velocity), apply upward force on wings
        if (localVelocity.y < -0.5f)
        {
            externalAcceleration.y = -localVelocity.y * 0.5f;
        }
        // When climbing or level, apply slight downward force (wing droop)
        else
        {
            externalAcceleration.y = -0.2f;
        }
        
        // Add turbulence based on speed
        if (speed > 3f)
        {
            externalAcceleration += new Vector3(
                Mathf.Sin(Time.time * 5f) * turbulenceAmount * 0.1f,
                Mathf.Sin(Time.time * 7f) * turbulenceAmount * 0.1f,
                Mathf.Sin(Time.time * 3f) * turbulenceAmount * 0.1f
            );
        }
        
        // Apply settings to both wings
        UpdateWingCloth(leftWingCloth, dynamicStiffness, dynamicBending, externalAcceleration);
        UpdateWingCloth(rightWingCloth, dynamicStiffness, dynamicBending, externalAcceleration);
    }
    
    void UpdateWingCloth(Cloth cloth, float stiffness, float bending, Vector3 acceleration)
    {
        if (cloth == null) return;
        
        // Update cloth physics parameters
        cloth.stretchingStiffness = stiffness;
        cloth.bendingStiffness = bending;
        cloth.externalAcceleration = acceleration;
    }
}