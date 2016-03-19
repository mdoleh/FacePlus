
using UnityEngine;
using System.Collections;
using Mixamo;

#if UNITY_EDITOR_WIN
public class FaceVideoAVProWMV : AVProWindowsMediaMovie, IFacePlusVideo {
#else
public class FaceVideoAVProWMV: AVProQuickTimeMovie, IFacePlusVideo {
#endif
	public uint DurationFrames { 
		get {
#if UNITY_EDITOR_WIN
			return base.MovieInstance.DurationFrames;
#else
			return base.MovieInstance.FrameCount;
#endif
		} 
	}

	public uint PositionFrames { 
		get {
#if UNITY_EDITOR_WIN
			return base.MovieInstance.PositionFrames;
#else
			return base.MovieInstance.Frame;
#endif
		}
		set {
#if UNITY_EDITOR_WIN
			base.MovieInstance.PositionFrames = value;
#else 
			base.MovieInstance.Frame = value;
#endif
		}
	}
	public float DurationSeconds { 
		get {
			return base.MovieInstance.DurationSeconds;
		}
	}

	public float PositionSeconds { 
		get {
			return base.MovieInstance.PositionSeconds;
		}
		set {
			base.MovieInstance.PositionSeconds = value;
		}
	}

	public int DisplayFrame { 
		get {
#if UNITY_EDITOR_WIN
			return base.MovieInstance.DisplayFrame;
#else
			return base.MovieInstance.DisplayFrameCount;
#endif
		}
	}


 	public bool LoadMovie(string folder, string filename, bool val) {
		base._folder = folder;
		base._filename = filename;
#if UNITY_EDITOR_WIN
		return LoadMovie (val);
#else
		return LoadMovie();
#endif
	}

	public Texture OutputTexture { get {
			return base.MovieInstance.OutputTexture;
		}
	}

	public float FrameRate { 
		get {
			return base.MovieInstance.FrameRate;
		}
	}

	public void Rewind() {
		base.MovieInstance.Rewind ();
		//this might not work for AVProQuickTime. look at it later if necessary.
	}

	public void Dispose(){
		if (MovieInstance != null) base.MovieInstance.Dispose ();
	}

	public void Play() {
		base.MovieInstance.Play ();
	}

	public void Pause() {
		base.MovieInstance.Pause ();
	}

	public void UpdateMovie(bool force) {
		base.MovieInstance.Update (force);
	}
#if UNITY_EDITOR_OSX
	public bool SeekToNextFrame(){
		return base.MovieInstance.SeekToNextFrame ();
	}
#endif
	void Start() {
#if UNITY_EDITOR_WIN
		var mediaDisplay = gameObject.AddComponent<AVProWindowsMediaGUIDisplay>();
		var mediaManager = gameObject.AddComponent<AVProWindowsMediaManager>();
#else
		var mediaDisplay = gameObject.AddComponent<AVProQuickTimeGUIDisplay>();
		var mediaManager = gameObject.AddComponent<AVProQuickTimeManager>();
#endif
		mediaDisplay._movie = this;
		mediaDisplay._x = 0.7f;
		mediaDisplay._width = 0.3f;
		mediaDisplay._height = 0.3f;
		mediaDisplay._fullScreen = false;
#if UNITY_EDITOR_WIN
		mediaManager._shaderBGRA32 = Shader.Find ("Hidden/AVProWindowsMedia/CompositeBGRA_2_RGBA");
		mediaManager._shaderHDYC = Shader.Find ("Hidden/AVProWindowsMedia/CompositeHDYC_2_RGBA");
		mediaManager._shaderUYVY = Shader.Find ("Hidden/AVProWindowsMedia/CompositeUYVY_2_RGBA");
		mediaManager._shaderNV12 = Shader.Find ("Hidden/AVProWindowsMedia/CompositeNV12_709");
		mediaManager._shaderYUY2 = Shader.Find ("Hidden/AVProWindowsMedia/CompositeYUY2_2_RGBA");
		mediaManager._shaderYUY2_709 = Shader.Find ("Hidden/AVProWindowsMedia/CompositeYUY2709_2_RGBA");
		mediaManager._shaderYVYU = Shader.Find ("Hidden/AVProWindowsMedia/CompositeYVYU_2_RGBA");
#else
		mediaManager._shaderBGRA =Shader.Find("Hidden/AVProQuickTime/CompositeRedBlueSwap");
		mediaManager._shaderYUV2 = Shader.Find("Hidden/AVProQuickTime/CompositeYUV_2_RGB");
		mediaManager._shaderYUV2_709 = Shader.Find("Hidden/AVProQuickTime/CompositeYUV709_2_RGB");
		mediaManager._shaderCopy = Shader.Find("AVProQuickTime_Shared");
		mediaManager._shaderHap_YCoCg = Shader.Find("Hidden/AVProQuickTime/CompositeYCoCg_2_RGB");
#endif
		base.Start ();
	}
}

