using System.Collections.Generic;
using UnityEngine;
using LearnXR.Core.Utilities;

public class MenuFeature : MonoBehaviour
{
    [SerializeField] private GameObject menu;
    [SerializeField] private OVRInput.Button buttonForMenuActivation;
    [SerializeField] private GameObject levelerPrefab;
    [SerializeField] private MeasureTapeFeature measureTapeFeature;

    private List<GameObject> levelersAdded = new();

    private void Awake()
    {
        menu.SetActive(false);
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
        var newLeveler = Instantiate(levelerPrefab);
        CommonUtilities.Instance.PlaceObjectInFrontOfCamera(newLeveler.transform);
        levelersAdded.Add(newLeveler);
    }

    public void DeleteAll()
    {
        foreach (var leveler in levelersAdded)
            Destroy(leveler);

        levelersAdded.Clear();

        if (measureTapeFeature != null)
        {
            measureTapeFeature.ClearAllTapes();
        }
    }
}
