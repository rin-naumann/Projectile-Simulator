using System;
using System.Collections;
using UnityEngine;

public class SimulationManager : MonoBehaviour
{
    public static SimulationManager Instance { get; private set; }

    [Header("References")]
    public GameObject projectilePrefab;
    public GameObject markerPrefab;
    public Transform simulationRoot;

    public SimulationData Data { get; private set; }

    public event Action OnSimStart;
    public event Action OnSimPause;
    public event Action OnSimResume;
    public event Action OnSimEnd;

    private GameObject _projectileInstance;
    private float _lastMarkerSecond;
    private float _launchTime;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // Called by UIStartPanel after writing inputs into Data
    public void Initialize(SimulationData data)
    {
        Data = data;
        Data.ComputeInitialComponents();
        Data.ResetRuntime();
    }

    // Begins the 3-second countdown then launches
    public void StartSimulation()
    {
        StartCoroutine(CountdownThenLaunch());
    }

    private IEnumerator CountdownThenLaunch()
    {
        yield return new WaitForSeconds(3f);

        Vector3 spawnPos = new Vector3(Data.startX, Data.startY, 0f);
        _projectileInstance = Instantiate(projectilePrefab, spawnPos, Quaternion.identity, simulationRoot);

        _launchTime = Time.time;
        _lastMarkerSecond = -1f;
        Data.isRunning = true;

        OnSimStart?.Invoke();
    }

    void Update()
    {
        if (Data == null || !Data.isRunning || Data.isPaused || Data.isComplete) return;

        float t = Time.time - _launchTime;
        Data.elapsedTime = t;

        // Closed-form kinematic update
        Data.currentX     = Data.startX + Data.v0x * t;
        Data.currentY     = Data.startY + Data.v0y * t - 0.5f * Data.gravity * t * t;
        Data.currentVx    = Data.v0x;
        Data.currentVy    = Data.v0y - Data.gravity * t;
        Data.currentSpeed = Mathf.Sqrt(
            Data.currentVx * Data.currentVx + Data.currentVy * Data.currentVy
        );

        // Straight-line distance from launch origin
        float dx = Data.currentX - Data.startX;
        float dy = Data.currentY - Data.startY;
        Data.distanceFromStart = Mathf.Sqrt(dx * dx + dy * dy);

        // Move projectile in scene
        if (_projectileInstance != null)
            _projectileInstance.transform.position = new Vector3(Data.currentX, Data.currentY, 0f);

        // Track max height
        if (Data.currentY > Data.maxHeight)
        {
            Data.maxHeight     = Data.currentY;
            Data.maxHeightTime = t;
        }

        // Per-second snapshot (always) and optional marker spawn
        // Recorded at the EXACT integer second, not whatever t happens to be this frame
        float currentSecond = Mathf.Floor(t);
        if (currentSecond > _lastMarkerSecond)
        {
            _lastMarkerSecond = currentSecond;
            RecordSecondSnapshot(currentSecond);

            if (Data.showPerSecondMarkers)
                SpawnMarker(Data.currentX, Data.currentY);
        }

        // Stop condition checks
        bool timeUp     = Data.targetTime     >= 0f && t                    >= Data.targetTime;
        bool distanceUp = Data.targetDistance >= 0f && Data.distanceFromStart >= Data.targetDistance;

        // Record cross-variable milestones once, recomputed at the exact target value
        if (distanceUp && Data.timeAtTargetDistance < 0f)
        {
            // Distance doesn't have a clean closed form for time, so we record
            // the frame-accurate time here. Close enough for this milestone.
            Data.timeAtTargetDistance = t;
        }

        if (timeUp && Data.distanceAtTargetTime < 0f)
        {
            // Recompute distance analytically at the exact targetTime, not the live t
            float xAt = Data.startX + Data.v0x * Data.targetTime;
            float yAt = Data.startY + Data.v0y * Data.targetTime - 0.5f * Data.gravity * Data.targetTime * Data.targetTime;
            float dxAt = xAt - Data.startX;
            float dyAt = yAt - Data.startY;
            Data.distanceAtTargetTime = Mathf.Sqrt(dxAt * dxAt + dyAt * dyAt);
        }

        // Stop when farthest condition is met
        bool bothProvided = Data.targetTime >= 0f && Data.targetDistance >= 0f;
        if (bothProvided)
        {
            if (timeUp && distanceUp) EndSimulation();
        }
        else
        {
            if (timeUp || distanceUp) EndSimulation();
        }
    }

    public void PauseSimulation()
    {
        if (Data == null || !Data.isRunning || Data.isComplete) return;
        Data.isPaused = true;
        OnSimPause?.Invoke();
    }

    public void ResumeSimulation()
    {
        if (Data == null || !Data.isPaused) return;
        // Rebase launch time so elapsed t continues correctly after pause
        _launchTime   = Time.time - Data.elapsedTime;
        Data.isPaused = false;
        OnSimResume?.Invoke();
    }

    // forceStop = true bypasses looping entirely (used by the pause menu's
    // "End Simulation" button). forceStop = false is used by the automatic
    // stop-condition checks in Update(), which respect the loop toggle.
    public void EndSimulation(bool forceStop = false)
    {
        Data.isRunning  = false;
        Data.isComplete = true;

        if (Data.loopSimulation && !forceStop)
        {
            RestartLoop();
            return;
        }

        OnSimEnd?.Invoke();
    }

    private void RestartLoop()
    {
        foreach (Transform child in simulationRoot)
            Destroy(child.gameObject);
        _projectileInstance = null;

        Data.ResetRuntime();
        StartSimulation(); // re-runs the 3s countdown then launches again
    }

    public void ResetSimulation()
    {
        StopAllCoroutines();
        foreach (Transform child in simulationRoot)
            Destroy(child.gameObject);
        _projectileInstance = null;
        Data.ResetRuntime();
    }

    // Always records position, speed, time, distance at the exact second boundary.
    // Recomputes analytically at exactTime rather than using frame-drifted
    // Data.currentX/Y/Speed, so values land exactly on whole seconds.
    private void RecordSecondSnapshot(float exactTime)
    {
        float exactX  = Data.startX + Data.v0x * exactTime;
        float exactY  = Data.startY + Data.v0y * exactTime - 0.5f * Data.gravity * exactTime * exactTime;

        float exactVx = Data.v0x;
        float exactVy = Data.v0y - Data.gravity * exactTime;
        float exactSpeed = Mathf.Sqrt(exactVx * exactVx + exactVy * exactVy);

        float dx = exactX - Data.startX;
        float dy = exactY - Data.startY;
        float exactDistance = Mathf.Sqrt(dx * dx + dy * dy);

        Data.secondMarkers.Add(new Vector2(exactX, exactY));
        Data.secondMarkerSpeeds.Add(exactSpeed);
        Data.secondMarkerTimes.Add(exactTime);
        Data.secondMarkerDistances.Add(exactDistance);
    }

    // Only called when showPerSecondMarkers is true
    private void SpawnMarker(float x, float y)
    {
        Vector3 pos = new Vector3(x, y, 0f);
        Instantiate(markerPrefab, pos, Quaternion.identity, simulationRoot);
    }
}