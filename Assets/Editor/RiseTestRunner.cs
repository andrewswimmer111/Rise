using System.IO;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace Rise.Editor
{
    [InitializeOnLoad]
    public sealed class RiseTestRunner : ICallbacks
    {
        private static TestRunnerApi api;

        static RiseTestRunner()
        {
            api = ScriptableObject.CreateInstance<TestRunnerApi>();
            api.RegisterCallbacks(new RiseTestRunner());
        }

        [MenuItem("Rise/Run Prototype Tests")]
        public static void Run()
        {
            api.Execute(new ExecutionSettings(new Filter
            {
                testMode = TestMode.PlayMode,
                assemblyNames = new[] { "Rise.PlayModeTests" }
            }));
        }

        public void RunStarted(ITestAdaptor testsToRun) { }
        public void TestStarted(ITestAdaptor test) { }
        public void TestFinished(ITestResultAdaptor result) { }
        public void RunFinished(ITestResultAdaptor result)
        {
            Directory.CreateDirectory("Temp");
            File.WriteAllText("Temp/Rise-test-results.xml", result.ToXml().OuterXml);
            Debug.Log($"Rise tests: {result.PassCount} passed, {result.FailCount} failed. See Temp/Rise-test-results.xml.");
        }
    }
}
