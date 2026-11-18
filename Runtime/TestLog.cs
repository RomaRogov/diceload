using System.Text;
using UnityEngine;

namespace Diceload
{
    /// <summary>
    /// Every line logged while a test runs — its own <c>Debug.Log</c> readings and the game's — stamped with
    /// the frame, with the stack of an error or exception.
    /// </summary>
    public sealed class TestLog
    {
        readonly StringBuilder text = new();

        public string Text => text.ToString();

        public void Start()
        {
            text.Clear();
            Application.logMessageReceived += Record;
        }

        public void Stop() => Application.logMessageReceived -= Record;

        void Record(string message, string stackTrace, LogType type)
        {
            text.Append("[frame ").Append(Time.frameCount).Append(' ').Append(type).Append("] ").AppendLine(message);
            if (type is LogType.Exception or LogType.Error) text.AppendLine(stackTrace);
        }
    }
}
