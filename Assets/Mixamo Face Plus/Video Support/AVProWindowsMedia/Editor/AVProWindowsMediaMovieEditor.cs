using UnityEngine;
using UnityEditor;
using System.Collections;
using System.IO;

//-----------------------------------------------------------------------------
// Copyright 2012 RenderHeads Ltd.  All rights reserved.
//-----------------------------------------------------------------------------

[CustomEditor(typeof(AVProWindowsMediaMovie))]
public class AVProWindowsMediaMovieEditor : Editor
{
	private AVProWindowsMediaMovie _movie;
	
	public override void OnInspectorGUI()
	{
		_movie = (this.target) as AVProWindowsMediaMovie;
		
		//DrawDefaultInspector();
		_movie._folder = EditorGUILayout.TextField("Folder", _movie._folder);
		_movie._filename = EditorGUILayout.TextField("Filename", _movie._filename);
		
		

		EditorGUILayout.BeginHorizontal();
		EditorGUILayout.PrefixLabel("Colour Format");
		_movie._colourFormat = (AVProWindowsMediaMovie.ColourFormat)EditorGUILayout.EnumPopup(_movie._colourFormat);
		EditorGUILayout.EndHorizontal();
				
		_movie._loop = EditorGUILayout.Toggle("Loop", _movie._loop);
		_movie._loadOnStart = EditorGUILayout.Toggle("Load On Start", _movie._loadOnStart);
		_movie._playOnStart = EditorGUILayout.Toggle("Play On Start", _movie._playOnStart);
		//_movie._editorPreview = EditorGUILayout.Toggle("Editor Preview", _movie._editorPreview);
		
		EditorGUILayout.BeginHorizontal();
		EditorGUILayout.PrefixLabel("Audio Volume");
		_movie._volume = EditorGUILayout.Slider(_movie._volume, 0.0f, 1.0f);
		EditorGUILayout.EndHorizontal();
		
		
		GUILayout.Space(8.0f);
		
		AVProWindowsMedia media = _movie.MovieInstance;
		if (media != null)
		{
			_movie._editorPreview = EditorGUILayout.Foldout(_movie._editorPreview, "Video Preview");
			
			if (_movie._editorPreview)
			{
				{
					Rect textureRect = GUILayoutUtility.GetRect(64.0f, 64.0f, GUILayout.MinWidth(64.0f), GUILayout.MinHeight(64.0f));
					Texture texture = _movie.OutputTexture;
					if (texture == null)
						texture = EditorGUIUtility.whiteTexture;
					GUI.DrawTexture(textureRect, texture, ScaleMode.ScaleToFit);
					
					if (Application.isPlaying && media != null)
					{			
						GUILayout.Label(string.Format("{0}x{1} @ {2}fps {3} secs", media.Width, media.Height, media.FrameRate.ToString("F2"), media.DurationSeconds.ToString("F2")));		
					}
					
					if (media.FramesTotal > 30)
					{
						GUILayout.Label("Displaying at " + media.DisplayFPS.ToString("F1") + " fps");
					}
					else
					{
						GUILayout.Label("Displaying at ... fps");	
					}				
				}
			
				if (Application.isPlaying && _movie.enabled)
				{
					if (media != null)
					{
						GUILayout.Space(8.0f);
						EditorGUILayout.BeginHorizontal();
						EditorGUILayout.PrefixLabel("Audio Balance");
						media.AudioBalance = EditorGUILayout.Slider(media.AudioBalance, -1.0f, 1.0f);
						EditorGUILayout.EndHorizontal();
						
						EditorGUILayout.LabelField("Frame:");
						uint currentFrame = media.PositionFrames;
						int newFrame = EditorGUILayout.IntSlider((int)currentFrame, 0, (int)media.DurationFrames);
						if (newFrame != currentFrame)
						{
							media.PositionFrames = (uint)newFrame;
						}
					
						if (!media.IsPlaying)
						{
							if (GUILayout.Button("Unpause Stream"))
							{
								_movie.Play();
							}						
						}
						else
						{
							if (GUILayout.Button("Pause Stream"))
							{
								_movie.Pause();
							}
						}
						if (_movie._editorPreview && media.IsPlaying)
						{
							UnityEditor.HandleUtility.Repaint();
						}
					}				
				}
			}
		}
	}
}