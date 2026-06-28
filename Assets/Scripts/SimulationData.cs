using System.Collections.Generic;
using UnityEngine;

public class SimulationData
{
    // --- Inputs ---
    public float initialSpeed;
    public float launchAngleDeg;
    public float gravity;
    public float startX;
    public float startY;
    public float targetTime;        // -1 if not set
    public float targetDistance;    // -1 if not set

    // --- Checkbox states ---
    public bool loopSimulation;
    public bool showMotionPath;
    public bool showPerSecondMarkers;
    public bool showLiveVariables;
    public bool lockCameraToObject;

    // --- Derived initial conditions ---
    public float v0x;
    public float v0y;

    // --- Runtime state ---
    public float currentX;
    public float currentY;
    public float currentVx;
    public float currentVy;
    public float currentSpeed;
    public float elapsedTime;
    public float distanceFromStart;

    // --- Analysis milestones ---
    public float maxHeight;
    public float maxHeightTime;
    public float timeAtTargetDistance;  // -1 if not reached
    public float distanceAtTargetTime;  // -1 if not reached

    // --- Per-second snapshots (always recorded) ---
    public List<Vector2> secondMarkers          = new List<Vector2>();
    public List<float>   secondMarkerSpeeds     = new List<float>();
    public List<float>   secondMarkerTimes      = new List<float>();
    public List<float>   secondMarkerDistances  = new List<float>();

    // --- Sim state flags ---
    public bool isRunning;
    public bool isPaused;
    public bool isComplete;

    // Derives v0x and v0y from speed and angle
    public void ComputeInitialComponents()
    {
        float angleRad = launchAngleDeg * Mathf.Deg2Rad;
        v0x = initialSpeed * Mathf.Cos(angleRad);
        v0y = initialSpeed * Mathf.Sin(angleRad);
    }

    // Resets all runtime and history data, keeps inputs intact
    public void ResetRuntime()
    {
        currentX            = startX;
        currentY            = startY;
        currentVx           = v0x;
        currentVy           = v0y;
        currentSpeed        = initialSpeed;
        elapsedTime         = 0f;
        distanceFromStart   = 0f;
        maxHeight           = startY;
        maxHeightTime       = 0f;
        timeAtTargetDistance = -1f;
        distanceAtTargetTime = -1f;
        isRunning           = false;
        isPaused            = false;
        isComplete          = false;
        secondMarkers.Clear();
        secondMarkerSpeeds.Clear();
        secondMarkerTimes.Clear();
        secondMarkerDistances.Clear();
    }
}
