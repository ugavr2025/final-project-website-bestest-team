using UnityEngine;

public class ControllerDebugger : MonoBehaviour
{
    private float logInterval = 2f;
    private float lastLogTime;

    void Update()
    {
        if (Time.time - lastLogTime >= logInterval)
        {
            LogControllerStatus();
            lastLogTime = Time.time;
        }

        CheckAllButtons();
    }

    private void LogControllerStatus()
    {
        Debug.Log("=== Controller Status ===");
        Debug.Log($"Active Controller: {OVRInput.GetActiveController()}");
        Debug.Log($"LTouch Connected: {OVRInput.IsControllerConnected(OVRInput.Controller.LTouch)}");
        Debug.Log($"RTouch Connected: {OVRInput.IsControllerConnected(OVRInput.Controller.RTouch)}");
        Debug.Log($"Hands Connected: {OVRInput.IsControllerConnected(OVRInput.Controller.Hands)}");
    }

    private void CheckAllButtons()
    {
        if (OVRInput.GetDown(OVRInput.Button.One))
            Debug.Log("Button One (A/X) pressed");
        
        if (OVRInput.GetDown(OVRInput.Button.Two))
            Debug.Log("Button Two (B/Y) pressed");
        
        if (OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger))
            Debug.Log("Primary Index Trigger pressed");
        
        if (OVRInput.GetDown(OVRInput.Button.PrimaryHandTrigger))
            Debug.Log("Primary Hand Trigger (grip) pressed");
        
        if (OVRInput.GetDown(OVRInput.Button.PrimaryThumbstick))
            Debug.Log("Primary Thumbstick pressed");
    }
}
