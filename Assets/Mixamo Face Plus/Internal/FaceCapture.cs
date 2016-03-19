using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;
using Mixamo;

namespace Mixamo {

public enum CaptureState {
	None,
	Live,
	Recording,
	Playing,
	RecordingFromVideo,
	PlayingWithVideo,
	BakingAnimation
}

public class FaceCapture : MonoBehaviour {
	// static interface
	public static FaceCapture Instance;

	// unity interface
	public string CommonPrefix = "";
	public bool ShowGUI = false;
	public bool OutputDebugImages = false;

	// public interface, hidden from unity
	[HideInInspector]
	public int CurrentVideoFrame = 0;

	[HideInInspector]
	public bool Live = false;

	[HideInInspector]
	public Dictionary<string, AnimationTarget> channelMapping;

	[HideInInspector]
	public bool spacePressed = false;
	
	[HideInInspector]
	public bool videoFinished = false; 
	// public dependency injection interface
	public IFacePlusVideo Movie;
	public IChannelMapper Mapper;

	// properties
	public CaptureState State {
		get { return state; }
	}
	
	public bool CanRecord {
		get { return state != CaptureState.BakingAnimation; }
	}
	
	public bool CanPlay {
		get { 
			return HasClip
				&& state != CaptureState.Recording
				&& state != CaptureState.BakingAnimation; 
		}
	}

	public bool CanPlayWithVideo {
		get {
			return HasClip
				&& state != CaptureState.RecordingFromVideo
				&& state != CaptureState.BakingAnimation;
		}
	}

	public bool HasClip {
		get {
			return currentTake != null;
		}
	}

	public bool HasMovie {
		get {
			return Movie != null;
		}
	}

	public float ClipLength {
		get {


			if (State == CaptureState.RecordingFromVideo && HasMovie) {
				return TotalVideoLength;
			} else if (currentTake != null) {
				return currentTake.Length;
			} else {
				return 0f;
			}
		}
	}

	public float ClipPosition {
		get {
			switch(State) {
			case CaptureState.Playing:
			case CaptureState.PlayingWithVideo:
			case CaptureState.Recording:
				return t;
			case CaptureState.RecordingFromVideo:
				return CurrentVideoTime;
			default:
				return 0f;
			}
		}
	}

	public int TotalVideoFrames {
		get {
			if (Movie != null) 
				return (int)Movie.DurationFrames;
			else
				return 0;
		}
	}
	
	public float CurrentVideoTime {
		get {
			return HasMovie ? Progress * TotalVideoLength
				: 0f;
		}
	}
	
	public float TotalVideoLength {
		get {
			return HasMovie ? Movie.DurationSeconds
				: 0f;
		}
	}
	
	
	public float Progress {
		get {
			float p = 0f;
			switch(State) {
			case CaptureState.RecordingFromVideo:
				if (Movie != null) 
					p = (1f*CurrentVideoFrame) / TotalVideoFrames;
				break;
			case CaptureState.PlayingWithVideo:
			case CaptureState.Playing:
				if (currentTake != null && currentTake.Length > 0f) 
					p = t/currentTake.Length;
				break;
			case CaptureState.BakingAnimation:
				p = currentTake.BakeProgress;
				break;
			default:
				break;
			}
			
			return p;
		}
	}

	// events
	public event Action OnStartRecording;
	public event Action OnStopRecording;
	public event Action OnStartPlayback;
	public event Action OnStopPlayback;
	public event Action OnChannelsUpdated;

	// private variables
	private Recording currentTake;
	private CaptureState state = CaptureState.None;
	private float t = 0f;
	private static Texture2D frameTexture;
	private static byte[] frameBuffer;
	
	void Start () { 

		Live = false;

		AnimationTarget.CommonPrefix = CommonPrefix;
		AnimationTarget.BasePath = transform.GetPath ();
		
		if (Mapper != null) channelMapping = Mapper.CreateMap ();

		Instance = this;
	}

