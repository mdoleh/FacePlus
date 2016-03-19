using UnityEngine;
using UnityEditor;
using System.Collections;
using System;
using System.IO;
using Mixamo;

namespace MixamoEditor {
public class FacePlusEditorWindow : EditorWindow
{
	private enum Mode {
		Realtime = 0,
//#if UNITY_EDITOR_WIN
		FromVideo = 1
//#endif
	}

	private struct Options {
		public bool enabled;
		public bool showLogout;
		public Mode mode;
		public bool manualBlendShapes;
		public bool manualBones;
		public bool spacebarRecord;
		public bool warnBeforeDelete;
		public bool requiresConfirmation;
		public bool recordAudio;
		public bool keyframeReduction;
		public bool smoothTangents;
		public bool changeCamera;
		public bool cameraLost;
	}

	private string[] modes = new string[] {"Realtime", "Video File"};
	private string[] mics;
	string currentMic;
	int currentMicIndex;

	private string[] cams;
	string currentCam = null;
	int currentCamIndex;
	
	FaceCapture character;

	AnimationClip clip;
	string takeBaseName = "take";
	Texture record;
	Texture stop;
	Texture play;
	Texture stopPlaying;
	Texture recordButtonTexture;
	AudioSource audioSource;
	
	Options options;

	Vector2 scrollPosition = Vector2.zero;
	
	float timeSlider;
	string displayName {
		get { return Authentication.User == null? "" : Authentication.User.Email; }
	}
	float frameRate = 30f;
	string videoPath;
	bool overwrite = false;
	Color highlightColor = new Color(1.0f, 170.0f/255.0f, 0);
	
	[MenuItem ("Window/Mixamo Face Plus")]
	
	static void Init ()
	{
		// Get existing open window or if none, make a new one
		FacePlusEditorWindow window = (FacePlusEditorWindow)EditorWindow.GetWindow (typeof(FacePlusEditorWindow), false, "Face Plus");
	}
	
	void OnEnable ()
	{
		FacePlus.StartUp();

		if (Application.HasProLicense ()) { // TODO: report pro license required
			record = Resources.Load ("Record Button") as Texture;
			stop = Resources.Load ("Stop Button") as Texture;
			play = Resources.Load ("Play Button") as Texture;
			stopPlaying = Resources.Load ("Stop Button - Gray") as Texture;
		}
		
		
		options = new Options() {
			mode = (Mode)EditorPrefs.GetInt ("FacePlus.Options.Mode", (int)Mode.Realtime),
			recordAudio = EditorPrefs.GetBool ("FacePlus.Options.RecordAudio", true),
			spacebarRecord = EditorPrefs.GetBool ("FacePlus.Options.SpacebarRecord", true),
			warnBeforeDelete = EditorPrefs.GetBool ("FacePlus.Options.WarnBeforeDelete", true),
			requiresConfirmation = EditorPrefs.GetBool ("FacePlus.Options.RequiresConfirmation", true),
			keyframeReduction = EditorPrefs.GetBool ("FacePlus.Options.KeyframeReduction", false),
			smoothTangents = EditorPrefs.GetBool ("FacePlus.Options.SmoothTangents", false),
			changeCamera = EditorPrefs.GetBool ("FacePlus.Options.ChangeCamera", false), //flag to change capture device
			cameraLost = EditorPrefs.GetBool ("FacePlus.Options.CameraLost", false) //flag to change capture device
		};
		
		recordButtonTexture = record;
	}
	
	void OnDisable() {
		EditorPrefs.SetInt  ("FacePlus.Options.Mode", (int)options.mode);
		EditorPrefs.SetBool ("FacePlus.Options.SpacebarRecord", options.spacebarRecord);
		EditorPrefs.SetBool ("FacePlus.Options.WarnBeforeDelete", options.warnBeforeDelete);
		EditorPrefs.SetBool ("FacePlus.Options.RequiresConfirmation", options.requiresConfirmation);
		EditorPrefs.SetBool ("FacePlus.Options.KeyframeReduction", options.keyframeReduction);
		EditorPrefs.SetBool ("FacePlus.Options.SmoothTangents", options.smoothTangents);
	}

	void TestGUI() {
//		if (GUILayout.Button ("Test")) {
//			Logger.Info ("FacePlus editor connectivity test: " + FacePlus.Echo (123));
//		}
	}
	
