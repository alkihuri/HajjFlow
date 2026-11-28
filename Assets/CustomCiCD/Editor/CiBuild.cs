using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace CustomCiCD
{
    /// <summary>Batch-mode WebGL build entry point. Usage: -executeMethod CustomCiCD.CiBuild.BuildWebGL [-buildPath path]</summary>
    public static class CiBuild
    {
        private const string DefaultBuildPath = "build1";

        public static void BuildWebGL()
        {
            var args = Environment.GetCommandLineArgs();
            int i = Array.IndexOf(args, "-buildPath");
            string path = i >= 0 && i + 1 < args.Length ? args[i + 1] : DefaultBuildPath;

            var options = new BuildPlayerOptions
            {
                scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
                locationPathName = path,
                target = BuildTarget.WebGL,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            bool ok = report.summary.result == BuildResult.Succeeded;
            Debug.Log($"[CiBuild] Result: {report.summary.result}");
            EditorApplication.Exit(ok ? 0 : 1);
        }
    }
}

