// Support for using externally created native textures, from Unity 4.3 upwards
#if UNITY_4_3 || UNITY_4_4 || UNITY_4_5 || UNITY_4_6
	//#define AVPROQUICKTIME_UNITYFEATURE_EXTERNALTEXTURES
#endif

// Support for DirectX and OpenGL native texture updating, from Unity 4.0 upwards
#if UNITY_4_5 || UNITY_4_4 || UNITY_4_6 || UNITY_4_3 || UNITY_4_2 || UNITY_4_1 || UNITY_4_0_1 || UNITY_4_0
	#define AVPRO_UNITY_4_X
#endif

// Support for power-of-2 texture detection, from Unity 4.1 upwards
#if UNITY_4_4 || UNITY_4_5 || UNITY_4_6 || UNITY_4_3 || UNITY_4_2 || UNITY_4_1
	#define AVPRO_UNITY_POT
#endif

using UnityEngine;
using System.Text;
using System.Collections.Generic;
using System.Runtime.InteropServices;

//-----------------------------------------------------------------------------
// Copyright 2012-2014 RenderHeads Ltd.  All rights reserved.
//-----------------------------------------------------------------------------

public class AVProQuickTimeFormatConverter : System.IDisposable
{
	private int _movieHandle;
	
	// Format conversion and texture output
	private Texture2D _rawTexture;
	private RenderTexture _finalTexture;
	private Material _conversionMaterial;
	private int _usedTextureWidth, _usedTextureHeight;
	private Vector4 _uv;	
	private int _lastFrameUploaded;
	private int _frameUploadCount;
	private int _lastFrameDisplayed;
	private int _frameDisplayCount;

#if !AVPRO_UNITY_4_X
	// For DirectX texture updates in Unity 3.x
	private GCHandle _frameHandle;
	private Color32[] _frameData;
#endif
	
	// Conversion params
	private int _width;
	private int _height;
	private bool _flipX;
	private bool _flipY;
	private AVProQuickTimePlugin.PixelFormat _sourceVideoFormat;
	private bool _requiresTextureCrop;
	
	public Texture OutputTexture
	{
		get { return _finalTexture; }
	}

	public int DisplayFrame
	{
		get { return _lastFrameDisplayed; }
	}

	public int DisplayFrameCount
	{
		get { return _frameDisplayCount; }
	}
	
	public bool	ValidPicture { get; private set; }
	
	public void Reset()
	{
		ValidPicture = false;
		_lastFrameUploaded = 0;
		_frameUploadCount = 0;
		_lastFrameDisplayed = 0;
		_frameDisplayCount = 0;
	}

