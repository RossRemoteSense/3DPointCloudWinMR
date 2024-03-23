using UnityEngine;
using System.Collections.Generic; // For using List
using IATK;

public class RaycastFromMouse : MonoBehaviour
{
    public GameObject vertexColliderPrefab; // Assign a prefab with a collider in the inspector
    private bool hasCreatedVertexColliders = false;
    private BigMesh mesh;

    // Call this method with your scatterplot vertices
    public void CreateVertexColliders(List<Vector3> vertices, float width, float height, float depth)
    {
        for (int i = 0; i < vertices.Count; i++)
        {
            GameObject vertexCollider = new GameObject(i.ToString());
            vertexCollider.transform.position = vertices[i];
            vertexCollider.tag = "Vertex"; // Tagging for raycast detection

            // Adding a BoxCollider
            BoxCollider collider = vertexCollider.AddComponent<BoxCollider>();
            collider.size = new Vector3(width, height, depth);

            // Instantiating a blue sphere at each location
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.transform.position = vertices[i];
            sphere.transform.localScale = new Vector3(width, height, depth);
            sphere.GetComponent<Renderer>().material.color = Color.blue;
        }
    }
    void Update()
    {
        if (!hasCreatedVertexColliders)
        {
            GameObject bigMeshGameObject = GameObject.Find("BigMesh");
            if (bigMeshGameObject != null)
            {
                mesh = bigMeshGameObject.GetComponent<BigMesh>();
            }
            else
            {
                Debug.LogError("BigMesh GameObject not found in the scene.");
            }
            CreateVertexColliders(new List<Vector3>(mesh.getBigMeshVertices()), 0.01f, 0.01f, 0.01f);
            hasCreatedVertexColliders = true;
        }

        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            Debug.Log("Mouse clicked");

            if (Physics.Raycast(ray, out hit))
            {
                Debug.Log("Raycast hit something");

                // Check if the hit object is a vertex collider
                if (hit.collider.gameObject.CompareTag("Vertex"))
                {
                    Debug.Log("Hit a vertex: " + hit.collider.gameObject.transform.position
                         + " with name " + hit.collider.gameObject.name);

                    // Add logic here for when a vertex is hit
                }
            }
        }
    }
}