using System.Collections.Generic;
using UnityEngine;

public class MeasureTapeFeature : MonoBehaviour
{
    [Range(0.005f, 0.05f)]
    [SerializeField] private float tapeWidth = 0.01f;

    [SerializeField] private OVRInput.Button tapeActionButton;
    [SerializeField] private Material tapeMaterial;
    [SerializeField] private GameObject measurementInfoPrefab;

    [SerializeField] private Transform leftControllerTapeArea;
    [SerializeField] private Transform rightControllerTapeArea;

    private List<GameObject> savedTapeLines = new();
    private LineRenderer lastTapeLineRenderer;

    void Start()
    {
        Debug.Log($"Controllers Connected - Left: {OVRInput.IsControllerConnected(OVRInput.Controller.LTouch)}, Right: {OVRInput.IsControllerConnected(OVRInput.Controller.RTouch)}");
        Debug.Log($"Active Controller: {OVRInput.GetActiveController()}");
    }

    void Update()
    {
        if (!OVRInput.IsControllerConnected(OVRInput.Controller.LTouch) && !OVRInput.IsControllerConnected(OVRInput.Controller.RTouch))
        {
            return;
        }

        HandleControllerActions(OVRInput.Controller.LTouch, leftControllerTapeArea);
        HandleControllerActions(OVRInput.Controller.RTouch, rightControllerTapeArea);
    }

    private void HandleControllerActions(OVRInput.Controller controller, Transform tapeArea)
    {
        if (OVRInput.GetDown(tapeActionButton, controller))
        {
            Debug.Log($"Tape button pressed on {controller}");
            HandleDownAction(tapeArea);
        }

        if (OVRInput.Get(tapeActionButton, controller))
        {
            HandleHoldAction(tapeArea);
        }

        if (OVRInput.GetUp(tapeActionButton, controller))
        {
            Debug.Log($"Tape button released on {controller}");
            HandleUpAction(tapeArea);
        }
    }

    private void HandleDownAction(Transform tapeArea)
    {
        CreateNewTapeLine(tapeArea.position);
    }

    private void HandleHoldAction(Transform tapeArea)
    {
        if (lastTapeLineRenderer != null)
        {
            lastTapeLineRenderer.SetPosition(1, tapeArea.position);
        }
    }

    private void HandleUpAction(Transform tapeArea)
    {
        // You may want to finalize the measurement here later
    }

    private void CreateNewTapeLine(Vector3 initialPosition)
    {
        var newTapeLine = new GameObject($"TapeLine_{savedTapeLines.Count}", typeof(LineRenderer));

        lastTapeLineRenderer = newTapeLine.GetComponent<LineRenderer>();
        lastTapeLineRenderer.positionCount = 2;
        lastTapeLineRenderer.startWidth = tapeWidth;
        lastTapeLineRenderer.endWidth = tapeWidth;
        lastTapeLineRenderer.material = tapeMaterial;
        lastTapeLineRenderer.SetPosition(0, initialPosition);
        //lastTapeLineRenderer.SetPosition(1, initialPosition);

        savedTapeLines.Add(newTapeLine);
    }
}
