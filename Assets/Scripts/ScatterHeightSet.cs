using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScatterHeightSet : MonoBehaviour
{
    public GameObject[] objectsToAdjust; // Array of GameObjects to adjust height
    public float height; // Height to set the y value of the GameObjects
    private bool first = true; // Boolean to ensure the loop runs only once

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (first)
        {
            foreach (GameObject obj in objectsToAdjust)
            {
                Vector3 newPosition = obj.transform.position;
                newPosition.y = height;
                obj.transform.position = newPosition;
            }
            first = false; // Ensure the loop doesn't run again
        }
    }
}
