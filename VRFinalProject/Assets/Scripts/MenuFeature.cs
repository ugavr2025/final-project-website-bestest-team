using System.Collections.Generic;
using UnityEngine;
using LearnXR.Core.Utilities;

public class MenuFeature : MonoBehaviour
{
    [SerializeField] private GameObject menu;
    [SerializeField] private OVRInput.Button buttonForMenuActivation;
    [SerializeField] private GameObject levelerPrefab;
    [SerializeField] private MeasureTapeFeature measureTapeFeature;
    [SerializeField] private GameObject levelerGameObject;

    private List<GameObject> levelersAdded = new();
    private bool isLevelerVisible = false;
    private float lastButtonPressTime = 0f;
    private float lastDeleteButtonPressTime = 0f;
    private const float BUTTON_COOLDOWN = 0.5f;

    private void Awake()
    {
        menu.SetActive(false);
        
        Debug.Log("MenuFeature Awake called");
        
        if (levelerGameObject != null)
        {
            Debug.Log($"Leveler GameObject found: {levelerGameObject.name} at position {levelerGameObject.transform.position}");
            Debug.Log($"Leveler is active in hierarchy: {levelerGameObject.activeInHierarchy}");
            Debug.Log($"Leveler child count: {levelerGameObject.transform.childCount}");
            SetLevelerVisibility(false);
            Debug.Log("Leveler visibility set to false in Awake");
        }
        else
        {
            Debug.LogError("Leveler GameObject is NULL in Awake! Not assigned in Inspector!");
        }
    }

    private void Update()
    {
        if (OVRInput.GetDown(buttonForMenuActivation))
        {
            menu.SetActive(!menu.activeSelf);
        }
    }

    public void AddLeveler()
    {
        if (Time.time - lastButtonPressTime < BUTTON_COOLDOWN)
        {
            Debug.Log($"Button pressed too soon! Ignoring. Time since last press: {Time.time - lastButtonPressTime}");
            return;
        }
        
        lastButtonPressTime = Time.time;
        
        Debug.Log("=== AddLeveler button pressed ===");
        
        if (levelerGameObject != null)
        {
            Debug.Log($"Leveler exists in scene: YES");
            Debug.Log($"Leveler name: {levelerGameObject.name}");
            Debug.Log($"Leveler active in hierarchy: {levelerGameObject.activeInHierarchy}");
            Debug.Log($"Leveler position: {levelerGameObject.transform.position}");
            Debug.Log($"BEFORE toggle - isLevelerVisible = {isLevelerVisible}");
            
            isLevelerVisible = !isLevelerVisible;
            
            Debug.Log($"AFTER toggle - isLevelerVisible = {isLevelerVisible}");
            
            if (isLevelerVisible)
            {
                Debug.Log(">>> Attempting to SHOW leveler");
                CommonUtilities.Instance.PlaceObjectInFrontOfCamera(levelerGameObject.transform);
                Debug.Log($">>> Leveler repositioned to: {levelerGameObject.transform.position}");
                SetLevelerVisibility(true);
                Debug.Log(">>> SetLevelerVisibility(true) completed");
            }
            else
            {
                Debug.Log(">>> Attempting to HIDE leveler");
                SetLevelerVisibility(false);
                Debug.Log(">>> SetLevelerVisibility(false) completed");
            }
        }
        else
        {
            Debug.LogError("Leveler GameObject is NULL! Not assigned in Inspector!");
        }
        
        Debug.Log("=== AddLeveler completed ===");
    }

    private void SetLevelerVisibility(bool visible)
    {
        if (levelerGameObject == null)
        {
            Debug.LogError("SetLevelerVisibility: levelerGameObject is NULL!");
            return;
        }
        
        Debug.Log($"SetLevelerVisibility called with visible={visible}");
        Debug.Log($"Leveler has {levelerGameObject.transform.childCount} children");
        
        int childrenToggled = 0;
        foreach (Transform child in levelerGameObject.transform)
        {
            Debug.Log($"  Toggling child: {child.name} to {visible}");
            child.gameObject.SetActive(visible);
            childrenToggled++;
        }
        Debug.Log($"Total children toggled: {childrenToggled}");
        
        var collider = levelerGameObject.GetComponent<Collider>();
        if (collider != null)
        {
            Debug.Log($"Toggling collider to {visible}");
            collider.enabled = visible;
        }
        else
        {
            Debug.LogWarning("No collider found on leveler GameObject");
        }
        
        Debug.Log($"SetLevelerVisibility completed for visible={visible}");
    }

    public void DeleteAll()
    {
        if (Time.time - lastDeleteButtonPressTime < BUTTON_COOLDOWN)
        {
            Debug.Log($"Delete button pressed too soon! Ignoring. Time since last press: {Time.time - lastDeleteButtonPressTime}");
            return;
        }
        
        lastDeleteButtonPressTime = Time.time;
        
        if (levelersAdded.Count > 0)
        {
            Destroy(levelersAdded[levelersAdded.Count - 1]);
            levelersAdded.RemoveAt(levelersAdded.Count - 1);
        }

        if (measureTapeFeature != null)
        {
            measureTapeFeature.DeleteLastTape();
        }
    }
}
