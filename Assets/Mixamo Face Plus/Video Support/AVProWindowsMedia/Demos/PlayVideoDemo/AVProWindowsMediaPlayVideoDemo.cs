using UnityEngine;
using System.Collections;
using System.IO;
using System.Runtime.InteropServices;

public class AVProWindowsMediaPlayVideoDemo : MonoBehaviour
{
	public GUISkin _skin;
	public AVProWindowsMediaMovie _movie;
	public AVProWindowsMediaGUIDisplay _display;
		
	private bool _visible = true;
	private float _alpha = 1.0f;
	private bool _playFromMemory = false;
	
	private GCHandle _bytesHandle;
	private System.IntPtr _moviePtr;
	private uint _movieLength;
	
	private void ReleaseMemoryFile()
	{
		if (_bytesHandle.IsAllocated)
			_bytesHandle.Free();
		_moviePtr = System.IntPtr.Zero;
		_movieLength = 0;
	}
	
	private void LoadFileToMemory(string folder, string filename)
	{
		string filePath = Path.Combine(folder, filename);
		
		// If we're running outside of the editor we may need to resolve the relative path
		// as the working-directory may not be that of the application EXE.
		if (!Application.isEditor && !Path.IsPathRooted(filePath))
		{
			string rootPath = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
			filePath = Path.Combine(rootPath, filePath);
		}
		
		ReleaseMemoryFile();
		if (File.Exists(filePath))
		{
			byte[] bytes = System.IO.File.ReadAllBytes(filePath);
			if (bytes.Length > 0)
			{
				_bytesHandle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
				_moviePtr = _bytesHandle.AddrOfPinnedObject();
				_movieLength = (uint)bytes.Length;
				
				_movie.LoadMovieFromMemory(true, filename, _moviePtr, _movieLength);
			}
		}
	}
	
	public void OnGUI()
	{
		GUI.skin = _skin;
	
		if (_visible)
		{
			GUI.color = new Color(1f, 1f, 1f, _alpha);
			GUILayout.BeginArea(new Rect(0, 0, 740, 300), GUI.skin.box);
			ControlWindow(0);
			GUILayout.EndArea();
		}
		GUI.color = new Color(1f, 1f, 1f, 1f - _alpha);
		GUI.Box(new Rect(0, 0, 128, 32), "Demo Controls");
	}
	
