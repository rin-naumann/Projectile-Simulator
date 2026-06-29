using System.Collections.Generic;
using System.Collections;
using UnityEngine.UI;
using UnityEngine;
using TMPro;

public class UIAnalysisPanel : MonoBehaviour
{
    [Header("Panels")]
    public GameObject analysisPanel;
    public GameObject startPanel;
    public GameObject livePanel;
    public GameObject analysisToggleButton;

    [Header("Summary Labels")]
    public TMP_Text finalPositionText;
    public TMP_Text maxHeightText;
    public TMP_Text totalRangeText;
    public TMP_Text timeOfFlightText;
    public TMP_Text impactSpeedText;
    public TMP_Text timeAtDistanceText;
    public TMP_Text distanceAtTimeText;

    [Header("Table")]
    public Transform tableContent;        // Content child of the Scroll View
    public GameObject tableRowPrefab;     // prefab: row with 6 TMP Text children
    public GameObject tableHeaderPrefab;  // prefab: header row, can be styled differently

    void Start()
    {
        SimulationManager.Instance.OnSimEnd += HandleSimEnd;
    }

    void OnDisable()
    {
        if (SimulationManager.Instance != null)
            SimulationManager.Instance.OnSimEnd -= HandleSimEnd;
    }

    private void HandleSimEnd()
    {
        analysisPanel.SetActive(true);
        analysisToggleButton.SetActive(true);
        PopulateLabels();
        BuildTable();

        StartCoroutine(RefreshLayoutNextFrame());
    }

    private IEnumerator RefreshLayoutNextFrame()
{
    yield return null; // wait one frame so rows are fully instantiated

    Canvas.ForceUpdateCanvases();

    // Rebuild innermost first, then outward
    LayoutRebuilder.ForceRebuildLayoutImmediate(tableContent.GetComponent<RectTransform>());
    LayoutRebuilder.ForceRebuildLayoutImmediate(tableContent.parent.GetComponent<RectTransform>()); // Content

    Canvas.ForceUpdateCanvases();
}

    // --- Summary labels ---

    private void PopulateLabels()
    {
        SimulationData d = SimulationManager.Instance.Data;

        finalPositionText.text = $"Final Position : {d.currentX:F2} , {d.currentY:F2}";
        impactSpeedText.text   = $"Impact Speed : {d.currentSpeed:F2} m/s";
        maxHeightText.text     = $"Max Height : {d.maxHeight:F2} m at t = {d.maxHeightTime:F2} s";

        float range           = d.currentX - d.startX;
        totalRangeText.text   = $"Total Range : {range:F2} m";
        timeOfFlightText.text = $"Time of Flight : {d.elapsedTime:F2} s";

        timeAtDistanceText.text = d.timeAtTargetDistance >= 0f
            ? $"Reached {d.targetDistance:F2} m at t = {d.timeAtTargetDistance:F2} s"
            : "Distance threshold not reached.";

        distanceAtTimeText.text = d.distanceAtTargetTime >= 0f
            ? $"At t = {d.targetTime:F2} s, distance was {d.distanceAtTargetTime:F2} m"
            : "Time threshold not reached.";
    }

    // --- Motion table ---

    private void BuildTable()
    {
        SimulationData d = SimulationManager.Instance.Data;

        // Clear any rows from a previous run
        foreach (Transform child in tableContent)
            Destroy(child.gameObject);

        // Header row
        GameObject header = Instantiate(tableHeaderPrefab, tableContent);
        SetRowTexts(header, "Time (s)", "X", "Y", "Speed (m/s)", "Dist. Start (m)", "Dist. Prev (m)");

        List<Vector2> markers   = d.secondMarkers;
        List<float>   speeds    = d.secondMarkerSpeeds;
        List<float>   times     = d.secondMarkerTimes;
        List<float>   distances = d.secondMarkerDistances;

        int count = markers.Count;

        if (count == 0)
        {
            // Fallback: no snapshots were recorded
            GameObject emptyRow = Instantiate(tableRowPrefab, tableContent);
            SetRowTexts(emptyRow, "-", "-", "-", "-", "-", "-");
            return;
        }

        for (int i = 0; i < count; i++)
        {
            // Distance from previous entry, 0 for the first row
            float distFromPrev = i == 0
                ? 0f
                : Vector2.Distance(markers[i], markers[i - 1]);

            GameObject row = Instantiate(tableRowPrefab, tableContent);
            SetRowTexts(
                row,
                times[i].ToString("F2"),
                markers[i].x.ToString("F2"),
                markers[i].y.ToString("F2"),
                speeds[i].ToString("F2"),
                distances[i].ToString("F2"),
                distFromPrev.ToString("F2")
            );
        }
    }

    // Fills TMP Text children of a row in order
    private void SetRowTexts(GameObject row, params string[] values)
    {
        TMP_Text[] cells = row.GetComponentsInChildren<TMP_Text>();
        for (int i = 0; i < Mathf.Min(cells.Length, values.Length); i++)
            cells[i].text = values[i];
    }

    // --- Buttons ---

    // Wired to "New Simulation" button
    public void OnNewSimulationPressed()
    {
        SimulationManager.Instance.ResetSimulation();

        foreach (Transform child in tableContent)
            Destroy(child.gameObject);

        analysisPanel.SetActive(false);
        livePanel.SetActive(false);
        startPanel.SetActive(true);
    }

    // Wired to "Exit Simulator" button
    public void OnExitPressed()
    {
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }

    public void OnTogglePressed()
    {
        if (analysisPanel.activeSelf)
        {
            analysisPanel.SetActive(false);
            livePanel.SetActive(true);
        }
        else
        {
            analysisPanel.SetActive(true);
            livePanel.SetActive(false);
        }
    }
}
