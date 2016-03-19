using UnityEngine;
using System.Collections;
using System.IO;

//-----------------------------------------------------------------------------
// Copyright 2012-2013 RenderHeads Ltd.  All rights reserverd.
//-----------------------------------------------------------------------------

[AddComponentMenu("AVPro Windows Media/Movie")]
public class AVProWindowsMediaMovie : MonoBehaviour
{
	protected AVProWindowsMedia _moviePlayer;
	public string _folder = "./";
	public string _filename = "movie.avi";
	public bool _loop = false;
	public ColourFormat _colourFormat = ColourFormat.YCbCr_HD;
	public bool _loadOnStart = true;
	public bool _playOnStart = true;
	public bool _editorPreview = false;
	public float _volume = 1.0f;
	
	public enum ColourFormat
	{
		RGBA32,
		YCbCr_SD,
		YCbCr_HD,
	}
	
	public Texture OutputTexture  
	{
		get { if (_moviePlayer != null) return _moviePlayer.OutputTexture; return null; }
	}
	
	public AVProWindowsMedia MovieInstance
	{
		get { return _moviePlayer; }
	}

	public void Start()
	{
		if (null == AVProWindowsMediaManager.Instance)
		{
			throw new System.Exception("You need to add AVProWindowsMediaManager component to your scene.");
		}
		if (_loadOnStart)
		{
			LoadMovie(_playOnStart);
		}
	}
	
	public bool LoadMovie(bool autoPlay)
	{
		bool result = true;
		
		if (_moviePlayer == null)
			_moviePlayer = new AVProWindowsMedia();
		
		bool allowNativeFormat = (_colourFormat != ColourFormat.RGBA32);
		
		string filePath = Path.Combine(_folder, _filename);
		
		// If we're running outside of the editor we may need to resolve the relative path
		// as the working-directory may not be that of the application EXE.
		if (!Application.isEditor && !Path.IsPathRooted(filePath))
		{
			string rootPath = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
			filePath = Path.Combine(rootPath, filePath);
		}
		
		if (_moviePlayer.StartVideo(filePath, _loop, allowNativeFormat, _colourFormat == ColourFormat.YCbCr_HD))
		{
			_moviePlayer.Volume = _volume;
			if (autoPlay)
			{
				_moviePlayer.Play();
			}
		}
		else
		{
			Debug.LogWarning("[AVProWindowsMedia] Couldn't load movie " + _filename);
			UnloadMovie();
			result = false;
		}
		
		return result;
	}
	
	public bool LoadMovieFromMemory(bool autoPlay, string name, System.IntPtr moviePointer, uint movieLength)
	{
		bool result = true;
		
		if (_moviePlayer == null)
			_moviePlayer = new AVProWindowsMedia();
		
		bool allowNativeFormat = (_colourFormat != ColourFormat.RGBA32);
		
		if (_moviePlayer.StartVideoFromMemory(name, moviePointer, movieLength, _loop, allowNativeFormat, _colourFormat == ColourFormat.YCbCr_HD))
		{
			_moviePlayer.Volume = _volume;
			if (autoPlay)
			{
				_moviePlayer.Play();
			}
		}
		else
		{
			Debug.LogWarning("[AVProWindowsMedia] Couldn't load movie " + _filename);
			UnloadMovie();
			result = false;
		}
		
		return result;
	}
	
	public void Update()
	{
		if (_moviePlayer != null)
		{
			_volume = Mathf.Clamp01(_volume);
			
			if (_volume != _moviePlayer.Volume)
				_moviePlayer.Volume = _volume;
		}
	}
	
	public void OnRenderObject()
	{
		// We only want to draw once per frame
		if (Camera.current != Camera.main)
			return;

		if (_moviePlayer != null)
		{
			_moviePlayer.Update(false);
		}
	}
	
	public void Play()
	{
		if (_moviePlayer != null)
			_moviePlayer.Play();
	}
	
	public void Pause()
	{
		if (_moviePlayer != null)
			_moviePlayer.Pause();
	}	
	
	public void UnloadMovie()
	{
		if (_moviePlayer != null)
		{
			_moviePlayer.Dispose();
			_moviePlayer = null;
		}
	}

	public void OnDestroy()
	{
		UnloadMovie();
	}

#if UNITY_EDITOR
	[ContextMenu("Save PNG")]
	private void SavePNG()
	{
		if (OutputTexture != null && _moviePlayer != null)
		{
			Texture2D tex = new Texture2D(OutputTexture.width, OutputTexture.height, TextureFormat.ARGB32, false);
			RenderTexture.active = (RenderTexture)OutputTexture;
			tex.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0, false);
			tex.Apply(false, false);
			
			byte[] pngBytes = tex.EncodeToPNG();
			System.IO.File.WriteAllBytes("AVProWindowsMedia-image" + Random.Range(0, 65536).ToString("X") + ".png", pngBytes);
			
			RenderTexture.active = null;
			Texture2D.Destroy(tex);
			tex = null;
		}
	}
#endif
}