using UnityEngine;
using System.Text;
using System.Collections.Generic;
using System.Runtime.InteropServices;

//-----------------------------------------------------------------------------
// Copyright 2012-2013 RenderHeads Ltd.  All rights reserved.
//-----------------------------------------------------------------------------

[AddComponentMenu("AVPro Windows Media/Manager (required)")]
public class AVProWindowsMediaManager : MonoBehaviour
{
	private static AVProWindowsMediaManager _instance;

	public enum ConversionMethod
	{
		Unknown,
		Unity4,
		Unity35_OpenGL,
		Unity34_OpenGL,
		UnityScript,
	}

	// Format conversion
	public Shader _shaderBGRA32;
	public Shader _shaderYUY2;
	public Shader _shaderYUY2_709;
	public Shader _shaderUYVY;
	public Shader _shaderYVYU;
	public Shader _shaderHDYC;
	public Shader _shaderNV12;

	private bool _isInitialised;
	private ConversionMethod _conversionMethod = ConversionMethod.Unknown;
	
	//-------------------------------------------------------------------------

	public static AVProWindowsMediaManager Instance  
	{
		get
		{
			if (_instance == null)
			{
				_instance = (AVProWindowsMediaManager)GameObject.FindObjectOfType(typeof(AVProWindowsMediaManager));
				if (_instance == null)
				{
					Debug.LogError("AVProWindowsMediaManager component required");
					return null;
				}
				else
				{
					if (!_instance._isInitialised)
						_instance.Init();
				}
			}
			
			return _instance;
		}
	}

	public ConversionMethod TextureConversionMethod
	{
		get { return _conversionMethod; }
	}
	
	//-------------------------------------------------------------------------
	
	void Start()
	{
		if (!_isInitialised)
		{
			_instance = this;
			Init();
		}
	}
	
	void OnDestroy()
	{
		Deinit();
	}
		
	protected bool Init()
	{
		try
		{
#if UNITY_3_5
			if (AVProWindowsMediaPlugin.Init(true))
#else
			if (AVProWindowsMediaPlugin.Init(false))
#endif
			{
				Debug.Log("[AVProWindowsMedia] version " + AVProWindowsMediaPlugin.GetPluginVersion().ToString("F2") + " initialised");
			}
			else
			{
				Debug.LogError("[AVProWindowsMedia] failed to initialise.");
				this.enabled = false;
				Deinit();
				return false;
			}
		}
		catch (System.DllNotFoundException e)
		{
			Debug.Log("Unity couldn't find the DLL, did you move the 'Plugins' folder to the root of your project?");
			throw e;
		}

		GetConversionMethod();

		_isInitialised = true;

		return _isInitialised;
	}


	private void GetConversionMethod()
	{
		bool swapRedBlue = false;

		_conversionMethod = ConversionMethod.UnityScript;

//#if UNITY_4_3 || UNITY_4_2 || UNITY_4_1 || UNITY_4_0_1 || UNITY_4_0
#if !UNITY_3_5 && !UNITY_3_4 && !UNITY_3_3 && !UNITY_3_2 && !UNITY_3_1 && !UNITY_3_0
		_conversionMethod = ConversionMethod.Unity4;
		if (SystemInfo.graphicsDeviceVersion.StartsWith("Direct3D 11"))
			swapRedBlue = true;

#elif UNITY_3_5 || UNITY3_4
		if (SystemInfo.graphicsDeviceVersion.StartsWith("OpenGL"))
		{
#if UNITY_3_4
			_conversionMethod = ConversionMethod.Unity34_OpenGL;
#elif UNITY_3_5
			_conversionMethod = ConversionMethod.Unity35_OpenGL;
#endif
		}
		else
		{
			swapRedBlue = true;
		}
#else

		_conversionMethod = ConversionMethod.UnityScript;
		swapRedBlue = true;
#endif

		if (swapRedBlue)
		{
			Shader.DisableKeyword("SWAP_RED_BLUE_OFF");
			Shader.EnableKeyword("SWAP_RED_BLUE_ON");
		}
		else
		{
			Shader.DisableKeyword("SWAP_RED_BLUE_ON");
			Shader.EnableKeyword("SWAP_RED_BLUE_OFF");
		}
	}

//#if UNITY_4_3 || UNITY_4_2 || UNITY_4_1 || UNITY_4_0_1 || UNITY_4_0 || UNITY_3_5
#if !UNITY_3_4 && !UNITY_3_3 && !UNITY_3_2 && !UNITY_3_1 && !UNITY_3_0
	void OnRenderObject()
	{
		// We only want to draw once per frame
		if (Camera.current != Camera.main)
			return;
		
		if (_conversionMethod == ConversionMethod.Unity4 || 
			_conversionMethod == ConversionMethod.Unity35_OpenGL)
		{
			GL.IssuePluginEvent(AVProWindowsMediaPlugin.PluginID | (int)AVProWindowsMediaPlugin.PluginEvent.UpdateAllTextures);
		}
	}
#endif

	public void Deinit()
	{
		// Clean up any open movies
		AVProWindowsMediaMovie[] movies = (AVProWindowsMediaMovie[])FindObjectsOfType(typeof(AVProWindowsMediaMovie));
		if (movies != null && movies.Length > 0)
		{
			for (int i = 0; i < movies.Length; i++)
			{
				movies[i].UnloadMovie();
			}
		}
		
		_instance = null;
		_isInitialised = false;
		
		AVProWindowsMediaPlugin.Deinit();
	}

	public Shader GetPixelConversionShader(AVProWindowsMediaPlugin.VideoFrameFormat format, bool useBT709)
	{
		Shader result = null;
		switch (format)
		{
		case AVProWindowsMediaPlugin.VideoFrameFormat.YUV_422_YUY2:
			result = _shaderYUY2;
			if (useBT709)
				result = _shaderYUY2_709;
			break;
		case AVProWindowsMediaPlugin.VideoFrameFormat.YUV_422_UYVY:
			result = _shaderUYVY;
			if (useBT709)
				result = _shaderHDYC;
			break;
		case AVProWindowsMediaPlugin.VideoFrameFormat.YUV_422_YVYU:
			result = _shaderYVYU;
			break;
		case AVProWindowsMediaPlugin.VideoFrameFormat.YUV_422_HDYC:
			result = _shaderHDYC;
			break;
		case AVProWindowsMediaPlugin.VideoFrameFormat.YUV_420_NV12:
			result = _shaderNV12;
			break;
		case AVProWindowsMediaPlugin.VideoFrameFormat.RAW_BGRA32:
			result= _shaderBGRA32;
			break;
		default:
			Debug.LogError("[AVProWindowsMedia] Unknown pixel format '" + format);
			break;
		}
		return result;
	}
}