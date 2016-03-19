using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using Mixamo;

[CanEditMultipleObjects]
[CustomEditor(typeof(FacePlusShaper))]
public class FacePlusShaperEditor : Editor {
	private FacePlusShaper currentlyEditing;

	public override void OnInspectorGUI() {
		DrawDefaultInspector();
		var mapping = ((FacePlusShaper)target).channelMapping;

		if (mapping != null) {
			if (GUILayout.Button ("Save")) {
				string path = EditorUtility.SaveFilePanelInProject("New Face Plus Preset", "preset.txt", "txt", "Where would you like to save this?");
				((FacePlusShaper)target).SavePreset (path);
			}

			foreach(var pair in mapping) {
				GUILayout.BeginHorizontal();
				float val = EditorGUILayout.FloatField (pair.Key, pair.Value.Scale);
				mapping[pair.Key].Scale = val;
				float val1 = EditorGUILayout.FloatField (pair.Value.Offset);
				mapping[pair.Key].Offset = val1;
				GUILayout.EndHorizontal ();
			}
		}
	}
}
