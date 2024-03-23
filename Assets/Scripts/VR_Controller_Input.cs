using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;


public class VR_Controller_Input : MonoBehaviour
{
    [SerializeField]
    public BrushingAndLinkingViews balv;
    [SerializeField]
    public Scatterplot3D pointCloudView;

    // Start is called before the first frame update
    void Start()
    {
        balv.SELECTION_TYPE = BrushingAndLinkingViews.SelectionType.ADD;
    }

    public SteamVR_Action_Boolean grabActionBrush;
    public SteamVR_Action_Boolean selectBrushIntent;



    public SteamVR_Input_Sources rightHand;
    public SteamVR_Input_Sources leftHand;


    void Update()
    {
        if (grabActionBrush.GetStateDown(rightHand))
        {
            if(balv!=null)
            {
                balv.isBrushing = true;
            }
            
        }

        if (grabActionBrush.GetStateUp(rightHand))
        {
            print("up!");

            if (balv != null)
            {
                balv.isBrushing = false;
            }

        }

        if (selectBrushIntent.GetStateDown(rightHand))
        {
             //TODO 
        }

        if(grabActionBrush.GetStateDown(leftHand))
        {
            //TODO
        }

        if(selectBrushIntent.GetStateDown(leftHand))
        {
            //TODO
        }

    }
}
