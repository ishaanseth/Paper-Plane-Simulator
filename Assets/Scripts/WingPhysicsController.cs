using UnityEngine;

public class WingPhysicsController : MonoBehaviour
{
    [Header("References")]
    public Transform leftWingTip;  // Body.005.L
    public Transform rightWingTip; // Body.005.R
    public Rigidbody planeRigidbody;
    
    [Header("Physics Settings")]
    [Range(0.1f, 10f)] public float springForce = 2.0f;
    [Range(0.01f, 1f)] public float damping = 0.1f;
    [Range(0.1f, 45f)] public float maxBendAngle = 15f;
    
    [Header("Aerodynamic Response")]
    public float aerodynamicResponseFactor = 0.5f;
    public float verticalVelocityInfluence = 0.2f;
    
    // Original rotations
    private Quaternion leftWingOriginalRotation;
    private Quaternion rightWingOriginalRotation;
    
    void Start()
    {
        // Store original rotations
        if (leftWingTip != null) leftWingOriginalRotation = leftWingTip.localRotation;
        if (rightWingTip != null) rightWingOriginalRotation = rightWingTip.localRotation;
        
        if (planeRigidbody == null)
            planeRigidbody = GetComponentInParent<Rigidbody>();
    }
    
    void FixedUpdate()
    {
        if (planeRigidbody == null || leftWingTip == null || rightWingTip == null)
            return;
            
        // Get velocity information
        Vector3 localVelocity = transform.InverseTransformDirection(planeRigidbody.velocity);
        
        // Calculate bend factors
        float gravityBendFactor = -Physics.gravity.y * 0.05f; // Wings droop due to gravity
        float verticalVelocityBendFactor = -localVelocity.y * verticalVelocityInfluence;
        float forwardVelocityBendFactor = localVelocity.z * 0.01f;
        
        // Wing bend angles
        float totalBendFactor = gravityBendFactor + verticalVelocityBendFactor + forwardVelocityBendFactor;
        
        // Apply spring physics (simplified)
        float springBendAngle = Mathf.Clamp(totalBendFactor, -maxBendAngle, maxBendAngle);
        
        // Apply different rotations for each wing
        ApplyWingRotation(leftWingTip, leftWingOriginalRotation, springBendAngle, true);
        ApplyWingRotation(rightWingTip, rightWingOriginalRotation, springBendAngle, false);
    }
    
    void ApplyWingRotation(Transform wingTip, Quaternion originalRotation, float bendAngle, bool isLeftWing)
    {
        // Apply rotation around local X axis (adjust axis based on your model)
        Quaternion bendRotation = Quaternion.Euler(isLeftWing ? bendAngle : -bendAngle, 0, 0);
        wingTip.localRotation = originalRotation * bendRotation;
    }
}