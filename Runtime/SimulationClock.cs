using System.Collections;
using UnityEngine;

namespace Diceload
{
    /// <summary>
    /// Game time while a simulation runs: <c>Time.captureDeltaTime</c> held at 1/60 s, so every
    /// <c>yield return null</c> is exactly one frame of game time — <c>WaitForSeconds</c> and
    /// <c>WaitTimeoutMode.InGameTime</c> count it — and frames run as fast as the machine allows.
    /// </summary>
    public static class SimulationClock
    {
        /// <summary>One frame of game time.</summary>
        public const float FrameSeconds = 1f / 60f;

        public static void Hold()
        {
            Time.captureDeltaTime = FrameSeconds;
            Application.runInBackground = true;
        }

        public static void Release() => Time.captureDeltaTime = 0;

        /// <summary>
        /// Run frames paced at one per <see cref="FrameSeconds"/> of real time for <paramref name="seconds"/>,
        /// so game time and unscaled time move together. Unity can't hold <c>Time.unscaledDeltaTime</c>:
        /// it is real time, and while frames run fast a fade on unscaled time, <c>WaitForSecondsRealtime</c>
        /// or a UI Toolkit transition barely moves.
        /// </summary>
        public static IEnumerator PlayInRealTime(float seconds)
        {
            var rate = Application.targetFrameRate;
            var vSync = QualitySettings.vSyncCount;
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = Mathf.RoundToInt(1 / FrameSeconds);
            try
            {
                for (var end = Time.realtimeSinceStartupAsDouble + seconds; Time.realtimeSinceStartupAsDouble < end;) yield return null;
            }
            finally
            {
                Application.targetFrameRate = rate;
                QualitySettings.vSyncCount = vSync;
            }
        }

        /// <summary>
        /// Stretch one frame so it ends just past a fixed step. Unity keeps the fixed-update clock running
        /// across scene loads; from here every load starts on the same phase of it, so FixedUpdate counts
        /// and interpolated positions repeat exactly.
        /// </summary>
        public static IEnumerator AlignToFixedStep()
        {
            var frame = Time.captureDeltaTime;
            var toNextStep = Time.fixedTimeAsDouble + Time.fixedDeltaTime - Time.timeAsDouble;
            Time.captureDeltaTime = (float)toNextStep + 1e-5f;
            yield return null;
            Time.captureDeltaTime = frame;
        }
    }
}
