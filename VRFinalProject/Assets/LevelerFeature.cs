using TMPro;
using Unity.Netcode;
using UnityEngine;

public class LevelerFeature : NetworkBehaviour
{
    [Range(1.0f, 10.0f)]
    [SerializeField] private float levelerTolerance = 5.0f;
    [SerializeField] private TextMeshPro levelerReadingText;
    [SerializeField] private Renderer levelerOuterRenderer;

    private Color levelerDefaultColor;
    private NetworkVariable<int> networkAngle = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private NetworkVariable<bool> networkIsLevel = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    private void Awake() => levelerDefaultColor = levelerOuterRenderer.material.color;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        networkAngle.OnValueChanged += OnAngleChanged;
        networkIsLevel.OnValueChanged += OnIsLevelChanged;
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        networkAngle.OnValueChanged -= OnAngleChanged;
        networkIsLevel.OnValueChanged -= OnIsLevelChanged;
    }

    private void Update()
    {
        if (!IsSpawned) return;

        if (IsOwner)
        {
            Vector3 objectUp = transform.up;
            Vector3 worldUp = Vector3.up;

            int angle = Mathf.RoundToInt(Vector3.Angle(objectUp, worldUp));

            Vector3 crossProduct = Vector3.Cross(worldUp, objectUp);
            if (crossProduct.z > 0) angle = -angle;

            networkAngle.Value = angle;
            networkIsLevel.Value = angle <= levelerTolerance && angle >= (levelerTolerance * -1);
        }

        UpdateVisuals();
    }

    private void OnAngleChanged(int previousValue, int newValue)
    {
        UpdateVisuals();
    }

    private void OnIsLevelChanged(bool previousValue, bool newValue)
    {
        UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        levelerReadingText.text = $"{networkAngle.Value:F0}°";
        levelerOuterRenderer.material.color = networkIsLevel.Value ? Color.green : levelerDefaultColor;
    }
}
