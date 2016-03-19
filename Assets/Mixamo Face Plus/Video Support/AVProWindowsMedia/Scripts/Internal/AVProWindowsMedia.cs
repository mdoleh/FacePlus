using UnityEngine;
using System.Text;
using System.Collections.Generic;
using System.Runtime.InteropServices;

//-----------------------------------------------------------------------------
// Copyright 2012-2013 RenderHeads Ltd.  All rights reserverd.
//-----------------------------------------------------------------------------

public class AVProWindowsMedia : System.IDisposable
{
	private int _movieHandle = -1;
	private AVProWindowsMediaFormatConverter _formatConverter;
	
#if UNITY_EDITOR
	private int _frameCount;
	private float _startFrameTime;
	
	public float DisplayFPS
	{
		get;
		private set;
	}
	
	public int FramesTotal
	{
		get;
		private set;
	}	
#endif
	
	//-----------------------------------------------------------------------------
	
	public int Handle
	{
		get { return _movieHandle; }
	}
	
	// Movie Properties

	public string Filename
	{
		get; private set;
	}
	
	public int Width
	{
		get; private set;
	}
	
	public int Height
	{
		get; private set;
	}
	
	public float AspectRatio
	{
		get { return (Width / (float)Height); }
	}	
	
	public float FrameRate
	{
		get; private set;
	}

	public float DurationSeconds
	{
		get; private set;
	}
	
	public uint DurationFrames
	{
		get; private set;
	}	
	
	// Playback State
	
	public bool IsPlaying
	{
		get; private set;
	}
	
	public bool Loop
	{
		set { AVProWindowsMediaPlugin.SetLooping(_movieHandle, value); }
		get { return AVProWindowsMediaPlugin.IsLooping(_movieHandle); }
	}	
	
	private int _audioDelay = 0;
	public int AudioDelay
	{
		set { _audioDelay = value; AVProWindowsMediaPlugin.SetAudioDelay(_movieHandle, _audioDelay); }
		get { return _audioDelay; }
	}	

	private float _volume = 1.0f;
	public float Volume 
	{
		set { _volume = value; AVProWindowsMediaPlugin.SetVolume(_movieHandle, _volume); }
		get { return _volume; }
	}
	
	public float PlaybackRate 
	{
		set { AVProWindowsMediaPlugin.SetPlaybackRate(_movieHandle, value); }
		get { return AVProWindowsMediaPlugin.GetPlaybackRate(_movieHandle); }
	}
	
	public float PositionSeconds
	{
		get { return AVProWindowsMediaPlugin.GetCurrentPositionSeconds(_movieHandle); }
		set { AVProWindowsMediaPlugin.SeekSeconds(_movieHandle, value); }
	}

	public uint PositionFrames
	{
		get { return AVProWindowsMediaPlugin.GetCurrentPositionFrames(_movieHandle); }
		set { AVProWindowsMediaPlugin.SeekFrames(_movieHandle, value); }
	}
	
	public float AudioBalance
	{
		set { AVProWindowsMediaPlugin.SetAudioBalance(_movieHandle, value); }
		get { return AVProWindowsMediaPlugin.GetAudioBalance(_movieHandle); }
	}
	
	public bool IsFinishedPlaying 
	{
        get { return AVProWindowsMediaPlugin.IsFinishedPlaying(_movieHandle); }
	}
	
	// Display
	
	public Texture OutputTexture
	{
		get { if (_formatConverter != null && _formatConverter.ValidPicture) return _formatConverter.OutputTexture; return null;}
	}
	
	public int DisplayFrame
	{
		get { if (_formatConverter != null && _formatConverter.ValidPicture) return _formatConverter.DisplayFrame; return -1; }
	}
	
	//-------------------------------------------------------------------------

