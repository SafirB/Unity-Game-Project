using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }


    public TextMeshProUGUI scoreText;
    private int score = 0;

    private bool uiVisible = true;
    private bool controlsVisible = false;
    public CanvasGroup uiCanvasGroup;
    public CanvasGroup controlsCanvasGroup;

    public GameObject gameOverPanel;

    public Slider alertLevelSlider;
    private float alertLevel = 0f;
    private float maxAlertLevel = 100f;

    public static Vector3 LastKnownPlayerPosition;
    private bool alertTriggered = false;


    void Start()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        UpdateUI();

    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.H)) // UI visibility toggle
        {
            ToggleUI();
        }


        if (Input.GetKeyDown(KeyCode.I)) //  Controls UI visibility toggle
        {
            ToggleControlsUI();
        }
    }

    public bool isHighAlert
    {
        get { return alertLevel >= maxAlertLevel; }
    }

    public void IncreaseAlert(float amount)
    {
        alertLevel = Mathf.Clamp(alertLevel + amount, 0f, maxAlertLevel);
        UpdateAlertUI();

        if (!alertTriggered && isHighAlert)
        {
            alertTriggered = true;
            TriggerGlobalGuardResponse();
        }
    }

    private void TriggerGlobalGuardResponse()
    {
        AdaptiveGuard[] adaptiveGuards = FindObjectsOfType<AdaptiveGuard>();
        AdaptivePatroller[] adaptivePatrollers = FindObjectsOfType<AdaptivePatroller>();
        Patroller[] patrollers = FindObjectsOfType<Patroller>();

        foreach (var g in adaptiveGuards)
            g.InvestigateFromAlert(LastKnownPlayerPosition);

        foreach (var g in adaptivePatrollers)
            g.InvestigateFromAlert(LastKnownPlayerPosition);

        foreach (var g in patrollers)
            g.InvestigateFromAlert(LastKnownPlayerPosition);
    }


    private void UpdateAlertUI()
    {
        if (alertLevelSlider != null)
        {
            alertLevelSlider.value = alertLevel;
        }
    }

    public void AddScore(int amount)
    {
        score += amount;
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (scoreText != null)
        {
            scoreText.text = "Score: " + score;
        }
    }

    public void ToggleUI()
    {
        uiVisible = !uiVisible;
        if (uiCanvasGroup != null)
        {
            uiCanvasGroup.alpha = uiVisible ? 1 : 0;
            uiCanvasGroup.interactable = uiVisible;
            uiCanvasGroup.blocksRaycasts = uiVisible;
        }
    }

    public void ToggleControlsUI()
    {
        controlsVisible = !controlsVisible;
        if (controlsCanvasGroup != null)
        {
            controlsCanvasGroup.alpha = controlsVisible ? 1 : 0;
            controlsCanvasGroup.interactable = controlsVisible;
            controlsCanvasGroup.blocksRaycasts = controlsVisible;
        }
    }

    public void ShowGameOver()
    {
        gameOverPanel.SetActive(true);
        Time.timeScale = 0f;
    }

    public void RetryLevel()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
