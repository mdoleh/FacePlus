using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using Mixamo;


[CanEditMultipleObjects]
[CustomEditor(typeof(FacePlusConnector))]
public class FacePlusConnectorEditor : Editor {
	private FacePlusConnector currentlyEditing;
	private bool showJointList;
	private int savedCount;

	// Use this for initialization
	public override void OnInspectorGUI(){
		DrawDefaultInspector();
		var rigType = ((FacePlusConnector)target).Type;
		var jointList = ((FacePlusConnector)target).FaceJoints;
		var SDK_def = ((FacePlusConnector)target).SDK_Definition;
		showJointList = (rigType == FacePlusConnector.RigType.Joint_SDK);
		var facemesh = ((FacePlusConnector)target).FaceMesh;

		Event e = Event.current;
		bool enterPressed = false;
		if(e.type == EventType.keyDown){
			if(e.keyCode == (KeyCode.Return))
				enterPressed = true;
		}
		if(showJointList){

			EditorGUILayout.BeginHorizontal();
			SDK_def = EditorGUILayout.ObjectField ("SDK Preset",SDK_def, typeof(TextAsset), true) as TextAsset;
			EditorGUILayout.EndHorizontal();
			((FacePlusConnector)target).SDK_Definition = SDK_def;


			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField ("Face Joints");
			GUI.SetNextControlName("count");
			int newCount = EditorGUILayout.IntField (jointList.Count);
			if(newCount != jointList.Count){
				savedCount = newCount;
				Debug.Log (savedCount);
			}
			EditorGUILayout.EndHorizontal();

			if(savedCount != jointList.Count && GUI.GetNameOfFocusedControl()=="count" && enterPressed){
				List<Transform> newList = new List<Transform>();
				for(int i  = 0; i< savedCount; i++){
					newList.Add (null);
					if(i < jointList.Count) newList[i] = jointList[i];

				}
				jointList = newList;
			}

			for(int i = 0; i < jointList.Count; i++){
				EditorGUILayout.BeginHorizontal ();
				jointList[i] = EditorGUILayout.ObjectField(jointList[i], typeof(Transform), true) as Transform ;
				EditorGUILayout.EndHorizontal ();
			}
			((FacePlusConnector)target).FaceJoints = jointList;

		}
		else{
			EditorGUILayout.BeginHorizontal ();
			facemesh = EditorGUILayout.ObjectField ("Face Mesh",facemesh, typeof(SkinnedMeshRenderer), true) as SkinnedMeshRenderer;
			EditorGUILayout.EndHorizontal ();
			((FacePlusConnector)target).FaceMesh = facemesh;
		}
	}
}