	public void StartLiveTracking(){
		//Debug.Log ("Start Live Tracking");
		Logger.Log ("FacePlus connectivity: " + (FacePlus.Echo (123) == 123 ? "Pass" : "FAIL"));
		Logger.Log ("FacePlus initializing...");
		//Logger.Log ("Initializing with: " + "VGA@CAM" + FacePlus.DeviceID.ToString ());

		FacePlus.Init ("VGA@CAM"+FacePlus.DeviceID.ToString());
		Live = true;
		
		float startTime = Time.time;
		StartCoroutine (FacePlus.AfterInit((bool success) => {
			float timePassed = Time.time - startTime;
			Logger.Info ("FacePlus completed initialization.");
			Logger.Log ("FacePlus initialized (success: "+success+") in " + timePassed + "s");
			
			if (success) {
				Live = true;
				state = CaptureState.Live;
				Logger.Debug ("starting tracking thread");
				FacePlus.TrackForeverThreaded();
				Logger.Debug ("done starting tracking thread");
				
			} else {
				Live = false;
			}
		}));
	}

	public void StopLiveTracking() {
		Logger.Info ("Stopping live tracking...");
		FacePlus.StopTracking ();
		Live = false;
		FacePlus.Teardown ();
	}

	void LateUpdate () {
		if (channelMapping == null 
			&& Mapper != null 
			&& FacePlus.IsInitComplete){
			Logger.Log ("Mapping Channels to Targets...");
			channelMapping = Mapper.CreateMap ();
		}

		t += Time.deltaTime;
		switch (state) {
		case CaptureState.Playing:
			EvaluateFrame (t);
			if (t > currentTake.Length) StopPlayback ();
			break;
		case CaptureState.Recording:
			UpdateChannels();
			CaptureFrame (t);
			break;
		case CaptureState.Live:
			
			if (FacePlus.IsTracking) UpdateChannels();
			break;
		case CaptureState.PlayingWithVideo:
			if (HasMovie) {
				t = Movie.PositionSeconds;
			}
			EvaluateFrame (t);
			if (t > currentTake.Length) StopPlaybackWithVideo ();
			break;
		case CaptureState.RecordingFromVideo:
			UpdateChannels();
			break;
		}
	}

	public void SetImportMovie(string folder, string filename){
		if (Live) StopLiveTracking();
		
		if (!FacePlus.IsInitStarted) {
			Logger.Info ("FacePlus initializing (video)...");
			FacePlus.Init ("VGA");
			float startTime = Time.time;
			StartCoroutine(FacePlus.AfterInit ((success) => {
				float timePassed = Time.time - startTime;
				Logger.Log ("FacePlus initialized (success: "+success+") in " + timePassed + "s");
				Logger.Log ("Setting import movie from " + folder + "/" + filename);
				StartCoroutine(SetImportMovieCoroutine(folder, filename));
			}));
		} else {
			Logger.Log ("Setting import movie from " + folder + "/" + filename);
			StartCoroutine(SetImportMovieCoroutine(folder, filename));
		}
	}
	
	IEnumerator SetImportMovieCoroutine(string folder, string filename){
		if (Movie == null) {
			Debug.LogError ("You must attach a FaceVideoAVProWMV component to your camera.");
			yield break;
		}
		
		bool loadMovieSuccess = Movie.LoadMovie(folder, filename, false);

		if(!loadMovieSuccess){
			Debug.LogError ("LoadMovie failed. Make sure the codec you're using is supported.");
			yield break;
		}
		
		while(Movie.OutputTexture == null){
			yield return 0;
		}

		Movie.PositionFrames = 0;

		int width = Movie.OutputTexture.width;
		int height = Movie.OutputTexture.height;
		
		frameTexture = new Texture2D(width, height, TextureFormat.ARGB32, false);
		
		Logger.Log ("FacePlus initializing...");
		FacePlus.InitBufferTracker (width, height, Movie.FrameRate);
		
		frameBuffer = new byte[3*width*height];

		StartRecordingFromVideo();
	}

#if UNITY_EDITOR
	public void Load(AnimationClip c) {
		currentTake = new Recording();
		foreach(var target in channelMapping.Values) {
			currentTake.SetMetadata(target.Name, target);
		}
		StartCoroutine(currentTake.Load (c));
	}
	
