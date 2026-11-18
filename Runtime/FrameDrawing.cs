using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Diceload
{
    /// <summary>
    /// Whether frames are drawn. A simulation doesn't draw them — Unity's <c>OnDemandRendering</c> skips
    /// the render, cameras stay enabled (so <c>Camera.main</c> works) — which is 2–6× faster. A
    /// screenshot draws on its own; a pause draws the frame it stops on. To watch a run, set
    /// <c>OnDemandRendering.renderFrameInterval</c> in the test: 1 draws every frame, 60 is a cheap preview.
    /// </summary>
    public static class FrameDrawing
    {
        const int Never = 1_000_000;

        public static void Stop() => OnDemandRendering.renderFrameInterval = Never;

        public static void Resume() => OnDemandRendering.renderFrameInterval = 1;

        /// <summary>
        /// Bring the Game view forward, draw the next frame, pause the Editor on it, and go on undrawn once a
        /// person resumes. It plays that one frame, so a replay with a pause stays a replay. Nothing in batch
        /// mode, where nobody resumes.
        /// </summary>
        public static IEnumerator PauseOnDrawnFrame()
        {
            if (Application.isBatchMode) yield break;
#if UNITY_EDITOR
            // Game cameras draw through the Game view: behind another tab, the frame isn't drawn or seen.
            EditorApplication.ExecuteMenuItem("Window/General/Game");
#endif
            var interval = OnDemandRendering.renderFrameInterval;
            OnDemandRendering.renderFrameInterval = 1;    // whether a frame is drawn is settled as it starts
            yield return null;
#if UNITY_EDITOR
            void Resumed(PauseState state)
            {
                if (state != PauseState.Unpaused) return;
                EditorApplication.pauseStateChanged -= Resumed;
                OnDemandRendering.renderFrameInterval = interval;     // undrawn from the first frame after it
            }
            EditorApplication.pauseStateChanged += Resumed;
#endif
            Debug.Break();                                // pause after this frame, which is drawn
            yield return null;                            // here again once the Editor runs again
            OnDemandRendering.renderFrameInterval = interval;
        }
    }
}
