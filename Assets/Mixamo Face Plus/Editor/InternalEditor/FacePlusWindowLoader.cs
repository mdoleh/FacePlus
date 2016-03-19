using UnityEngine;
using UnityEditor;
using System.Collections;
using Mixamo;

//utility to pop out faceplus editor if a character witha FacePlusConnector exists in the scene.
namespace MixamoEditor {
[InitializeOnLoad]
public class FacePlusWindowLoader : MonoBehaviour {

	static bool windowLoaded;
	static FacePlusWindowLoader()
	{
			EditorApplication.update += Update;
			windowLoaded = false;
	}
	
	// Update is called once per frame
	static void Update () {
		if (EditorApplication.isPlaying) 
		{
			//check if the scene has a faceplusconnector component
			if(Object.FindObjectOfType<FacePlusConnector>() && !windowLoaded)
				{
					//popup faceplus editor window
					FacePlusEditorWindow window = (FacePlusEditorWindow)EditorWindow.GetWindow (typeof(FacePlusEditorWindow), false, "Face Plus");
					windowLoaded = true;
				}
		}
		if (!EditorApplication.isPlaying)
				windowLoaded = false;
	
	}
}//facepluswindowloader
}//namespace
