using System.Collections;
using NUnit.Framework;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

namespace Diceload
{
    /// <summary>
    /// A replayable start: the fixed clock aligned (<see cref="SimulationClock.AlignToFixedStep"/>),
    /// <c>UnityEngine.Random</c> seeded, the scene loaded fresh, and every object past <c>Start</c>.
    /// </summary>
    public static class SceneLoader
    {
        /// <param name="scenePath">Asset path, e.g. <c>Assets/Scenes/Level1.unity</c>. In the Editor it
        /// need not be in the build's scene list.</param>
        public static IEnumerator Load(string scenePath, int seed)
        {
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
    }
}
