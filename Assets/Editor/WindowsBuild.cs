using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;

public static class WindowsBuild
{
    private const string OutputDirectory = "Builds/Windows";
    private const string ExecutableName = "CODE-7.exe";

    [MenuItem("Build/Build Windows x64")]
    public static void BuildWindowsFromMenu()
    {
        BuildWindowsPlayer();
    }

    // This method can be called from CLI:
    // Unity.exe -batchmode -quit -projectPath <path> -executeMethod WindowsBuild.BuildWindowsFromCli
    public static void BuildWindowsFromCli()
    {
        BuildWindowsPlayer();
    }

    private static void BuildWindowsPlayer()
    {
        var scenes = EditorBuildSettings.scenes
            .Where(scene => scene.enabled)
            .Select(scene => scene.path)
            .ToArray();

        if (scenes.Length == 0)
        {
            throw new InvalidOperationException("No enabled scenes found in Build Settings.");
        }

        var outputPath = Path.Combine(OutputDirectory, ExecutableName);
        Directory.CreateDirectory(OutputDirectory);

        var options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = outputPath,
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result != BuildResult.Succeeded)
        {
            throw new Exception($"Build failed: {report.summary.result}");
        }

        UnityEngine.Debug.Log($"Windows build generated at: {outputPath}");
    }
}
