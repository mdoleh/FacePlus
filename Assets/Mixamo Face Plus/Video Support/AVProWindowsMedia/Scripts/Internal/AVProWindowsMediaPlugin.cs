
using UnityEngine;
using System.Text;
using System.Collections.Generic;
using System.Runtime.InteropServices;


//-----------------------------------------------------------------------------
// Copyright 2012-2013 RenderHeads Ltd.  All rights reserverd.
//-----------------------------------------------------------------------------

public class AVProWindowsMediaPlugin
{
	public enum VideoFrameFormat
	{
		RAW_BGRA32,
		YUV_422_YUY2,
		YUV_422_UYVY,
		YUV_422_YVYU,
		YUV_422_HDYC,
		YUV_420_NV12=7,
	}
	
	// Used by GL.IssuePluginEvent
	public const int PluginID = 0xFA10000;
	public enum PluginEvent
	{
		UpdateAllTextures = 0,
	}
	
	// Initialisation
	
#if UNITY_64
	[DllImport("AVProWindowsMedia-x64")]
#else
	[DllImport("AVProWindowsMedia")]
#endif
	public static extern bool Init(bool supportUnity35OpenGL);

#if UNITY_64
	[DllImport("AVProWindowsMedia-x64")]
#else
	[DllImport("AVProWindowsMedia")]
#endif
	public static extern void Deinit();

#if UNITY_64
	[DllImport("AVProWindowsMedia-x64")]
#else
	[DllImport("AVProWindowsMedia")]
#endif
	public static extern float GetPluginVersion();
	
	// Open and Close handle

#if UNITY_64
	[DllImport("AVProWindowsMedia-x64")]
#else
	[DllImport("AVProWindowsMedia")]
#endif
	public static extern int GetInstanceHandle();

#if UNITY_64
	[DllImport("AVProWindowsMedia-x64")]
#else
	[DllImport("AVProWindowsMedia")]
#endif
	public static extern void FreeInstanceHandle(int handle);
	
	// Loading

#if UNITY_64
	[DllImport("AVProWindowsMedia-x64")]
#else
	[DllImport("AVProWindowsMedia")]
#endif
	public static extern bool LoadMovie(int handle, System.IntPtr filename, bool loop, bool playFromMemory, bool allowNativeFormat);

#if UNITY_64
	[DllImport("AVProWindowsMedia-x64")]
#else
	[DllImport("AVProWindowsMedia")]
#endif
	public static extern bool LoadMovieFromMemory(int handle, System.IntPtr moviePointer, uint movieLength, bool loop, bool allowNativeFormat);
	
#if UNITY_64
	[DllImport("AVProWindowsMedia-x64")]
#else
	[DllImport("AVProWindowsMedia")]
#endif
	public static extern bool LoadAudio(int handle, System.IntPtr filename, bool loop);
	
	// Get Properties

#if UNITY_64
	[DllImport("AVProWindowsMedia-x64")]
#else
	[DllImport("AVProWindowsMedia")]
#endif
	public static extern int GetWidth(int handle);

#if UNITY_64
	[DllImport("AVProWindowsMedia-x64")]
#else
	[DllImport("AVProWindowsMedia")]
#endif
	public static extern int GetHeight(int handle);

#if UNITY_64
	[DllImport("AVProWindowsMedia-x64")]
#else
	[DllImport("AVProWindowsMedia")]
#endif
	public static extern float GetFrameRate(int handle);

#if UNITY_64
	[DllImport("AVProWindowsMedia-x64")]
#else
	[DllImport("AVProWindowsMedia")]
#endif
	public static extern long GetFrameDuration(int handle);

#if UNITY_64
	[DllImport("AVProWindowsMedia-x64")]
#else
	[DllImport("AVProWindowsMedia")]
#endif
	public static extern int GetFormat(int handle);
	
#if UNITY_64
	[DllImport("AVProWindowsMedia-x64")]
#else
	[DllImport("AVProWindowsMedia")]
#endif
	public static extern float GetDurationSeconds(int handle);

#if UNITY_64
	[DllImport("AVProWindowsMedia-x64")]
#else
	[DllImport("AVProWindowsMedia")]
#endif
	public static extern uint GetDurationFrames(int handle);
	
#if UNITY_64
	[DllImport("AVProWindowsMedia-x64")]
#else
	[DllImport("AVProWindowsMedia")]
#endif
	public static extern bool IsOrientedTopDown(int handle);
	
	// Playback
	
#if UNITY_64
	[DllImport("AVProWindowsMedia-x64")]
#else
	[DllImport("AVProWindowsMedia")]
#endif
	public static extern void Play(int handle);

#if UNITY_64
	[DllImport("AVProWindowsMedia-x64")]
#else
	[DllImport("AVProWindowsMedia")]
#endif
	public static extern void Pause(int handle);

#if UNITY_64
	[DllImport("AVProWindowsMedia-x64")]
#else
	[DllImport("AVProWindowsMedia")]
#endif
	public static extern void Stop(int handle);
	
