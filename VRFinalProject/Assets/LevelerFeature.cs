using TMPro;
using UnityEngine;

public class LevelerFeature : MonoBehaviour
{
    [Range(1.0f, 10.0f)]
    [SerializeField] private float levelerTolerance = 5.0f;
    [SerializeField] private TextMeshPro levelerReadingText;
    [SerializeField] private Renderer levelerOuterRenderer;

    private Color levelerDefaultColor;
    private int currentAngle;
    private bool isLevel;

    private void Awake()
    {
        levelerDefaultColor = levelerOuterRenderer.material.color;
    }

    private void Update()
    {
        Vector3 objectUp = transform.up;
        Vector3 worldUp = Vector3.up;

        int angle = Mathf.RoundToInt(Vector3.Angle(objectUp, worldUp));

        Vector3 crossProduct = Vector3.Cross(worldUp, objectUp);
        if (crossProduct.z > 0) angle = -angle;

        currentAngle = angle;
        isLevel = angle <= levelerTolerance && angle >= (levelerTolerance * -1);

        UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        levelerReadingText.text = $"{currentAngle:F0}°";
        levelerOuterRenderer.material.color = isLevel ? Color.green : levelerDefaultColor;
    }
}
