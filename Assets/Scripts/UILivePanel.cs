using UnityEngine;
using TMPro;

public class UILivePanel : MonoBehaviour
{
    [Header("HUD Labels")]
    public TMP_Text speedText;
    public TMP_Text positionText;
    public TMP_Text distanceText;
    public TMP_Text timeText;

    [Header("Camera")]
    public Camera simCamera;
    public float cameraMargin = 5f;
    public float zoomSpeed = 2f;
    public float minOrthoSize = 2f;
    public float maxOrthoSize = 200f;
    public float dragSpeed = 1f;

    [Header("Motion Path")]
    public LineRenderer motionPathRenderer;

    [Header("Panels")]
    public GameObject livePanel;
    public GameObject pausePanel;

    private float _simMinX;
    private float _simMaxX;
    private float _simMinY;
    private float _simMaxY;
    private int   _pathPointCount;

    // Drag state
    private bool    _isDragging;
    private Vector3 _dragOrigin;

    void OnEnable()
    {
        SimulationManager.Instance.OnSimStart += HandleSimStart;
        SimulationManager.Instance.OnSimEnd   += HandleSimEnd;
    }

    void OnDisable()
    {
        SimulationManager.Instance.OnSimStart -= HandleSimStart;
        SimulationManager.Instance.OnSimEnd   -= HandleSimEnd;
    }

    private void HandleSimStart()
    {
        SimulationData d = SimulationManager.Instance.Data;

        // Pre-compute sim bounds analytically
        float timeOfFlight = 2f * d.v0y / d.gravity;
        _simMinX = d.startX - cameraMargin;
        _simMaxX = d.startX + d.v0x * timeOfFlight + cameraMargin;
        _simMaxY = d.startY + (d.v0y * d.v0y) / (2f * d.gravity) + cameraMargin;
        _simMinY = d.startY - cameraMargin;

        // Expand if targetTime pushes bounds further
        if (d.targetTime >= 0f)
        {
            float xAtTime = d.startX + d.v0x * d.targetTime;
            float yAtTime = d.startY + d.v0y * d.targetTime - 0.5f * d.gravity * d.targetTime * d.targetTime;
            _simMaxX = Mathf.Max(_simMaxX, xAtTime + cameraMargin);
            _simMaxY = Mathf.Max(_simMaxY, yAtTime + cameraMargin);
            _simMinY = Mathf.Min(_simMinY, yAtTime - cameraMargin);
        }

        // Motion path setup
        if (motionPathRenderer != null)
        {
            motionPathRenderer.positionCount = 0;
            _pathPointCount = 0;
            motionPathRenderer.gameObject.SetActive(d.showMotionPath);
        }

        // Place camera at start position
        FitCameraToPoint(d.startX, d.startY);
    }

    private void HandleSimEnd()
    {
        SimulationManager.Instance.Data.lockCameraToObject = false;
        FitCameraToTrajectory();
    }

    void Update()
    {
        if (SimulationManager.Instance.Data == null) return;

        SimulationData d = SimulationManager.Instance.Data;

        // Post-sim: zoom and drag only
        if (d.isComplete)
        {
            HandleZoom();
            HandleDrag();
            return;
        }

        if (!d.isRunning) return;

        // ESC to pause
        if (Input.GetKeyDown(KeyCode.Escape) && !d.isPaused)
        {
            SimulationManager.Instance.PauseSimulation();
            pausePanel.SetActive(true);
            return;
        }

        if (d.isPaused) return;

        // HUD labels
        if (d.showLiveVariables)
        {
            speedText.text    = $"Speed : {d.currentSpeed:F2} m/s";
            positionText.text = $"Position : {d.currentX:F2} , {d.currentY:F2}";
            distanceText.text = $"Distance from Start : {d.distanceFromStart:F2} m";
            timeText.text     = $"Time : {d.elapsedTime:F2} s";
        }

        // Motion path
        if (d.showMotionPath && motionPathRenderer != null)
        {
            _pathPointCount++;
            motionPathRenderer.positionCount = _pathPointCount;
            motionPathRenderer.SetPosition(
                _pathPointCount - 1,
                new Vector3(d.currentX, d.currentY, 0f)
            );
        }

        // Camera follows object during sim
        if (d.lockCameraToObject)
            MoveCamera(d.currentX, d.currentY);
        else
        {
            HandleZoom();
            HandleDrag();
        }
    }

    // Instantly moves camera to center on a world point
    private void MoveCamera(float worldX, float worldY)
    {
        simCamera.transform.position = new Vector3(worldX, worldY, simCamera.transform.position.z);
    }

    // Zooms camera via scroll wheel
    private void HandleZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) < 0.001f) return;

        simCamera.orthographicSize -= scroll * zoomSpeed * simCamera.orthographicSize;
        simCamera.orthographicSize  = Mathf.Clamp(simCamera.orthographicSize, minOrthoSize, maxOrthoSize);
    }

    // Click and drag to pan camera
    private void HandleDrag()
    {
        if (Input.GetMouseButtonDown(0))
        {
            _isDragging = true;
            _dragOrigin = simCamera.ScreenToWorldPoint(Input.mousePosition);
        }

        if (Input.GetMouseButtonUp(0))
            _isDragging = false;

        if (_isDragging && Input.GetMouseButton(0))
        {
            Vector3 currentPos = simCamera.ScreenToWorldPoint(Input.mousePosition);
            Vector3 delta      = _dragOrigin - currentPos;
            simCamera.transform.position += delta;
            // No need to update _dragOrigin — ScreenToWorldPoint recalculates
            // relative to the new camera position each frame naturally
        }
    }

    // Fits camera to show a single point at launch
    private void FitCameraToPoint(float x, float y)
    {
        simCamera.transform.position = new Vector3(x, y, simCamera.transform.position.z);
        simCamera.orthographicSize   = 20f;
    }

    // Fits camera to frame the entire trajectory on sim end
    private void FitCameraToTrajectory()
    {
        float centerX = (_simMinX + _simMaxX) * 0.5f;
        float centerY = (_simMinY + _simMaxY) * 0.5f;

        float width   = _simMaxX - _simMinX;
        float height  = _simMaxY - _simMinY;

        // Fit the larger dimension, accounting for aspect ratio
        float requiredByWidth  = (width  * 0.5f) / simCamera.aspect;
        float requiredByHeight =  height * 0.5f;

        simCamera.orthographicSize   = Mathf.Max(requiredByWidth, requiredByHeight);
        simCamera.transform.position = new Vector3(centerX, centerY, simCamera.transform.position.z);
    }
}