	void Update()
	{
		Rect r = new Rect(0, 0, 740, 310);
		if (r.Contains(new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y)))
		{
			_visible = true;
			_alpha = 1.0f;
		}
		else
		{
			_alpha -= Time.deltaTime * 4f;
			if (_alpha <= 0.0f)
			{
				_alpha = 0.0f;
				_visible = false;
			}
		}
	}
	
	public void ControlWindow(int id)
	{		
		if (_movie == null)
			return;
		
		GUILayout.Space(16f);
		
		GUILayout.BeginVertical();
		
		GUILayout.BeginHorizontal();
		GUILayout.Label("Folder: ", GUILayout.Width(80));
		_movie._folder = GUILayout.TextField(_movie._folder, 192);
		GUILayout.EndHorizontal();

		GUILayout.BeginHorizontal();
		GUILayout.Label("File: ", GUILayout.Width(80));
		_movie._filename = GUILayout.TextField(_movie._filename, 128, GUILayout.Width(440));
		if (GUILayout.Button("Load File", GUILayout.Width(90)))
		{
			if (!_playFromMemory)
			{
				_movie.LoadMovie(true);
			}
			else
			{
				LoadFileToMemory(_movie._folder, _movie._filename);
			}
		}
		GUILayout.EndHorizontal();
		
		
		if (_display != null)
		{
			GUILayout.BeginHorizontal();
			GUILayout.Space(80f);
			/*if (_display._alphaBlend)
				_display._alphaBlend = GUILayout.Toggle(_display._alphaBlend, "Rendering with Transparency");
			else
			_display._alphaBlend = GUILayout.Toggle(_display._alphaBlend, "Rendering without Transparency");*/
			
			if (_display._alphaBlend != GUILayout.Toggle(_display._alphaBlend, "Render with Transparency"))
			{
				_display._alphaBlend = !_display._alphaBlend;
				if (_display._alphaBlend)
				{
					_movie._colourFormat = AVProWindowsMediaMovie.ColourFormat.RGBA32;
				}
				else
				{
					_movie._colourFormat = AVProWindowsMediaMovie.ColourFormat.YCbCr_HD;
				}
				
				if (!_playFromMemory)
				{
					_movie.LoadMovie(true);
				}
				else
				{
					LoadFileToMemory(_movie._folder, _movie._filename);
				}
			}
			
			
			
			if (_playFromMemory != GUILayout.Toggle(_playFromMemory, "Play from Memory"))
			{
				_playFromMemory = !_playFromMemory;
				if (_movie.MovieInstance != null)
				{
					if (!_playFromMemory)
					{
						_movie.LoadMovie(true);
					}
					else
					{
						LoadFileToMemory(_movie._folder, _movie._filename);
					}
				}
				
			}
						
			GUILayout.EndHorizontal();
		}
		
		AVProWindowsMedia moviePlayer = _movie.MovieInstance;
		if (moviePlayer != null)
		{
			GUILayout.BeginHorizontal();
			GUILayout.Label("Info:", GUILayout.Width(80f));
			GUILayout.Label(moviePlayer.Width + "x" + moviePlayer.Height + " @ " + moviePlayer.FrameRate.ToString("F2") + " FPS");
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			GUILayout.Label("Volume ", GUILayout.Width(80));
			float volume = _movie._volume;
			float newVolume = GUILayout.HorizontalSlider(volume, 0.0f, 1.0f, GUILayout.Width(200));
			if (volume != newVolume)
			{
				_movie._volume = newVolume;
			}
			GUILayout.Label(_movie._volume.ToString("F1"));
			GUILayout.EndHorizontal();


			GUILayout.BeginHorizontal();
			GUILayout.Label("Balance ", GUILayout.Width(80));
			float balance = moviePlayer.AudioBalance;
			float newBalance = GUILayout.HorizontalSlider(balance, -1.0f, 1.0f, GUILayout.Width(200));
			if (balance != newBalance)
			{
				moviePlayer.AudioBalance = newBalance;
			}
			GUILayout.Label(moviePlayer.AudioBalance.ToString("F1"));
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			GUILayout.Label("Delay ", GUILayout.Width(80));
			int delay = moviePlayer.AudioDelay;
			int newDelay = Mathf.FloorToInt(GUILayout.HorizontalSlider(delay, 0.0f, 1000.0f, GUILayout.Width(200)));
			if (delay != newDelay)
			{
				moviePlayer.AudioDelay = newDelay;
			}
			GUILayout.Label(moviePlayer.AudioDelay.ToString());
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			GUILayout.Label("Time ", GUILayout.Width(80));
			float position = moviePlayer.PositionSeconds;
			float newPosition = GUILayout.HorizontalSlider(position, 0.0f, moviePlayer.DurationSeconds, GUILayout.Width(200));
			if (position != newPosition)
			{
				moviePlayer.PositionSeconds = newPosition;
			}
			GUILayout.Label(moviePlayer.PositionSeconds.ToString("F1") + " / " + moviePlayer.DurationSeconds.ToString("F1") + "s");

			if (GUILayout.Button("Play"))
			{
				moviePlayer.Play();
			}
			if (GUILayout.Button("Pause"))
			{
				moviePlayer.Pause();
			}
			GUILayout.EndHorizontal();
			
			
			GUILayout.BeginHorizontal();
			GUILayout.Label("Frame", GUILayout.Width(80f));
			GUILayout.Label(moviePlayer.PositionFrames.ToString() + " / " + moviePlayer.DurationFrames.ToString());
	
			if (GUILayout.Button("<", GUILayout.Width(50)))
			{
				moviePlayer.Pause();
				if (moviePlayer.PositionFrames > 0)
				{
					moviePlayer.PositionFrames--;
				}
			}
			if (GUILayout.Button(">", GUILayout.Width(50)))
			{
				moviePlayer.Pause();
				if (moviePlayer.PositionFrames <  moviePlayer.DurationFrames)
				{
					moviePlayer.PositionFrames++;
				}
			}
			
			GUILayout.EndHorizontal();
			


			GUILayout.BeginHorizontal();
			GUILayout.Label("Rate ", GUILayout.Width(80f));
			GUILayout.Label(moviePlayer.PlaybackRate.ToString("F2") + "x");
			if (GUILayout.Button("-", GUILayout.Width(50)))
			{
				moviePlayer.PlaybackRate = moviePlayer.PlaybackRate * 0.5f;
			}

			if (GUILayout.Button("+", GUILayout.Width(50)))
			{
				moviePlayer.PlaybackRate = moviePlayer.PlaybackRate * 2.0f;
			}
			GUILayout.EndHorizontal();
		}

		GUILayout.EndVertical();
	}
}
