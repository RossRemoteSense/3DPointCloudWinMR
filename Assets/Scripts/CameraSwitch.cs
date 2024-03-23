using UnityEngine;
using Valve.VR; // Import the SteamVR namespace

public class CameraSwitch : MonoBehaviour
{
    public Camera desktopCamera; // Reference to the desktop camera
    public GameObject vrCameraRig; // Reference to the SteamVR camera rig (e.g., SteamVR_PlayArea)

    private bool isVRMode = true;

    void Start()
    {
        // Ensure VR mode is enabled
        UnityEngine.XR.XRSettings.enabled = false;
        desktopCamera.enabled = true;
    }   

    void Update()
    {
        // Check if the V key is pressed
        if (Input.GetKeyDown(KeyCode.V))
        {
            // Toggle between VR and desktop mode
            isVRMode = !isVRMode;

            // Switch camera modes
            SwitchCameraMode();
        }
    }

    void SwitchCameraMode()
    {
        if (isVRMode)
        {
            // Enable SteamVR camera rig and disable desktop camera
            vrCameraRig.SetActive(true);
            desktopCamera.enabled = false;
            // Ensure VR mode is enabled
            UnityEngine.XR.XRSettings.enabled = true;
        }
        else
        {
            // Disable SteamVR camera rig and enable desktop camera
            vrCameraRig.SetActive(false);
            desktopCamera.enabled = true;
            // Disable VR mode to switch to a normal camera view
            UnityEngine.XR.XRSettings.enabled = false;
        }
    }
}