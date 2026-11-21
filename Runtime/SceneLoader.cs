using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

namespace Diceload
{
    /// <summary>
    /// A replayable start: what an earlier test kept with <c>DontDestroyOnLoad</c> gone, the fixed clock
    /// aligned (<see cref="SimulationClock.AlignToFixedStep"/>), <c>UnityEngine.Random</c> seeded, the
    /// scene loaded fresh, and every object past <c>Start</c>.
    /// </summary>
    public static class SceneLoader
    {
        /// <param name="scenePath">Asset path, e.g. <c>Assets/Scenes/Level1.unity</c>. In the Editor it
        /// need not be in the build's scene list.</param>
        /// <param name="keepAcrossScenes">Keep what the game kept with <c>DontDestroyOnLoad</c>, as a level
        /// the game loads itself would.</param>
        public static IEnumerator Load(string scenePath, int seed, bool keepAcrossScenes = false)
        {
            if (!keepAcrossScenes)
            {
                foreach (var kept in Kept()) Object.Destroy(kept);
                yield return null;                // destroyed at the end of that frame: gone before the load
            }
            yield return SimulationClock.AlignToFixedStep();
            Random.InitState(seed);
#if UNITY_EDITOR
            EditorSceneManager.LoadSceneInPlayMode(scenePath, new LoadSceneParameters(LoadSceneMode.Single));
#else
            SceneManager.LoadScene(scenePath, LoadSceneMode.Single);
#endif
            for (var i = 0; !SceneManager.GetSceneByPath(scenePath).isLoaded; i++)
            {
                if (i > 600) Assert.Fail($"Load: {scenePath} did not load in 600 frames.");
                yield return null;
            }
            yield return null;                    // one more frame: every object has run Start
        }

        /// <summary>
        /// Stop the game where it stands: every root object of every loaded scene, and of those kept across
        /// scenes, made inactive — its <c>OnDisable</c> runs now, while the test's input and log still do,
        /// and no frame after the test runs its <c>Update</c>.
        /// </summary>
        public static void Stop()
        {
            var roots = new List<GameObject>(Kept());
            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene.isLoaded) roots.AddRange(scene.GetRootGameObjects());
            }
            foreach (var root in roots)
                if (root != null && !IsRunner(root)) root.SetActive(false);
        }

        /// <summary>The root objects kept with <c>DontDestroyOnLoad</c>, but the test runner's own.</summary>
        static IEnumerable<GameObject> Kept()
        {
            // The scene they are in can only be reached through an object in it.
            var probe = new GameObject("Diceload probe");
            Object.DontDestroyOnLoad(probe);
            var roots = probe.scene.GetRootGameObjects();
            Object.DestroyImmediate(probe);
            foreach (var root in roots)
                if (root != null && root != probe && !IsRunner(root)) yield return root;
        }

        /// <summary>An object the Test Framework runs the tests from: never the game's.</summary>
        static bool IsRunner(GameObject root)
        {
            foreach (var c in root.GetComponents<Component>())
                if (c != null && c.GetType().Assembly.GetName().Name.Contains("TestRunner")) return true;
            return false;
        }
    }
}
