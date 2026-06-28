using UnityEngine;

public class UIPausePanel : MonoBehaviour
{
    [Header("Panels")]
    public GameObject pausePanel;
    public GameObject livePanel;
    public GameObject startPanel;

    void OnEnable()
    {
        SimulationManager.Instance.OnSimResume += HandleResume;
    }

    void OnDisable()
    {
        SimulationManager.Instance.OnSimResume -= HandleResume;
    }

    // Wired to "Continue" button
    public void OnContinuePressed()
    {
        SimulationManager.Instance.ResumeSimulation();
        // Panel closes via HandleResume once manager confirms resume
    }

    // Wired to "End Simulation" button
    public void OnEndSimulationPressed()
    {
        SimulationManager.Instance.EndSimulation(forceStop: true);
        pausePanel.SetActive(false);
    }

    // Wired to "Return to Start" button
    public void OnReturnToStartPressed()
    {
        SimulationManager.Instance.ResetSimulation();
        pausePanel.SetActive(false);
        livePanel.SetActive(false);
        startPanel.SetActive(true);
    }

    private void HandleResume()
    {
        pausePanel.SetActive(false);
    }
}
