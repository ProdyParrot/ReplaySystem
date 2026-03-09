using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using UnityEngine;
using static PastSessionEntryScript;

public class ReplayWriter : MonoBehaviour
{
    [Serializable]
    public class AnimatorData
    {
        public AnimatorControllerParameterType type;
        public int paramHash;
        public float valFloat;
        public int valInt;
        public bool valBool;
    }

    [Serializable]
    public class ObjectData
    {
        public Vector3 pos;
        public Quaternion rot;
        public float time;

        // Animation
        public int animHash;
        public float animTime;
        public float animSpeed;
        public List<AnimatorData> dataList = new List<AnimatorData>(); // One elem for each parameter
    }

    [Serializable]
    public class ReplayFrameData
    {
        public float totalTime;
        public List<ObjectData> objectDataAllFrames = new List<ObjectData>();
    }

    public static ReplayWriter instance;

    ReplayFrameData currentReplayData;

    // Session
    public List<GameObject> trackedObjectList;
    [HideInInspector] public List<Transform> trackedTransformList = new List<Transform>();
    [HideInInspector] public List<Animator> trackedAnimatorList = new List<Animator>();

    // Viewing replay
    public List<GameObject> trackedObjectReplayList;
    [HideInInspector] public List<Transform> trackedTransformReplayList = new List<Transform>();
    [HideInInspector] public List<Animator> trackedAnimatorReplayList = new List<Animator>();

    bool writingReplay;
    float elapsedTime;
    string currentFolderPath;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Debug.LogError("Can only have one replay playback handler!");
        }
    }

    private void OnDestroy()
    {
        if (writingReplay)
        {
            ScenarioStopCallback();
        }
    }

    void SetupTrackedLists()
    {
        // Gameplay lists
        trackedTransformList.Clear();
        trackedAnimatorList.Clear();
        for (int i = 0; i < trackedObjectList.Count; i++)
        {
            trackedTransformList.Add(trackedObjectList[i].transform);
        }
        for (int i = 0; i < trackedObjectList.Count; i++)
        {
            trackedAnimatorList.Add(trackedObjectList[i].GetComponent<Animator>());
        }
    }

    private void FixedUpdate()
    {
        if (writingReplay)
        {
            ObjectData frameData = new ObjectData();
            elapsedTime += Time.fixedDeltaTime;

            // Transform data
            for (int i = 0; i < trackedTransformList.Count; i++)
            {
                frameData.pos = trackedTransformList[i].position;
                frameData.rot = trackedTransformList[i].rotation;
            }

            // Animator data
            for (int i = 0; i < trackedAnimatorList.Count; i++)
            {
                foreach (AnimatorControllerParameter elem in trackedAnimatorList[i].parameters)
                {
                    AnimatorData animatorFrameData = new AnimatorData();
                    switch (elem.type)
                    {
                        case AnimatorControllerParameterType.Float:
                            float valFloat = trackedAnimatorList[i].GetFloat(elem.nameHash);
                            animatorFrameData.valFloat = valFloat;
                            break;
                        case AnimatorControllerParameterType.Int:
                            int valInt = trackedAnimatorList[i].GetInteger(elem.nameHash);
                            animatorFrameData.valInt = valInt;
                            break;
                        case AnimatorControllerParameterType.Bool:
                            bool valBool = trackedAnimatorList[i].GetBool(elem.nameHash);
                            animatorFrameData.valBool = valBool;
                            break;
                    }
                    animatorFrameData.type = elem.type;
                    animatorFrameData.paramHash = elem.nameHash;
                    frameData.dataList.Add(animatorFrameData);
                }
                AnimatorStateInfo state = trackedAnimatorList[i].GetCurrentAnimatorStateInfo(0);
                frameData.animHash = state.fullPathHash;
                frameData.animTime = state.normalizedTime;
                frameData.animSpeed = state.speed;
            }

            // Also track elapsed time
            frameData.time = elapsedTime;

            // Add the frame data to the list
            currentReplayData.objectDataAllFrames.Add(frameData);
        }
    }

    public void ScenarioStartCallback()
    {
        currentReplayData = new ReplayFrameData();
        writingReplay = true;
        CreateDateTimeFolder();
        SetupTrackedLists();
        elapsedTime = 0f;
    }

    public void ScenarioStopCallback()
    {
        GenerateMetadataJSON();
        currentReplayData.totalTime = elapsedTime;
        writingReplay = false;
        SaveReplay();
    }

    #region Save/Load

    public void GenerateMetadataJSON()
    {
        PastSessionEntryMetadata currentMetadata = new PastSessionEntryMetadata();

        // Write the date to metadata
        currentMetadata.date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        currentMetadata.sessionDuration = elapsedTime;

        // Convert to JSON string
        string json = JsonUtility.ToJson(currentMetadata, true);

        // Save to file in the folder we created
        string path = Path.Combine(currentFolderPath, "metadata.json");
        File.WriteAllText(path, json);
    }

    string CreateDateTimeFolder()
    {
        // Get the folder where the exe is running
        string exePath = Application.dataPath;
        string rootFolder = Directory.GetParent(exePath).FullName;

        // Create folder name
        string folderName = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        string sessionFolder = Path.Combine("Sessions", folderName);
        currentFolderPath = Path.Combine(rootFolder, sessionFolder);

        // Create the directory if it doesn't exist
        if (!Directory.Exists(currentFolderPath))
        {
            Directory.CreateDirectory(currentFolderPath);
        }

        return currentFolderPath;
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

    void SaveReplay()
    {
        string replayJsonData = JsonUtility.ToJson(currentReplayData);
        string replayFilename = Path.Combine(currentFolderPath, "replay");
        var bytes = Encoding.UTF8.GetBytes(replayJsonData);
        using (var msi = new MemoryStream(bytes))
        using (var mso = new MemoryStream())
        {
            using (var gs = new GZipStream(mso, CompressionMode.Compress))
            {
                CopyTo(msi, gs);
            }
            File.WriteAllBytes(replayFilename, mso.ToArray());
        }
    }

    #endregion
}
