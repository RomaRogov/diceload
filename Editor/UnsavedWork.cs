using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace Diceload
{
    /// <summary>
    /// Getting the open Editor ready for a test run without a dialog. The Test Framework asks "save
    /// changes?" — a modal dialog that stops the Editor until a person answers — when a run starts with
    /// unsaved changes, and saving an untitled scene asks where to put it. This saves what can be saved
    /// without asking (scenes with a path, the prefab open in Prefab Mode) and reports what can't.
    /// </summary>
    public static class UnsavedWork
    {
        /// <summary>
        /// Save every open scene and prefab that has unsaved changes and a file. Returns "ready", or what
        /// would make Unity open a dialog. <c>unity-test</c> calls it before every run.
        /// </summary>
        public static string Save()
        {
            var saved = new List<string>();
            var problems = new List<string>();
            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (!scene.isDirty) continue;
                if (string.IsNullOrEmpty(scene.path))
                    problems.Add($"an untitled scene has unsaved changes: saving it would open a \"Save As\" dialog. Save it to a path with EditorSceneManager.SaveScene(scene, \"Assets/…/Name.unity\"), or discard it by opening a saved scene with EditorSceneManager.OpenScene(\"Assets/…/Name.unity\") (the API doesn't ask)");
                else if (EditorSceneManager.SaveScene(scene)) saved.Add(scene.path);
                else problems.Add($"{scene.path} couldn't be saved");
            }
            var stage = PrefabStageUtility.GetCurrentPrefabStage();
            if (stage != null && stage.scene.isDirty)
            {
                PrefabUtility.SaveAsPrefabAsset(stage.prefabContentsRoot, stage.assetPath);
                stage.ClearDirtiness();
                saved.Add(stage.assetPath);
            }
            if (problems.Count > 0) return "not ready: " + string.Join("; ", problems);
            return saved.Count == 0 ? "ready" : $"ready (saved {string.Join(", ", saved)})";
        }
    }
}
