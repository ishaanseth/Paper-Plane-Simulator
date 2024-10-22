using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SkinnedMeshRenderer))]
public class PaperPhysics : MonoBehaviour
{
    SkinnedMeshRenderer skinnedMeshRenderer;
    Mesh mesh;
    Vector3[] originalVertices;  // Original vertices from the SkinnedMeshRenderer
    Vector3[] modifiedVertices;  // Modified vertices to apply physics
    Vector3[] velocities;  // Store velocity for each vertex

    public float gravity = -9.81f;  // Gravity force
    public Vector3 externalAcceleration = Vector3.zero;  // External acceleration
    public float stiffness = 0.1f;  // Stiffness factor for resisting free flow

    public int topFixedRow = 1;  // Number of top rows to remain fixed
    private int width;  // To store the width from PaperSkinnedMeshGenerator

    void Start()
    {
        // Get skinned mesh renderer and mesh
        skinnedMeshRenderer = GetComponent<SkinnedMeshRenderer>();
        mesh = skinnedMeshRenderer.sharedMesh;

        // Get width from PaperSkinnedMeshGenerator
        PaperSkinnedMeshGenerator generator = GetComponent<PaperSkinnedMeshGenerator>();
        width = generator.width;

        // Get the original vertices and make a copy to modify them
        originalVertices = mesh.vertices;
        modifiedVertices = new Vector3[originalVertices.Length];
        originalVertices.CopyTo(modifiedVertices, 0);  // Copy original to modified array

        // Initialize velocities array
        velocities = new Vector3[originalVertices.Length];
    }

    void Update()
    {
        ApplyPhysicsToVertices();
        UpdateMesh();
    }

    void ApplyPhysicsToVertices()
    {
        // For each vertex, apply gravity and external acceleration
        for (int i = 0; i < modifiedVertices.Length; i++)
        {
            // Skip the vertices that belong to the top rows (fixed)
            if (IsTopFixedVertex(i)) continue;

            // Apply external forces (gravity + acceleration)
            Vector3 force = new Vector3(0, gravity, 0) + externalAcceleration;

            // Integrate velocity (v = v + a * dt) and position (p = p + v * dt)
            velocities[i] += force * Time.deltaTime;
            modifiedVertices[i] += velocities[i] * Time.deltaTime;

            // Apply some stiffness to resist free flow (simulating paper rigidity)
            velocities[i] *= (1 - stiffness);
        }
    }

    // Check if a vertex is in the top row
    bool IsTopFixedVertex(int vertexIndex)
    {
        // Since each cell has 8 vertices, find which row this vertex belongs to
        int row = vertexIndex / (width * 8);  // Use the width from PaperSkinnedMeshGenerator
        return row < topFixedRow;
    }

    void ValidateVertices(Vector3[] vertices)
    {
        for (int i = 0; i < vertices.Length; i++)
        {
            if (float.IsNaN(vertices[i].x) || float.IsNaN(vertices[i].y) || float.IsNaN(vertices[i].z))
            {
                Debug.LogError($"Vertex {i} has NaN values: {vertices[i]}");
            }

            if (vertices[i].magnitude > 1000f)  // Adjust based on your scale
            {
                Debug.LogError($"Vertex {i} is too large: {vertices[i]}");
            }
        }
    }

    void UpdateMesh()
    {
        ValidateVertices(modifiedVertices);  // Call the validation before assigning vertices
        mesh.vertices = modifiedVertices;
        mesh.RecalculateNormals();
        skinnedMeshRenderer.sharedMesh = mesh;
    }

}