	public void SaveToClip(AnimationClip clip) {
		currentTake.SaveToClip (clip);
		
	}
#endif
	public void Save(string location) {
		currentTake.Save (location);
	}
	
	public void StartRecording() {
		Logger.Debug ("Channel mapping is null: " + (channelMapping == null));
		state = CaptureState.Recording;
		currentTake = new Recording();
		t = 0f;
		
		if (OnStartRecording != null) OnStartRecording();
	}
	
	public void StopRecording(Action callback) {
		Logger.Debug ("Channel mapping: " + (channelMapping == null));
		if (channelMapping == null) return; 
		
		foreach(var target in channelMapping.Values) {
			currentTake.SetMetadata(target.Name, target);
		}
#if UNITY_EDITOR		
		StartCoroutine(Bake (()=>{
			if (callback != null) callback();
			state = CaptureState.Live;
		}));
#endif
	}
#if UNITY_EDITOR
	IEnumerator Bake(Action callback) {
		state = CaptureState.BakingAnimation;
		yield return StartCoroutine(currentTake.Bake ());
		Logger.Info ("Finished recording of length: " + (currentTake.BakeTime - currentTake.CreationTime));
		Logger.Info ("Number of channels: " + currentTake.ChannelCount);
		Logger.Info ("Number of frames: " + currentTake.GetChannel(currentTake.ChannelNames.First()).keyframes.Count);
		if (callback != null) callback();
		if (OnStopRecording != null) OnStopRecording();
	}

#endif

	public void StartPlayback() {
		t = 0f;
		state = CaptureState.Playing;
		
		if (OnStartPlayback != null) OnStartPlayback();
	}
	
	public void StopPlayback() {
		state = CaptureState.Live;
		
		if (OnStopPlayback != null) OnStopPlayback();
	}
	
	void EvaluateFrame(float t) {
		foreach(var target in channelMapping.Values) {
			target.AmountAnim = currentTake.Evaluate (target.Name, t);
		}
	}
	
	void CaptureFrame(float t) {
		foreach(var target in channelMapping.Values) {
			currentTake.AddKeyframe(target.Name, t, target.AmountAnim);
		}
	}

	void UpdateChannels() {		
		if (!FacePlus.IsInitSuccessful) return;
		if (!FacePlus.IsTracking) return;
		#if !UNITY_4_2 && UNITY_EDITOR
		if (UnityEditor.AnimationUtility.InAnimationMode()) return;
		#endif
		
		float[] channelVector = FacePlus.GetCurrentVector ();
		var doneKeys = new List<string>();

		for(int i=0; i<channelVector.Length; i++) {
			string channel = FacePlus.GetChannelName(i);
			if (channelMapping.ContainsKey (channel)) {
				doneKeys.Add(channel);
				float amount = channelMapping[channel].Offset + (channelMapping[channel].Scale * channelVector[i]);
				/*if(channel.Contains ("Mix::"))
				{
						float s = Mathf.Clamp((amount+ channelMapping[channel].Offset)/100f, 0f, 1f);
						amount = amount * s * s * (3 - 2 * s);
						//amount = amount * s * s * s* (s*(6*s - 15)+10);
				}*/
				channelMapping[channel].Amount = amount;
			}
		}
		foreach (var shape in channelMapping.Keys) {
			if(! doneKeys.Contains (shape)){
				channelMapping[shape].Amount = channelMapping[shape].Offset;
			}
		}

		if (OnChannelsUpdated != null) OnChannelsUpdated();
	}

	public void StartRecordingFromVideo() {
		state = CaptureState.RecordingFromVideo;
		currentTake = new Recording();
		
		StartCoroutine(RecordFromVideo());
		
		if (OnStartRecording != null) OnStartRecording();
	}
	
