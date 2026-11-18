using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace Diceload
{
    /// <summary>
    /// A test run in the open Editor that a shell can wait for without reading JSON: <see cref="Start"/>
    /// gets the Editor ready, checks the filter and starts the run; the outcome is written as plain text to
    /// <see cref="ReportPath"/>, its first line "&lt;run&gt; running" or "&lt;run&gt; done". <c>unity-test</c>
    /// starts a run and waits on that file — the same script in macOS, Linux and Windows (Git Bash).
    /// </summary>
    /// <remarks>
    /// A PlayMode run reloads the domain, so the reporter is registered again on every load and the run's
    /// id lives in the file itself.
    /// </remarks>
    [InitializeOnLoad]
    public static class TestRun
    {
        public const string ReportPath = "Library/Diceload/test-run.txt";

        static TestRun() => TestRunnerApi.RegisterTestCallback(new Reporter());

        /// <summary>
        /// Start running the tests whose full name matches <paramref name="filter"/> (a regex, else a
        /// substring) in <paramref name="mode"/> ("playmode" or "editmode"). Returns the run's id. Unsaved work is
        /// saved first (<see cref="UnsavedWork"/>); when it can't be, or no test matches, the run is
        /// done at once and the report says why.
        /// </summary>
        public static string Start(string filter, string mode)
        {
            var id = "run-" + DateTime.UtcNow.ToString("yyyyMMddHHmmssfff");
            var playMode = !string.Equals(mode, "editmode", StringComparison.OrdinalIgnoreCase);

            var ready = UnsavedWork.Save();
            if (!ready.StartsWith("ready")) return Done(id, "not ready", ready);

            if (!TestFilter.Matching(filter).Any())
                return Done(id, "no test", TestFilter.WhyNothingRan(filter, playMode ? "playmode" : "editmode"));

            Write(id, "running", $"started {DateTime.Now:HH:mm:ss}: {filter} ({(playMode ? "playmode" : "editmode")})");
            var pattern = IsRegex(filter) ? filter : Regex.Escape(filter);
            ScriptableObject.CreateInstance<TestRunnerApi>().Execute(new ExecutionSettings(new Filter
            {
                testMode = playMode ? TestMode.PlayMode : TestMode.EditMode,
                groupNames = new[] { pattern },
            }));
            return id;
        }

        static string Done(string id, string outcome, string text)
        {
            Write(id, "done", outcome + ": " + text);
            return id;
        }

        static bool IsRegex(string filter)
        {
            try { _ = new Regex(filter); return true; }
            catch (ArgumentException) { return false; }
        }

        static string CurrentId() =>
            File.Exists(ReportPath) && File.ReadLines(ReportPath).FirstOrDefault() is { } first ? first.Split(' ')[0] : "run-unknown";

        static void Write(string id, string state, string body)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ReportPath));
            File.WriteAllText(ReportPath + ".tmp", $"{id} {state}\n{body}\n");
            if (File.Exists(ReportPath)) File.Delete(ReportPath);
            File.Move(ReportPath + ".tmp", ReportPath);      // a reader never sees half a report
        }

        class Reporter : ICallbacks
        {
            public void RunStarted(ITestAdaptor tests) { }
            public void TestStarted(ITestAdaptor test) { }
            public void TestFinished(ITestResultAdaptor result) { }

            public void RunFinished(ITestResultAdaptor run)
            {
                var text = new StringBuilder();
                var leaves = Leaves(run).ToList();
                text.AppendLine($"{(run.FailCount > 0 ? "failed" : "passed")} in {run.Duration:F1} s: " +
                                $"{leaves.Count} tests, {run.PassCount} passed, {run.FailCount} failed, {run.SkipCount} skipped, {run.InconclusiveCount} inconclusive");
                foreach (var t in leaves)
                {
                    if (t.TestStatus == TestStatus.Failed)
                        text.AppendLine($"FAILED {t.FullName}\n  {(t.Message ?? "").Trim().Replace("\n", "\n  ")}");
                    var folder = Path.Combine("SimulationResults", Safe(t.Test.TypeInfo?.FullName ?? ""), Safe(t.Name));
                    if (Directory.Exists(folder))
                        text.AppendLine($"  results: {folder.Replace('\\', '/')} -> {string.Join(", ", Directory.GetFiles(folder).Select(Path.GetFileName).OrderBy(n => n))}");
                }
                Write(CurrentId(), "done", text.ToString().TrimEnd());
            }

            static IEnumerable<ITestResultAdaptor> Leaves(ITestResultAdaptor r) =>
                r.HasChildren ? r.Children.SelectMany(Leaves) : new[] { r };

            // As ResultsFolder names a test's folder.
            static string Safe(string text)
            {
                foreach (var c in Path.GetInvalidFileNameChars()) text = text.Replace(c, '_');
                return text.Replace('(', '_').Replace(')', '_').Replace(',', '_').Replace('"', '_');
            }
        }
    }
}
