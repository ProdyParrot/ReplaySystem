using System;
using System.IO;
using TMPro;
using UnityEngine;

public class PastSessionEntryScript : MonoBehaviour
{
    [Serializable]
    public class PastSessionEntryMetadata
    {
        public string date;
        public float sessionDuration;

        public PastSessionEntryMetadata()
        {
            
        }

        public PastSessionEntryMetadata(PastSessionEntryMetadata rhs)
        {
            date = rhs.date;
            sessionDuration = rhs.sessionDuration;
        }
    }

    [HideInInspector] public PastSessionEntryMetadata entryMetadata;
    [SerializeField] private TMP_Text dateText;
    [SerializeField] private TMP_Text sessionDurationText;
    [HideInInspector] public string replayFilepath;
    [HideInInspector] public string jsonFilepath;

    private void Update()
    {
        if (entryMetadata != null)
        {
            dateText.text = entryMetadata.date;
            TimeSpan t = TimeSpan.FromSeconds(entryMetadata.sessionDuration);
            string timestamp = $"{t.Hours:D2}:{t.Minutes:D2}:{t.Seconds:D2}";
            sessionDurationText.text = timestamp;
        }
    }

    public void UpdateJSONMetadata()
    {
        PastSessionEntryMetadata newMetadata = new PastSessionEntryMetadata(entryMetadata);

        // Convert to JSON string
        string json = JsonUtility.ToJson(newMetadata, true);

        // Save to file in the folder we created
        File.WriteAllText(jsonFilepath, json);
    }

    public void ViewReplayButtonCallback()
    {
        ReplayViewerScript.instance.ViewReplayButtonCallback(this);
    }
}
