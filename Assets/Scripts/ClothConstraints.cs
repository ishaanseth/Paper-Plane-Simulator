using UnityEngine;
using System.Collections;

public class ClothSetup : MonoBehaviour
{
    // Reference to the Cloth component
    private Cloth cloth;

    // Threshold to identify the top vertices based on height
    public float heightThreshold; // Adjust based on your mesh's top vertex positions
    private int height;

    // Coroutine-based Start method
    IEnumerator Start()
    {
        PaperSkinnedMeshGenerator generator = GetComponent<PaperSkinnedMeshGenerator>();
        height = generator.height;

        heightThreshold = (float)(height * 0.5 - 1);
        // Get the Cloth component attached to this GameObject
        cloth = GetComponent<Cloth>();

        // Wait for 1 frame to ensure Cloth is initialized
        yield return null;

        // Apply the vertex freezing logic after the first frame
        FreezeTopVertices();
    }

    void FreezeTopVertices()
    {
        // Get the vertex positions of the cloth mesh
        Vector3[] vertices = cloth.vertices;

        // Create an array to hold the constraints
        ClothSkinningCoefficient[] constraints = cloth.coefficients;

        // Check if cloth has valid vertices
        if (vertices == null || vertices.Length == 0)
        {
            Debug.LogError("Cloth vertices are not initialized or empty.");
            return;
        }

        // Loop through each vertex and print its position
        for (int i = 0; i < vertices.Length; i++)
        {
            Debug.Log("Vertex " + i + " position: " + vertices[i]);

            // If the vertex is above the height threshold, lock it in place
            if (vertices[i].y > heightThreshold)
            {
                constraints[i].maxDistance = 0f;  // Prevent movement for the top vertices
                Debug.Log("Freezing vertex: " + i + " at position: " + vertices[i]);
            }
        }

        // Assign the modified constraints back to the cloth component
        cloth.coefficients = constraints;

        Debug.Log("Cloth constraints applied. Top vertices are now stationary.");
    }
}
