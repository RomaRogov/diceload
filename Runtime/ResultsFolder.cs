using System.IO;
using NUnit.Framework;

namespace Diceload
{
    /// <summary>
    /// The running test's folder, <c>SimulationResults/&lt;Fixture&gt;/&lt;Test&gt;/</c> in the project: its
    /// screenshots, and <c>result.txt</c> — the outcome, the failure message and the test's log. A runner
    /// reports pass or fail; this keeps what the test saw next to its images.
    /// </summary>
    public static class ResultsFolder
    {
        public static string Current
        {
            get
            {
                var test = TestContext.CurrentContext.Test;
                return Path.Combine(Path.GetFullPath("SimulationResults"), Safe(test.ClassName), Safe(test.Name));
            }
        }

        /// <summary>A file in the folder, its name made safe for the file system.</summary>
        public static string File(string name) => Path.Combine(Current, Safe(name));

        /// <summary>Start the test with an empty folder.</summary>
        public static void Empty()
        {
            if (Directory.Exists(Current)) Directory.Delete(Current, true);
            Directory.CreateDirectory(Current);
        }

        public static void WriteResult(string log)
        {
            var outcome = TestContext.CurrentContext.Result;
            // What the test itself threw has no other record: its stack says the line. An assertion's message is enough.
            var stack = outcome.Outcome.Status == NUnit.Framework.Interfaces.TestStatus.Failed && outcome.Outcome.Label == "Error" ? $"{outcome.StackTrace}\n" : "";
            System.IO.File.WriteAllText(File("result.txt"), $"{outcome.Outcome}\n{outcome.Message}\n{stack}\n{log}");
        }

        static string Safe(string text)
        {
            foreach (var c in Path.GetInvalidFileNameChars()) text = text.Replace(c, '_');
            return text.Replace('(', '_').Replace(')', '_').Replace(',', '_').Replace('"', '_');
        }
    }
}
