using System;
using System.Text;
using System.Runtime.InteropServices;

//-----------------------------------------------------------------------------
// Copyright 2012-2014 RenderHeads Ltd.  All rights reserverd.
//-----------------------------------------------------------------------------

public class AVProQuickTimePlugin
{
	public enum MovieSource
	{
		LocalFile,
		URL,
		Memory,
	};

	public enum PlaybackState
	{
		Unknown,
		Loading,
		Loaded,
		Playing,
		Stopped,
	};

	public enum PixelFormat
	{
		Unknown,
		RGBA32,
		YCbCr,				// YCbCr (YUV422)
		Hap_RGB,			// Standard quality 24-bit RGB, using DXT1 compression
		Hap_RGB_HQ,			// High quality 24-bit, using DXT5 compression with YCoCg color-space trick
		Hap_RGBA,			// Standard quality 32-bit RGBA, using DXT5 compression
	}

	// Used by GL.IssuePluginEvent
	public const int PluginID = 0xFA40000;
	public enum PluginEvent
	{
		UpdateAllTextures = 0,
	}

	// Global Init/Deinit

	[DllImport("AVProQuickTime")]
	public static extern bool IsQuickTimeInstalled();

	[DllImport("AVProQuickTime")]
	public static extern bool Init();

	[DllImport("AVProQuickTime")]
	public static extern void Deinit();

	[DllImport("AVProQuickTime")]
	public static extern float GetPluginVersion();

	[DllImport("AVProQuickTime")]
	public static extern void SetUnityFeatures(bool supportsExternalTextures, bool supportsUnity35OpenGLTextures);


	// Create/Free an Instance

	[DllImport("AVProQuickTime")]
	public static extern int GetInstanceHandle();

	[DllImport("AVProQuickTime")]
	public static extern void FreeInstanceHandle(int handle);

	// Loading

	[DllImport("AVProQuickTime")]
	public static extern bool LoadMovieFromFile(int handle, System.IntPtr filename, bool loop, bool isYUV);

	[DllImport("AVProQuickTime")]
	public static extern bool LoadMovieFromURL(int handle, [MarshalAs(UnmanagedType.LPStr)] string filenameURL, bool loop, bool isYUV);

	[DllImport("AVProQuickTime")]
	public static extern bool LoadMovieFromMemory(int handle, IntPtr buffer, UInt32 bufferSize, bool loop, bool isYUV);

	// Loading Status

	[DllImport("AVProQuickTime")]
	public static extern bool IsMovieLoadable(int handle);

	[DllImport("AVProQuickTime")]
	public static extern bool IsMoviePlayable(int handle);

	[DllImport("AVProQuickTime")]
	public static extern bool IsMoviePropertiesLoaded(int handle);

	[DllImport("AVProQuickTime")]
	public static extern bool LoadMovieProperties(int handle);

	[DllImport("AVProQuickTime")]
	public static extern float GetLoadedFraction(int handle);

	// Playback Controls

	[DllImport("AVProQuickTime")]
	public static extern void Play(int handle);

	[DllImport("AVProQuickTime")]
	public static extern void Stop(int handle);

	[DllImport("AVProQuickTime")]
	public static extern bool SeekToNextFrame(int handle);

	[DllImport("AVProQuickTime")]
	public static extern bool SeekToPreviousFrame(int handle);

	[DllImport("AVProQuickTime")]
	public static extern void SeekUnit(int handle, float position);

	[DllImport("AVProQuickTime")]
	public static extern void SeekSeconds(int handle, float position);

	[DllImport("AVProQuickTime")]
	public static extern void SeekFrame(int handle, uint frame);

	[DllImport("AVProQuickTime")]
	public static extern void SetVolume(int handle, float volume);

	[DllImport("AVProQuickTime")]
	public static extern void SetAudioBalance(int handle, float volume);

	[DllImport("AVProQuickTime")]
	public static extern void SetPlaybackRate(int handle, float rate);

	[DllImport("AVProQuickTime")]
	public static extern void SetLooping(int handle, bool loop);

	// Update

	[DllImport("AVProQuickTime")]
	public static extern void Update(int handle);

	[DllImport("AVProQuickTime")]
	public static extern void SetActive(int handle, bool active);

	// Get Movie Properties

	[DllImport("AVProQuickTime")]
	public static extern int GetWidth(int handle);

	[DllImport("AVProQuickTime")]
	public static extern int GetHeight(int handle);

	[DllImport("AVProQuickTime")]
	public static extern int GetFramePixelFormat(int handle);

	[DllImport("AVProQuickTime")]
	public static extern float GetDurationSeconds(int handle);

	[DllImport("AVProQuickTime")]
	public static extern float GetFrameRate(int handle);

	[DllImport("AVProQuickTime")]
	public static extern float GetFrameCount(int handle);

	// Get Movie State

	[DllImport("AVProQuickTime")]
	public static extern float GetCurrentPositionSeconds(int handle);

	[DllImport("AVProQuickTime")]
	public static extern uint GetCurrentFrame(int handle);

	[DllImport("AVProQuickTime")]
	public static extern float GetCurrentPosition(int handle);

	[DllImport("AVProQuickTime")]
	public static extern float GetNextPosition(int handle);

	[DllImport("AVProQuickTime")]
	public static extern float GetPlaybackRate(int handle);

	[DllImport("AVProQuickTime")]
	public static extern bool IsFinishedPlaying(int handle);

	[DllImport("AVProQuickTime")]
	public static extern uint GetNumFramesDrawn(int handle);

	// Frame Grabbing

	[DllImport("AVProQuickTime")]
	public static extern int GetLastFrameUploaded(int handle);

	[DllImport("AVProQuickTime")]
	public static extern int GetFrameUploadCount(int handle);

	[DllImport("AVProQuickTime")]
	public static extern bool GetFramePixelsRGBA32(int handle, System.IntPtr data, int bufferWidth, int bufferHeight, ref int frameNumber);

	[DllImport("AVProQuickTime")]
	public static extern bool GetFramePixelsYUV2(int handle, System.IntPtr data, int bufferWidth, int bufferHeight, ref int frameNumber);

	[DllImport("AVProQuickTime")]
	public static extern bool UpdateTextureGL(int handle, int textureID);

	[DllImport("AVProQuickTime")]
	public static extern bool SetTexturePointer(int handle, System.IntPtr data);

	[DllImport("AVProQuickTime")]
	public static extern System.IntPtr GetTexturePointer(int handle);
}
