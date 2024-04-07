using UnityEngine;
using System.IO;
using System;
using Newtonsoft.Json;
using System.Collections.Generic;

public class RotateAndZoom : MonoBehaviour
{
    public float rotationSpeed = 2.5f; // Speed of rotation
    public float zoomSpeed = 2.5f; // Speed of zooming
    public float panSpeed = 2.5f; // Speed of panning
    public GameObject[] objectsToTransform; // Array of GameObjects to be transformed
    public string fileName = "TransformData.json"; // Name of the file to store data
    public Camera mainCamera; // The camera to be controlled    
    public Vector3 centerOfMass; // Center of mass of the objects made public
    public GameObject brushObject; // The object to be used for brushing
    public BrushingAndLinkingViews brushLinkView; // The BrushingAndLinkingViews object

    private StreamWriter fileWriter;
    private GameObject sphere;

    void Start()
    {
        fileWriter = new StreamWriter(fileName, false);
        SetInitialCameraPosition();
    }

    void OnEnable()
    {
        fileWriter = new StreamWriter(fileName, false);
        SetInitialCameraPosition();
        // Assign brushObject to the public variables input1 and input2 in the brushLinkView object
        brushLinkView.input1 = brushObject.transform;
        brushLinkView.input2 = brushObject.transform;
    }

    void OnDisable()
    {
    }

    void SetInitialCameraPosition()
    {
        mainCamera.transform.position = new Vector3(centerOfMass.x, centerOfMass.y, centerOfMass.z - 1.0f);
    }

    void Update()
    {
        // Create a dictionary to store data
        var data = new Dictionary<string, object>
        {
            { "Date", DateTime.Now.ToShortDateString() },
            { "TimeStamp", DateTime.Now.TimeOfDay.ToString() },
            { "Mouse X", Input.mousePosition.x },
            { "Mouse Y", Input.mousePosition.y },
            { "Mouse Button Left Status", Input.GetMouseButton(0) },
            { "Mouse Button Right Status", Input.GetMouseButton(1) },
            { "Mouse Button Middle Status", Input.GetMouseButton(2) },
            { "Mouse Wheel Value", Input.GetAxis("Mouse ScrollWheel") },
            { "Shift Button Status", Input.GetKey(KeyCode.LeftShift) },
            { "Alt Button Status", Input.GetKey(KeyCode.LeftAlt) }
        };
        // Convert the dictionary to json and write it to the file
        string json = JsonConvert.SerializeObject(data, Formatting.Indented);
        fileWriter.WriteLine(json);

        // Handle Rotation
        if (Input.GetMouseButton(0) && !Input.GetKey(KeyCode.LeftShift))
        {
            // Calculate rotation based on mouse movement
            float rotationX = Input.GetAxis("Mouse X") * rotationSpeed;
            float rotationY = Input.GetAxis("Mouse Y") * rotationSpeed;

            Quaternion rotation = Quaternion.Euler(rotationY, -rotationX, 0);
            foreach (var obj in objectsToTransform)
            {
                obj.transform.RotateAround(centerOfMass, Vector3.up, -rotationX);
                obj.transform.RotateAround(centerOfMass, Vector3.right, rotationY);
            }
        }

        if (Input.GetKey(KeyCode.LeftShift) && Input.GetMouseButton(0))
        {
            brushLinkView.isBrushing = true;
            brushObject.SetActive(true);

            // Convert mouse position to world position
            Vector3 mousePos = Input.mousePosition;
            // Adjust mousePos.z to properly unproject the screen point to world coordinates
            mousePos.z = mainCamera.nearClipPlane; // Use the camera's near clip plane to properly set the Z distance
            Vector3 worldPosition = mainCamera.ScreenToWorldPoint(mousePos);
            worldPosition.z += mainCamera.transform.forward.z * brushObject.transform.localScale.z / 2.0f;

            // Update the position of the brushObject to follow the mouse in the world view
            brushObject.transform.position = worldPosition;
        }
        else if (!Input.GetKey(KeyCode.LeftShift))
        {
            brushLinkView.isBrushing = false;
            brushObject.SetActive(false);
            // Optionally, reset brushObject's position or state if needed
        }

        // Handle Zoom
        float scroll = Input.GetAxis("Mouse ScrollWheel") * zoomSpeed;
        
        if (scroll != 0f)
        {
            Vector3 zoomVector = mainCamera.transform.forward * scroll;
            foreach (var obj in objectsToTransform)
            {
                obj.transform.position -= zoomVector;
            }
        }

        // Handle Panning
        if (Input.GetMouseButton(1))
        {
            float panX = Input.GetAxis("Mouse X") * panSpeed;
            float panY = Input.GetAxis("Mouse Y") * panSpeed;

            Vector3 cameraRight = mainCamera.transform.right;
            Vector3 cameraUp = mainCamera.transform.up;
            Vector3 panMovement = cameraRight * panX + cameraUp * panY;

            foreach (var obj in objectsToTransform)
            {
                obj.transform.position += panMovement;
            }
        }

        // Reset objects to initial state
        if (Input.GetKeyDown(KeyCode.Space))
        {
            foreach (var obj in objectsToTransform)
            {
                obj.transform.position = Vector3.zero;
                obj.transform.rotation = Quaternion.identity;
            }
        }
    }

    void OnDestroy()
    {
        // Close the file writer when the object is destroyed
        fileWriter.Close();
    }
}