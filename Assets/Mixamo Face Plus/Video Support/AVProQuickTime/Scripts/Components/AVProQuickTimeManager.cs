// Support for using externally created native textures, from Unity 4.3 upwards
#if UNITY_4_3 || UNITY_4_4 || UNITY_4_5 || UNITY_4_6 || UNITY_5_0
	//#define AVPROQUICKTIME_UNITYFEATURE_EXTERNALTEXTURES
#endif

// Support for DirectX and OpenGL native texture updating, from Unity 4.0 upwards
#if UNITY_4_4 || UNITY_4_5 || UNITY_4_6 || UNITY_5_0 || UNITY_4_3 || UNITY_4_2 || UNITY_4_1 || UNITY_4_0_1 || UNITY_4_0
	#define AVPRO_UNITY_4_X
#endif

using UnityEngine;
using System;
using System.Text;
using System.Collections;
using System.Runtime.InteropServices;

//-----------------------------------------------------------------------------
// Copyright 2012-2014 RenderHeads Ltd.  All rights reserverd.
//-----------------------------------------------------------------------------

[AddComponentMenu("AVPro QuickTime/Manager (required)")]
public class AVProQuickTimeManager : MonoBehaviour
{
	private static AVProQuickTimeManager _instance;

	public enum ConversionMethod
	{
		Unknown,
		Unity4,
		Unity35_OpenGL,
		Unity34_OpenGL,
		UnityScript,
	}

	// Format conversion
	public Shader _shaderBGRA;
	public Shader _shaderYUV2;
	public Shader _shaderYUV2_709;
	public Shader _shaderCopy;
	public Shader _shaderHap_YCoCg;

	private bool _isInitialised;
	private ConversionMethod _conversionMethod = ConversionMethod.Unknown;
	
	//-----------------------------------------------------------------------------
	
	public static AVProQuickTimeManager Instance  
	{
		get
		{
			if (_instance == null)
			{
				_instance = (AVProQuickTimeManager)GameObject.FindObjectOfType(typeof(AVProQuickTimeManager));
				if (_instance == null)
				{
					Debug.LogError("AVProQuickTimeManager component required");
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

#if UNITY_EDITOR
	[ContextMenu("Copy Plugin DLLs")]
	private void CopyPluginDLLs()
	{
		AVProQuickTimeCopyPluginWizard.DisplayCopyDialog();
	}
#endif
	
	protected bool Init()
	{
		try
		{
			if (AVProQuickTimePlugin.Init())
			{
				Debug.Log("[AVProQuickTime] version " + AVProQuickTimePlugin.GetPluginVersion().ToString("F2") + " initialised");
			}
			else
			{
				Debug.LogError("[AVProQuickTime] failed to initialise.");
				this.enabled = false;
				Deinit();
				return false;
			}
		}
		catch (DllNotFoundException e)
		{
			Debug.Log("[AVProQuickTime] Unity couldn't find the DLL, did you move the 'Plugins' folder to the root of your project?");
#if UNITY_EDITOR
			AVProQuickTimeCopyPluginWizard.DisplayCopyDialog();
#endif
			throw e;
		}
		
		GetConversionMethod();
		SetUnityFeatures();

        Debug.Log("[AVProQuickTime] Conversion method: " + _conversionMethod);

#if AVPRO_UNITY_4_X || UNITY_3_5
		//StartCoroutine("FinalRenderCapture");
#endif

		_isInitialised = true;

		return _isInitialised;
	}

	private void GetConversionMethod()
	{
		bool swapRedBlue = false;

		_conversionMethod = ConversionMethod.UnityScript;

#if AVPRO_UNITY_4_X
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

	private void SetUnityFeatures()
	{
		bool unitySupportsExternalTextures = false;
		bool unitySupportsUnity35OpenGLTextures = false;
#if AVPROQUICKTIME_UNITYFEATURE_EXTERNALTEXTURES
		unitySupportsExternalTextures = true;
#endif
#if UNITY_3_5
		unitySupportsUnity35OpenGLTextures = true;
#endif
		AVProQuickTimePlugin.SetUnityFeatures(unitySupportsExternalTextures, unitySupportsUnity35OpenGLTextures);
	}

#if AVPRO_UNITY_4_X || UNITY_3_5	

	void Update()
	{
		GL.IssuePluginEvent(AVProQuickTimePlugin.PluginID | (int)AVProQuickTimePlugin.PluginEvent.UpdateAllTextures);
	}
	private IEnumerator FinalRenderCapture()
	{
		while (Application.isPlaying)
		{
			yield return new WaitForEndOfFrame();
			
			GL.IssuePluginEvent(AVProQuickTimePlugin.PluginID | (int)AVProQuickTimePlugin.PluginEvent.UpdateAllTextures);
		}
	}

#endif

	public void Deinit()
	{
		// Clean up any open movies
		AVProQuickTimeMovie[] movies = (AVProQuickTimeMovie[])FindObjectsOfType(typeof(AVProQuickTimeMovie));
		if (movies != null && movies.Length > 0)
		{
			for (int i = 0; i < movies.Length; i++)
			{
				movies[i].UnloadMovie();
			}
		}

		_instance = null;
		_isInitialised = false;
		
		AVProQuickTimePlugin.Deinit();
	}
	
	public Shader GetPixelConversionShader(AVProQuickTimePlugin.PixelFormat format, bool yuvHD)
	{
		Shader result = null;
		switch (format)
		{
		case AVProQuickTimePlugin.PixelFormat.RGBA32:
			result = _shaderBGRA;
			break;
		case AVProQuickTimePlugin.PixelFormat.YCbCr:
			result = _shaderYUV2;
			if (yuvHD)
				result = _shaderYUV2_709;
			break;
		case AVProQuickTimePlugin.PixelFormat.Hap_RGB:
			result = _shaderCopy;
			break;
		case AVProQuickTimePlugin.PixelFormat.Hap_RGBA:
			result = _shaderCopy;
			break;
		case AVProQuickTimePlugin.PixelFormat.Hap_RGB_HQ:
			result = _shaderHap_YCoCg;
			break;
		default:
			Debug.LogError("[AVProQuickTime] Unknown video format '" + format);
			break;
		}
		return result;
	}
}