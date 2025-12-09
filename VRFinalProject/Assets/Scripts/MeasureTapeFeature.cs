using LearnXR.Core.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class MeasureTapeFeature : NetworkBehaviour
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
    private Dictionary<ulong, List<MeasuringTape>> clientTapeLines = new();
    private TextMeshPro lastMeasurementInfo;
    private LineRenderer lastTapeLineRenderer;

    private OVRInput.Controller? currentController;
    private OVRCameraRig cameraRig;
    
    private const float NETWORK_UPDATE_INTERVAL = 0.05f;
    private float lastNetworkUpdateTime;


    //might have issues with findfirstobjectbytype because findobjectoftype is deprecated
    private void Awake() => cameraRig = FindFirstObjectByType<OVRCameraRig>();
    void Start()
    {
        Debug.Log($"Controllers Connected - Left: {OVRInput.IsControllerConnected(OVRInput.Controller.LTouch)}, Right: {OVRInput.IsControllerConnected(OVRInput.Controller.RTouch)}");
        Debug.Log($"Active Controller: {OVRInput.GetActiveController()}");
    }

    void Update()
    {
        if (!IsSpawned) return;

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
        Debug.Log($"[TapeMeasure] Creating tape at world position: {tapeArea.position}, local: {tapeArea.localPosition}");
        CreateNewTapeLine(tapeArea.position);
        AttachAndDetachMeasurementInfo(tapeArea);
        
        if (IsSpawned)
        {
            CreateTapeLineServerRpc(savedTapeLines.Count - 1, tapeArea.position);
        }
    }

    private void HandleHoldAction(Transform tapeArea)
    {
        if (lastTapeLineRenderer == null) return;

        lastTapeLineRenderer.SetPosition(1, tapeArea.position);
        CalculateMeasurements();
        AttachAndDetachMeasurementInfo(tapeArea);
        
        if (IsSpawned && Time.time - lastNetworkUpdateTime >= NETWORK_UPDATE_INTERVAL)
        {
            UpdateTapeLineServerRpc(savedTapeLines.Count - 1, tapeArea.position);
            lastNetworkUpdateTime = Time.time;
        }
    }

    private void HandleUpAction(Transform tapeArea)
    {
        if (lastTapeLineRenderer == null) return;

        AttachAndDetachMeasurementInfo(tapeArea, false);
        
        if (IsSpawned)
        {
            Vector3 actualEndPosition = lastTapeLineRenderer.GetPosition(1);
            FinalizeTapeLineServerRpc(savedTapeLines.Count - 1, actualEndPosition);
        }
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
        lastTapeLineRenderer.SetPosition(1, initialPosition);

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
        
        Debug.Log($"[TapeMeasure] Local measurement - Distance: {distance}m, Inches: {inches:F2}, CM: {centimeters:F2}");
        Debug.Log($"[TapeMeasure] Positions - Start: {lastTapeLineRenderer.GetPosition(0)}, End: {lastTapeLineRenderer.GetPosition(1)}");
    }

    public void DeleteLastTape()
    {
        if (savedTapeLines.Count > 0)
        {
            var lastTape = savedTapeLines[savedTapeLines.Count - 1];
            
            if (lastTape.TapeLine != null)
                Destroy(lastTape.TapeLine);
            if (lastTape.TapeInfo != null)
                Destroy(lastTape.TapeInfo.gameObject);
            
            savedTapeLines.RemoveAt(savedTapeLines.Count - 1);

            if (IsSpawned)
            {
                DeleteLastTapeServerRpc();
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void DeleteLastTapeServerRpc(ServerRpcParams rpcParams = default)
    {
        DeleteLastTapeClientRpc(rpcParams.Receive.SenderClientId);
    }

    [ClientRpc]
    private void DeleteLastTapeClientRpc(ulong senderClientId)
    {
        if (NetworkManager.Singleton.LocalClientId == senderClientId) return;

        if (clientTapeLines.ContainsKey(senderClientId) && clientTapeLines[senderClientId].Count > 0)
        {
            var lastTape = clientTapeLines[senderClientId][clientTapeLines[senderClientId].Count - 1];
            
            if (lastTape.TapeLine != null)
                Destroy(lastTape.TapeLine);
            if (lastTape.TapeInfo != null)
                Destroy(lastTape.TapeInfo.gameObject);
            
            clientTapeLines[senderClientId].RemoveAt(clientTapeLines[senderClientId].Count - 1);
        }
    }

    public void ClearAllTapes()
    {
        foreach (var tapeLine in savedTapeLines)
        {
            if (tapeLine.TapeLine != null)
                Destroy(tapeLine.TapeLine);
            if (tapeLine.TapeInfo != null)
                Destroy(tapeLine.TapeInfo.gameObject);
        }
        savedTapeLines.Clear();

        if (IsSpawned)
        {
            ClearTapesServerRpc();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void ClearTapesServerRpc(ServerRpcParams rpcParams = default)
    {
        ClearTapesClientRpc(rpcParams.Receive.SenderClientId);
    }

    [ClientRpc]
    private void ClearTapesClientRpc(ulong senderClientId)
    {
        if (NetworkManager.Singleton.LocalClientId == senderClientId) return;

        if (clientTapeLines.ContainsKey(senderClientId))
        {
            foreach (var tapeLine in clientTapeLines[senderClientId])
            {
                if (tapeLine.TapeLine != null)
                    Destroy(tapeLine.TapeLine);
                if (tapeLine.TapeInfo != null)
                    Destroy(tapeLine.TapeInfo.gameObject);
            }
            clientTapeLines[senderClientId].Clear();
        }
    }

    private void OnDestroy()
    {
        foreach (var tapeLine in savedTapeLines)
        {
            if (tapeLine.TapeLine != null)
                Destroy(tapeLine.TapeLine);
        }
        savedTapeLines.Clear();

        foreach (var clientTapes in clientTapeLines.Values)
        {
            foreach (var tapeLine in clientTapes)
            {
                if (tapeLine.TapeLine != null)
                    Destroy(tapeLine.TapeLine);
            }
        }
        clientTapeLines.Clear();
    }

    [ServerRpc(RequireOwnership = false)]
    private void CreateTapeLineServerRpc(int index, Vector3 initialPosition, ServerRpcParams rpcParams = default)
    {
        CreateTapeLineClientRpc(index, initialPosition, rpcParams.Receive.SenderClientId);
    }

    [ClientRpc]
    private void CreateTapeLineClientRpc(int index, Vector3 initialPosition, ulong senderClientId)
    {
        if (NetworkManager.Singleton.LocalClientId == senderClientId) return;

        Debug.Log($"[TapeMeasure] Remote client {NetworkManager.Singleton.LocalClientId} creating tape from client {senderClientId} at position: {initialPosition}");

        if (!clientTapeLines.ContainsKey(senderClientId))
        {
            clientTapeLines[senderClientId] = new List<MeasuringTape>();
        }

        CreateNewTapeLineForClient(senderClientId, initialPosition);
    }

    [ServerRpc(RequireOwnership = false)]
    private void UpdateTapeLineServerRpc(int index, Vector3 endPosition, ServerRpcParams rpcParams = default)
    {
        UpdateTapeLineClientRpc(index, endPosition, rpcParams.Receive.SenderClientId);
    }

    [ClientRpc]
    private void UpdateTapeLineClientRpc(int index, Vector3 endPosition, ulong senderClientId)
    {
        if (NetworkManager.Singleton.LocalClientId == senderClientId) return;

        if (clientTapeLines.ContainsKey(senderClientId) && index >= 0 && index < clientTapeLines[senderClientId].Count)
        {
            var tape = clientTapeLines[senderClientId][index];
            if (tape.TapeLine != null)
            {
                var lineRenderer = tape.TapeLine.GetComponent<LineRenderer>();
                if (lineRenderer != null)
                {
                    lineRenderer.SetPosition(1, endPosition);
                    UpdateMeasurementForClientTape(senderClientId, index);
                }
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void FinalizeTapeLineServerRpc(int index, Vector3 endPosition, ServerRpcParams rpcParams = default)
    {
        FinalizeTapeLineClientRpc(index, endPosition, rpcParams.Receive.SenderClientId);
    }

    [ClientRpc]
    private void FinalizeTapeLineClientRpc(int index, Vector3 endPosition, ulong senderClientId)
    {
        if (NetworkManager.Singleton.LocalClientId == senderClientId) return;

        if (clientTapeLines.ContainsKey(senderClientId) && index >= 0 && index < clientTapeLines[senderClientId].Count)
        {
            var tape = clientTapeLines[senderClientId][index];
            if (tape.TapeLine != null)
            {
                var lineRenderer = tape.TapeLine.GetComponent<LineRenderer>();
                if (lineRenderer != null)
                {
                    lineRenderer.SetPosition(1, endPosition);
                    UpdateMeasurementForClientTape(senderClientId, index);
                    
                    if (tape.TapeInfo != null)
                    {
                        tape.TapeInfo.transform.SetParent(lineRenderer.transform);
                        var lineDirection = lineRenderer.GetPosition(0) - lineRenderer.GetPosition(1);
                        Vector3 lineCrossProduct = Vector3.Cross(lineDirection, Vector3.up);
                        Vector3 lineMidPoint = (lineRenderer.GetPosition(0) + lineRenderer.GetPosition(1)) / 2.0f;
                        tape.TapeInfo.transform.position = lineMidPoint + (lineCrossProduct.normalized * measurementInfoLength);
                    }
                }
            }
        }
    }

    private void CreateNewTapeLineForClient(ulong clientId, Vector3 initialPosition)
    {
        var newTapeLine = new GameObject($"TapeLine_Client{clientId}_{clientTapeLines[clientId].Count}", typeof(LineRenderer));

        var lineRenderer = newTapeLine.GetComponent<LineRenderer>();
        lineRenderer.positionCount = 2;
        lineRenderer.startWidth = tapeWidth;
        lineRenderer.endWidth = tapeWidth;
        lineRenderer.material = tapeMaterial;
        lineRenderer.SetPosition(0, initialPosition);
        lineRenderer.SetPosition(1, initialPosition);

        Debug.Log($"[TapeMeasure] Created remote tape line for client {clientId} at world position: {initialPosition}");
        Debug.Log($"[TapeMeasure] LineRenderer width: {tapeWidth}, Local CameraRig position: {cameraRig.transform.position}, scale: {cameraRig.transform.localScale}");

        var measurementInfo = Instantiate(measurementInfoPrefab, Vector3.zero, Quaternion.identity).GetComponent<TextMeshPro>();
        measurementInfo.GetComponent<BillboardAlignment>().AttachTo(cameraRig.centerEyeAnchor);
        measurementInfo.gameObject.SetActive(true);

        clientTapeLines[clientId].Add(new MeasuringTape
        {
            TapeLine = newTapeLine,
            TapeInfo = measurementInfo,
        });
    }

    private void UpdateMeasurementForClientTape(ulong clientId, int index)
    {
        if (clientTapeLines.ContainsKey(clientId) && index >= 0 && index < clientTapeLines[clientId].Count)
        {
            var tape = clientTapeLines[clientId][index];
            if (tape.TapeLine != null && tape.TapeInfo != null)
            {
                var lineRenderer = tape.TapeLine.GetComponent<LineRenderer>();
                var distance = Vector3.Distance(lineRenderer.GetPosition(0), lineRenderer.GetPosition(1));
                var inches = MeasuringTape.MetersToInches(distance);
                var centimeters = MeasuringTape.MetersToCentimeters(distance);
                tape.TapeInfo.text = string.Format(measurementInfoFormat, $"{inches:F2}″ <i>{centimeters:F2}cm</i>");
                
                Debug.Log($"[TapeMeasure] Remote measurement from client {clientId} - Distance: {distance}m, Inches: {inches:F2}, CM: {centimeters:F2}");
                Debug.Log($"[TapeMeasure] Remote positions - Start: {lineRenderer.GetPosition(0)}, End: {lineRenderer.GetPosition(1)}");
            }
        }
    }
}
