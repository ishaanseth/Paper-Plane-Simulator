using System;
using UnityEngine;
using UnityEngine.UI; // Required for UI components

public class Flying : MonoBehaviour
{
    [Header("Aerodynamic Properties")]
    public float PAtm = 101325f; // Atmospheric pressure in Pascals
    public Vector3 WindVelocity = Vector3.zero;
    private Vector3 initialWindDirection = Vector3.zero;
    public float density = 1.225f; // Air density at sea level in kg/m³
    public float gravity = 9.81f; // Acceleration due to gravity in m/s²
    public Rigidbody rb;
    public float radius; // Not typically used in standard aerodynamics
    public float wingArea = 0.1f; // Total wing area in m²
    public float liftCoefficient = 0.6f; // Example lift coefficient
    public float dragCoefficient = 0.06f; // Example drag coefficient

     [Header("Wind Settings")]
    [Tooltip("Slider to control wind velocity magnitude")]
    public Slider windVelocitySlider;  // Single slider for wind speed

    [Header("Rotation Properties")]
    public float rotationSpeed = 3f; // Reduced rotation speed for smoother alignment
    public float velocityThreshold = 0.5f; // Increased threshold to prevent rotation at low speeds
    public float maxBankAngle = 60f; // Maximum bank angle to prevent over-rotation
    public float bankFactor = 0.1f; // Controls banking intensity based on lateral movement

    [Header("Control Properties")]
    public float forwardThrust = 10f; // Thrust applied when pressing W
    public float backwardThrust = 5f; // Deceleration applied when pressing S
    public float strafeForce = 0.5f; // Force applied for strafing with A and D
    public float rotationForce = 0.5f; // Torque applied for A/D rotation

    [Header("UI Components")]
    public Slider forceSlider; // Reference to the Initial Force Slider
    public Slider rotationSlider; // Reference to the Initial Rotation Slider
    public Button launchButton; // Reference to the Launch Button
    public Button resetButton; // Reference to the Reset Button

    public Text velocityText; // Reference to the Velocity Text
    public Text displacementText; // Reference to the Displacement Text


    // Variables to store selected force and rotation
    private float selectedForce = 0f;
    private float selectedRotation = 0f;

    float timer = 0f;
    float end = 0.2f;
    bool timerStart = false;
    bool hasLaunched = false; // To prevent multiple launches
    bool isLanded = false; // Flag to indicate if the plane has landed


    void Start()
    {
        // Initialize Rigidbody
        if (rb == null)
        {
            rb = GetComponent<Rigidbody>();
            if (rb == null)
            {
                Debug.LogError("Rigidbody component missing on the paper plane.");
                return;
            }
        }

        // Optionally disable gravity until launch
        rb.useGravity = false;

        // Initialize UI sliders if they are assigned
        if (forceSlider != null)
        {
            forceSlider.minValue = 0f;
            forceSlider.maxValue = 50f; // Updated max value
            forceSlider.value = 15f; // Default value
            forceSlider.onValueChanged.AddListener(OnForceSliderChanged);
            selectedForce = forceSlider.value;
        }
        else
        {
            Debug.LogWarning("Force Slider not assigned in the Inspector.");
        }

        if (rotationSlider != null)
        {
            rotationSlider.minValue = 0f;
            rotationSlider.maxValue = 45f; // Updated max value
            rotationSlider.value = 0f; // Default value
            rotationSlider.onValueChanged.AddListener(OnRotationSliderChanged);
            selectedRotation = -rotationSlider.value; // Negative as per user script
        }
        else
        {
            Debug.LogWarning("Rotation Slider not assigned in the Inspector.");
        }

        // Assign Launch Button Listener
        if (launchButton != null)
        {
            launchButton.onClick.AddListener(Launch);
        }
        else
        {
            Debug.LogWarning("Launch Button not assigned in the Inspector.");
        }

        // Assign Reset Button Listener
        if (resetButton != null)
        {
            resetButton.onClick.AddListener(ResetPlane);
        }
        else
        {
            Debug.LogWarning("Reset Button not assigned in the Inspector.");
        }

        // Initialize Velocity and Displacement Texts
        if (velocityText != null)
        {
            velocityText.text = "Velocity: 0 m/s";
        }
        else
        {
            Debug.LogWarning("Velocity Text not assigned in the Inspector.");
        }

        if (displacementText != null)
        {
            displacementText.text = "Position:(0, 0, 0)";
        }
        else
        {
            Debug.LogWarning("Position Text not assigned in the Inspector.");
        }

        // ========== Wind Velocity Slider ==========
        if (windVelocitySlider != null)
        {
            // Store the initial direction of wind
            // If WindVelocity is zero, default to Vector3.forward
            if (WindVelocity.magnitude > 0.001f)
            {
                initialWindDirection = WindVelocity.normalized;
            }
            else
            {
                initialWindDirection = Vector3.right;
            }

            // Set the slider range
            windVelocitySlider.minValue = -30f;
            windVelocitySlider.maxValue = 30f;
            // Start the slider at the current wind speed
            windVelocitySlider.value = WindVelocity.magnitude;

            // Listen for changes
            windVelocitySlider.onValueChanged.AddListener(OnWindVelocitySliderChanged);
        }
    }

