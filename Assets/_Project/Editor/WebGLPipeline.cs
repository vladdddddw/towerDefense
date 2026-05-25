using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// One-button WebGL packaging into /docs (so GitHub Pages picks it up).
/// Can be invoked from the editor menu or via -executeMethod in batch mode.
/// </summary>
public static class WebGLPipeline
{
    [MenuItem("Vanguard/3 — Package WebGL → /docs")]
    public static void PackageForPages()
    {
        string output = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "docs");

        if (Directory.Exists(output))
        {
            try { Directory.Delete(output, true); } catch { /* ignore */ }
        }
        Directory.CreateDirectory(output);

        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene("Assets/Scenes/Title.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/Match.unity", true),
        };

        // WebGL platform settings (use the modern NamedBuildTarget overload)
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.WebGL, ScriptingImplementation.IL2CPP);
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
        PlayerSettings.WebGL.exceptionSupport  = WebGLExceptionSupport.None;
        PlayerSettings.WebGL.dataCaching       = true;
        PlayerSettings.WebGL.memorySize        = 256;
        PlayerSettings.runInBackground         = true;
        PlayerSettings.companyName             = "modertok";
        PlayerSettings.productName             = "Vanguard";
        PlayerSettings.SplashScreen.show       = false;

        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[]
            {
                "Assets/Scenes/Title.unity",
                "Assets/Scenes/Match.unity"
            },
            locationPathName = output,
            target           = BuildTarget.WebGL,
            options          = BuildOptions.None,
        });

        bool ok = report.summary.result == BuildResult.Succeeded;
        if (ok)
            Debug.Log($"[WebGLPipeline] OK → {output} ({report.summary.totalSize / 1024 / 1024} MB)");
        else
        {
            Debug.LogError($"[WebGLPipeline] failed: {report.summary.result}");
            if (Application.isBatchMode) EditorApplication.Exit(1);
        }
    }
}