	public bool Build(int movieHandle, int width, int height, AVProQuickTimePlugin.PixelFormat format, bool yuvHD, bool flipX, bool flipY)
	{
		Reset();

		_movieHandle = movieHandle;
		
		_width = width;
		_height = height;
		_sourceVideoFormat = format;
		_flipX = flipX;
		_flipY = flipY;

		if (CreateMaterial(yuvHD))
		{
#if AVPROQUICKTIME_UNITYFEATURE_EXTERNALTEXTURES
			System.IntPtr texPtr = AVProQuickTimePlugin.GetTexturePointer(_movieHandle);
			if (texPtr != System.IntPtr.Zero)
			{
				int texWidth = _width;
				TextureFormat textureFormat = TextureFormat.RGBA32;
				switch (format)
				{
					case AVProQuickTimePlugin.PixelFormat.YCbCr:
						texWidth = _width / 2;
						break;
					case AVProQuickTimePlugin.PixelFormat.Hap_RGBA:
					case AVProQuickTimePlugin.PixelFormat.Hap_RGB_HQ:
						textureFormat = TextureFormat.DXT5;
						break;
					case AVProQuickTimePlugin.PixelFormat.Hap_RGB:
						textureFormat = TextureFormat.DXT1;
						break;
				}

				_usedTextureWidth = texWidth;
				_usedTextureHeight = _height;
				_rawTexture = Texture2D.CreateExternalTexture(texWidth, _height, textureFormat, false, false, texPtr);
			}
#else
			CreateTexture();
#endif
			if (_rawTexture != null)
			{
				_requiresTextureCrop = (_usedTextureWidth != _rawTexture.width || _usedTextureHeight != _rawTexture.height);

				if (_requiresTextureCrop)
				{
					CreateUVs(_flipX, _flipY);
				}

				switch (AVProQuickTimeManager.Instance.TextureConversionMethod)
				{
					case AVProQuickTimeManager.ConversionMethod.Unity4:
#if !AVPROQUICKTIME_UNITYFEATURE_EXTERNALTEXTURES
#if AVPRO_UNITY_4_X
						AVProQuickTimePlugin.SetTexturePointer(_movieHandle, _rawTexture.GetNativeTexturePtr());
#endif
#endif
						break;
					case AVProQuickTimeManager.ConversionMethod.Unity34_OpenGL:
						// We set the texture per-frame
						break;
					case AVProQuickTimeManager.ConversionMethod.Unity35_OpenGL:
#if UNITY_3_5
						AVProQuickTimePlugin.SetTexturePointer(_movieHandle, new System.IntPtr(_rawTexture.GetNativeTextureID()));
#endif
						break;
					case AVProQuickTimeManager.ConversionMethod.UnityScript:
#if !AVPRO_UNITY_4_X
						CreateBuffer();
#endif
						break;
				}

				// TODO: we don't require conversion for HAP_RGB and HAP_RGBA formats!

				CreateRenderTexture();

				_conversionMaterial.mainTexture = _rawTexture;
				if (!_requiresTextureCrop)
				{
					if (_flipX)
					{
						// Flip X and offset back to get back to normalised range
						_conversionMaterial.mainTextureScale = new Vector2(-1.0f, _conversionMaterial.mainTextureScale.y);
						_conversionMaterial.mainTextureOffset = new Vector2(1.0f, _conversionMaterial.mainTextureOffset.y);
					}
					if (_flipY)
					{
						// Flip Y and offset back to get back to normalised range
						_conversionMaterial.mainTextureScale = new Vector2(_conversionMaterial.mainTextureScale.x, -1.0f);
						_conversionMaterial.mainTextureOffset = new Vector2(_conversionMaterial.mainTextureOffset.x, 1.0f);
					}
				}
				bool formatIs422 = (_sourceVideoFormat == AVProQuickTimePlugin.PixelFormat.YCbCr);
				if (formatIs422)
				{
					_conversionMaterial.SetFloat("_TextureWidth", _finalTexture.width);
				}
			}
		}
		
		return (_conversionMaterial != null);;
	}
		
	public bool Update()
	{
		bool result = UpdateTexture();
		if (result)
		{
			DoFormatConversion();
		}
		return result;
	}

	private bool UpdateTexture()
	{
		bool result = false;

		AVProQuickTimeManager.ConversionMethod method = AVProQuickTimeManager.Instance.TextureConversionMethod;

#if AVPRO_UNITY_4_X || UNITY_3_5
		if (method == AVProQuickTimeManager.ConversionMethod.Unity4 ||
			method == AVProQuickTimeManager.ConversionMethod.Unity35_OpenGL)
		{
			// We update all the textures from AVProQuickTimeManager.Update()
			// so just check if the update was done
			int frameUploadCount = AVProQuickTimePlugin.GetFrameUploadCount(_movieHandle);
			if (_frameUploadCount != frameUploadCount)
			{
				_lastFrameUploaded = AVProQuickTimePlugin.GetLastFrameUploaded(_movieHandle);;
				_frameUploadCount = frameUploadCount;
				result = true;
			}
			result = true;
			return result;
		}
#endif

#if UNITY_3_4
		// Update the OpenGL texture directly
		if (method == AVProQuickTimeManager.ConversionMethod.Unity34_OpenGL)
		{
			result = AVProQuickTimePlugin.UpdateTextureGL(_movieHandle, _rawTexture.GetNativeTextureID());
			GL.InvalidateState();
		}
#endif

#if !AVPRO_UNITY_4_X
		// Update the texture using Unity scripting, this is the slowest method
		if (method == AVProQuickTimeManager.ConversionMethod.UnityScript)
		{
			bool formatIs422 = (_sourceVideoFormat == AVProQuickTimePlugin.PixelFormat.YCbCr);
			if (formatIs422)
			{
				result = AVProQuickTimePlugin.GetFramePixelsYUV2(_movieHandle, _frameHandle.AddrOfPinnedObject(), _rawTexture.width, _rawTexture.height, ref _lastFrameUploaded);
			}
			else
			{
				result = AVProQuickTimePlugin.GetFramePixelsRGBA32(_movieHandle, _frameHandle.AddrOfPinnedObject(), _rawTexture.width, _rawTexture.height, ref _lastFrameUploaded);
			}
			if (result)
			{
				_rawTexture.SetPixels32(_frameData);
				_rawTexture.Apply(false, false);				
			}
		}
#endif
		
		return result;
	}