    // Callback for Force Slider value change
    void OnForceSliderChanged(float value)
    {
        selectedForce = value;
    }

    // Callback for Rotation Slider value change
    void OnRotationSliderChanged(float value)
    {
        selectedRotation = -value;
    }

    void Update()
    {
        if (!isLanded)
        {
            HandlePlayerInput();
        }
        // Grab the plane's local scale
        Vector3 scale = transform.localScale;
        float x = scale.x;
        float y = scale.y;
        float z = scale.z;

        // 1) Wing Area = x² / 10
        wingArea = (x * x) / 10f;

        // 2) Drag Coefficient = (x² + y² + z²) / 10
        dragCoefficient = (x * x + y * y + z * z) / 10f;

        // 3) Lift Coefficient = (x² + y² + 4f) / 10
        liftCoefficient = (x * x + y * y + 4f) / 10f;

    }

    void FixedUpdate()
    {
        if (!hasLaunched && !isLanded)
        {
            transform.localRotation = Quaternion.Euler(selectedRotation, 0f, 0f);
        }

        if (!timerStart && hasLaunched && !isLanded)
        {
            ApplyAerodynamicForces();
            OrientToVelocity();
            UpdateUITexts(); // Update UI texts each FixedUpdate
        }
        HandleTimer();
    }

    void HandlePlayerInput()
    {
        // Forward thrust (W key)
        if (Input.GetKey(KeyCode.W))
        {
            rb.AddForce(transform.forward * forwardThrust, ForceMode.Force);
        }

        // Backward thrust (S key)
        if (Input.GetKey(KeyCode.S))
        {
            rb.AddForce(-transform.forward * backwardThrust * 0.1f, ForceMode.Force);
        }

        // Strafing (A/D keys)
        if (Input.GetKey(KeyCode.A))
        {
            
            rb.AddTorque(Vector3.up * -rotationForce, ForceMode.Force); // Apply slight rotation
            rb.AddTorque(Vector3.forward * rotationForce, ForceMode.Force);
        }

        if (Input.GetKey(KeyCode.D))
        {
            
            rb.AddTorque(Vector3.up * rotationForce, ForceMode.Force); // Apply slight rotation
            rb.AddTorque(Vector3.forward * -rotationForce, ForceMode.Force);
        }
    }

    void OnWindVelocitySliderChanged(float newMagnitude)
    {
        // Recompute wind velocity based on the initial direction
        WindVelocity = initialWindDirection * newMagnitude;
    }

    // Method to apply lift and drag based on current velocity
    void ApplyAerodynamicForces()
    {
        // Relative wind velocity
        Vector3 relativeVelocity = rb.velocity - WindVelocity;
        float speed = relativeVelocity.magnitude;

        if (speed == 0)
            return; // Avoid division by zero

        // Calculate angle of attack (simplified)
        float angleOfAttack = Vector3.Angle(transform.forward, relativeVelocity) * Mathf.Deg2Rad;

        // Calculate Lift and Drag
        float lift = 0.5f * density * speed * speed * wingArea * liftCoefficient * Mathf.Cos(angleOfAttack);
        float drag = 0.5f * density * speed * speed * wingArea * dragCoefficient;

        // Lift direction is perpendicular to the relative wind and the plane's right direction
        Vector3 liftDirection = Vector3.Cross(relativeVelocity, transform.right).normalized;

        // Drag direction is opposite to the relative wind
        Vector3 dragDirection = -relativeVelocity.normalized;

        // Apply Lift and Drag
        rb.AddForce(lift * liftDirection, ForceMode.Force);
        rb.AddForce(drag * dragDirection, ForceMode.Force);

        // Optional: Visualize forces in the Unity Editor for debugging
        Debug.DrawRay(transform.position, liftDirection * lift, Color.green);
        Debug.DrawRay(transform.position, dragDirection * drag, Color.red);
    }

