#if !NR_FREE
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Text;
using UnityEditor.Callbacks;

namespace VisualDesignCafe.Rendering.Nature.Editor.SourceCode
{
    public class SourceCodeWindow : EditorWindow
    {
        private class SourceMapData
        {
            public string ScriptGuid;
            public string ScriptFileId;
            public string ScriptName;
            public string PluginGuid;
            public string PluginName;
        }

        private static SourceCodeWindow _window;

        private GUIStyle _titleStyle;
        private GUIStyle _subtitleStyle;
        private GUIStyle _headerStyle;
        private GUIStyle _labelStyle;

        private bool _sourceFileExists;

        [MenuItem("Help/Nature Renderer/Convert project to use Source Code", false, 500)]
        [MenuItem("Window/Nature Renderer/Convert project to use Source Code", false, 500)]
        public static void Open()
        {
            _window = SourceCodeWindow.GetWindow<SourceCodeWindow>(true);
            _window.titleContent = new GUIContent("Source Code");
            _window.maxSize = new Vector2(320, 750);
            _window.minSize = _window.maxSize;
            _window.ShowUtility();
        }

        /// <summary>
        /// Opens the window when trying to manually import the source code package.
        /// Manually importing the package may result in a corrupt Unity project.
        /// </summary>
        /// <param name="instanceId"></param>
        /// <param name="line"></param>
        /// <returns></returns>
        [OnOpenAsset(int.MinValue)]
        private static bool OnOpenAsset(int instanceId, int line)
        {
            var asset = EditorUtility.InstanceIDToObject(instanceId);
            if (asset != null
                && asset.name.Contains("com.vdc.nature-renderer.", StringComparison.OrdinalIgnoreCase)
                && asset.name.Contains(".source.", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    if (!LoadBundleIdAndVersion().Contains(".source.", StringComparison.OrdinalIgnoreCase))
                    {
                        Open();
                        return true;
                    }
                }
                catch
                {
                    return false;
                }
            }

            return false;
        }

        private void OnEnable()
        {
            try
            {
                _sourceFileExists = !string.IsNullOrEmpty(FindSourcePackage());
            }
            catch (Exception)
            {
                _sourceFileExists = false;
            }
        }

        private void OnGUI()
        {
            if (_window == null)
                Open();

            GUILayout.BeginArea(new Rect(0, 0, _window.position.width, _window.position.height));

            DrawHeader(_window.position.width);

            if (!_sourceFileExists)
            {
                GUILayout.Space(-EditorGUIUtility.singleLineHeight);
                EditorGUILayout.HelpBox("Could not find source code package in project", MessageType.Error);
                GUILayout.Space(EditorGUIUtility.singleLineHeight);
            }
            using (new EditorGUI.DisabledScope(!_sourceFileExists))
            {
                // Source code needs to be imported first in order to generate new GUIDs for the files that clash with the plugins.
                // If the code is imported only after deleting the plugins then Unity will freeze.
                DrawImportSourceCode(GetStepCompleted("import-source") ? "✓ 1. " : "1. ");
                DrawSeparator(true, _window.position.width, null, 1f, _window.position.width);

                // Delete the plugins after importing the source code. If the plugins are deleted first then Unity will freeze when
                // importing the source code because it is trying to reload the non-existing plugin files.
                DrawDeletePlugins(GetStepCompleted("delete-plugins") ? "✓ 2. " : "2. ");
                DrawSeparator(true, _window.position.width, null, 1f, _window.position.width);

                // Unity needs to be restarted in order to force a reload of the plugin and asset database. Without a restart, 
                // the plugins stay loaded and Unity will freeze when compiling because it tries to load the non-existing plugin files.
                // It is not possible to restart Unity earlier because it will detect compilation errors and go into safe mode, which will
                // hide this window and prevents the next steps from running.
                DrawRestartUnity(GetStepCompleted("restart-unity") ? "✓ 3. " : "3. ");
                DrawSeparator(true, _window.position.width, null, 1f, _window.position.width);

                // The first import of the source code caused incorrect GUIDs on the folders. This second reimport will fix those GUIDs.
                DrawReImportSourceCode(GetStepCompleted("reimport-source") ? "✓ 4. " : "4. ");
                DrawSeparator(true, _window.position.width, null, 1f, _window.position.width);

                // The script references need to be fixed in all assets in the project. All references to the plugins need to be
                // changed to references to the new script files.
                DrawPatchAssets(GetStepCompleted("patch-assets") ? "✓ 5. " : "5. ");
            }
            GUILayout.EndArea();
        }

