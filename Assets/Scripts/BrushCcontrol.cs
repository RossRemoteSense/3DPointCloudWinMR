using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using Newtonsoft.Json;
using Valve.VR;

public class BrushCcontrol : MonoBehaviour
{
    public GameObject brushObject;
    public BrushingAndLinkingViews[] brushingViews;
    public GameObject cameraRig;
    public SteamVR_Behaviour_Pose leftHandController;
    public SteamVR_Behaviour_Pose rightHandController;
    public string fileName;
    private StreamWriter writer;
    
    public class SerializableVector3
    {
        public float x;
        public float y;
        public float z;

        public SerializableVector3(Vector3 vector3)
        {
            x = vector3.x;
            y = vector3.y;
            z = vector3.z;
        }
    }

    public class SerializableQuaternion
    {
        public float x;
        public float y;
        public float z;
        public float w;

        public SerializableQuaternion(Quaternion quaternion)
        {
            x = quaternion.x;
            y = quaternion.y;
            z = quaternion.z;
            w = quaternion.w;
        }
    }

    private class FrameData
    {
        public string Date;
        public string Timestamp;
        public SerializableVector3 CameraPosition;
        public SerializableQuaternion CameraRotation;
        public SerializableVector3 LeftControllerPosition;
        public SerializableQuaternion LeftControllerRotation;
        public SerializableVector3 RightControllerPosition;
        public SerializableQuaternion RightControllerRotation;
        public bool RightTriggerState;
    }

    // Start is called before the first frame update
    void Start()
    {
        writer = new StreamWriter(fileName, true);
    }

    // Update is called once per frame
    void Update()
    {
        if (cameraRig == null)
        {
            Debug.LogError("cameraRig is null");
        }

        if (SteamVR_Actions.default_GrabPinch.GetStateDown(SteamVR_Input_Sources.LeftHand))
        {
            brushObject.SetActive(true);

            foreach (var view in brushingViews)
            {
                view.isBrushing = true;
            }
        }
        
        if (SteamVR_Actions.default_GrabPinch.GetStateUp(SteamVR_Input_Sources.LeftHand))
        {
            brushObject.SetActive(false);

            foreach (var view in brushingViews)
            {
                view.isBrushing = false;
            }
        }

        Vector3 leftPosition = leftHandController.transform.position;
        Quaternion leftRotation = leftHandController.transform.rotation;

        Vector3 rightPosition = rightHandController.transform.position;
        Quaternion rightRotation = rightHandController.transform.rotation;

        FrameData frameData = new FrameData
        {
            // ...
            CameraPosition = new SerializableVector3(cameraRig.transform.position),
            CameraRotation = new SerializableQuaternion(cameraRig.transform.rotation),
            LeftControllerPosition = new SerializableVector3(leftPosition),
            LeftControllerRotation = new SerializableQuaternion(leftRotation),
            RightControllerPosition = new SerializableVector3(rightPosition),
            RightControllerRotation = new SerializableQuaternion(rightRotation),
            // ...
        };
        string json = JsonConvert.SerializeObject(frameData, Formatting.Indented);
        writer.WriteLine(json);

    }
    private void OnApplicationQuit()
    {
        writer.Close();
    }
}