	void OnPlaybackStopped() {
		Repaint ();
	}

	void Update() {
		if (needsRepainting) {
			Repaint ();
			needsRepainting = false;
		}

		if (needsLoginSaved) {
			EditorPrefs.SetString ("FacePlus.User", username);
			EditorPrefs.SetString ("FacePlus.Password", StringCipher.Encrypt (password, "Ips@F$ct0!Me3nyM03!")); // it's impolite to store unencrypted passwords
			needsLoginSaved = false;
		}
		
		if (needsLoginCleared) {
			EditorPrefs.DeleteKey ("FacePlus.Password");
			needsLoginCleared = false;
		}

		if (needsLoginStringCleared){
			_login_str = null;
			Repaint ();
			needsLoginStringCleared = false;
		}
		

		if (   !Authentication.IsLoggingIn
		 	&& !Authentication.IsAuthenticated
		    && EditorPrefs.HasKey ("FacePlus.User") 
		    && EditorPrefs.HasKey ("FacePlus.Password")) {
			username = EditorPrefs.GetString ("FacePlus.User");
			password = StringCipher.Decrypt (EditorPrefs.GetString ("FacePlus.Password"), "Ips@F$ct0!Me3nyM03!");
			DoLogin ();
		}

	}

	private string username = "Email Address";
	private string password = "Password";
	private string loginError = null;
	private string _login_str = null;
	private bool needsRepainting = false;
	private bool needsLoginSaved = false;
	private bool needsLoginCleared = false;
	private bool needsLoginStringCleared = false;

	void LoginWindow() {
		if (loginError != null) {
			GUILayout.BeginHorizontal ();  
			Color previousContent = GUI.contentColor;
			Color previousBackground = GUI.backgroundColor;
			GUI.contentColor = Color.red;
			GUI.backgroundColor = Color.white;
			GUILayout.FlexibleSpace ();
			EditorGUILayout.LabelField (loginError);
			GUILayout.FlexibleSpace();
			GUI.contentColor = previousContent;
			GUI.backgroundColor = previousBackground;
			GUILayout.EndHorizontal ();
		}
		if(_login_str !=null  && _login_str.Length>0 && !Authentication.CanUseFacePlus){
			EditorUtility.DisplayDialog("Mixamo Log In message",_login_str,"OK");
			_login_str = null;
		}//for error
		
		GUILayout.BeginVertical ();
		
		username = EditorGUILayout.TextField (username);
		password = EditorGUILayout.PasswordField (password);

		GUI.enabled = !Authentication.IsLoggingIn;
		if (GUILayout.Button("Log in with your Mixamo Account")) {
			DoLogin ();
		}
		GUI.enabled = true;

		Divider();
		LinkGUI ();
	
		TestGUI ();
		
		GUILayout.EndVertical ();
	}

	void DoLogin() {
		Authentication.Login (username, password, (string message)=> {
			Logger.Info ("Login Success");
			loginError = null;
			_login_str = message;
			needsRepainting = true; // can't call repaint from another thread
			needsLoginSaved = true;
		}, (string error,string log_in_error_message) => {
			Logger.Info ("Login Error: " + error);
			loginError = error;
			_login_str = log_in_error_message;
			needsRepainting = true;
			needsLoginCleared = true;
		});
	}

	void CalibrateGUI(){
		GUILayout.Label ("A Calibration Preset helps track your face better!");
		if(GUILayout.Button ("Create a Calibration Preset")){
			FacePlusCalibrationWindow.Init();
			if(character)
				GetWindow<FacePlusCalibrationWindow>().SetCharacter(character.gameObject);
		}
		EditorGUILayout.Space();
		Divider();	
	}
	void FuseGUI(){
		GUILayout.BeginHorizontal();
		GUILayout.Label ("NEW:", GUILayout.ExpandWidth(false)); 
		Color defaultCol = GUI.skin.label.normal.textColor;
		GUI.skin.label.normal.textColor = highlightColor;
		if (GUILayout.Button("Fuse", GUI.skin.label, GUILayout.ExpandWidth(false)))
		{
			Application.OpenURL("http://hubs.ly/y02VJ10");
		}
		GUI.skin.label.normal.textColor = defaultCol;
		GUILayout.Label ("characters are FacePlus compatible!", GUILayout.ExpandWidth(false));
		GUILayout.EndHorizontal();
		if(GUILayout.Button ("Configure Fuse Character")){
			FuseConfigWindow.Init();
		}
		EditorGUILayout.Space();
        Divider();
	}

