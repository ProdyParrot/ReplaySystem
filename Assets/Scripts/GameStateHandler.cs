using UnityEngine;

public class GameStateHandler : MonoBehaviour
{
    public static GameStateHandler instance;

    [Header("References Objects")]
    [SerializeField] private GameObject playerObj;
    [SerializeField] private GameObject playerReplayObj;
    [SerializeField] private GameObject playerFollowCameraObj;
    [SerializeField] private GameObject replayCamera;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Debug.LogError("Can only have one game state handler at a time!");
        }
    }

    public void ScenarioStartCallback()
    {
        playerObj.SetActive(true);
        playerFollowCameraObj.SetActive(true);
    }

    public void ScenarioStopCallback()
    {
        playerObj.SetActive(false);
        playerFollowCameraObj.SetActive(false);
    }

    public void ReplayStartCallback()
    {
        playerReplayObj.SetActive(true);
        replayCamera.SetActive(true);
    }

    public void ReplayQuitCallback()
    {
        playerReplayObj.SetActive(false);
        replayCamera.SetActive(false);
    }
}
