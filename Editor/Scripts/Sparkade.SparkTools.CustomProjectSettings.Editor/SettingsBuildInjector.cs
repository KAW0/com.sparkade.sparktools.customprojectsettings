namespace Sparkade.SparkTools.CustomProjectSettings.Editor
{
    using System.IO;
    using UnityEditor;
    using UnityEditor.Build;
    using UnityEditor.Build.Reporting;
    using UnityEngine;

    /// <summary>
    /// Injects custom settings into builds.
    /// </summary>
    public class SettingsBuildInjector : IPreprocessBuildWithReport, IPostprocessBuildWithReport
    {
        /// <summary>
        /// Gets the relative callback order for callbacks. Callbacks with lower values are called before ones with higher values.
        /// </summary>
        public int callbackOrder => 0;

        /// <summary>
        /// Places all settings in the Resources folder, to be added to the build.
        /// </summary>
        /// <param name="report">This parameter is unused and should be ignored.</param>
        public void OnPreprocessBuild(BuildReport report = default)
        {
            Application.logMessageReceived += this.OnBuildError;

            if (!Directory.Exists(EditorSettingsManager.SettingsPath))
            {
                return;
            }

            string[] filePaths = Directory.GetFiles(EditorSettingsManager.SettingsPath);

            if (filePaths.Length == 0)
            {
                return;
            }

            if (!Directory.Exists(SettingsManager.SettingsPath))
            {
                Directory.CreateDirectory(SettingsManager.SettingsPath);
            }

            for (int i = 0; i < filePaths.Length; i += 1)
            {
                if (Path.GetExtension(filePaths[i]) == ".asset")
                {
                    ScriptableObject asset = ScriptableObject.CreateInstance(Path.GetFileNameWithoutExtension(filePaths[i]));
                    JsonUtility.FromJsonOverwrite(File.ReadAllText(filePaths[i]), asset);
                    AssetDatabase.CreateAsset(asset, Path.Combine(SettingsManager.RelativeSettingsPath, Path.GetFileName(filePaths[i])));
                }
            }
        }

        /// <summary>
        /// Removes any files added through Pre-Processing.
        /// </summary>
        /// <param name="report">This parameter is unused and should be ignored.</param>
        public void OnPostprocessBuild(BuildReport report = default)
        {
            Application.logMessageReceived -= this.OnBuildError;

            if (!Directory.Exists(EditorSettingsManager.SettingsPath))
            {
                return;
            }

            string[] filePaths = Directory.GetFiles(EditorSettingsManager.SettingsPath);

            if (filePaths.Length == 0)
            {
                return;
            }

            for (int i = 0; i < filePaths.Length; i += 1)
            {
                if (Path.GetExtension(filePaths[i]) == ".asset")
                {
                    AssetDatabase.DeleteAsset(Path.Combine(SettingsManager.RelativeSettingsPath, Path.GetFileName(filePaths[i])));
                }
            }

            string relativePath = SettingsManager.RelativeSettingsPath;
            while (relativePath != "Assets")
            {
                string absolutePath = Path.Combine(Directory.GetParent(Application.dataPath).FullName, relativePath);

                if (Directory.Exists(absolutePath))
                {
                    filePaths = Directory.GetFiles(absolutePath);
                    if (filePaths.Length == 0)
                    {
                        AssetDatabase.DeleteAsset(relativePath);
                    }
                    else
                    {
                        break;
                    }
                }

                int index = relativePath.LastIndexOf(Path.DirectorySeparatorChar);
                if (index > 0)
                {
                    relativePath = relativePath.Substring(0, index);
                }
                else
                {
                    break;
                }
            }
        }

        private void OnBuildError(string condition, string stacktrace, LogType type)
        {
            if (BuildPipeline.isBuildingPlayer && type == LogType.Error)
            {
                this.OnPostprocessBuild();
            }
        }
    }
}