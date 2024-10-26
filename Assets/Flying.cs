using System;
using UnityEngine;
using UnityEngine.UI; // Required for UI components

public class Flying : MonoBehaviour
{
    [Header("Aerodynamic Properties")]
    public float PAtm = 101325f; // Atmospheric pressure in Pascals
    public Vector3 WindVelocity = Vector3.zero;
    public float density = 1.225f; // Air density at sea level in kg/m³
    public float gravity = 9.81f; // Acceleration due to gravity in m/s²
    public Rigidbody rb;
    public float radius; // Not typically used in standard aerodynamics
    public float wingArea = 0.1f; // Total wing area in m²
    public float liftCoefficient = 0.5f; // Example lift coefficient
    public float dragCoefficient = 0.05f; // Example drag coefficient

    [Header("Rotation Properties")]
    public float rotationSpeed = 3f; // Reduced rotation speed for smoother alignment
    public float velocityThreshold = 1f; // Increased threshold to prevent rotation at low speeds
    public float maxBankAngle = 30f; // Maximum bank angle to prevent over-rotation
    public float bankFactor = 0.1f; // Controls banking intensity based on lateral movement

    [Header("UI Components")]
    public Slider forceSlider; // Reference to the Initial Force Slider
    public Slider rotationSlider; // Reference to the Initial Rotation Slider
    public Button launchButton; // Reference to the Launch Button
    public Button resetButton; // Reference to the Reset Button

    public Text velocityText; // Reference to the Velocity Text
    public Text displacementText; // Reference to the Displacement Text

    [Header("Camera Components")]
    public Transform[] cameraPositions; // Array to hold the three camera positions
    public Transform mainCameraTransform; // Reference to the Main Camera's Transform

    // Variables to store selected force and rotation
    private float selectedForce = 0f;
    private float selectedRotation = 0f;

    float timer = 0f;
    float end = 0.2f;
    bool timerStart = false;
    bool hasLaunched = false; // To prevent multiple launches
    bool isLanded = false; // Flag to indicate if the plane has landed

    private int currentCameraIndex = 0; // Tracks the current camera position

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
            rotationSlider.maxValue = 60f; // Updated max value
            rotationSlider.value = 30f; // Default value
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
            displacementText.text = "Displacement: (0, 0, 0) m";
        }
        else
        {
            Debug.LogWarning("Displacement Text not assigned in the Inspector.");
        }

        // Initialize Camera Components
        if (cameraPositions == null || cameraPositions.Length != 3)
        {
            Debug.LogError("Please assign exactly three camera positions in the Inspector.");
        }

        if (mainCameraTransform == null)
        {
            // Attempt to find the Main Camera among child objects
            Camera cam = GetComponentInChildren<Camera>();
            if (cam != null)
            {
                mainCameraTransform = cam.transform;
            }
            else
            {
                Debug.LogError("Main Camera not found as a child of the plane. Please assign it in the Inspector.");
            }
        }

        // Set the initial camera position
        if (cameraPositions != null && cameraPositions.Length == 3 && mainCameraTransform != null)
        {
            SwitchCamera(currentCameraIndex);
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
        // Detect "C" key press to switch camera positions
        if (Input.GetKeyDown(KeyCode.C))
        {
            SwitchToNextCamera();
        }
    }

    void FixedUpdate()
    {
        if (!timerStart && hasLaunched && !isLanded)
        {
            ApplyAerodynamicForces();
            OrientToVelocity();
            UpdateUITexts(); // Update UI texts each FixedUpdate
        }
        HandleTimer();
    }

    // Method to switch to the next camera position
    void SwitchToNextCamera()
    {
        if (cameraPositions == null || cameraPositions.Length != 3)
        {
            Debug.LogWarning("Camera positions are not properly assigned.");
            return;
        }

        currentCameraIndex = (currentCameraIndex + 1) % cameraPositions.Length;
        SwitchCamera(currentCameraIndex);
    }

    // Method to switch the camera to a specified position
    void SwitchCamera(int index)
    {
        if (mainCameraTransform == null || cameraPositions == null || cameraPositions.Length < 3)
        {
            Debug.LogWarning("Camera or camera positions are not properly assigned.");
            return;
        }

        // Move the camera to the new position and rotation
        mainCameraTransform.localPosition = cameraPositions[index].localPosition;
        mainCameraTransform.localRotation = cameraPositions[index].localRotation;

        Debug.Log($"Switched to Camera Position {index + 1}");
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

        // Compute the desired rotation
        Quaternion desiredRotation = Quaternion.LookRotation(desiredForward, desiredUp);

        // Smoothly interpolate between the current rotation and the desired rotation
        Quaternion smoothedRotation = Quaternion.Slerp(rb.rotation, desiredRotation, rotationSpeed * Time.fixedDeltaTime);

        // Apply the rotation using Rigidbody.MoveRotation for smooth physics-based rotation
        rb.MoveRotation(smoothedRotation);

        // Limit the bank angle to prevent over-rotation
        Vector3 euler = rb.rotation.eulerAngles;

        // Convert euler.z from [0, 360] to [-180, 180] for clamping
        euler.z = Mathf.Clamp(Mathf.DeltaAngle(0, euler.z), -maxBankAngle, maxBankAngle);

        Quaternion clampedRotation = Quaternion.Euler(euler);
        rb.MoveRotation(clampedRotation);

        // Optional: Visualize desired forward direction
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

        // Reset Camera to the first position
        currentCameraIndex = 0;
        SwitchCamera(currentCameraIndex);

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
