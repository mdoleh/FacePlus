#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections;
using System.IO;

public class AVProQuickTimeCopyPluginWizard
{
	public static void DisplayCopyDialog()
	{
#if UNITY_EDITOR_WIN
        const string title = "AVPro QuickTime - Installation";
		if (EditorUtility.DisplayDialog(title, "Plugin DLL not found.  Unity needs the native plugin DLL files to be copied to the /Assets/Plugins folder.\n\nWould you like us to do that for you?", "Yes, copy", "Cancel"))
		{
			if (CopyPlugins("AVProQuickTime*.dll*"))
			{
				EditorUtility.DisplayDialog(title, "The DLL files copied successfully.\n\nYou need to restart your scene for the plugin to start working.  If the plugin still doesn't work try restarting Unity.", "Ok");
			}
			else
			{
				EditorUtility.DisplayDialog(title, "The DLL files failed to copy for some reason.  You may have to copy them manually and then restart Unity.", "Ok");
			}
		}
#endif
	}

	private static bool CopyPlugins(string filePattern)
	{
		bool result = false;
		string projectPath = Path.GetFullPath(".");
		
		try
		{
			// Find DLLs
			string[] filePaths = Directory.GetFiles(projectPath, filePattern, SearchOption.AllDirectories);
			if (filePaths != null && filePaths.Length > 0)
			{
                // Create target folder
				if (!Directory.Exists("Assets/Plugins/"))
				{
					Directory.CreateDirectory("Assets/Plugins/");
				}
				
				if (Directory.Exists("Assets/Plugins/"))
				{
                    // Copy files
					foreach (string filePath in filePaths)
					{
						string sourcePath = filePath.Replace('\\', '/');
						string targetPath = Path.Combine("Assets/Plugins/", Path.GetFileName(filePath));
						if (!File.Exists(targetPath))
						{
							FileUtil.CopyFileOrDirectory(sourcePath, targetPath);
							Debug.Log("Copying [" + sourcePath + "] to [" + targetPath + "]");
						}
					}
					result = true;
				}
				else
				{
					Debug.LogError("Unable to create Plugins folder");
				}
			}
			else
			{
				Debug.LogError("Unable to find plugin DLLs");
			}
		}
		catch(System.Exception ex)
		{
            Debug.LogError(ex.Message);
			throw;
		}
		
		return result;
	}
}
#endif