    // Method to orient the plane based on its velocity
    void OrientToVelocity()
    {
        Vector3 velocity = rb.velocity;

        // Check if the velocity is above the threshold to avoid erratic rotations
        if (velocity.magnitude < velocityThreshold)
            return;

        // Determine the desired forward direction based on velocity
        Vector3 desiredForward = velocity.normalized;

        // Calculate lateral (sideways) velocity
        Vector3 lateralVelocity = Vector3.Project(rb.velocity, transform.right);

        // Adjust the up vector based on lateral movement to simulate banking
        Vector3 desiredUp = Vector3.up - lateralVelocity * bankFactor;
        desiredUp = desiredUp.normalized;

        // Compute the desired rotation using LookRotation
        Quaternion desiredRotation = Quaternion.LookRotation(desiredForward, desiredUp);

        // Smoothly interpolate between the current rotation and the desired rotation
        Quaternion smoothedRotation = Quaternion.Slerp(rb.rotation, desiredRotation, rotationSpeed * Time.fixedDeltaTime);

        // Combine smoothing with clamping:
        // Convert the smoothed rotation to Euler angles
        Vector3 euler = smoothedRotation.eulerAngles;
        // Convert euler.z to a signed angle and clamp it
        float bankAngle = Mathf.DeltaAngle(0, euler.z);
        bankAngle = Mathf.Clamp(bankAngle, -maxBankAngle, maxBankAngle);
        euler.z = bankAngle;
        // Reconstruct the final rotation with clamped bank angle
        Quaternion finalRotation = Quaternion.Euler(euler);

        // Apply the final rotation once
        rb.MoveRotation(finalRotation);

        // Optional: Visualize the desired forward direction for debugging
        Debug.DrawRay(transform.position, desiredForward * 2f, Color.blue);
    }

    // Method to handle the launch timer
    void HandleTimer()
    {
        if (timerStart)
        {
            timer += Time.deltaTime;
        }

        if (timer > end)
        {
            Debug.Log("End");
            // Instead of destroying the script, reset timer flags
            timerStart = false;
            // Optionally, you can disable aerodynamic forces here
            // For now, we'll just stop the timer
        }
    }

    // Method to launch the plane with selected force and rotation
    public void Launch()
    {
        if (hasLaunched)
        {
            Debug.LogWarning("Plane has already been launched.");
            return;
        }

        // Apply initial rotation around the X-axis (pitch) based on the selected rotation
        // Adjust the axis if your model's orientation differs
        transform.Rotate(Vector3.right, selectedRotation);

        // Enable gravity now that the plane is launching
        rb.useGravity = true;

        float mathAngle = selectedRotation * Mathf.Deg2Rad;

        // Apply initial force in the Y and Z directions based on the selected force and rotation
        float ForceY = selectedForce * Mathf.Sin(mathAngle) * -0.5f; // Adjusted per user code
        float ForceZ = selectedForce * Mathf.Cos(mathAngle);

        Vector3 launchForce = new Vector3(0f, ForceY, ForceZ);
        rb.AddForce(launchForce, ForceMode.Impulse);

        hasLaunched = true;

        Debug.Log($"Launched with Force: {selectedForce} and Rotation: {selectedRotation} degrees.");
    }

    // Method to reset the plane (Reset Launch)
    public void ResetPlane()
    {
        if (!hasLaunched && !isLanded)
        {
            Debug.LogWarning("Plane hasn't been launched yet.");
            return;
        }

        // Reset position to origin or a predefined spawn point
        transform.position = Vector3.zero;

        // Reset rotation to initial state
        transform.rotation = Quaternion.identity;

        // Reset Rigidbody velocity
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        // Reset launch flags
        hasLaunched = false;
        timerStart = false;
        isLanded = false;

        // Disable gravity again
        rb.useGravity = false;

        // Reset UI Texts
        if (velocityText != null)
        {
            velocityText.text = "Velocity: 0 m/s";
        }

        if (displacementText != null)
        {
            displacementText.text = "Displacement: (0, 0, 0) m";
        }


        Debug.Log("Plane has been reset.");
    }

    // Collision handling remains unchanged
    void OnCollisionEnter(Collision collision)
    {
        if (collision != null)
        {
            timerStart = true;
            isLanded = true; // Set the landed flag
            hasLaunched = false; // Prevent further aerodynamic force application
            Debug.Log("Collision detected. Plane has landed.");
        }
    }

    // Method to update UI Texts with current velocity and displacement
    void UpdateUITexts()
    {
        if (velocityText != null)
        {
            // Display velocity magnitude with one decimal place
            velocityText.text = $"Velocity: {rb.velocity.magnitude:F1} m/s";
        }

        if (displacementText != null)
        {
            // Display displacement as a Vector3 with one decimal place
            Vector3 displacement = transform.position;
            displacementText.text = $"Displacement: ({displacement.x:F1}, {displacement.y:F1}, {displacement.z:F1}) m";
        }
    }
}
