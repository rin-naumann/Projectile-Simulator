using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UILivePanel : MonoBehaviour
{
    [Header("HUD Labels")]
    public TMP_Text speedText;
    public TMP_Text positionText;
    public TMP_Text distanceText;
    public TMP_Text timeText;

    [Header("Scrollbars")]
    public Scrollbar horizontalScrollbar;
    public Scrollbar verticalScrollbar;

    [Header("Camera")]
    public Camera simCamera;
    public float cameraMargin = 5f;

    [Header("Motion Path")]
    public LineRenderer motionPathRenderer;

    [Header("Panels")]
    public GameObject livePanel;
    public GameObject pausePanel;

    private float _simMaxX;
    private float _simMaxY;
    private float _simMinY;
    private float _camHalfWidth;
    private float _camHalfHeight;
    private int   _pathPointCount;

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
        _simMaxX = d.startX + d.v0x * timeOfFlight + cameraMargin;
        _simMaxY = d.startY + (d.v0y * d.v0y) / (2f * d.gravity) + cameraMargin;
        _simMinY = d.startY - cameraMargin; 

        // Expand bounds if targetTime pushes further
        if (d.targetTime >= 0f)
        {
            float xAtTime = d.startX + d.v0x * d.targetTime;
            float yAtTime = d.startY + d.v0y * d.targetTime - 0.5f * d.gravity * d.targetTime * d.targetTime;
            _simMaxX = Mathf.Max(_simMaxX, xAtTime + cameraMargin);
            _simMaxY = Mathf.Max(_simMaxY, yAtTime + cameraMargin);
            _simMinY = Mathf.Min(_simMinY, yAtTime - cameraMargin);
        }

        // Camera half-extents in world units
        _camHalfHeight = simCamera.orthographicSize;
        _camHalfWidth  = _camHalfHeight * simCamera.aspect;

        // Motion path setup
        if (motionPathRenderer != null)
        {
            motionPathRenderer.positionCount = 0;
            _pathPointCount = 0;
            motionPathRenderer.gameObject.SetActive(d.showMotionPath);
        }
    }

    private void HandleSimEnd()
    {
        livePanel.SetActive(false);
    }

    void Update()
    {
        if (SimulationManager.Instance.Data == null)      return;
        if (!SimulationManager.Instance.Data.isRunning)   return;

        // ESC to pause
        if (Input.GetKeyDown(KeyCode.Escape) && !SimulationManager.Instance.Data.isPaused)
        {
            SimulationManager.Instance.PauseSimulation();
            pausePanel.SetActive(true);
            return;
        }

        if (SimulationManager.Instance.Data.isPaused) return;

        SimulationData d = SimulationManager.Instance.Data;

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

        // Camera control
        if (d.lockCameraToObject)
            MoveCamera(d.currentX, d.currentY);
        else
            HandleScrollbarCamera();
    }

    // Centers camera on world position, clamped to sim bounds
    private void MoveCamera(float worldX, float worldY)
    {
        float clampedX = Mathf.Clamp(worldX, _camHalfWidth, _simMaxX - _camHalfWidth);
        float clampedY = Mathf.Clamp(worldY, _simMinY + _camHalfHeight, _simMaxY - _camHalfHeight);
        simCamera.transform.position = new Vector3(clampedX, clampedY, simCamera.transform.position.z);
        SyncScrollbarsToCamera();
    }

    // Reads scrollbar values and moves camera accordingly
    private void HandleScrollbarCamera()
{
    float scrollableWidth  = Mathf.Max(0f, _simMaxX - 2f * _camHalfWidth);
    float scrollableHeight = Mathf.Max(0f, (_simMaxY - _simMinY) - 2f * _camHalfHeight);

    float camX = _camHalfWidth + horizontalScrollbar.value * scrollableWidth;
    float camY = _simMinY + _camHalfHeight + verticalScrollbar.value * scrollableHeight;

    simCamera.transform.position = new Vector3(camX, camY, simCamera.transform.position.z);
}

    private void SyncScrollbarsToCamera()
    {
        float scrollableWidth  = Mathf.Max(1f, _simMaxX - 2f * _camHalfWidth);
        float scrollableHeight = Mathf.Max(1f, (_simMaxY - _simMinY) - 2f * _camHalfHeight);

        horizontalScrollbar.value = (simCamera.transform.position.x - _camHalfWidth) / scrollableWidth;
        verticalScrollbar.value   = (simCamera.transform.position.y - (_simMinY + _camHalfHeight)) / scrollableHeight;
    }
}