	public bool StartVideo(string filename, bool loop, bool allowNativeFormat, bool useBT709)
	{
		Filename = filename;
		if (!string.IsNullOrEmpty(Filename))
		{
			if (_movieHandle < 0)
			{
				_movieHandle = AVProWindowsMediaPlugin.GetInstanceHandle();
			}
						
			// Note: we're marshaling the string to IntPtr as the pointer of type wchar_t 
			System.IntPtr filenamePtr = Marshal.StringToHGlobalUni(Filename);
			if (AVProWindowsMediaPlugin.LoadMovie(_movieHandle, filenamePtr, loop, false, allowNativeFormat))
			{
				CompleteVideoLoad(useBT709);
			}
			else
			{
				Debug.LogWarning("[AVProWindowsMedia] Movie failed to load");
				Close();
			}
			Marshal.FreeHGlobal(filenamePtr);			
		}
		else
		{
			Debug.LogWarning("[AVProWindowsMedia] No movie file specified");
			Close();
		}
		
		return _movieHandle >= 0;
	}
	

	public bool StartVideoFromMemory(string name, System.IntPtr moviePointer, uint movieLength, bool loop, bool allowNativeFormat, bool useBT709)
	{
		Filename = name;
		if (moviePointer != System.IntPtr.Zero && movieLength > 0)
		{
			if (_movieHandle < 0)
			{
				_movieHandle = AVProWindowsMediaPlugin.GetInstanceHandle();
			}
						
			if (AVProWindowsMediaPlugin.LoadMovieFromMemory(_movieHandle, moviePointer, movieLength, loop, allowNativeFormat))
			{
				CompleteVideoLoad(useBT709);
			}
			else
			{
				Debug.LogWarning("[AVProWindowsMedia] Movie failed to load");
				Close();
			}
		}
		else
		{
			Debug.LogWarning("[AVProWindowsMedia] No movie file specified");
			Close();
		}
		
		return _movieHandle >= 0;
	}
	
	private void CompleteVideoLoad(bool useBT709)
	{
		// Gather properties
		Volume = _volume;
		Width = AVProWindowsMediaPlugin.GetWidth(_movieHandle);
		Height = AVProWindowsMediaPlugin.GetHeight(_movieHandle);
		FrameRate = AVProWindowsMediaPlugin.GetFrameRate(_movieHandle);
		DurationSeconds = AVProWindowsMediaPlugin.GetDurationSeconds(_movieHandle);
		DurationFrames = AVProWindowsMediaPlugin.GetDurationFrames(_movieHandle);

		AVProWindowsMediaPlugin.VideoFrameFormat sourceFormat = (AVProWindowsMediaPlugin.VideoFrameFormat)AVProWindowsMediaPlugin.GetFormat(_movieHandle);
		Debug.Log(string.Format("[AVProWindowsMedia] Loaded video '{0}' ({1}x{2} @ {3} fps) {4} frames, {5} seconds - format: {6}", Filename, Width, Height, FrameRate.ToString("F2"), DurationFrames, DurationSeconds.ToString("F2"), sourceFormat.ToString()));
		
		// Create format converter
		if (Width < 0 || Width > 4096 || Height < 0 || Height > 4096)
		{
			Debug.LogWarning("[AVProWindowsMedia] invalid width or height");
			Width = Height = 0;
			if (_formatConverter != null)
			{
				_formatConverter.Dispose();
				_formatConverter = null;
			}
		}
		else
		{
			bool isTopDown = AVProWindowsMediaPlugin.IsOrientedTopDown(_movieHandle);
								
			if (_formatConverter == null)
			{
				_formatConverter = new AVProWindowsMediaFormatConverter();
			}
			if (!_formatConverter.Build(_movieHandle, Width, Height, sourceFormat, useBT709, false, isTopDown))
			{
				Debug.LogWarning("[AVProWindowsMedia] unable to convert video format");
				Width = Height = 0;
				if (_formatConverter != null)
				{
					_formatConverter.Dispose();
					_formatConverter = null;
				}
				// TODO: close movie here?
			}
		}
		
		PreRoll();		
	}
	
