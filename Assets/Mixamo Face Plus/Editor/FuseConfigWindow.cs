using UnityEngine;
using UnityEditor;
using Mixamo;
using System.Collections;
using System.Collections.Generic;
/*
 * Configures your lovely Fuse characaters for Face Plus tracking fun time
 */

namespace MixamoEditor{
public class FuseConfigWindow : EditorWindow {
	
	GameObject charOBJ;
	string warningText = "";
	
	FacePlusConnector fpConnector;
	FacePlusShaper fpShaper;
	FacePlusExtras_MoreBlendshapeMeshes fpExtraBlendshapes;
	FacePlusExtras_SmoothHeadRotation fpSmoothHeadRot;
	
	//character properties
	Transform[] childTransforms;
	Transform headJoint;
	Transform leftEye;
	Transform rightEye;
	SkinnedMeshRenderer faceMesh;
	List<SkinnedMeshRenderer> extraMeshes;
	
	//success
	bool configured = false;
	bool triedToConfigure = false;
	Color errorColor = Color.red;
	Color successColor = Color.green;
    
	public static void Init ()
	{
		// Get existing open window or if none, make a new one
		FuseConfigWindow window = (FuseConfigWindow)EditorWindow.GetWindow (typeof(FuseConfigWindow), false, "Fuse Character");
	}

	void OnGUI(){
		GUILayout.BeginHorizontal ();
		GUILayout.Label ("Fuse Character GameObject:", GUILayout.Width (200));
		var input = EditorGUILayout.ObjectField (charOBJ, typeof(GameObject), true, GUILayout.ExpandWidth(true));
		if(input != null && PrefabUtility.GetPrefabType(input) == PrefabType.ModelPrefab)
			charOBJ = Instantiate(input) as GameObject;
		else
			charOBJ = (GameObject) input;
		//charOBJ = (GameObject) EditorGUILayout.ObjectField (charOBJ, typeof(GameObject), true, GUILayout.ExpandWidth(true));
		GUILayout.EndHorizontal ();
		EditorGUILayout.Space ();
		
		GUILayout.Label(warningText);

		Color defaultCol = GUI.skin.label.normal.textColor;
		if(configured || !triedToConfigure)
			AutoConfigGUI();
			
		if(!configured && triedToConfigure)
		{	
			
			GUI.skin.label.normal.textColor = errorColor;
			GUILayout.Label ("Darn! Couldn't find the following things in your rig. Try specifying them below");
			GUI.skin.label.normal.textColor = defaultCol;
			EditorGUILayout.Space ();
			if(!headJoint)
				GUILayout.Label("No head joint found in rig!");
			if(!leftEye)
				GUILayout.Label ("No left eye joint found in rig!");
			if(!rightEye)
				GUILayout.Label ("No right eye joint found in rig!");
			if(!faceMesh)
				GUILayout.Label ("No mesh with facial blendshapes found! For fuse characters this is the Body mesh");
			EditorGUILayout.Space ();
			
			ManualConfigGUI();
			GUI.skin.label.normal.textColor = defaultCol;
		}
		else if(triedToConfigure)
		{
			GUI.skin.label.normal.textColor = successColor;
			GUILayout.Label ("Success!");
			GUI.skin.label.normal.textColor = defaultCol;
			EditorGUILayout.Space ();
			GUILayout.Label ("Calibrate Face Plus to for better tracking results!");
			if(GUILayout.Button ("Create a Calibration Preset")){
				FacePlusCalibrationWindow.Init();
				GetWindow<FacePlusCalibrationWindow>().SetCharacter(charOBJ);
				this.Close ();
				
			}
		}
		
	
			
	}
	
	void GatherProperties()
	{
		childTransforms = charOBJ.GetComponentsInChildren<Transform>();
		headJoint = FindTransformByName("mixamorig:Head");
		leftEye = FindTransformByName("mixamorig:LeftEye");
		rightEye = FindTransformByName("mixamorig:RightEye");
		faceMesh = FindMeshByName("Body");
		extraMeshes = new List<SkinnedMeshRenderer>();
		GatherExtraMeshes();
	}
	
