using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static ReplayWriter;

public class ReplayViewerScript : MonoBehaviour
{
    public static ReplayViewerScript instance;

    // Playback proeprties (variables)
    int currentFrame;
    float currentTime;
    [HideInInspector] public float totalTime;
    bool firstFrame = true;
    [HideInInspector] public bool pauseButtonStatus = false;
    bool viewingReplay;
    string replayPath;
    ReplayFrameData replayLoadedData;
    string replayLengthTimestamp;
    List<float> frameElapsedRealTime = new List<float>();
    [HideInInspector] public bool scrubbing = false;
    float replayPlaybackSpeed = 1f;
    [HideInInspector] public bool pauseReplay = true;

    [Header("References")]
    [SerializeField] TMP_Text timestampText;
    [SerializeField] TMP_Text loadingReplayText;
    [SerializeField] Slider replaySlider;
    [SerializeField] Button playButton;
    [SerializeField] Button pauseButton;
    [SerializeField] Button rewindButton;
    [SerializeField] TMP_Dropdown speedDropdown;

    // References to objects on the scene
    List<GameObject> trackedObjectReplayList;
    List<Transform> trackedTransformList = new List<Transform>();
    List<Animator> trackedAnimatorList = new List<Animator>();

    // Thread
    Thread loadReplayThread;
    bool loadedReplay = false;

    

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Debug.LogError("Can only have one replay viewer script at a time!");
        }
    }

    public void ViewReplayButtonCallback(PastSessionEntryScript pastSessionEntryScript)
    {
        // Toggle the UI
        MenuScreenScript.instance.ReplayStartCallback();
        GameStateHandler.instance.ReplayStartCallback();
        speedDropdown.SetValueWithoutNotify(1); // Set speed to the 1.0 value

        // Read the replay from the path inside the entry
        StartCoroutine(ReadReplay(pastSessionEntryScript.replayFilepath));

        // Get pointers from replay writer
        trackedObjectReplayList = ReplayWriter.instance.trackedObjectReplayList;
        SetupReplayLists();

        // Set the mode
        viewingReplay = true;

        // Start paused
        pauseReplay = true;
    }

    void SetupReplayLists()
    {
        // Replay lists
        trackedTransformList.Clear();
        trackedAnimatorList.Clear();
        for (int i = 0; i < trackedObjectReplayList.Count; i++)
        {
            trackedTransformList.Add(trackedObjectReplayList[i].transform);
        }
        for (int i = 0; i < trackedObjectReplayList.Count; i++)
        {
            trackedAnimatorList.Add(trackedObjectReplayList[i].GetComponent<Animator>());
        }
    }

    private void Update()
    {
        if (viewingReplay && loadedReplay)
        {
            // Show the current frame we are pointing at
            currentFrame = Mathf.Abs(frameElapsedRealTime.BinarySearch(currentTime));
            //Debug.Log("Showing frame " + currentFrame + " / " + replayLoadedData.objectDataAllFrames.Count);
            currentFrame = Mathf.Min(currentFrame, frameElapsedRealTime.Count - 1); // Make sure it doesn't go over the video size

            if (firstFrame)
            {
                SetupFrame();
                replaySlider.value = 0f;
                firstFrame = false;
            }
            else
            {
                if (scrubbing)
                {
                    ScrubSlider();
                    SetupFrame();

                    if (currentTime < totalTime)
                    {
                        rewindButton.gameObject.SetActive(false);
                        playButton.gameObject.SetActive(true);
                        pauseButton.gameObject.SetActive(false);
                    }
                    else
                    {
                        rewindButton.gameObject.SetActive(true);
                        playButton.gameObject.SetActive(false);
                        pauseButton.gameObject.SetActive(false);
                    }
                }
                else if (!pauseReplay)
                {
                    // If not paused, continue to advance the replay
                    if (currentTime < totalTime)
                    {
                        currentTime += Time.deltaTime * replayPlaybackSpeed;
                    }
                    else
                    {
                        currentTime = totalTime;
                        pauseReplay = true;
                        rewindButton.gameObject.SetActive(true);
                        playButton.gameObject.SetActive(false);
                        pauseButton.gameObject.SetActive(false);
                    }

                    replaySlider.value = currentTime;
                    SetupFrame();
                }
                else
                {
                    // Pause animators when replay is paused
                    for (int i = 0; i < trackedAnimatorList.Count; i++)
                    {
                        if (trackedAnimatorList[i] != null)
                        {
                            trackedAnimatorList[i].speed = 0f;
                        }
                    }
                }
            }
        }
    }

    #region Replay Viewer UI

    public void PausePlayButton()
    {
        //Debug.Log("PausePlayButton");
        pauseReplay = !pauseReplay;
        pauseButtonStatus = pauseReplay;
        PausePlayButtonVisualToggle();
    }

    public void PausePlayButtonVisualToggle()
    {
        if (pauseReplay)
        {
            pauseButton.gameObject.SetActive(false);
            playButton.gameObject.SetActive(true);
        }
        else
        {
            pauseButton.gameObject.SetActive(true);
            playButton.gameObject.SetActive(false);
        }
    }

    public void RewindButton()
    {
        currentTime = 0f;
        currentFrame = 0;
        pauseReplay = false;
        pauseButton.gameObject.SetActive(true);
        playButton.gameObject.SetActive(false);
        rewindButton.gameObject.SetActive(false);
    }

    public void ScrubSlider()
    {
        currentTime = replaySlider.value;
    }

    public void SpeedDropdownCallback()
    {
        float newSpeed;
        if (float.TryParse(speedDropdown.options[speedDropdown.value].text, out newSpeed))
        {
            replayPlaybackSpeed = newSpeed;
        }
    }

    public void BackButton()
    {
        // Toggle the UI
        MenuScreenScript.instance.ReplayQuitCallback();
        GameStateHandler.instance.ReplayQuitCallback();

        ResetReplayPlaybackVariables();
        viewingReplay = false;
        loadedReplay = false;
    }

    public void ResetReplayPlaybackVariables()
    {
        currentTime = 0f;
        currentFrame = 0;

        pauseReplay = true;
        firstFrame = true;

        frameElapsedRealTime.Clear();

        pauseButton.gameObject.SetActive(false);
        playButton.gameObject.SetActive(true);
        rewindButton.gameObject.SetActive(false);
        speedDropdown.SetValueWithoutNotify(1); // Set speed to the 1.0 value
    }

    string WriteTimeStamp(float seconds)
    {
        TimeSpan timeSpan = TimeSpan.FromSeconds(seconds);
        string timestamp = $"{timeSpan.Hours:D2}:{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}";
        return timestamp;
    }

    #endregion

    #region Replay IO

    public IEnumerator ReadReplay(string filepath)
    {
        replayPath = filepath;
        loadingReplayText.text = "Loading Replay...";

        // Disable controls while replay is loading
        rewindButton.gameObject.SetActive(false);
        playButton.gameObject.SetActive(false);
        pauseButton.gameObject.SetActive(false);

        // Do the loading of replay in another thread
        loadReplayThread = new Thread(() => LoadReplayThreadFunction());
        loadReplayThread.Start();

        while (!loadedReplay)
        {
            yield return null;
        }

        loadingReplayText.text = "";

        // Enable controls when replay is loaded
        playButton.gameObject.SetActive(true);

        // Setup replay
        replayLengthTimestamp = WriteTimeStamp(totalTime);
        timestampText.text = replayLengthTimestamp;
        replaySlider.maxValue = totalTime;

        viewingReplay = true;
    }

    void LoadReplayThreadFunction()
    {
        LoadReplay();
        totalTime = replayLoadedData.totalTime;
        loadedReplay = true;
    }

    void LoadReplay()
    {
        if (File.Exists(replayPath))
        {
            byte[] bytes = File.ReadAllBytes(replayPath);
            using (var msi = new MemoryStream(bytes))
            using (var mso = new MemoryStream())
            {
                using (var gs = new GZipStream(msi, CompressionMode.Decompress))
                {
                    CopyTo(gs, mso);
                }
                string jsonData = Encoding.UTF8.GetString(mso.ToArray());
                //Debug.Log("Json length: " + jsonData.Length);
                replayLoadedData = JsonUtility.FromJson<ReplayFrameData>(jsonData);
                for (int i = 0; i < replayLoadedData.objectDataAllFrames.Count; i++)
                {
                    frameElapsedRealTime.Add(replayLoadedData.objectDataAllFrames[i].time);
                }
                //Debug.Log("Loaded replay");
            }
        }
    }

    void CopyTo(Stream src, Stream dest)
    {
        byte[] bytes = new byte[4096];

        int cnt;

        while ((cnt = src.Read(bytes, 0, bytes.Length)) != 0)
        {
            dest.Write(bytes, 0, cnt);
        }
    }

    #endregion

    #region Replay Playback

    void SetupFrame()
    {
        UpdateTimestamp();
        ApplyTransformData();
        ApplyAnimationData();
    }

    void UpdateTimestamp()
    {
        timestampText.text = WriteTimeStamp(currentTime) + " / " + replayLengthTimestamp;
    }

    void ApplyTransformData()
    {
        for (int i = 0; i < trackedTransformList.Count; i++)
        {
            if (trackedTransformList[i] != null && trackedTransformList[i].gameObject.activeInHierarchy)
            {
                trackedTransformList[i].position = replayLoadedData.objectDataAllFrames[currentFrame].pos;
                trackedTransformList[i].rotation = replayLoadedData.objectDataAllFrames[currentFrame].rot;
            }
        }
    }

    void ApplyAnimationData()
    {
        for (int i = 0; i < trackedAnimatorList.Count; i++)
        {
            int stateHash = replayLoadedData.objectDataAllFrames[currentFrame].animHash;
            float time = replayLoadedData.objectDataAllFrames[currentFrame].animTime;
            if (trackedAnimatorList[i] != null && trackedAnimatorList[i].gameObject.activeInHierarchy)
            {
                trackedAnimatorList[i].speed = replayLoadedData.objectDataAllFrames[currentFrame].animSpeed;
                for (int j = 0; j < replayLoadedData.objectDataAllFrames[currentFrame].dataList.Count; j++)
                {
                    AnimatorControllerParameterType type = replayLoadedData.objectDataAllFrames[currentFrame].dataList[j].type;
                    int nameHash = replayLoadedData.objectDataAllFrames[currentFrame].dataList[j].paramHash;
                    switch (type)
                    {
                        case AnimatorControllerParameterType.Float:
                            trackedAnimatorList[i].SetFloat(nameHash, replayLoadedData.objectDataAllFrames[currentFrame].dataList[j].valFloat);
                            break;
                        case AnimatorControllerParameterType.Bool:
                            trackedAnimatorList[i].SetBool(nameHash, replayLoadedData.objectDataAllFrames[currentFrame].dataList[j].valBool);
                            break;
                        case AnimatorControllerParameterType.Int:
                            trackedAnimatorList[i].SetInteger(nameHash, replayLoadedData.objectDataAllFrames[currentFrame].dataList[j].valInt);
                            break;
                    }
                }
                trackedAnimatorList[i].Play(stateHash, 0, time);
            }
        }
    }

    #endregion
}
