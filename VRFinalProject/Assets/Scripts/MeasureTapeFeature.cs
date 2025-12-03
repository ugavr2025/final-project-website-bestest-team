using LearnXR.Core.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class MeasureTapeFeature : MonoBehaviour
{
    [Range(0.005f, 0.05f)]
    [SerializeField] private float tapeWidth = 0.01f;

    [SerializeField] private OVRInput.Button tapeActionButton;
    [SerializeField] private Material tapeMaterial;
    [SerializeField] private GameObject measurementInfoPrefab;
    [SerializeField] private Vector3 measurementInfoControllerOffset = new(0, 0.045f, 0);

    [SerializeField] private string measurementInfoFormat = "<mark=#0000005A padding=\"20, 20, 10, 10\"><color=white>{0}</color></mark>";
    private float measurementInfoLength = 0.01f;

    [SerializeField] private Transform leftControllerTapeArea;
    [SerializeField] private Transform rightControllerTapeArea;


    private List<MeasuringTape> savedTapeLines = new();
    private TextMeshPro lastMeasurementInfo;
    private LineRenderer lastTapeLineRenderer;

    private OVRInput.Controller? currentController;
    private OVRCameraRig cameraRig;


    //might have issues with findfirstobjectbytype because findobjectoftype is deprecated
    private void Awake() => cameraRig = FindFirstObjectByType<OVRCameraRig>();
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
        if (currentController != controller && currentController != null) return;
    
        if (OVRInput.GetDown(tapeActionButton, controller))
        {
            currentController = controller;
            Debug.Log($"Tape button pressed on {controller}");
            HandleDownAction(tapeArea);
        }

        if (OVRInput.Get(tapeActionButton, controller))
        {
            HandleHoldAction(tapeArea);
        }

        if (OVRInput.GetUp(tapeActionButton, controller))
        {
            currentController = null;
            Debug.Log($"Tape button released on {controller}");
            HandleUpAction(tapeArea);
        }
    }

    private void HandleDownAction(Transform tapeArea)
    {
        CreateNewTapeLine(tapeArea.position);
        AttachAndDetachMeasurementInfo(tapeArea);
    }

    private void HandleHoldAction(Transform tapeArea)
    {
            lastTapeLineRenderer.SetPosition(1, tapeArea.position);
            CalculateMeasurements();
            AttachAndDetachMeasurementInfo(tapeArea);
    }

    private void HandleUpAction(Transform tapeArea)
    {
        AttachAndDetachMeasurementInfo(tapeArea, false);

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

        lastMeasurementInfo = Instantiate(measurementInfoPrefab, Vector3.zero, Quaternion.identity).GetComponent<TextMeshPro>();
        lastMeasurementInfo.GetComponent<BillboardAlignment>().AttachTo(cameraRig.centerEyeAnchor);
        lastMeasurementInfo.gameObject.SetActive(false);


        savedTapeLines.Add(new MeasuringTape
        {
            TapeLine = newTapeLine,
            TapeInfo = lastMeasurementInfo,
        });
    }

    private void AttachAndDetachMeasurementInfo(Transform tapeArea, bool attachToController = true)
    {
        //Attached to controller while measuring
        if (attachToController)
        {
            lastMeasurementInfo.gameObject.SetActive(true);
            lastMeasurementInfo.transform.SetParent(tapeArea.transform.parent);
            lastMeasurementInfo.transform.localPosition = measurementInfoControllerOffset;
        }
        //places between two points
        else
        {
            lastMeasurementInfo.transform.SetParent(lastTapeLineRenderer.transform);
            var lineDirection = lastTapeLineRenderer.GetPosition(0) - lastTapeLineRenderer.GetPosition(1);

            Vector3 lineCrossProduct = Vector3.Cross(lineDirection, Vector3.up);

            Vector3 lineMidPoint = (lastTapeLineRenderer.GetPosition(0) + lastTapeLineRenderer.GetPosition(1)) / 2.0f;

            lastMeasurementInfo.transform.position = lineMidPoint + (lineCrossProduct.normalized * measurementInfoLength);
        }
    }

    private void CalculateMeasurements()
    {
        var distance = Vector3.Distance(lastTapeLineRenderer.GetPosition(0), lastTapeLineRenderer.GetPosition(1));
        var inches = MeasuringTape.MetersToInches(distance);
        var centimeters = MeasuringTape.MetersToCentimeters(distance);
        var lastLine = savedTapeLines.Last();
        lastLine.TapeInfo.text = string.Format(measurementInfoFormat, $"{inches:F2}″ <i>{centimeters:F2}cm</i>");
    }

    private void OnDestroy()
    {
        foreach (var tapeLine in savedTapeLines)
        {
            Destroy(tapeLine.TapeLine);
        }
        savedTapeLines.Clear();
    }
}
