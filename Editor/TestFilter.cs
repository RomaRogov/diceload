using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor.Compilation;

namespace Diceload
{
    /// <summary>
    /// Why a test run found nothing, from the test methods loaded now: Unity matches the filter against a
    /// test's full name (Namespace.Fixture.Method), PlayMode doesn't load an Editor-only assembly, and a
    /// simulation (<c>[UnityTest]</c>) runs in playmode. <c>unity-test</c> asks it when no test matches.
    /// </summary>
    public static class TestFilter
    {
        readonly struct Test
        {
            public readonly string Name, Assembly;
            public readonly bool IsUnityTest;
            public Test(string name, string assembly, bool isUnityTest) => (Name, Assembly, IsUnityTest) = (name, assembly, isUnityTest);
        }

        /// <param name="mode">"playmode" or "editmode".</param>
        public static string WhyNothingRan(string filter, string mode)
        {
            var loaded = LoadedTests();
            var matched = loaded.Where(t => Matches(t.Name, filter)).ToList();
            if (matched.Count == 0)
            {
                var fixtures = loaded.Select(t => t.Name.Substring(0, t.Name.LastIndexOf('.'))).Distinct().Take(12).ToList();
                return $"no test's full name (Namespace.Fixture.Method) matches \"{filter}\"" +
                       (fixtures.Count > 0 ? $"; the loaded fixtures are: {string.Join(", ", fixtures)}" : "; no test is loaded at all") +
                       ". A test that was just written may not have compiled: unity recompile shows the errors.";
            }
            if (mode == "playmode" && matched.Select(t => t.Assembly).Distinct().FirstOrDefault(EditorOnly) is { } editorOnly)
                return $"{matched.Count(t => t.Assembly == editorOnly)} test(s) match, in {editorOnly} ({Asmdef(editorOnly)}), which is Editor-only " +
                       "(\"includePlatforms\": [\"Editor\"]): PlayMode doesn't load it. For simulations, remove includePlatforms from it, " +
                       "as in Diceload's README; EditMode tests run with editmode.";
            if (mode == "editmode" && matched.All(t => t.IsUnityTest))
                return $"{matched.Count} test(s) match, all [UnityTest]: a simulation runs in playmode (the default).";
            return $"{matched.Count} test(s) match ({string.Join(", ", matched.Take(5).Select(t => t.Name))}) but Unity ran none in {mode}: " +
                   "check unity recompile for errors, and the other mode.";
        }

        /// <summary>Every NUnit or UTF test method in the project's assemblies (not Unity's or the runtime's).</summary>
        static List<Test> LoadedTests()
        {
            var tests = new List<Test>();
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var name = assembly.GetName().Name;
                if (name.StartsWith("Unity") || name.StartsWith("System") || name.StartsWith("nunit") || name.StartsWith("mscorlib")) continue;
                Type[] types;
                try { types = assembly.GetTypes(); }
                catch { continue; }
                foreach (var type in types)
                foreach (var method in type.GetMethods())
                {
                    var attributes = method.GetCustomAttributes(true).Select(a => a.GetType().Name).ToList();
                    if (attributes.Any(a => a is "TestAttribute" or "UnityTestAttribute" or "TestCaseAttribute" or "TestCaseSourceAttribute"))
                        tests.Add(new Test($"{type.FullName}.{method.Name}", name, attributes.Contains("UnityTestAttribute")));
                }
            }
            return tests;
        }

        static bool Matches(string name, string filter)
        {
            try { return Regex.IsMatch(name, filter); }
            catch (ArgumentException) { return name.Contains(filter); }
        }

        static string Asmdef(string assembly) => CompilationPipeline.GetAssemblyDefinitionFilePathFromAssemblyName(assembly);

        static bool EditorOnly(string assembly) =>
            Asmdef(assembly) is { } path && File.Exists(path) &&
            Regex.IsMatch(File.ReadAllText(path), "\"includePlatforms\"\\s*:\\s*\\[\\s*\"Editor\"\\s*\\]");
    }
}
