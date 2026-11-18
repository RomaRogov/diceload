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