	void LinkGUI() {
		EditorGUILayout.Space();
		if(GUILayout.Button("Go to Blendshapes Example Scene"))
				OpenDemoScene("Assets/Mixamo Face Plus/Examples/Example-BlendShape.unity");
			if(GUILayout.Button("Go to Fuse Example Scene"))
				OpenDemoScene("Assets/Mixamo Face Plus/Examples/Example-Fuse.unity");
			if(GUILayout.Button("Go to Joint-Based Example Scene"))
				OpenDemoScene("Assets/Mixamo Face Plus/Examples/Example-Joint.unity");
			EditorGUILayout.Space ();
			Divider ();
			EditorGUILayout.Space ();
		if (GUILayout.Button("Vist Face Plus Documentation Page"))
				Application.OpenURL("http://hubs.ly/y02V-70");
		if (GUILayout.Button("Need Characters or Animations?"))
				Application.OpenURL("http://hubs.ly/y02VJB0");
		if (GUILayout.Button("Feedback"))
				Application.OpenURL("http://hubs.ly/y02V-h0");
			//Application.OpenURL("http://community.mixamo.com/mixamo?view=overheard");
		EditorGUILayout.Space ();
	}

	void OptionsGUI() {
		options.enabled = EditorGUILayout.Foldout (options.enabled, "Options");
		if (options.enabled) {

			//Spacebar Play and Stop Option
			EditorGUILayout.BeginHorizontal ();
			GUILayout.Label ("  Use spacebar to begin and stop recording?");
			GUILayout.FlexibleSpace();
			options.spacebarRecord = EditorGUILayout.Toggle (options.spacebarRecord, GUILayout.Width (30));
			EditorGUILayout.EndHorizontal ();
			EditorGUILayout.Space ();
			
			
			//Ask for overwrite confermation Option
			EditorGUILayout.BeginHorizontal ();
			GUILayout.Label ("  Ask before overwriting a take?");
			GUILayout.FlexibleSpace();
			options.requiresConfirmation = EditorGUILayout.Toggle (options.requiresConfirmation, GUILayout.Width (30));
			EditorGUILayout.EndHorizontal ();
			EditorGUILayout.Space ();

			EditorGUILayout.BeginHorizontal ();
			GUILayout.Label ("  Use smooth tangents?");
			GUILayout.FlexibleSpace();
			Recording.ReduceKeyframes = options.keyframeReduction = EditorGUILayout.Toggle (options.keyframeReduction, GUILayout.Width (30));
			EditorGUILayout.EndHorizontal ();
			EditorGUILayout.Space ();

			EditorGUILayout.BeginHorizontal ();
			GUILayout.Label ("  Use keyframe reduction?");
			GUILayout.FlexibleSpace();
			Recording.SmoothTangents = options.smoothTangents = EditorGUILayout.Toggle (options.smoothTangents, GUILayout.Width (30));
			EditorGUILayout.EndHorizontal ();
			EditorGUILayout.Space ();

			Divider();
		}
	}

	void CameraSelectGUI(){ //handles camera selection
		bool changeFlag = false;
		EditorGUILayout.BeginHorizontal();
		GUILayout.Label ("Camera:", GUILayout.Width (100));
		
		//Camera selection dropdown
		int cameraCount = FacePlus.GetCameraCount ();
		cams = new string[cameraCount]; 
		for(int j =0; j < cameraCount ;j++){
			cams[j] = FacePlus.GetCameraDeviceName(j);
		}
		if(cams == null || cams.Length < 1) 
			cams = new string[]{"No Cameras detected"};
		if(currentCamIndex > cams.Length-1) 
				currentCamIndex = 0;
		
		//check for changes in status
		if (!string.IsNullOrEmpty(currentCam)){
			if(currentCam.CompareTo(cams[currentCamIndex])!=0){
				int j;
				for(j =0; j < cameraCount ;j++){
					if(cams[j].CompareTo(currentCam)==0){
						currentCamIndex=j;
						break;
					}
				}
				//camera no longer exists
				if(j==cameraCount)
					options.cameraLost = true;
			}
		}

		int index = EditorGUILayout.Popup (currentCamIndex, cams);
		currentCam = cams[index];
		if(index < cams.Length+1){
			//if the camera has changed or been lost
			if ((index != currentCamIndex)||options.cameraLost){
				options.cameraLost = false;
				options.changeCamera = true;
				Debug.Log("New Camera Device Selected.");
			}
			currentCamIndex = index;  
		}
		FacePlus.DeviceID = index;
		EditorGUILayout.EndHorizontal();
	}

