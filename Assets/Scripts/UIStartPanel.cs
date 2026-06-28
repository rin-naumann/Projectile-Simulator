using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIStartPanel : MonoBehaviour
{
    [Header("Input Fields")]
    public TMP_InputField distanceField;
    public TMP_InputField timeField;
    public TMP_InputField speedField;
    public TMP_InputField angleField;
    public TMP_InputField gravityField;
    public TMP_InputField positionXField;
    public TMP_InputField positionYField;

    [Header("Toggles")]
    public Toggle loopSimToggle;
    public Toggle motionPathToggle;
    public Toggle perSecondMarkerToggle;
    public Toggle liveVariableToggle;
    public Toggle lockToObjectToggle;

    [Header("Panels")]
    public GameObject startPanel;
    public GameObject livePanel;

    [Header("Error Display")]
    public TMP_Text errorText;

    // Wired to Start button OnClick
    public void OnStartButtonPressed()
    {
        errorText.text = "";

        // Speed is the only required field
        if (!TryParseFloat(speedField.text, out float speed) || speed <= 0f)
        {
            errorText.text = "Speed is required and must be greater than 0.";
            return;
        }

        // Optional fields with defaults
        float angle   = TryParseFloat(angleField.text,     out float parsedAngle)                       ? parsedAngle   : 0f;
        float gravity = TryParseFloat(gravityField.text,   out float parsedGravity) && parsedGravity > 0f ? parsedGravity : 9.81f;
        float posX    = TryParseFloat(positionXField.text, out float parsedX)                           ? parsedX       : 0f;
        float posY    = TryParseFloat(positionYField.text, out float parsedY)                           ? parsedY       : 0f;

        // Stop conditions (-1 = not set)
        float targetTime = TryParseFloat(timeField.text,     out float parsedTime) ? parsedTime : -1f;
        float targetDist = TryParseFloat(distanceField.text, out float parsedDist) ? parsedDist : -1f;

        // At least one stop condition required
        if (targetTime < 0f && targetDist < 0f)
        {
            errorText.text = "Provide at least a Time or Distance to stop the simulation.";
            return;
        }

        // Build data object
        SimulationData data = new SimulationData
        {
            initialSpeed         = speed,
            launchAngleDeg       = angle,
            gravity              = gravity,
            startX               = posX,
            startY               = posY,
            targetTime           = targetTime,
            targetDistance       = targetDist,
            loopSimulation       = loopSimToggle.isOn,
            showMotionPath       = motionPathToggle.isOn,
            showPerSecondMarkers = perSecondMarkerToggle.isOn,
            showLiveVariables    = liveVariableToggle.isOn,
            lockCameraToObject   = lockToObjectToggle.isOn,
        };

        SimulationManager.Instance.Initialize(data);
        SimulationManager.Instance.StartSimulation();

        startPanel.SetActive(false);
        livePanel.SetActive(true);
    }

    private bool TryParseFloat(string input, out float result)
    {
        return float.TryParse(input, out result);
    }
}
