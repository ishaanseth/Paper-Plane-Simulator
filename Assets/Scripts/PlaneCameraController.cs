using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlaneCameraController : MonoBehaviour
{
    [Header("Target Settings")]
    public Transform planeTransform; // Reference to the plane's transform
    
    [Header("Camera Position Settings")]
    public Vector3[] positionOffsets = new Vector3[3]; // Offsets for the 3 camera positions
    
    [Header("Follow Settings")]
    public float followSpeed = 5.0f; // How quickly the camera follows the plane for positions 1 and 3
    
    [Header("Collision Settings")]
    public float minYPosition = 0.5f; // Minimum Y position for the camera
    public LayerMask groundLayer; // Layer for the ground
    public float raycastDistance = 1.0f; // Distance to check below the camera
    
    private int currentPositionIndex = 0; // Track which camera position is active
    private Camera mainCamera;


    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        // Get reference to the camera component
        mainCamera = GetComponent<Camera>();
        if (mainCamera == null)
        {
            Debug.LogError("Camera component not found on this GameObject!");
        }
        
        // Validate plane reference
        if (planeTransform == null)
        {
            Debug.LogError("Plane transform reference is not set! Please assign it in the Inspector.");
        }
        
        // Set initial camera position and rotation
        UpdateCameraTransform();
    }

    void Update()
    {
        // Check for camera switch input
        if (Input.GetKeyDown(KeyCode.C))
        {
            SwitchCameraPosition();
        }
    }

    void LateUpdate()
    {    
        if (planeTransform != null)
        {
            Vector3 targetPosition = Vector3.zero;
            
            switch (currentPositionIndex)
            {
                case 0: // First camera position - smooth follow with fixed rotation
                    // Calculate target position based on plane position and offset
                    targetPosition = planeTransform.position + planeTransform.TransformDirection(positionOffsets[0]);
                    
                    // Smoothly move camera to target position
                    transform.position = Vector3.Lerp(transform.position, targetPosition, followSpeed * Time.deltaTime);
                    
                    // Keep the fixed rotation for this camera position
                    transform.rotation = Quaternion.Euler(40, 0, 0);
                    break;
                    
                case 1: // Second camera position - fixed offset from plane (no smooth transition)
                    // Calculate target position based on plane position and offset
                    targetPosition = planeTransform.position + planeTransform.TransformDirection(positionOffsets[1]);
                    
                    // Set camera position directly with no lerp
                    transform.position = targetPosition;
                    
                    break;
                    
                case 2: // Third camera position - smooth follow with free rotation
                    // Calculate target position based on plane position and offset
                    targetPosition = planeTransform.position + planeTransform.TransformDirection(positionOffsets[2]);
                    
                    // Smoothly move camera to target position
                    transform.position = Vector3.Lerp(transform.position, targetPosition, followSpeed * Time.deltaTime);
                    
                    // Match the plane's rotation (free rotation)
                    transform.rotation = planeTransform.rotation;
                    break;
            }
            
            // Ensure camera doesn't go below minimum Y position
            EnsureCameraAboveGround();
        }
    }
    
    void EnsureCameraAboveGround()
    {
        // Method 1: Simple Y position check
        if (transform.position.y < minYPosition)
        {
            Vector3 pos = transform.position;
            pos.y = minYPosition;
            transform.position = pos;
        }
        
        // Method 2: Ground collision check using raycast
        RaycastHit hit;
        if (Physics.Raycast(transform.position, Vector3.down, out hit, raycastDistance, groundLayer))
        {
            // If we're about to hit the ground, move up to maintain a safe distance
            float safeDistance = 0.5f; // Adjust this safe distance as needed
            Vector3 pos = transform.position;
            pos.y = hit.point.y + safeDistance;
            transform.position = pos;
        }
    }

    void SwitchCameraPosition()
    {
        // Cycle through camera positions
        currentPositionIndex = (currentPositionIndex + 1) % 3;
        
        Debug.Log($"Switched to camera position {currentPositionIndex + 1}");
        
        // Update camera transform immediately when switching positions
        if (currentPositionIndex == 1 && planeTransform != null)
        {
            // When switching to position 2, set position immediately
            Vector3 pos = planeTransform.position + planeTransform.TransformDirection(positionOffsets[1]);
            transform.position = pos;
        }
        
        // Update the rotation
        UpdateCameraTransform();
        
        // Ensure new position is above ground
        EnsureCameraAboveGround();
    }
    
    void UpdateCameraTransform()
    {
        // Initial rotation setup when switching positions
        if (planeTransform != null)
        {
            switch (currentPositionIndex)
            {
                case 0:
                    transform.rotation = Quaternion.Euler(40, 0, 0);
                    break;
                    
                case 1:
                    transform.rotation = planeTransform.rotation;
                    break;
                    
                case 2:
                    transform.rotation = planeTransform.rotation;
                    break;
            }
        }
    }
    
    // For visualization in the editor
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.down * raycastDistance);
    }
}