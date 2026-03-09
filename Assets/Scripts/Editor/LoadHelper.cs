using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace Project.Editor.Helpers
{
    public static class LoadHelper
    {
        public static readonly string ProjectScenesFolder = $"Assets/Scenes";
        public static readonly string StarterAssetsScenesFolder = $"Assets/StarterAssets/ThirdPersonController/Scenes";

        public static readonly string Standby = $"{ProjectScenesFolder}/Standby.unity";
        public static readonly string Menu = $"{ProjectScenesFolder}/Menu.unity";
        public static readonly string Playground = $"{StarterAssetsScenesFolder}/Playground.unity";

        public static readonly IList<string> allScenes = new List<string>() {
            Standby,
            Menu,
            Playground
        };

        [MenuItem("Project/Load Scene/Standby", priority = 1)]
        public static void LoadStandbyScene() => LoadAndPlayScenes(
            scenes: new Queue<string>(new[] { Standby }),
            isPlayImmediately: false,
            beforeLoad: null,
            afterLoad: null,
            afterPlay: null);

        [MenuItem("Project/Load Scene/Standby (Play Immediately)", priority = 1)]
        public static void LoadAndPlayStandbyScene() => LoadAndPlayScenes(
            scenes: new Queue<string>(new[] { Standby }),
            isPlayImmediately: true,
            beforeLoad: null,
            afterLoad: null,
            afterPlay: null);

        [MenuItem("Project/Load Scene/Menu", priority = 12)]
        public static void LoadEnvironmentAndOnlineSceneScene() => LoadAndPlayScenes(
            scenes: new Queue<string>(new[] { Menu }),
            isPlayImmediately: false,
            beforeLoad: null,
            afterLoad: null,
            afterPlay: null);

        [MenuItem("Project/Load Scene/Playground", priority = 12)]
        public static void LoadOnlineSceneScene() => LoadAndPlayScenes(
            scenes: new Queue<string>(new[] { Playground }),
            isPlayImmediately: false,
            beforeLoad: null,
            afterLoad: null,
            afterPlay: null);

        public static void LoadAndPlayScenes(Queue<string> scenes, bool isPlayImmediately, string activeScene = null, Action beforeLoad = null, Action afterLoad = null, Action afterPlay = null)
        {
            bool proceed = true;
            if (SceneManager.sceneCount > 0)
            {
                for (int i = 0; i < SceneManager.sceneCount; ++i)
                {
                    Scene scene = SceneManager.GetSceneAt(i);
                    if (scene.isDirty)
                    {
                        proceed = EditorUtility.DisplayDialog("Unsaved scene detected",
                "There are unsaved changes to one or more of the scenes open, discard changes and proceed?", "OK", "Cancel");
                        break;
                    }
                }
            }

            if (!proceed)
            {
                return;
            }

            beforeLoad?.Invoke();

            EditorSceneManager.OpenScene(scenes.Dequeue());

            while (scenes.Count != 0)
            {
                EditorSceneManager.OpenScene(scenes.Dequeue(), OpenSceneMode.Additive);
            }

            if (!string.IsNullOrEmpty(activeScene))
            {
                string activeSceneName = System.IO.Path.GetFileNameWithoutExtension(activeScene);
                Scene scene =  EditorSceneManager.GetSceneByName(activeSceneName);
                EditorSceneManager.SetActiveScene(scene);
            }

            afterLoad?.Invoke();

            if (isPlayImmediately)
            {
                EditorApplication.isPlaying = isPlayImmediately;

                afterPlay?.Invoke();
            }
        }
    }
}