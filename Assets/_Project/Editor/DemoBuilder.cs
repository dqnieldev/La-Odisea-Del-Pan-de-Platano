using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Odisea.EditorTools
{
    /// <summary>
    /// Genera un .exe de Windows solo con la escena de prueba del jugador, para enseñar
    /// las mecánicas sin abrir Unity. No toca la lista de escenas de Build Settings.
    /// La carpeta Build/ está en .gitignore.
    /// </summary>
    public static class DemoBuilder
    {
        private const string ScenePath = "Assets/_Project/Scenes/Sandbox/PlayerSandbox.unity";
        private const string OutputPath = "Build/DemoJugador/LaOdiseaDelPanDePlatano.exe";

        [MenuItem("Odisea/Build/Demo del jugador (Windows)")]
        public static void BuildPlayerDemo()
        {
            var options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = OutputPath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None,
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;
            if (summary.result != BuildResult.Succeeded)
                throw new System.InvalidOperationException($"La build falló: {summary.result}, {summary.totalErrors} errores.");

            string fullPath = Path.GetFullPath(OutputPath);
            Debug.Log($"[Odisea] Demo generada en {fullPath} ({summary.totalSize / (1024 * 1024)} MB, {summary.totalTime.TotalSeconds:F0} s)");
            EditorUtility.RevealInFinder(fullPath);
        }
    }
}
