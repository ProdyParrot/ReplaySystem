using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using static PastSessionEntryScript;

public class PastSessionsTableScript : MonoBehaviour
{
    public static PastSessionsTableScript instance;

    [SerializeField] private GameObject pastSessionEntryPrefab;
    [SerializeField] private GameObject pastSessionEntryHolder;
    [SerializeField] private ScrollRect pastSessionTableScrollbar;

    List<PastSessionEntryScript> pastSessionEntryRefList = new List<PastSessionEntryScript>();

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

    public void RefreshButton()
    {
        ClearEntryList();
        CreateEntryList();
        StartCoroutine(SetScrollLogToTop());
    }

    private IEnumerator SetScrollLogToTop()
    {
        // Wait 4 frames for layout to refresh
        yield return null;
        yield return null;
        yield return null;
        yield return null;
        pastSessionTableScrollbar.verticalNormalizedPosition = 1f;
    }

    public List<string> GetFolders(string parentPath)
    {
        var folders = new List<string>();

        if (!Directory.Exists(parentPath))
            return folders;

        var option = SearchOption.TopDirectoryOnly;

        foreach (var dir in Directory.GetDirectories(parentPath, "*", option)
            .OrderByDescending(d => Directory.GetCreationTime(d))
            .ToArray())
        {
            folders.Add(dir);
        }

        return folders;
    }

    void CreateEntryList()
    {
        // Path of the build folder (where the .exe is)
        string exeFolder = Application.dataPath;
        exeFolder = Directory.GetParent(Application.dataPath).FullName;
        string sessionsPath = Path.Combine(exeFolder, "Sessions");

        if (!Directory.Exists(sessionsPath))
        {
            Debug.LogWarning($"Sessions folder not found at: {sessionsPath}");
            Debug.Log($"Attempting to create sessions at: {sessionsPath}");
            try
            {
                Directory.CreateDirectory(sessionsPath);
                Debug.Log($"Created sessions folder at: {sessionsPath}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to create sessions folder at: {sessionsPath}\n" + e);
            }
        }

        List<string> folderList = GetFolders(sessionsPath);
        List<string> validFolders = new List<string>();
        for (int i = 0; i < folderList.Count; i++)
        {
            try
            {
                string[] jsonFiles = Directory.GetFiles(folderList[i], "*.json", SearchOption.TopDirectoryOnly);
                string[] replayFiles = Directory.GetFiles(folderList[i], "replay", SearchOption.TopDirectoryOnly);
                if (jsonFiles.Length > 0 && replayFiles.Length > 0)
                {
                    string json = File.ReadAllText(jsonFiles[0]);
                    PastSessionEntryMetadata metadata = JsonUtility.FromJson<PastSessionEntryMetadata>(json);
                    pastSessionEntryRefList.Add(CreateEntry(metadata, replayFiles[0], jsonFiles[0]));
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError("Folder " + folderList[i] + " has problems loading\n" + e);
            }
        }
        RefreshLayout(pastSessionEntryHolder.gameObject, 2);
    }

    PastSessionEntryScript CreateEntry(PastSessionEntryMetadata metadata, string replayPath, string jsonPath)
    {
        GameObject newEntry = Instantiate(pastSessionEntryPrefab, pastSessionEntryHolder.transform);
        PastSessionEntryScript entryScript = newEntry.GetComponent<PastSessionEntryScript>();
        entryScript.entryMetadata = metadata;
        entryScript.jsonFilepath = jsonPath;
        entryScript.replayFilepath = replayPath;
        return entryScript;
    }

    public void ClearEntryList()
    {
        foreach (PastSessionEntryScript elem in pastSessionEntryRefList)
        {
            if (elem != null)
                Destroy(elem.gameObject);
        }
        pastSessionEntryRefList.Clear();
    }

    public void RefreshLayout(GameObject panel, int reiterateCount)
    {
        StartCoroutine(ForceUpdateLayout(panel, reiterateCount));
    }
    internal IEnumerator ForceUpdateLayout(GameObject panel, int reiterateCount)
    {
        yield return null;
        var layoutgroup = panel.GetComponentInChildren<LayoutGroup>();
        for (int i = 0; i < reiterateCount; i++)
        {
            yield return null;
            if (layoutgroup != null)
            {
                layoutgroup.enabled = false;
                layoutgroup.enabled = true;
            }
        }
    }
}