	void RealtimeCaptureGUI() {
		EditorGUILayout.BeginHorizontal ();
		GUILayout.Label ("Microphone:", GUILayout.Width (100));
		//Microphone selection dropdown
		string[] devices = Microphone.devices;
		mics = new string[devices.Length + 1];
		mics[0] = "Disabled";
		for(int i = 0; i < devices.Length; i++){
			mics[i+1] = devices[i];
		}
		if(mics == null || mics.Length <=1) mics = new string[]{"No microphones detected"};
		if(EditorPrefs.HasKey ("FacePlus.Microphone")){
			currentMicIndex = EditorPrefs.GetInt ("FacePlus.Microphone");
		}
		if(currentMicIndex > mics.Length-1) currentMicIndex = 0;
		int index = EditorGUILayout.Popup (currentMicIndex, mics);
		if(index < mics.Length){
			currentMic = mics[index];
			currentMicIndex = index;
		}
		EditorPrefs.SetInt ("FacePlus.Microphone", currentMicIndex);
		if(currentMic == "No microphones detected" || currentMic == "Disabled") options.recordAudio = false;
		else options.recordAudio = true;
		
		EditorGUILayout.EndHorizontal();

		//camera selection GUI
		CameraSelectGUI ();

		//  track
		EditorGUILayout.BeginHorizontal(GUILayout.ExpandWidth (true));

		// record or stop recording
		if (character.State == CaptureState.Recording) {
			if (GUILayout.Button (stop, GUILayout.Width (60)) || SpacePressed ()) {
				if(options.recordAudio){
					Microphone.End(currentMic);
				}
				
				character.StopRecording (()=> {
					SaveClip ();
					SaveAudioClip ();
					Repaint (); 
				});
			}
		} else {
			GUI.enabled = character.CanRecord;
			if (GUILayout.Button (record, GUILayout.Width (60)) || SpacePressed ()) {
				if (!character.Live) {
					character.StartLiveTracking();
				}
				if(clip !=null && clip.length > 1.00f && options.requiresConfirmation){
					overwrite = EditorUtility.DisplayDialog ("This take will be overwritten!", "You can create a new take to avoid overwriting.", 
					                                         "Record anyway", "Cancel");
					if(!overwrite){
						return;
					}
					overwrite = false;
				}
				character.OnStartRecording += StartRecordAudio; //add audiorecord to event so it doesn't go too early
				character.StartRecording();
				Repaint ();
			}
		}
		
		if (character.State == CaptureState.Playing) {
			if (GUILayout.Button (stopPlaying, GUILayout.Width (60))) {
				character.StopPlayback ();
					PlayBackAudio (false);
				Repaint ();
			}
		} else {
			GUI.enabled = character.CanPlay;
			if (GUILayout.Button (play, GUILayout.Width (60))) {
				character.StartPlayback ();
					PlayBackAudio (true);
				Repaint ();
			}
		}

		if (options.changeCamera) {
			options.changeCamera = false;
			if(character.State == CaptureState.Live) 
					FacePlus.StopTracking();
			character.StartLiveTracking();
		}

		if (character != null && character.State == CaptureState.BakingAnimation) {
			ProgressBar (character.Progress, "Baking Animation");
		} else {
			TimeSliderGUI ();
		}

		GUI.enabled = true;

		EditorGUILayout.EndHorizontal();

		EditorGUILayout.BeginHorizontal ();
		GUILayout.FlexibleSpace ();

		//	GUILayout.Label ("Record Audio:");
		//	options.recordAudio = EditorGUILayout.Toggle (options.recordAudio, GUILayout.Width (15));
		if (character != null) {
			if (options.mode == Mode.Realtime && !character.Live && Authentication.CanUseFacePlus) {
					Logger.Info ("start live tracking - toggled checkbox");
					if (character.Movie != null)
					 	character.Movie.Dispose ();
					character.StartLiveTracking ();
					Logger.Info ("done calling startlivetracking");
			} 
		}
		EditorGUILayout.EndHorizontal ();

		if (GUILayout.Button("Check Minimum Requirements")){
				Application.OpenURL("http://hubs.ly/y02VLJ0");
		}		
	}

