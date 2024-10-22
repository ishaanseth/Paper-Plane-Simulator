using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PhysicsSim : MonoBehaviour
{
    public float PAtm;
    public Vector3 WindVelocity = Vector3.zero;
    public float density;
    public float g;
    public GameObject paperPlane; // The GameObject with the Cloth component
    public PaperSkinnedMeshGenerator meshGenerator;
    
    private Cloth clothComponent;

    // Start is called before the first frame update
    void Start()
    {
        // Get the Cloth component attached to the paperPlane object
        clothComponent = paperPlane.GetComponent<Cloth>();
        if (clothComponent == null)
        {
            Debug.LogError("No Cloth component found on the paperPlane!");
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (clothComponent == null)
            return;

        List<Vector3> cellNorm = meshGenerator.cellNormals;
        List<Vector3> cellCen = meshGenerator.cellCenters;

        Vector3[] ForceField = new Vector3[cellCen.Count];
        Vector3 totalForce = Vector3.zero; // To accumulate the total force

        // Now you can apply force at each cell center
        for (int i = 0; i < ForceField.Length; i++)
        {
            // Calculate the force at each cell center
            ForceField[i] = cellNorm[i] * (float)PressureFinder(WindVelocity, cellCen[i].y);
            totalForce += ForceField[i]; // Sum the forces to apply as external acceleration
        }

        // Calculate the average force or apply the total force
        Vector3 averageForce = totalForce / cellCen.Count;

        // Apply the external acceleration to the Cloth component
        clothComponent.externalAcceleration = averageForce + new Vector3 (0,-1,0); // Simulates wind or pressure
        Debug.Log($"Applied external acceleration: {averageForce}");
    }

    double PressureFinder(Vector3 velocity, float height)
    {
        return PAtm + 0.5 * density * Mathf.Pow(velocity.magnitude, 2) + density * g * height;
    }
}
