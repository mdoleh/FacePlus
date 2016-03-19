using UnityEngine;
using UnityEditor;
using Mixamo;
using System.Collections;

namespace MixamoEditor{
public class FacePlusCalibrationWindow : EditorWindow {
	
	struct CalibrationModule{
		public string name;
		string[] affectedBlendshapes;
		public bool recorded;
		
		public CalibrationModule(string name_, string[] affectedBlendshapes_)
		{
			name = name_;
			affectedBlendshapes = affectedBlendshapes_;
			recorded = false;
		}
		
		public void RecordModule()
		{
			for(int i = 0; i < affectedBlendshapes.Length; i++)
			{
				string shape = affectedBlendshapes[i];
				
				float[] channelVector = FacePlus.GetCurrentVector ();
				for(int j=0; j<channelVector.Length; j++) {
					string channel = FacePlus.GetChannelName(j);
					if (shape == channel) {
							//shaper.channelMapping[channel].Offset *= sensitivity;
							float amount = 100f/(channelVector[j] + shaper.channelMapping[channel].Offset/100f);
							//float s = Mathf.Clamp((amount-100f)/300f, 0f, 1f);
							//amount = amount * s * s * (3 - 2 * s);
							//amount = amount * s * s * s* (s*(6*s - 15)+10);
							
							shaper.channelMapping[channel].Scale = amount;
							Debug.Log ("recorded " + shape + " scale: " + shaper.channelMapping[shape].Scale 
							           + " offset : " + shaper.channelMapping[shape].Offset);
					}
				}
			}
		}
		
		public void ResetModule()
		{
			for(int i = 0; i < affectedBlendshapes.Length; i++)
			{
				string shape = affectedBlendshapes[i];
				shaper.channelMapping[shape].Scale = 100f;
				//currentlyEditing.channelMapping[shape].MaxInput = 1f;
			}
		}	
	}
	
	static GameObject cached_charOBJ;
	static FacePlusShaper shaper;
	GameObject charOBJ;
	bool isTracking;
	bool shouldTrack;
	
	//calibration properties
	bool baselineRecorded = false;
	CalibrationModule[] calibrationModules = 
		new CalibrationModule[]{
			new CalibrationModule("Eyes Closed", new string[]{"Mix::Blink_Left", "Mix::Blink_Right"}),
			new CalibrationModule("Eyes Wide", new string[]{"Mix::EyesWide_Left", "Mix::EyesWide_Right"}),
			new CalibrationModule("Brows Up", new string[]{"Mix::BrowsUp_Right", "Mix::BrowsUp_Left"}),
			new CalibrationModule("Brows Down (Furrowed)", new string[]{"Mix::BrowsDown_Right", "Mix::BrowsDown_Left"}),
			new CalibrationModule("Big Smile", new string[]{"Mix::Smile_Right","Mix::Smile_Left"}),
			new CalibrationModule("Mouth Wide Open", new string[]{"Mix::MouthOpen"}),
			new CalibrationModule("Mouth Left (( <----- ))", new string[]{"Mix::Midmouth_Left"}),
			new CalibrationModule("Mouth Right (( -----> ))", new string[]{"Mix::Midmouth_Right"}),
			new CalibrationModule("Nose Scrunch", new string[]{"Mix::NoseScrunch_Left", "Mix::NoseScrunch_Right"}),
			new CalibrationModule("Squint", new string[]{"Mix::Squint_Left", "Mix::Squint_Right"}),
			new CalibrationModule("Whistle", new string[]{"Mix::MouthNarrow_Left", "Mix::MouthNarrow_Right"})
		};
	
	Texture checkMark;
	

	public static void Init ()
	{
		// Get existing open window or if none, make a new one
		FacePlusCalibrationWindow window = (FacePlusCalibrationWindow)EditorWindow
					.GetWindow (typeof(FacePlusCalibrationWindow), false, "FacePlus Preset Calibration");
	}
	
	public void SetCharacter(GameObject char_)
	{
		charOBJ = char_;
	}
	