	bool ConfigureFuseCharacter()
	{
		//TODO: check if character already configured
		//TODO: be able to get char properties by user input.

		if(!leftEye || !rightEye)
			warningText = "No left eye or right eye joint found! That's ok though.";
		if(!headJoint ||/* !leftEye || !rightEye || */!faceMesh)
			return false;
		
		//Add appropriate components

		fpConnector = charOBJ.GetComponent<FacePlusConnector>() ? charOBJ.GetComponent<FacePlusConnector>() 
						: charOBJ.AddComponent<FacePlusConnector>();
		fpConnector.Type = FacePlusConnector.RigType.BlendShape;
		fpConnector.HeadJoint = headJoint;
		fpConnector.LeftEyeTransform = leftEye;
		fpConnector.RightEyeTransform = rightEye;
		fpConnector.FaceMesh = faceMesh;
		
		fpShaper = charOBJ.GetComponent<FacePlusShaper>() ? charOBJ.GetComponent<FacePlusShaper>()
					 : charOBJ.AddComponent<FacePlusShaper>();
		
		fpExtraBlendshapes = charOBJ.GetComponent<FacePlusExtras_MoreBlendshapeMeshes>() ? charOBJ.GetComponent<FacePlusExtras_MoreBlendshapeMeshes>() 
					 : charOBJ.AddComponent<FacePlusExtras_MoreBlendshapeMeshes>();
								
		fpExtraBlendshapes.FaceMesh = faceMesh;
		fpExtraBlendshapes.ExtraBlendshapeMeshes = extraMeshes;
		
		/* smooth head rotation is broked, let's not use it for now!
		
		fpSmoothHeadRot = charOBJ.GetComponent<FacePlusExtras_SmoothHeadRotation>() ? charOBJ.GetComponent<FacePlusExtras_SmoothHeadRotation>()
						: charOBJ.AddComponent<FacePlusExtras_SmoothHeadRotation>();
		fpSmoothHeadRot.HeadJoint = headJoint;
		*/
		
		return true;
	}
	
	void AutoConfigGUI()
	{
		if(GUILayout.Button ("Configure this character"))
		{
			triedToConfigure = true;
			if(charOBJ == null)
				warningText = "No fuse character gameobject selected!";
			else{
				GatherProperties();
				configured = ConfigureFuseCharacter();	
			}
			
		}
	}
	
	void ManualConfigGUI()
	{
	//	if(!headJoint){
			GUILayout.BeginHorizontal ();
			GUILayout.Label ("Head Joint:", GUILayout.Width (200));
			headJoint = (Transform) EditorGUILayout.ObjectField (headJoint, typeof(Transform), true, GUILayout.ExpandWidth(true));
			GUILayout.EndHorizontal ();
			EditorGUILayout.Space ();
	//	}
	//	if(!rightEye){
			GUILayout.BeginHorizontal ();
			GUILayout.Label ("Right Eye Joint:", GUILayout.Width (200));
			rightEye = (Transform) EditorGUILayout.ObjectField (rightEye, typeof(Transform), true, GUILayout.ExpandWidth(true));
			GUILayout.EndHorizontal ();
			EditorGUILayout.Space ();
	//	}
	//	if(!leftEye){
			GUILayout.BeginHorizontal ();
			GUILayout.Label ("Left Eye Joint:", GUILayout.Width (200));
			leftEye = (Transform) EditorGUILayout.ObjectField (leftEye, typeof(Transform), true, GUILayout.ExpandWidth(true));
			GUILayout.EndHorizontal ();
			EditorGUILayout.Space ();
	//	}
	//	if(!faceMesh){
			GUILayout.BeginHorizontal ();
			GUILayout.Label ("Face Mesh:", GUILayout.Width (200));
			faceMesh = (SkinnedMeshRenderer) EditorGUILayout.ObjectField (faceMesh, typeof(SkinnedMeshRenderer), true, GUILayout.ExpandWidth(true));
			GUILayout.EndHorizontal ();
			EditorGUILayout.Space ();
	//	}
	
			if(GUILayout.Button ("Configure this character"))
			{
				configured = ConfigureFuseCharacter();			
			}
	}
	
	Transform FindTransformByName(string query)
	{
		for(int i = 0; i < childTransforms.Length; i++)
		{
			if(childTransforms[i].gameObject.name == query)
				return childTransforms[i];
		}
		
		return null;
	}
	
	SkinnedMeshRenderer FindMeshByName(string query)
	{
		for(int i = 0; i < childTransforms.Length; i++)
		{
			if(childTransforms[i].gameObject.name == query
				   && childTransforms[i].gameObject.GetComponent<SkinnedMeshRenderer>())
				return childTransforms[i].gameObject.GetComponent<SkinnedMeshRenderer>();
		}
		
		return null;
	}
	
	void GatherExtraMeshes(){
			for(int i = 0; i < childTransforms.Length; i++)
			{
				if(childTransforms[i].GetComponent<SkinnedMeshRenderer>() &&
				   childTransforms[i].GetComponent<SkinnedMeshRenderer>() != faceMesh &&
				   childTransforms[i].GetComponent<SkinnedMeshRenderer>().sharedMesh.blendShapeCount > 0)
					extraMeshes.Add (childTransforms[i].GetComponent<SkinnedMeshRenderer>());
			}
	}
	

}
}//namespace mixamoEditor