	public void StopRecordingFromVideo(Action callback) {
		if (channelMapping == null) {
			Debug.LogError ("Channel Mapping is null."); 
			return; 
		}
		
		foreach(var target in channelMapping.Values) {
			currentTake.SetMetadata(target.Name, target);
		}
		
		Movie.Pause ();
#if UNITY_EDITOR
		StartCoroutine(Bake(() => {
			if (callback != null) callback();
			state = CaptureState.None;
			Movie.Rewind ();
		}));
#endif
	}

	public void StartPlaybackWithVideo() {
		state = CaptureState.PlayingWithVideo;

		if(Movie == null) return;

		Movie.Play();
		Movie.PositionSeconds = 0;		
	
		if (OnStartPlayback != null) OnStartPlayback();
	}
	
	public void StopPlaybackWithVideo() {
		state = CaptureState.None;
		
		if(Movie != null){
			Movie.Pause();
			Movie.PositionSeconds = 0;
		}
		
		if (OnStopPlayback != null) OnStopPlayback();
	}
	
	// TODO: move to IFacePlusVideo?
	IEnumerator Seek(IFacePlusVideo moviePlayer, uint frame) {
		moviePlayer.PositionFrames = frame;
		while (moviePlayer.DisplayFrame != frame) {
			moviePlayer.UpdateMovie(false);
			yield return null;
		}
	}

	IEnumerator SeekNextFrame(IFacePlusVideo moviePlayer, int lastFrame)
	{
		while (Movie.DisplayFrame <= lastFrame)
		{
			Movie.UpdateMovie (false);
			yield return null;
		}
	}

	
	IEnumerator RecordFromVideo(){

		Logger.Info ("Recording, DurationFrames=" + Movie.DurationFrames);
#if UNITY_EDITOR_OSX
			Movie.Pause ();
			Movie.Rewind (); //plugin seems to skip movie ahead a few frames at startup. this reverses that. 
			int currentFrameCount = Movie.DisplayFrame;
			while(Movie.SeekToNextFrame())
			{
				if (   state == CaptureState.None 
				    || state == CaptureState.BakingAnimation) yield break; // exit early if interrupted


				currentFrameCount = Movie.DisplayFrame;
				int frame = currentFrameCount;
				CurrentVideoFrame = currentFrameCount;
				UpdateFrameTexture();
				if (UpdateFrameBuffer()) {
					UpdateChannels();
				}
				Debug.Log ("Capturing frame... " + currentFrameCount);
				CaptureFrame ((frame-1) * (1f/Movie.FrameRate));

				yield return StartCoroutine(SeekNextFrame(Movie, currentFrameCount));
			}
#else
		yield return StartCoroutine (Seek (Movie, 1));
		for(uint frame = 1; frame < Movie.DurationFrames ; frame++){
			Debug.Log("on frame " + frame + " of " + Movie.DurationFrames);
			if (   state == CaptureState.None 
			    || state == CaptureState.BakingAnimation) yield break; // exit early if interrupted
			CurrentVideoFrame = (int)frame;

			yield return StartCoroutine (Seek (Movie, frame));

			UpdateFrameTexture();
			if (UpdateFrameBuffer()) {
				UpdateChannels();
			}
			Debug.Log ("Capturing frame... " + (frame-1) * (1f/Movie.FrameRate));
			CaptureFrame ((frame-1) * (1f/Movie.FrameRate));
		}
#endif		
		videoFinished = true;
		//StopRecordingFromVideo(null);

	}
	
	int GetUnityIndex(int x, int y, int width, int height) {
		return x+y*width;
	}
	
	int GetBufferIndex(int x, int y, int width, int height) {
		//return 3*(x+y*width);
		return 3*((height-1-y)*width+x);
	}
	
	void UpdateFrameTexture(){
		RenderTexture stream = (RenderTexture)Movie.OutputTexture;
		if (stream)
		{
			RenderTexture.active = stream;
			frameTexture.ReadPixels(new Rect(0,0,stream.width, stream.height), 0, 0, false);
			RenderTexture.active = null;
		}
		else{
			Debug.LogError ("stream is null");
		}
	}
	