	public bool StartAudio(string filename, bool loop)
	{
		Filename = filename;
		Width = Height = 0;
		if (!string.IsNullOrEmpty(Filename))
		{
			if (_movieHandle < 0)
			{
				_movieHandle = AVProWindowsMediaPlugin.GetInstanceHandle();
			}
			
			if (_formatConverter != null)
			{
				_formatConverter.Dispose();
				_formatConverter = null;
			}

			// Note: we're marshaling the string to IntPtr as the pointer of type wchar_t 
			System.IntPtr filenamePtr = Marshal.StringToHGlobalUni(Filename);
			if (AVProWindowsMediaPlugin.LoadAudio(_movieHandle, filenamePtr, loop))
			{
				Volume = _volume;
				DurationSeconds = AVProWindowsMediaPlugin.GetDurationSeconds(_movieHandle);
				
				Debug.Log("[AVProWindowsMedia] Loaded audio " + Filename + " " + DurationSeconds.ToString("F2") + " sec");
			}
			else
			{
				Debug.LogWarning("[AVProWindowsMedia] Movie failed to load");
				Close();
			}
			Marshal.FreeHGlobal(filenamePtr);
		}
		else
		{
			Debug.LogWarning("[AVProWindowsMedia] No movie file specified");
			Close();			
		}
		
		return _movieHandle >= 0;
	}	
	
	private void PreRoll()
	{
		if (_movieHandle < 0)
			return;
		
		float vol = Volume;
		Volume = 0.0f;
		Play();
		Pause();
		AVProWindowsMediaPlugin.SeekFrames(_movieHandle, 0);
		Volume = vol;
	}
	
	public bool Update(bool force)
	{
		bool updated = false;
		if (_movieHandle >= 0)
		{
			AVProWindowsMediaPlugin.Update(_movieHandle);
			if (_formatConverter != null)
			{
				bool ready = true;
				
//#if UNITY_4_3 || UNITY_4_2 || UNITY_4_1 || UNITY_4_0_1 || UNITY_4_0
#if !UNITY_3_5 && !UNITY_3_4 && !UNITY_3_3 && !UNITY_3_2 && !UNITY_3_1 && !UNITY_3_0
				ready = true;
#else
				if (AVProWindowsMediaManager.ConversionMethod.Unity35_OpenGL == AVProWindowsMediaManager.Instance.TextureConversionMethod)
				{
					ready = true;
				}
				else if (!force)
				{
					ready = AVProWindowsMediaPlugin.IsNextFrameReadyForGrab(_movieHandle);
				}
#endif

				if (ready)
				{
					updated = _formatConverter.Update();
#if UNITY_EDITOR
					if (updated)
					{
						UpdateFPS();
					}
#endif
				}
			}
			else
			{
				updated = false;
			}
		}
		return updated;
	}
	
#if UNITY_EDITOR
	protected void ResetFPS()
	{
		_frameCount = 0;
		FramesTotal = 0;
		DisplayFPS = 0.0f;
		_startFrameTime = 0.0f;
	}
	
	public void UpdateFPS()
	{
		_frameCount++;
		FramesTotal++;
		
		float timeNow = Time.realtimeSinceStartup;
		float timeDelta = timeNow - _startFrameTime;
		if (timeDelta >= 1.0f)
		{
			DisplayFPS = (float)_frameCount / timeDelta;
			_frameCount  = 0;
			_startFrameTime = timeNow;
		}
	}	
#endif
	
	public void Play()
	{
		if (_movieHandle >= 0)
		{
			AVProWindowsMediaPlugin.Play(_movieHandle);
			IsPlaying = true;
		}
	}
	
	public void Pause()
	{
		if (_movieHandle >= 0)
		{
			AVProWindowsMediaPlugin.Pause(_movieHandle);
			IsPlaying = false;
		}
	}
	
	public void Rewind()
	{
		if (_movieHandle >= 0)
		{
			PositionSeconds = 0.0f;
		}
	}
	
	public void Dispose()
	{
		Close();
		if (_formatConverter != null)
		{
			_formatConverter.Dispose();
			_formatConverter = null;
		}
	}
	
	private void Close()
	{
		Pause();
		AVProWindowsMediaPlugin.Stop(_movieHandle);
		
#if UNITY_EDITOR
		ResetFPS();
#endif
		
		Width = Height = 0;
		
		if (_movieHandle >= 0)
		{
			AVProWindowsMediaPlugin.FreeInstanceHandle(_movieHandle);
			_movieHandle = -1;
		}
	}
}