using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class MenuScreenScript : MonoBehaviour
{
    public static MenuScreenScript instance;

    [Header("UI references menu")]
    [SerializeField] private GameObject titleScreenGroup;
    [SerializeField] private GameObject pastSessionsScreenGroup;
    [SerializeField] private GameObject menuScreenGroup;

    [Header("Game state group object")]
    [SerializeField] private GameObject gameGroup;
    [SerializeField] private GameObject replayGroup;

    [Header("UI References game")]
    [SerializeField] private TMP_Text elapsedTimeText;
    float elapsedTime = 0f;
    bool gameStarted;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Debug.LogError("Can only have one menu screen script at a time!");
        }
    }

    #region Screen Navigation

    public void StartButton()
    {
        menuScreenGroup.SetActive(false);
        gameGroup.SetActive(true);
        gameStarted = true;
        elapsedTime = 0f;
        ReplayWriter.instance.ScenarioStartCallback();
        GameStateHandler.instance.ScenarioStartCallback();
    }

    public void PastSessionsButton()
    {
        titleScreenGroup.SetActive(false);
        pastSessionsScreenGroup.SetActive(true);
        PastSessionsTableScript.instance.RefreshButton();
    }

    public void BackToTitle()
    {
        pastSessionsScreenGroup.SetActive(false);
        titleScreenGroup.SetActive(true);
    }

    public void ExitButton()
    {
        Application.Quit();
    }

    public void ReplayStartCallback()
    {
        menuScreenGroup.SetActive(false);
        replayGroup.SetActive(true);
        elapsedTime = 0f;
    }

    public void ReplayQuitCallback()
    {
        menuScreenGroup.SetActive(true);
        replayGroup.SetActive(false);
    }

    #endregion

    private void Update()
    {
        if (gameStarted)
        {
            UpdateTimestamp();
        }

        // Stop session by pressing escape
        if (gameStarted && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            menuScreenGroup.SetActive(true);
            gameGroup.SetActive(false);
            gameStarted = false;
            ReplayWriter.instance.ScenarioStopCallback();
            GameStateHandler.instance.ScenarioStopCallback();
        }
    }

    void UpdateTimestamp()
    {
        elapsedTime += Time.deltaTime;

        // Convert into minutes, seconds
        int minutes = Mathf.FloorToInt(elapsedTime / 60f);
        int seconds = Mathf.FloorToInt(elapsedTime % 60f);

        // Update the UI text
        string currentTimestamp = string.Format("{0:00}:{1:00}", minutes, seconds);
        elapsedTimeText.text = "[" + currentTimestamp + "]";
    }
}