	bool UpdateFrameBuffer(){
		if (!FacePlus.IsInitSuccessful) { 
			return false; 
		}
		
		Color32[] colors = frameTexture.GetPixels32();
		byte[] frameBuffer = new byte[3*frameTexture.width*frameTexture.height];
		
		if (colors.Length*3 != frameBuffer.Length) {
			Debug.LogError ("Frame buffer size mismatch.");
			return false;
		}
		
		int pixelX = 320;
		int pixelY = 240;
		
		
		int width = frameTexture.width;
		int height = frameTexture.height;
		// Logger.Info ("Random sample: " + colors[GetUnityIndex(pixelX, pixelY, width, height)].r);
		for(int x=0; x<width; x++) {
			for(int y=0; y<height; y++) {
				int unityIndex = GetUnityIndex(x, y, width, height);
				int bufferIndex = GetBufferIndex (x, y, width, height);

				frameBuffer[bufferIndex] = colors[unityIndex].r;
				frameBuffer[bufferIndex+1] = colors[unityIndex].g;
				frameBuffer[bufferIndex+2] = colors[unityIndex].b;
			}
		}
		
		// Logger.Debug ("Frame buffer sample: " + frameBuffer[GetBufferIndex(pixelX, pixelY, width, height)]);

		var result = FacePlus.TrackSynchBuffer (frameBuffer, OutputDebugImages);
		return result;
		
	}
	

	void OnDisable() {
		Logger.Debug ("Disabling. Stopping tracking...");
		FacePlus.StopTracking (); // stop the tracking thread
		FacePlus.Teardown (); // relinquish control of the camera
	}
	
	void OnDrawGizmos() {
		if (!ShowGUI) return;
		Gizmos.color = FacePlus.IsTracking ? Color.green : Color.red;
		Gizmos.DrawSphere (new Vector3(0f, 1.5f, 0f), 0.25f);
	}
	
	void OnGUI() {
					
		if (   Event.current.type == EventType.KeyDown 
		    && Event.current.keyCode == KeyCode.Space) {
			// Detect spacebar input because the GUI one works only when the gui window is in focus.
			spacePressed = !spacePressed;
			Event.current.Use ();
		}
	
		if (!ShowGUI) return;
		
		Rect windowRect = new Rect(0f, 0f, 400f, Screen.height - 60f);
		GUILayout.Window (0, windowRect, CreateWindow, "Expression");
		if(GUI.Button (new Rect(Screen.width - 100, Screen.height - 24, 100, 24), 
			state == CaptureState.Recording ? "Stop Recording" : "Start Recording")) 
		{
			if(state != CaptureState.Recording) {
				StartRecording ();
			} else {
				StopRecording (null);
			}
		}
		
		if (currentTake != null) {
			if(GUI.Button (new Rect(Screen.width - 100, Screen.height - 50, 100, 24), 
				state == CaptureState.Playing ? "Stop" : "Play")) 
			{
				if(state != CaptureState.Playing) {
					StartPlayback ();
				} else {
					StopPlayback ();
				}
			}
			
			if(GUI.Button (new Rect(Screen.width - 100, Screen.height - 75, 100, 24), "Save")) {
				Save ("Assets/FacePlus.anim");
			}
		}
	}
	
	private Vector2 scrollPosition = new Vector2(0f,0f);
	void CreateWindow(int id) {
		scrollPosition = GUILayout.BeginScrollView (scrollPosition);
		foreach(var pair in channelMapping.OrderBy ((p) => p.Key)) {
			var target = pair.Value;
			GUILayout.BeginHorizontal();
			
			float amount = GUILayout.HorizontalSlider (target.Amount, target.Minimum, target.Maximum, GUILayout.Width (80));
			if (state == CaptureState.None || !Live) // allow adjusting blendshapes manually when not live
				target.Amount = amount;
				
			GUILayout.Label (target.DisplayName);
			GUILayout.EndHorizontal();
		}
		GUILayout.EndScrollView ();
	}
}
}