	void OfflineCaptureGUI() {
		
		/*EditorGUILayout.BeginHorizontal(); //Titles Grouping Start
		EditorGUILayout.LabelField("Animate from imported movie", EditorStyles.boldLabel);
		EditorGUILayout.EndHorizontal(); // Titles Grouping End
		*/
		
		EditorGUILayout.BeginHorizontal(); // Video Select Grouping Start
		GUILayout.Label("Video:", GUILayout.Width(100));
		if (GUILayout.Button("Browse...", GUILayout.Width (70))){
			videoPath = EditorUtility.OpenFilePanel("Select your Video", "", "");
		}
		GUILayout.Label(Path.GetFileName (videoPath)); // TODO: Clip the path!
		EditorGUILayout.EndHorizontal(); // Video Select Grouping End

		EditorGUILayout.BeginHorizontal();
		GUILayout.Label ("Frame Rate: ", GUILayout.Width (100));
		frameRate = EditorGUILayout.FloatField(frameRate, GUILayout.Width (35));
		EditorGUILayout.EndHorizontal();


		EditorGUILayout.BeginHorizontal();


		if (character.State == CaptureState.RecordingFromVideo) {
			
			if (GUILayout.Button (stop, GUILayout.Width (60)) || SpacePressed () || character.videoFinished) { //if video finishes
				character.StopRecordingFromVideo (() => {
					character.videoFinished =false;
					SaveClip ();
					Repaint ();
				});	
			}
		} else {
			GUI.enabled = character.CanRecord;
			if (GUILayout.Button (record, GUILayout.Width (60)) || SpacePressed ()) {
					if(clip !=null && clip.length >0f && options.requiresConfirmation){
						Logger.Info ("overwriting");

						overwrite = EditorUtility.DisplayDialog ("This take will be overwritten!", "You can create a new take to avoid overwriting", 
						                                         "Record anyway", "Cancel");
						if(!overwrite){
							return;
						}
						overwrite = false;
					}
					if (!string.IsNullOrEmpty(videoPath)) {
					var videoFolder = Path.GetDirectoryName(videoPath);
					var videoFilename = Path.GetFileName (videoPath);
					
					character.SetImportMovie(videoFolder, videoFilename);
				} else { // TODO: error about no file selected
				}
				Repaint ();
			}
		}
		
		if (character.State == CaptureState.PlayingWithVideo) {
			if (GUILayout.Button (stopPlaying, GUILayout.Width (60))) {
				character.StopPlaybackWithVideo ();
				Repaint ();
			}
		} else {
			GUI.enabled = character.CanPlayWithVideo;
			if (GUILayout.Button (play, GUILayout.Width (60))) {
				character.StartPlaybackWithVideo();
				Repaint ();
			}
		}


		if (character != null && character.State == CaptureState.RecordingFromVideo) {
			GUI.enabled = true;
			ProgressBar(character.Progress, "Processing Video: " + character.CurrentVideoFrame + " / " + character.TotalVideoFrames);
		} else if (character != null && character.State == CaptureState.BakingAnimation) {
			GUI.enabled = true;
			ProgressBar (character.Progress, "Baking Animation");
		} else { 
			TimeSliderGUI ();
		}
		EditorGUILayout.EndHorizontal();

		EditorGUILayout.Space();
		GUI.enabled = true;
	}

	void TimeSliderGUI() {

		GUILayout.BeginVertical ();
		timeSlider = character.Progress;
		timeSlider = GUILayout.HorizontalSlider (timeSlider, 0.0f, 1.0f); // 0 - 1 so you can force normalized playback across the timeline
		GUILayout.BeginHorizontal ();
		GUILayout.FlexibleSpace ();
		var tmp = GUI.enabled;
		GUI.enabled = true;
		GUILayout.Label (string.Format ("Time: {0,5:##0.0} / {1,2:0.0}", 
		                                character.ClipPosition,
		                                character.ClipLength));
		GUI.enabled = tmp;
		GUILayout.EndHorizontal ();
		GUILayout.EndVertical ();
	}
	