        private void DrawHeader(float parentWidth)
        {
            CreateTitleStyles();

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(EditorGUIUtility.singleLineHeight);
                using (new EditorGUILayout.VerticalScope())
                {
                    GUILayout.Space(EditorGUIUtility.singleLineHeight * 0.5f);
                    {
                        GUILayout.Label("How to use Nature Renderer source code", _titleStyle);
                        GUILayout.Label("Converting your project to use the Nature Renderer source code requires a few steps:", _subtitleStyle);
                    }
                    GUILayout.Space(EditorGUIUtility.singleLineHeight * 1f);

                    GUILayout.Space(1);
                    if (Event.current.type == EventType.Repaint)
                        EditorGUI.DrawRect(new Rect(0, GUILayoutUtility.GetLastRect().y, parentWidth, 1), new Color(0, 0, 0, EditorGUIUtility.isProSkin ? 0.4f : 0.2f));
                }
                GUILayout.Space(EditorGUIUtility.singleLineHeight);
            }
            GUILayout.Space(EditorGUIUtility.singleLineHeight * 1f);
        }

        private void DrawDeletePlugins(string stepNumber)
        {
            CreateLabelStyles();

            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Space(EditorGUIUtility.singleLineHeight);
                using (new GUILayout.VerticalScope())
                {
                    GUILayout.Label(stepNumber + "Delete plugins", _headerStyle);
                    GUILayout.Label("Delete the Nature Renderer plugins (.dll) from your project to get rid of duplicate code.", _labelStyle);

                    GUILayout.Space(EditorGUIUtility.singleLineHeight * 0.5f);

                    if (GUILayout.Button("Delete Plugins"))
                        DeletePluginsFromMenu();
                }
                GUILayout.Space(EditorGUIUtility.singleLineHeight);
            }
        }

        private void DrawImportSourceCode(string stepNumber)
        {
            CreateLabelStyles();

            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Space(EditorGUIUtility.singleLineHeight);
                using (new GUILayout.VerticalScope())
                {
                    GUILayout.Label(stepNumber + "Import source code", _headerStyle);
                    GUILayout.Label("Import the package with the source code.", _labelStyle);

                    GUILayout.Space(EditorGUIUtility.singleLineHeight * 0.5f);

                    if (GUILayout.Button("Import source code"))
                        ImportSourceCodeFromMenu("import-source");
                }
                GUILayout.Space(EditorGUIUtility.singleLineHeight);
            }
        }

        private void DrawReImportSourceCode(string stepNumber)
        {
            CreateLabelStyles();

            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Space(EditorGUIUtility.singleLineHeight);
                using (new GUILayout.VerticalScope())
                {
                    GUILayout.Label(stepNumber + "Re-Import source code", _headerStyle);
                    GUILayout.Label("Re-import the package with the source code to fix file IDs.", _labelStyle);

                    GUILayout.Space(EditorGUIUtility.singleLineHeight * 0.5f);

                    if (GUILayout.Button("Import source code"))
                        ImportSourceCodeFromMenu("reimport-source");
                }
                GUILayout.Space(EditorGUIUtility.singleLineHeight);
            }
        }

        private void DrawRestartUnity(string stepNumber)
        {
            CreateLabelStyles();

            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Space(EditorGUIUtility.singleLineHeight);
                using (new GUILayout.VerticalScope())
                {
                    GUILayout.Label(stepNumber + "Restart Unity", _headerStyle);
                    GUILayout.Label("Restart Unity to force a refresh of the loaded plugins", _labelStyle);

                    GUILayout.Space(EditorGUIUtility.singleLineHeight * 0.5f);

                    if (GUILayout.Button("Restart Unity"))
                        RestartEditor();
                }
                GUILayout.Space(EditorGUIUtility.singleLineHeight);
            }
        }

        private void DrawPatchAssets(string stepNumber)
        {
            CreateLabelStyles();

            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Space(EditorGUIUtility.singleLineHeight);
                using (new GUILayout.VerticalScope())
                {
                    GUILayout.Label(stepNumber + "Patch assets", _headerStyle);
                    GUILayout.Label("Finally, all the assets in your project that reference Nature Renderer scripts need to be remapped from the plugin files to the scripts. Otherwise, your components will show as missing.", _labelStyle);

                    GUILayout.Space(EditorGUIUtility.singleLineHeight * 0.5f);

                    if (GUILayout.Button("Patch assets"))
                        PatchProjectFromMenu();
                }
                GUILayout.Space(EditorGUIUtility.singleLineHeight);
            }
        }

        private void CreateTitleStyles()
        {
            if (_titleStyle == null)
            {
                _titleStyle = 
                    new GUIStyle(EditorGUIUtility.isProSkin ? EditorStyles.whiteLabel : EditorStyles.boldLabel)
                    {
                        alignment = TextAnchor.MiddleCenter,
                        fontSize = EditorStyles.whiteLargeLabel.fontSize,
                        fontStyle = FontStyle.Bold
                    };
            }

            if (_subtitleStyle == null)
            {
                _subtitleStyle = 
                    new GUIStyle(EditorStyles.wordWrappedLabel)
                    {
                        alignment = TextAnchor.MiddleCenter
                    };
            }
        }

        private void CreateLabelStyles()
        {
            if (_labelStyle == null)
            {
                _labelStyle =
                    new GUIStyle(EditorStyles.label)
                    {
                        alignment = TextAnchor.MiddleLeft,
                        stretchWidth = true,
                        wordWrap = true
                    };
            }
            if (_headerStyle == null)
            {
                _headerStyle =
                    new GUIStyle(EditorGUIUtility.isProSkin ? EditorStyles.whiteLabel : EditorStyles.boldLabel)
                    {
                        fontStyle = FontStyle.Bold,
                        alignment = TextAnchor.MiddleLeft,
                        stretchWidth = true
                    };
            }
        }

        private void DrawSeparator(bool line, float width, string label, float space, float parentWidth)
        {
            GUILayout.Space(EditorGUIUtility.singleLineHeight * space);
            GUILayout.Space(1);
            if (Event.current.type == EventType.Repaint)
            {
                Rect rect = GUILayoutUtility.GetLastRect();
                if (line)
                    EditorGUI.DrawRect(new Rect(parentWidth * 0.5f - width * 0.5f, rect.y, width, 1), new Color(0, 0, 0, EditorGUIUtility.isProSkin ? 0.4f : 0.2f));
                if (!string.IsNullOrEmpty(label))
                    GUI.Label(new Rect(0, rect.y - 8, parentWidth, 16), label, EditorStyles.centeredGreyMiniLabel);
            }
            GUILayout.Space(EditorGUIUtility.singleLineHeight * space);
        }

        public static void DeletePluginsFromMenu()
        {
            if (!EditorUtility.DisplayDialog(
                "Delete plugins?",
                "This will delete the Nature Renderer plugin files from your project. This should be done before importing the source code.",
                "Delete",
                "Cancel"))
            {
                return;
            }

            EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();

            try
            {
                DeletePlugins();
                SetStepCompleted("delete-plugins", true);

                EditorUtility.DisplayDialog(
                    "Completed",
                    "The Nature Renderer plugins have been deleted from your project",
                    "Ok");
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                EditorUtility.DisplayDialog("Failed to delete plugins", e.Message, "Ok");
            }
        }

        public static void ImportSourceCodeFromMenu(string stepName)
        {
            if (!EditorUtility.DisplayDialog(
                "Import source code?",
                "This will import the source code for Nature Renderer. First delete the Nature Renderer plugins from your project!",
                "Import",
                "Cancel"))
            {
                return;
            }

            EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();

            try
            {
                AssetDatabase.ImportPackage(FindSourcePackage(), true);
                SetStepCompleted(stepName, true);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                EditorUtility.DisplayDialog("Failed to delete plugins", e.Message, "Ok");
            }
        }

        public static void PatchProjectFromMenu()
        {
            if (!EditorUtility.DisplayDialog(
                "Convert project?",
                "This will scan all assets in the project and replace Nature Renderer components that "
                    + "reference plugins (.dll) to reference the C# source code scripts (.cs) instead. "
                    + "This should be done after importing the Nature Renderer souce code in your project."
                    + "\n\nWARNING: Make a backup of your project before proceeding.",
                "Convert",
                "Cancel"))
            {
                return;
            }

            if (!EditorUtility.DisplayDialog(
                "Convert project?",
                "WARNING: Make a backup of your project before proceeding! This action will automatically change files in your project and cannot be undone",
                "I made a backup",
                "Cancel"))
            {
                return;
            }

            EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();

            try
            {
                PatchProject();

                SetStepCompleted("patch-assets", true);

                EditorUtility.DisplayDialog(
                    "Completed",
                    "The project has been converted to use the source code version of Nature Renderer",
                    "Ok");
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                EditorUtility.DisplayDialog("Error", e.Message, "Ok");
                return;
            }
        }

        private static void PatchProject()
        {
            string mapPath = GetSourceMapPath();
            Dictionary<string, SourceMapData> sourceLookup = new();
            foreach (var entry in LoadSourceMap(mapPath))
            {
                sourceLookup[entry.Value.PluginGuid + "." + entry.Value.ScriptFileId] = entry.Value;
            }

            ValidateSourceMap(sourceLookup);

            string[] paths =
                AssetDatabase.GetAllAssetPaths()
                    .Where(path => path.StartsWith("Assets"))
                    .Where(path => path.EndsWith(".prefab") || path.EndsWith(".unity"))
                    .ToArray();

            AssetDatabase.StartAssetEditing();
            try
            {
                float current = 0;
                float total = paths.Length;
                foreach (var path in paths)
                {
                    EditorUtility.DisplayProgressBar("Remapping scripts", Path.GetFileName(path), current / total);
                    RemapFile(path, sourceLookup);
                    current++;
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                AssetDatabase.StopAssetEditing();
                AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            }
        }

        private static string GetSourceMapPath()
        {
            string mapPath = "Assets/Visual Design Cafe/Nature Renderer/Plugins/guids.txt";
            if (File.Exists(mapPath))
                return mapPath;

            mapPath = AssetDatabase.GUIDToAssetPath("beb8e93c9df3c414e9b88b282619753f");
            if (!mapPath.EndsWith("guids.txt"))
                throw new Exception("Could not find conversion map");

            return mapPath;
        }

        private static Dictionary<string, SourceMapData> LoadSourceMap(string path)
        {
            var map = new Dictionary<string, SourceMapData>();
            string[] lines = File.ReadAllLines(path);
            foreach (var line in lines)
            {
                if (!string.IsNullOrWhiteSpace(line))
                {
                    try
                    {
                        string[] data = line.Split(":");
                        string scriptGuid = data[0];
                        map[scriptGuid] =
                            new SourceMapData()
                            {
                                ScriptGuid = scriptGuid,
                                ScriptName = data[1],
                                PluginGuid = data[2],
                                ScriptFileId = data[3],
                                PluginName = data[4]
                            };
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                    }
                }
            }

            return map;
        }

        /// <summary>
        /// Checks the project to ensure that the plugins in the map no longer
        /// exist in the project and that all the source code files do exist.
        /// </summary>
        private static void ValidateSourceMap(Dictionary<string, SourceMapData> map)
        {
            foreach (var data in map.Values)
            {
                if (string.IsNullOrEmpty(data.ScriptName)
                    || string.IsNullOrEmpty(data.ScriptGuid)
                    || string.IsNullOrEmpty(data.PluginGuid)
                    || string.IsNullOrEmpty(data.ScriptFileId))
                {
                    continue;
                }

                string pluginPath = AssetDatabase.GUIDToAssetPath(data.PluginGuid);
                string expectedPluginName = data.PluginName;
                string actualPluginName = Path.GetFileName(pluginPath);

                if (actualPluginName.Equals(expectedPluginName, StringComparison.OrdinalIgnoreCase))
                {
                    throw new Exception(
                        "Invalid project. Expected plugin with name "
                            + expectedPluginName
                            + " and GUID "
                            + data.PluginGuid
                            + " to not exist in project");
                }

                string expectedName = data.ScriptName;
                string actualPath = AssetDatabase.GUIDToAssetPath(data.ScriptGuid);
                string actualName = Path.GetFileName(actualPath);

                if (string.IsNullOrEmpty(actualName))
                {
                    throw new Exception(
                        "Invalid project. Expected script with name "
                            + expectedName
                            + " and GUID "
                            + data.ScriptGuid
                            + " to exist");
                }

                if (!expectedName.Equals(actualName, StringComparison.OrdinalIgnoreCase))
                {
                    throw new Exception(
                        "Invalid project. Expected script with GUID "
                            + data.ScriptGuid
                            + " to be "
                            + expectedName
                            + " but the script in project is "
                            + actualName);
                }
            }
        }

        private static void RemapFile(string filePath, Dictionary<string, SourceMapData> pluginMap)
        {
            Debug.Log("<b>Remapping script references in " + filePath + "</b>");

            int count = 0;
            string[] lines = File.ReadAllLines(filePath);
            for (var i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                if (line.Trim().StartsWith("m_Script: {fileID: "))
                {
                    try
                    {
                        var reference = line.Split("{")[1].Trim().Trim('}').Split(",");
                        var fileId = reference[0].Trim().Split(":");
                        var guid = reference[1].Trim().Split(":");
                        var type = reference[2].Trim().Split(":");
                        var key = guid[1].Trim() + "." + fileId[1].Trim();

                        if (pluginMap.TryGetValue(key, out SourceMapData mapData))
                        {
                            lines[i] = "m_Script: {fileID: 11500000, guid: " + mapData.ScriptGuid + ", type: 3}";
                            count++;

                            Debug.Log("    - " + mapData.PluginName + " (" + mapData.ScriptFileId + ") -> " + mapData.ScriptName);
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                    }
                }
            }

            if (count > 0)
            {
                File.WriteAllLines(filePath, lines);
            }
            else
            {
                Debug.Log("    No references found");
            }
        }

        private static string FindSourcePackage()
        {
            string bundleIdAndVersion = LoadBundleIdAndVersion();

            if (!bundleIdAndVersion.Contains(".source."))
            {
                bundleIdAndVersion =
                    bundleIdAndVersion
                        .Replace(".pro.", ".pro.source.")
                        .Replace(".enterprise.", ".enterprise.source.");
            }

            string sourceBundlePath = "/" + bundleIdAndVersion + ".unitypackage";

            string path = AssetDatabase.GetAllAssetPaths().FirstOrDefault(path => path.EndsWith(sourceBundlePath, StringComparison.OrdinalIgnoreCase));

            if (!File.Exists(path))
                throw new Exception("Could not find source bundle in project: " + sourceBundlePath);

            return path;
        }

        private static bool DeletePlugins()
        {
            Debug.Log("Deleting plugins");

            var plugins =
                new string[]{
                    "VisualDesignCafe.Rendering.Instancing.Editor.dll",
                    "VisualDesignCafe.Rendering.Instancing.dll",
                    "VisualDesignCafe.Rendering.Nature.Editor.dll",
                    "VisualDesignCafe.Rendering.Nature.dll",
                    "VisualDesignCafe.Memory.dll"
                };

            var allPlugins =
                AssetDatabase.GetAllAssetPaths().Where(path => path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)).ToArray();

            var pluginPaths = new List<string>();

            foreach (var plugin in plugins)
            {
                string path = allPlugins.FirstOrDefault(path => path.EndsWith(plugin, StringComparison.OrdinalIgnoreCase));
                if (string.IsNullOrEmpty(path))
                    throw new Exception("Could not find plugin with name: " + plugin);

                pluginPaths.Add(path);
                Debug.Log("    Found " + path);
            }

            var deleted = false;
            foreach (var path in pluginPaths)
            {
                if (AssetDatabase.DeleteAsset(path))
                {
                    deleted = true;
                    Debug.Log("    Deleted " + path);
                }

                if (AssetDatabase.DeleteAsset(path.Replace(".dll", ".pdb")))
                {
                    Debug.Log("    Deleted " + path.Replace(".dll", ".pdb"));
                }
            }

            return deleted;
        }

        private static string LoadBundleIdAndVersion()
        {
            string assetPath = AssetDatabase.GUIDToAssetPath("c34b4f167b2c16542b4dd862579bc4c4");
            if (!assetPath.EndsWith("Nature Renderer.asset", StringComparison.OrdinalIgnoreCase))
                throw new Exception("Could not find Nature Renderer package");

            string bundleId = "";
            string bundleVersion = "";
            string[] lines = File.ReadAllLines(assetPath);
            foreach (var line in lines)
            {
                string trimmed = line.Trim();
                if (trimmed.StartsWith("_bundleIdentifier:"))
                    bundleId = trimmed.Split(":")[1].Trim();
                else if (trimmed.StartsWith("_versionNumber:"))
                    bundleVersion = trimmed.Split(":")[1].Trim();
            }

            if (string.IsNullOrEmpty(bundleId))
                throw new Exception("Could not find bundle ID");

            if (string.IsNullOrEmpty(bundleVersion))
                throw new Exception("Could not find bundle version");

            return bundleId + "-" + bundleVersion;
        }

        private static void RestartEditor()
        {
            SetStepCompleted("restart-unity", true);
            EditorApplication.OpenProject(Directory.GetCurrentDirectory());
        }

        private static void SetStepCompleted(string name, bool completed)
        {
            EditorPrefs.SetBool("vdc.nr.source-code-conversion." + name + "." + Base64Encode(Directory.GetCurrentDirectory()), completed);
        }

        private static bool GetStepCompleted(string name)
        {
            return EditorPrefs.GetBool("vdc.nr.source-code-conversion." + name + "." + Base64Encode(Directory.GetCurrentDirectory()), false);
        }

        private static string Base64Encode(string plainText)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(plainText));
        }

    }
}
#endif
