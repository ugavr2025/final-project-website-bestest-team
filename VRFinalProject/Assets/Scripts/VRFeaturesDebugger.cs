using UnityEngine;
using Unity.Netcode;

public class VRFeaturesDebugger : MonoBehaviour
{
    [SerializeField] private GameObject menu;
    [SerializeField] private MeasureTapeFeature tapeMeasure;
    [SerializeField] private MenuFeature menuFeature;

    private float lastStatusLog = 0f;
    private const float STATUS_LOG_INTERVAL = 5f;

    void Update()
    {
        if (Time.time - lastStatusLog >= STATUS_LOG_INTERVAL)
        {
            LogNetworkStatus();
            lastStatusLog = Time.time;
        }

        if (OVRInput.GetDown(OVRInput.Button.Start))
        {
            Debug.Log($"[VRDebug] START/MENU button pressed! Menu active: {menu.activeSelf}");
        }

        if (OVRInput.GetDown(OVRInput.Button.One))
        {
            Debug.Log("[VRDebug] A button (ConvAI Talk) pressed on RIGHT controller!");
        }

        if (OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.LTouch))
        {
            Debug.Log("[VRDebug] LEFT trigger (Tape Measure) pressed!");
            if (tapeMeasure != null)
            {
                Debug.Log($"[VRDebug] TapeMeasure IsSpawned: {tapeMeasure.IsSpawned}");
            }
        }

        if (OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.RTouch))
        {
            Debug.Log("[VRDebug] RIGHT trigger (Tape Measure) pressed!");
            if (tapeMeasure != null)
            {
                Debug.Log($"[VRDebug] TapeMeasure IsSpawned: {tapeMeasure.IsSpawned}");
            }
        }
    }

    void LogNetworkStatus()
    {
        if (NetworkManager.Singleton == null)
        {
            Debug.LogWarning("[VRDebug] NetworkManager.Singleton is NULL!");
            return;
        }

        Debug.Log($"[VRDebug] === NETWORK STATUS ===");
        Debug.Log($"[VRDebug] IsListening: {NetworkManager.Singleton.IsListening}");
        Debug.Log($"[VRDebug] IsServer: {NetworkManager.Singleton.IsServer}");
        Debug.Log($"[VRDebug] IsHost: {NetworkManager.Singleton.IsHost}");
        Debug.Log($"[VRDebug] IsClient: {NetworkManager.Singleton.IsClient}");
        Debug.Log($"[VRDebug] IsConnectedClient: {NetworkManager.Singleton.IsConnectedClient}");
        
        if (tapeMeasure != null)
        {
            Debug.Log($"[VRDebug] TapeMeasure IsSpawned: {tapeMeasure.IsSpawned}");
            Debug.Log($"[VRDebug] TapeMeasure NetworkObjectId: {tapeMeasure.NetworkObjectId}");
        }
        else
        {
            Debug.LogWarning("[VRDebug] TapeMeasure reference is NULL!");
        }
        
        Debug.Log($"[VRDebug] =====================");
    }
}