	public void Dispose()
	{
		ValidPicture = false;
		_width = _height = 0;
		
		if (_conversionMaterial != null)
		{
			_conversionMaterial.mainTexture = null;
			Material.Destroy(_conversionMaterial);
			_conversionMaterial = null;
		}
		
		if (_finalTexture != null)
		{
			RenderTexture.ReleaseTemporary(_finalTexture);
			_finalTexture = null;
		}
#if !AVPROQUICKTIME_UNITYFEATURE_EXTERNALTEXTURES		
		if (_rawTexture != null)
		{			
			Texture2D.Destroy(_rawTexture);
			_rawTexture = null;
		}
#else
		_rawTexture = null;
#endif

#if !AVPRO_UNITY_4_X
		if (_frameHandle.IsAllocated)
		{
			_frameHandle.Free();
			_frameData = null;
		}
#endif
	}

	private bool CreateMaterial(bool yuvHD)
	{	
		Shader shader = AVProQuickTimeManager.Instance.GetPixelConversionShader(_sourceVideoFormat, yuvHD);
		if (shader)
		{
			if (_conversionMaterial != null)
			{
				if (_conversionMaterial.shader != shader)
				{
					Material.Destroy(_conversionMaterial);
					_conversionMaterial = null;
				}
			}
			
			if (_conversionMaterial == null)
			{
				_conversionMaterial = new Material(shader);
				_conversionMaterial.name = "AVProQuickTime-Material";
			}
		}
		
		return (_conversionMaterial != null);
	}

	private void CreateTexture()
	{	
		_usedTextureWidth = _width;
		_usedTextureHeight = _height;

		// Calculate texture size and format
		int textureWidth = _usedTextureWidth;
		int textureHeight = _usedTextureHeight;
		TextureFormat textureFormat = TextureFormat.RGBA32;
		switch (_sourceVideoFormat)
		{
		case AVProQuickTimePlugin.PixelFormat.Hap_RGBA:
		case AVProQuickTimePlugin.PixelFormat.Hap_RGB_HQ:
			textureFormat = TextureFormat.DXT5;
			break;
		case AVProQuickTimePlugin.PixelFormat.Hap_RGB:
			textureFormat = TextureFormat.DXT1;
			break;
		case AVProQuickTimePlugin.PixelFormat.YCbCr:
			textureFormat = TextureFormat.RGBA32;
			_usedTextureWidth /= 2;	// YCbCr422 modes need half width
			textureWidth = _usedTextureWidth;
			break;
		}

		bool requiresPOT = true;
#if AVPRO_UNITY_POT		
		requiresPOT = (SystemInfo.npotSupport == NPOTSupport.None);
#endif
		// If the texture isn't a power of 2
		if (requiresPOT)
		{
			// We use a power-of-2 texture as Unity makes these internally anyway and not doing it seems to break things for texture updates
			if (!Mathf.IsPowerOfTwo(_width) || !Mathf.IsPowerOfTwo(_height))
			{
				textureWidth = Mathf.NextPowerOfTwo(textureWidth);
				textureHeight = Mathf.NextPowerOfTwo(textureHeight);
			}
		}
		
		// Create texture that stores the initial raw frame
		// If there is already a texture, only destroy it if it's too small
		if (_rawTexture != null)
		{
			if (_rawTexture.width != textureWidth || 
				_rawTexture.height < textureHeight || 
				_rawTexture.format != textureFormat)
			{
				Texture2D.Destroy(_rawTexture);
				_rawTexture = null;
			}
		}
		
		if (_rawTexture == null)
		{
			_rawTexture = new Texture2D(textureWidth, textureHeight, textureFormat, false);
			_rawTexture.wrapMode = TextureWrapMode.Clamp;
			_rawTexture.filterMode = FilterMode.Point;
			_rawTexture.name = "AVProQuickTime-RawTexture";
			bool makeUnreadable = true;
#if !AVPRO_UNITY_4_X
			makeUnreadable = false;
#endif
			_rawTexture.Apply(false, makeUnreadable);
		}
	}
	