	void OnEnable()
	{
		checkMark =  Resources.Load("check-white") as Texture;
		if(!checkMark)
			Debug.Log ("did not load checkmark");

	}
	void OnGUI()
	{
		GUILayout.BeginHorizontal ();
		GUILayout.Label ("Select character to use for calibration:", GUILayout.Width (200));
		charOBJ = (GameObject) EditorGUILayout.ObjectField (charOBJ, 
							typeof(GameObject), true, GUILayout.ExpandWidth(true));
		GUILayout.EndHorizontal ();
		if(!charOBJ)
			return;
		
		shaper = charOBJ.GetComponent<FacePlusShaper>();
		if(charOBJ && (!shaper || !charOBJ.GetComponent<FacePlusConnector>())){
			GUILayout.Label("Character does not have FacePlusShaper and/or FacePlusConnector component!");
			return;
		}
		
		EditorGUILayout.Space ();
		//TODO: check if tracking is already happening and use the tracked character.
		if(isTracking)
			CaptureGUI();
		else
			OfflineGUI();

	}
	
	void OfflineGUI()
	{
		
		if(GUILayout.Button ("Start Calibrating!")){
			if(!EditorApplication.isPlaying)
			{
				EditorApplication.SaveCurrentSceneIfUserWantsTo(); 
				EditorApplication.isPlaying = true;
			}

			shouldTrack = true;
		}
	}
	void CaptureGUI()
	{
		GUI.skin.label.wordWrap = true;
		GUILayout.Label ("Make the following facial expressions and hit Set." +
			"\nFor best results, calibrate ALL the expressions first, then set neutral face." + 
			"\nFor best results, make sure your face is evenly lit with no backlight.");
		//GUILayout.Label ("The model will readjust to match your expression for better recording.");
		EditorGUILayout.Space ();
		
		for(int i = 0; i < calibrationModules.Length; i++)
		{
			GUILayout.BeginHorizontal();
			GUILayout.Label (calibrationModules[i].name, GUILayout.Width(150));
			if(calibrationModules[i].recorded)
			{
				if(GUILayout.Button("Reset To Default",  GUILayout.Width(160)))
				{
					//record channel
					calibrationModules[i].ResetModule();
					calibrationModules[i].recorded = false;
				}
				GUILayout.Label (checkMark);
			}
			else
			{
				if(GUILayout.Button("Set",  GUILayout.Width(160)))
				{
					//record channel
					calibrationModules[i].RecordModule();
					calibrationModules[i].recorded = true;
				}

			}
			GUILayout.EndHorizontal();
		}
		EditorGUILayout.Space ();
		Divider();
		GUILayout.Label ("After calibrating all the above shapes, set the character's neutral pose.");
		
		GUILayout.BeginHorizontal();
		GUILayout.Label ("Neutral Face", GUILayout.Width(150));
		if(baselineRecorded){
			if(GUILayout.Button("Reset to Default", GUILayout.Width (160)))
			{
				ResetBaseline();
				baselineRecorded = false;
			}
			GUILayout.Label (checkMark);
		}
		else{
			if(GUILayout.Button("Set", GUILayout.Width (160)))
			{
				SetBaseline();
				baselineRecorded = true;
			}
		}
		GUILayout.EndHorizontal();
		Divider();
		EditorGUILayout.Space ();
		EditorGUILayout.Space ();
		if (GUILayout.Button ("Save")) {
			string path = EditorUtility.SaveFilePanelInProject("New Face Plus Preset", "preset.txt", "txt", "Where would you like to save this?");
			shaper.SavePreset (path);
		}
	}
	
	void SetBaseline()
	{
		
		float[] channelVector = FacePlus.GetCurrentVector ();
		
		
		for(int i=0; i<channelVector.Length; i++) {
			string channel = FacePlus.GetChannelName(i);
			if(!channel.Contains ("Mix::"))
				continue;
			if (shaper.channelMapping.ContainsKey (channel)) {
				shaper.channelMapping[channel].Offset = -channelVector[i] * shaper.channelMapping[channel].Scale;
				//Debug.Log ("set baseline for " + channel + " as  " + channelVector[i]);
			}
		}
		
		
	}
	
	void ResetBaseline()
	{
		foreach (var pair in shaper.channelMapping)
		{
			// read current value from camera
			
			// set min for blendshapes only
			if (pair.Key.Contains("Mix::"))
			{
				float min = pair.Value.Amount;
				shaper.channelMapping[pair.Key].Offset = 0f;
				//currentlyEditing.channelMapping[pair.Key].MinInput = 0f;
				
			}
		}
	}
	
	void Update()
	{
		if(!EditorApplication.isPlaying && isTracking){
			isTracking = false;
			shouldTrack = false;
			baselineRecorded = false;
			Repaint();
		}
		
		if(EditorApplication.isPlaying && shouldTrack && !isTracking)
		{
			isTracking = true;
			Repaint();
		}
	}
	
	private void Divider (){
		GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1)); //Divider
	}


}
} //namespace MixamoEidtor