	// Seeking & Position
	
#if UNITY_64
	[DllImport("AVProWindowsMedia-x64")]
#else
	[DllImport("AVProWindowsMedia")]
#endif
	public static extern void SeekUnit(int handle, float position);

#if UNITY_64
	[DllImport("AVProWindowsMedia-x64")]
#else
	[DllImport("AVProWindowsMedia")]
#endif
	public static extern void SeekSeconds(int handle, float position);

#if UNITY_64
	[DllImport("AVProWindowsMedia-x64")]
#else
	[DllImport("AVProWindowsMedia")]
#endif
	public static extern void SeekFrames(int handle, uint position);
	
#if UNITY_64
	[DllImport("AVProWindowsMedia-x64")]
#else
	[DllImport("AVProWindowsMedia")]
#endif
	public static extern float GetCurrentPositionSeconds(int handle);

#if UNITY_64
	[DllImport("AVProWindowsMedia-x64")]
#else
	[DllImport("AVProWindowsMedia")]
#endif
	public static extern uint GetCurrentPositionFrames(int handle);
	
	// Get Current State
	
#if UNITY_64
	[DllImport("AVProWindowsMedia-x64")]
#else
	[DllImport("AVProWindowsMedia")]
#endif
	public static extern bool IsLooping(int handle);
	
#if UNITY_64
	[DllImport("AVProWindowsMedia-x64")]
#else
	[DllImport("AVProWindowsMedia")]
#endif
	public static extern float GetPlaybackRate(int handle);
	
#if UNITY_64
	[DllImport("AVProWindowsMedia-x64")]
#else
	[DllImport("AVProWindowsMedia")]
#endif
	public static extern float GetAudioBalance(int handle);

#if UNITY_64
	[DllImport("AVProWindowsMedia-x64")]
#else
	[DllImport("AVProWindowsMedia")]
#endif
	public static extern bool IsFinishedPlaying(int handle);
	
	// Set Current State

#if UNITY_64
	[DllImport("AVProWindowsMedia-x64")]
#else
	[DllImport("AVProWindowsMedia")]
#endif
	public static extern void SetVolume(int handle, float volume);

#if UNITY_64
	[DllImport("AVProWindowsMedia-x64")]
#else
	[DllImport("AVProWindowsMedia")]
#endif
	public static extern void SetLooping(int handle, bool loop);

#if UNITY_64
	[DllImport("AVProWindowsMedia-x64")]
#else
	[DllImport("AVProWindowsMedia")]
#endif
	public static extern void SetPlaybackRate(int handle, float rate);
	
#if UNITY_64
	[DllImport("AVProWindowsMedia-x64")]
#else
	[DllImport("AVProWindowsMedia")]
#endif
	public static extern void SetAudioBalance(int handle, float balance);
	
#if UNITY_64
	[DllImport("AVProWindowsMedia-x64")]
#else
	[DllImport("AVProWindowsMedia")]
#endif
	public static extern void SetAudioChannelMatrix(int handle, float[] values, int numValues);

#if UNITY_64
	[DllImport("AVProWindowsMedia-x64")]
#else
	[DllImport("AVProWindowsMedia")]
#endif
	public static extern void SetAudioDelay(int handle, int ms);
	
	// Update
	
#if UNITY_64
	[DllImport("AVProWindowsMedia-x64")]
#else
	[DllImport("AVProWindowsMedia")]
#endif
	public static extern bool Update(int handle);
		
	// Frame Update
	
#if UNITY_64
	[DllImport("AVProWindowsMedia-x64")]
#else
	[DllImport("AVProWindowsMedia")]
#endif
	public static extern bool IsNextFrameReadyForGrab(int handle);

#if UNITY_64
	[DllImport("AVProWindowsMedia-x64")]
#else
	[DllImport("AVProWindowsMedia")]
#endif
	public static extern int GetLastFrameUploaded(int handle);

#if UNITY_64
	[DllImport("AVProWindowsMedia-x64")]
#else
	[DllImport("AVProWindowsMedia")]
#endif
	public static extern bool UpdateTextureGL(int handle, int textureID, ref int frameNumber);

#if UNITY_64
	[DllImport("AVProWindowsMedia-x64")]
#else
	[DllImport("AVProWindowsMedia")]
#endif
	public static extern bool GetFramePixels(int handle, System.IntPtr data, int bufferWidth, int bufferHeight, ref int frameNumber);
	
#if UNITY_64
	[DllImport("AVProWindowsMedia-x64")]
#else
	[DllImport("AVProWindowsMedia")]
#endif
	public static extern bool SetTexturePointer(int handle, System.IntPtr data);
	
	// Live Stats

#if UNITY_64
	[DllImport("AVProWindowsMedia-x64")]
#else
	[DllImport("AVProWindowsMedia")]
#endif
	public static extern float GetCaptureFrameRate(int handle);
	
	// Internal Frame Buffering

#if UNITY_64
	[DllImport("AVProWindowsMedia-x64")]
#else
	[DllImport("AVProWindowsMedia")]
#endif
	public static extern void SetFrameBufferSize(int handle, int read, int write);
	
#if UNITY_64
	[DllImport("AVProWindowsMedia-x64")]
#else
	[DllImport("AVProWindowsMedia")]
#endif
	public static extern long GetLastFrameBufferedTime(int handle);

#if UNITY_64
	[DllImport("AVProWindowsMedia-x64")]
#else
	[DllImport("AVProWindowsMedia")]
#endif
	public static extern System.IntPtr GetLastFrameBuffered(int handle);
	
#if UNITY_64
	[DllImport("AVProWindowsMedia-x64")]
#else
	[DllImport("AVProWindowsMedia")]
#endif
	public static extern System.IntPtr GetFrameFromBufferAtTime(int handle, long time);

	//-----------------------------------------------------------------------------
}