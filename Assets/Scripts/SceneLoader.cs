using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneLoader : MonoBehaviour
{
    [Header("Scene Names")]
    public List<string> startingScenes;

    [Header("UI Objects")]
    public GameObject loadScreen;
    public Slider progressBar;
    public TMP_Text sceneBeingLoadedText;

    private void Start()
    {
        Application.targetFrameRate = 60;
        StartCoroutine(LoadStartingScenes());
    }

    public void UnloadSceneIfLoaded(string sceneName)
    {
        Scene scene = SceneManager.GetSceneByName(sceneName);
        if (scene.isLoaded)
        {
            SceneManager.UnloadSceneAsync(sceneName);
        }
    }

    public string GetLoadText(string sceneName)
    {
        string returnText = "Loading...";
        switch (sceneName)
        {
            case "Menu":
                returnText = "Loading menu...";
                break;
            case "Playground":
                returnText = "Loading environment...";
                break;
        }
        return returnText;
    }

    IEnumerator LoadStartingScenes()
    {
        loadScreen.SetActive(true);
        for (int i = 0; i < startingScenes.Count; i++)
        {
            string sceneName = startingScenes[i];
            sceneBeingLoadedText.text = GetLoadText(sceneName);

            // Begin loading scene additively
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            asyncLoad.allowSceneActivation = true;

            // Update progress bar while loading
            while (!asyncLoad.isDone)
            {
                float totalProgress = (i + asyncLoad.progress) / startingScenes.Count;
                progressBar.value = Mathf.Clamp01(totalProgress);
                yield return null;
            }
            yield return null;
        }
        loadScreen.gameObject.SetActive(false);
    }
}