	private void CreateRenderTexture()
	{	
		// Create RenderTexture for post transformed frames
		// If there is already a renderTexture, only destroy it smaller than desired size
		if (_finalTexture != null)
		{
			if (_finalTexture.width != _width || _finalTexture.height != _height)
			{
				RenderTexture.ReleaseTemporary(_finalTexture);
				_finalTexture = null;
			}
		}

		if (_finalTexture == null)
		{
			ValidPicture = false;
			_finalTexture = RenderTexture.GetTemporary(_width, _height, 0, RenderTextureFormat.ARGB32);
			_finalTexture.wrapMode = TextureWrapMode.Clamp;
			_finalTexture.filterMode = FilterMode.Bilinear;
			_finalTexture.useMipMap = false;
			_finalTexture.name = "AVProQuickTime-FinalTexture";
			_finalTexture.Create();
		}
	}

#if !AVPRO_UNITY_4_X
	private void CreateBuffer()
	{
		// Allocate buffer for non-opengl updates
		if (_frameHandle.IsAllocated && _frameData != null)
		{
			if (_frameData.Length < _rawTexture.width * _rawTexture.height)
			{
				_frameHandle.Free();
				_frameData = null;
			}
		}
		if (_frameData == null)
		{
			_frameData = new Color32[_rawTexture.width * _rawTexture.height];
			_frameHandle = GCHandle.Alloc(_frameData, GCHandleType.Pinned);
		}	
	}
#endif
	
	private void DoFormatConversion()
	{
		if (_finalTexture == null)
			return;

		_finalTexture.DiscardContents();

		if (!_requiresTextureCrop)
		{
			Graphics.Blit(_rawTexture, _finalTexture, _conversionMaterial, 0);
		}
		else
		{
			RenderTexture prev = RenderTexture.active;
			RenderTexture.active = _finalTexture;

			_conversionMaterial.SetPass(0);

			GL.PushMatrix();
			GL.LoadOrtho();
			DrawQuad(_uv);
			GL.PopMatrix();
			
			RenderTexture.active = prev;
		}

		_lastFrameDisplayed = _lastFrameUploaded;
		_frameDisplayCount = _frameUploadCount;
		ValidPicture = true;
	}


	private void CreateUVs(bool invertX, bool invertY)
	{				
		float x1, x2;
		float y1, y2;
		if (invertX)
		{
			x1 = 1.0f; x2 = 0.0f;
		}
		else
		{
			x1 = 0.0f; x2 = 1.0f;
		}
		if (invertY)
		{
			y1 = 1.0f; y2 = 0.0f;
		}
		else
		{
			y1 = 0.0f; y2 = 1.0f;
		}
		
		// Alter UVs if we're only using a portion of the texture
		if (_usedTextureWidth != _rawTexture.width)
		{
			float xd = _usedTextureWidth / (float)_rawTexture.width;
			x1 *= xd; x2 *= xd;
		}
		if (_usedTextureHeight != _rawTexture.height)
		{
			float yd = _usedTextureHeight / (float)_rawTexture.height;
			y1 *= yd; y2 *= yd;
		}
			
		_uv = new Vector4(x1, y1, x2, y2);
	}	

	private static void DrawQuad(Vector4 uv)
	{
		GL.Begin(GL.QUADS);
		
		GL.TexCoord2(uv.x, uv.y);
		GL.Vertex3(0.0f, 0.0f, 0.1f);
		
		GL.TexCoord2(uv.z, uv.y);
		GL.Vertex3(1.0f, 0.0f, 0.1f);
		
		GL.TexCoord2(uv.z, uv.w);		
		GL.Vertex3(1.0f, 1.0f, 0.1f);
		
		GL.TexCoord2(uv.x, uv.w);
		GL.Vertex3(0.0f, 1.0f, 0.1f);
		
		GL.End();
	}
}