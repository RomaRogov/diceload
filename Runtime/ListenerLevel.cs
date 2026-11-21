using UnityEngine;

namespace Diceload
{
    /// <summary>
    /// How loud the game is: the RMS of what the <c>AudioListener</c> put out in its last block, 0 for
    /// silence. Sound runs on real time, not the held clock, so read it after
    /// <see cref="SimulationClock.PlayInRealTime"/> has let a sound start.
    /// </summary>
    public static class ListenerLevel
    {
        public static float Read(int samples = 1024)
        {
            var data = new float[samples];
            var channels = AudioSettings.speakerMode == AudioSpeakerMode.Mono ? 1 : 2;
            var sum = 0f;
            for (var c = 0; c < channels; c++)
            {
                AudioListener.GetOutputData(data, c);
                foreach (var v in data) sum += v * v;
            }
            return Mathf.Sqrt(sum / (samples * channels));
        }
    }
}