	void FacePlusErrorGUI(){
		Color previousContent = GUI.contentColor;
		EditorGUILayout.BeginHorizontal ();
		GUI.contentColor = Color.red;
		GUILayout.Label ("Faceplus Error: " + FacePlus.GetError ());
		EditorGUILayout.EndHorizontal();
		GUI.contentColor = previousContent;
	}
	
	void FacePlusMessageGUI(){
		GUILayout.BeginHorizontal ();
		GUIStyle style = new GUIStyle(EditorStyles.label);
		style.wordWrap = true;
		style.padding = new RectOffset(5,5,5,5);
		GUILayout.Label(_login_str,  style);
		if(GUILayout.Button ("Dismiss")){
			_login_str = null;
		}
		GUILayout.EndHorizontal();
		Divider ();
		EditorGUILayout.Space ();
	}

	void MainGUI() {
		EditorGUILayout.BeginHorizontal();
		GUILayout.FlexibleSpace();
		
		if (GUILayout.Button(displayName, EditorStyles.miniButton)) {
			Authentication.Logout();
			_login_str = null;
			EditorPrefs.DeleteKey ("FacePlus.Password");
			password = "";
			if(character.State == CaptureState.Live) 
					character.StopLiveTracking();
			needsRepainting = true;
		}
		
		EditorGUILayout.EndHorizontal();
		
		Divider();
		EditorGUILayout.Space();
		//Errors
		if(FacePlus.HasError && FacePlus.GetError() != ""){
				FacePlusErrorGUI();
		}
		//Messages
		if (_login_str != null && _login_str != "") 
		{
				FacePlusMessageGUI();
		}
		//Takes
		GUILayout.BeginHorizontal ();
		GUILayout.Label ("Current Take:", GUILayout.Width (100));
		var newClip = (AnimationClip) EditorGUILayout.ObjectField (clip, typeof(AnimationClip));
		GUILayout.EndHorizontal ();
		
		// load a clip if the clip changes
		if (newClip != clip && newClip != null) {
			clip = newClip;
			if (character != null && character.channelMapping != null) {
				character.Load (clip);
			}
		}

		
		// load a clip if we don't have one already
		if (character != null && clip != null && !character.HasClip && character.channelMapping != null) {
			character.Load (clip);
		}
		
		EditorGUILayout.BeginHorizontal ();
		GUILayout.Label ("", GUILayout.Width (100));
		if (GUILayout.Button ("Create New")) {
			clip = CreateClip ();
		}
		
		// Disable following buttons when there's no clip.
		GUI.enabled = clip != null;
		if (GUILayout.Button ("Find in Assets")) {
			//Find the clip within the Project window in Unity.
			EditorGUIUtility.PingObject (clip);
		}
		
		GUI.enabled = true;
		
		EditorGUILayout.EndHorizontal ();
		
		GUILayout.EndVertical();
		
		EditorGUILayout.BeginVertical(); //Super V
		EditorGUILayout.Space ();
		
		if (!EditorApplication.isPlaying) {
			needsLoginStringCleared = true;
			if (GUILayout.Button ("Start Scene")) {
				EditorApplication.isPlaying = true;
				Repaint ();
			}
			EditorGUILayout.Space ();
		} else if (character == null) {
			GUILayout.Label ("Please attach a FaceCapture component to your Character.");
		} else {
			Divider();
			
			// Mode Selection Dropdown
			EditorGUILayout.Space ();
			EditorGUILayout.BeginHorizontal();
			
			GUILayout.Label ("Capture Mode:", GUILayout.Width (100));
			int newMode = EditorGUILayout.Popup ((int)options.mode, modes);
			if ((Mode)newMode != options.mode) {
				// TODO: do cleanup when switching modes
				if (newMode != (int)Mode.Realtime)
					character.StopLiveTracking ();
				options.mode = (Mode)newMode;
			}
			EditorGUILayout.EndHorizontal ();
			
			if (options.mode == Mode.Realtime) {
				/*if(character.State != CaptureState.Live){
						character.Movie.Dispose();
					}*/
				RealtimeCaptureGUI();
			} else {
				OfflineCaptureGUI ();
			}
			EditorGUILayout.Space ();
		}
		
		//Divider();
		OptionsGUI ();
		EditorGUILayout.Space ();
		Divider();
		CalibrateGUI();
		FuseGUI();
		
		LinkGUI ();

	}
    
