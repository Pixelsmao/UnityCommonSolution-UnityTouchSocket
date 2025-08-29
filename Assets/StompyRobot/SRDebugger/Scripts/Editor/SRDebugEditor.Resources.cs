using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace SRDebugger.Editor
{
    public partial class SRDebugEditor
    {
        internal const string DisabledDirectoryPostfix = "_DISABLED~";

        // Paths to enable/disable (relative to SRDebugger root directory)
        private static readonly string[] _resourcePaths = new[]
        {
            "Resources",
            "usr",
            "UI/Prefabs"
        };

        private static void SetResourcesEnabled(bool enable)
        {
            AssetDatabase.StartAssetEditing();

            foreach (var d in GetResourcePaths())
            {
                d.SetDirectoryEnabled(enable);
            }

            AssetDatabase.StopAssetEditing();

            AssetDatabase.Refresh();

            AssetDatabase.ImportAsset(SRInternalEditorUtil.GetRootPath(
            ), ImportAssetOptions.ImportRecursive | ImportAssetOptions.ForceUpdate);
        }

        internal static IEnumerable<ResourceDirectory> GetResourcePaths()
        {
            foreach (var resourcePath in _resourcePaths)
            {
                var enabledPath = Path.Combine(SRInternalEditorUtil.GetRootPath(), resourcePath);
                var disabledPath = Path.Combine(SRInternalEditorUtil.GetRootPath(), resourcePath) + DisabledDirectoryPostfix;

                yield return new ResourceDirectory(enabledPath, disabledPath);
            }
        }


        internal class ResourceDirectory
        {
            public readonly string EnabledPath;
            public readonly string DisabledPath;

            public readonly string EnabledPathMetaFile;
            public readonly string DisabledPathMetaFile;
            public readonly string DisabledPathBackupMetaFile;

            public bool IsEnabled
            {
                get { return Directory.Exists(this.EnabledPath); }
            }

            public bool IsDisabled
            {
                get { return Directory.Exists(this.DisabledPath); }
            }

            public ResourceDirectory(string enabledPath, string disabledPath)
            {
                this.EnabledPath = enabledPath;
                this.DisabledPath = disabledPath;

                this.EnabledPathMetaFile = enabledPath + ".meta";
                this.DisabledPathMetaFile = disabledPath + ".meta";
                this.DisabledPathBackupMetaFile = disabledPath + ".meta.bak~";
            }

            public void SetDirectoryEnabled(bool enable)
            {
                if (this.IsEnabled && enable)
                {
                    return;
                }

                if (this.IsDisabled && !enable)
                {
                    return;
                }

                if (this.IsEnabled && this.IsDisabled)
                {
                    // TODO
                    throw new Exception();
                }

                var title = string.Format("SRDebugger - {0} Resources", enable ? "Enable" : "Disable");

                var oldPath = enable ? this.DisabledPath : this.EnabledPath;
                var newPath = enable ? this.EnabledPath : this.DisabledPath;
                var useAssetDatabase = !enable;

                string error = null;

                if (useAssetDatabase)
                {
                    error = AssetDatabase.MoveAsset(oldPath, newPath);

                    if (!string.IsNullOrEmpty(error))
                    {
                        if (EditorUtility.DisplayDialog(title, this.GetErrorMessage(enable, error), "Force Move", "Abort"))
                        {
                            useAssetDatabase = false;
                        }
                    }
                }

                if (!useAssetDatabase)
                {
                    try
                    {
                        Directory.Move(oldPath, newPath);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError("Error moving directory");
                        Debug.LogException(e);
                        error = "Exception occurred, see console for details.";
                    }
                }

                if (!string.IsNullOrEmpty(error))
                {
                    var message = string.Format(
                        "An error occurred while attempting to {3} SRDebugger resource directory.\n\n Old Path: {0}\n New Path: {1}\n\n Error: \n{2}",
                        this.EnabledPath, this.DisabledPath, error, enable ? "enable" : "disable");

                    EditorUtility.DisplayDialog(title, message, "Continue");
                    return;
                }

                if (!enable)
                {
                    // Disable meta files
                    if (File.Exists(this.DisabledPathMetaFile))
                    {
                        if (File.Exists(this.DisabledPathBackupMetaFile))
                        {
                            File.Delete(this.DisabledPathBackupMetaFile);
                        }

                        File.Move(this.DisabledPathMetaFile, this.DisabledPathBackupMetaFile);
                    }
                }
                else
                {
                    // Enable backed up meta files
                    if (File.Exists(this.DisabledPathBackupMetaFile))
                    {
                        if (File.Exists(this.EnabledPathMetaFile))
                        {
                            File.Delete(this.EnabledPathMetaFile);
                        }

                        File.Move(this.DisabledPathBackupMetaFile, this.EnabledPathMetaFile);
                    }
                }

            }

            private string GetErrorMessage(bool enable, string error)
            {
                return string.Format(
                    "An error occurred while attempting to {3} SRDebugger resources. \n\n Old Path: {0}\n New Path: {1}\n\n Error: \n{2}",
                    this.EnabledPath, this.DisabledPath, error, enable ? "enable" : "disable");
            }
        }
    }
}