	void OnGUI ()
	{	
		autoRepaintOnSceneChange = true;
	
		
		var c = FaceCapture.Instance;
		if (character == null && c != null && Authentication.CanUseFacePlus) {
			if (options.mode == Mode.Realtime){
					Logger.Info ("Starting tracking...");
					c.StartLiveTracking ();
				}
		}
		character = c;
		

		if (character) {
			character.OnStopPlayback -= OnPlaybackStopped;
			character.OnStopPlayback += OnPlaybackStopped;
		}
		
		EditorGUILayout.Space ();
		GUILayout.BeginVertical (); // 1
		
		if (!Authentication.CanUseFacePlus) {
			LoginWindow();
		} else {
			MainGUI ();
		}

		EditorGUILayout.EndVertical ();
	}

	private AnimationClip CreateClip() {
		string path = EditorUtility.SaveFilePanelInProject("New Animation Clip",
		                                                   FirstAvailableClipName(), "anim", "Where would you like to save this?");

		Logger.Info ("Got this path to create clip in: " + path);

		if (!string.IsNullOrEmpty(path)) {
			AnimationClip emptyClip = new AnimationClip();
				
			#if !UNITY_4_2
			AnimationUtility.SetAnimationType (emptyClip, ModelImporterAnimationType.Generic);
			#endif
				
			AssetDatabase.CreateAsset (emptyClip, path);
			AssetDatabase.Refresh ();

			Logger.Info ("Clip is null? " + (emptyClip==null));
			return emptyClip;
		}

		return null;
	}

	private void SaveClip() {
		if (clip == null) clip = CreateClip();
		character.SaveToClip (clip);
	}

	private bool SpacePressed() {
		Event e = Event.current;

		if (e.type == EventType.keyDown) {
			if (options.spacebarRecord && Event.current.keyCode == (KeyCode.Space)) {
				return true;
			}
		}
		
		if (options.spacebarRecord && character.spacePressed) {
			character.spacePressed = false;
			return true;
		}

		return false;
	}
	
	private void ProgressBar (float value, string label) {		
		Rect rect = GUILayoutUtility.GetRect (200, 30, "TextField");
		EditorGUI.ProgressBar (rect, value, label);
	}

	private void Divider (){
		GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1)); //Divider
	}
	
	private string FirstAvailableClipName() {
		int takeNumber = 1;
		string basePath = "Assets/";
		
		string clipName;
		do {
			clipName = string.Format ("{0}-{1,3:D3}.anim", takeBaseName, takeNumber);
			takeNumber++;
		} while( System.IO.File.Exists (basePath + clipName) );
		
		return clipName;
	}

	private AudioSource GetOrCreateAudioSource(){
			AudioSource audioSource = new AudioSource();
			audioSource = FaceCapture.Instance.gameObject.GetComponent<AudioSource>();
			if(audioSource == null) audioSource = FaceCapture.Instance.gameObject.AddComponent<AudioSource>();
			return audioSource;
	}
	private void PlayBackAudio(bool play){
			AudioSource a = FaceCapture.Instance.gameObject.GetComponent<AudioSource>();
			if(a != null){
				if(play)a.Play ();
				else a.Stop ();
			}
	}


	private void SaveAudioClip(){
			if(audioSource == null) return;
			if(audioSource.clip == null) return;
			string path = AssetDatabase.GetAssetPath (clip);
			path = path.Substring (0,path.LastIndexOf('.'));
			Logger.Info ("Saving audio to path: " + path);
			Debug.Log (clip.length);
			audioSource.clip = SavWav.TrimSilence (audioSource.clip, 0.0f);
			SavWav.Save(path,audioSource.clip);
			AssetDatabase.Refresh ();
	}

	private void OpenDemoScene(string level){

			EditorApplication.isPlaying = false;
			EditorApplication.SaveCurrentSceneIfUserWantsTo(); 
			Debug.Log ("opening new scene");
			EditorApplication.OpenScene (level);
			EditorApplication.isPlaying = true;
	}

	private void StartRecordAudio(){
			Debug.Log ("started recording audio");
			if(options.recordAudio){
				audioSource = GetOrCreateAudioSource();
				audioSource.clip = new AudioClip();
				audioSource.clip = Microphone.Start (currentMic, false, 1000,44100);
			}else{
				//remove audio clip;
				DestroyImmediate(audioSource);
			}
			character.OnStartRecording -= StartRecordAudio;
	}
}
}
