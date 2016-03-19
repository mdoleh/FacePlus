using UnityEngine;
using System.Text;
using System.Collections.Generic;
using System.Runtime.InteropServices;

//-----------------------------------------------------------------------------
// Copyright 2012-2013 RenderHeads Ltd.  All rights reserved.
//-----------------------------------------------------------------------------

public class AVProWindowsMediaFormatConverter : System.IDisposable
{
	private int _movieHandle;
	
	// Format conversion and texture output
	private Texture2D _rawTexture;
	private RenderTexture _finalTexture;
	private Material _conversionMaterial;
	private int _usedTextureWidth, _usedTextureHeight;
	private Vector4 _uv;	
	private int _lastFrameUploaded = -1;

//#if !UNITY_4_3 && !UNITY_4_2 && !UNITY_4_1 && !UNITY_4_0_1 && !UNITY_4_0 
#if UNITY_3_5 || UNITY_3_4 || UNITY_3_3 || UNITY_3_2 || UNITY_3_1 || UNITY_3_0
	// For DirectX texture updates in Unity 3.x
	private GCHandle _frameHandle;
	private Color32[] _frameData;
#endif
	
	// Conversion params
	private int _width;
	private int _height;
	private bool _flipX;
	private bool _flipY;
	private AVProWindowsMediaPlugin.VideoFrameFormat _sourceVideoFormat;
	private bool _useBT709;
	
	public Texture OutputTexture
	{
		get { return _finalTexture; }
	}
	
	public int DisplayFrame
	{
		get { return _lastFrameUploaded; }
	}

	public bool	ValidPicture { get; private set; }
	
	public void Reset()
	{
		ValidPicture = false;
		_lastFrameUploaded = -1;
	}
	
	public bool Build(int movieHandle, int width, int height, AVProWindowsMediaPlugin.VideoFrameFormat format, bool useBT709, bool flipX, bool flipY)
	{
		Reset();

		_movieHandle = movieHandle;

		_width = width;
		_height = height;
		_sourceVideoFormat = format;
		_flipX = flipX;
		_flipY = flipY;
		_useBT709 = useBT709;
		
		if (CreateMaterial())
		{
			CreateTexture();
			CreateUVs(_flipX, _flipY);

			switch (AVProWindowsMediaManager.Instance.TextureConversionMethod)
			{
				case AVProWindowsMediaManager.ConversionMethod.Unity4:
//#if UNITY_4_3 || UNITY_4_2 || UNITY_4_1 || UNITY_4_0_1 || UNITY_4_0
#if !UNITY_3_5 && !UNITY_3_4 && !UNITY_3_3 && !UNITY_3_2 && !UNITY_3_1 && !UNITY_3_0
					AVProWindowsMediaPlugin.SetTexturePointer(_movieHandle, _rawTexture.GetNativeTexturePtr());
#endif
					break;
				case AVProWindowsMediaManager.ConversionMethod.Unity34_OpenGL:
					// We set the texture per-frame
					break;
				case AVProWindowsMediaManager.ConversionMethod.Unity35_OpenGL:
#if UNITY_3_5
					AVProWindowsMediaPlugin.SetTexturePointer(_movieHandle, new System.IntPtr(_rawTexture.GetNativeTextureID()));
#endif
					break;
				case AVProWindowsMediaManager.ConversionMethod.UnityScript:
//#if !UNITY_4_3 && !UNITY_4_2 && !UNITY_4_1 && !UNITY_4_0_1 && !UNITY_4_0 
#if UNITY_3_5 || UNITY_3_4 || UNITY_3_3 || UNITY_3_2 || UNITY_3_1 || UNITY_3_0
					CreateBuffer();
#endif
					break;
			}
						
			CreateRenderTexture();
			
			_conversionMaterial.mainTexture = _rawTexture;	
			bool formatIs422 = (_sourceVideoFormat != AVProWindowsMediaPlugin.VideoFrameFormat.RAW_BGRA32);
			if (formatIs422)
			{
				_conversionMaterial.SetFloat("_TextureWidth", _finalTexture.width);
			}
		}
		
		return (_conversionMaterial != null);
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

		AVProWindowsMediaManager.ConversionMethod method = AVProWindowsMediaManager.Instance.TextureConversionMethod;
		
//#if UNITY_4_3 || UNITY_4_2 || UNITY_4_1 || UNITY_4_0_1 || UNITY_4_0 || UNITY_3_5
#if !UNITY_3_4 && !UNITY_3_3 && !UNITY_3_2 && !UNITY_3_1 && !UNITY_3_0
		if (method == AVProWindowsMediaManager.ConversionMethod.Unity4 ||
			method == AVProWindowsMediaManager.ConversionMethod.Unity35_OpenGL)
		{
			// We update all the textures from AVProQuickTimeManager.Update()
			// so just check if the update was done
			int lastFrameUploaded = AVProWindowsMediaPlugin.GetLastFrameUploaded(_movieHandle);
			if (_lastFrameUploaded != lastFrameUploaded)
			{			
				_lastFrameUploaded = lastFrameUploaded;
				result = true;
			}
			return result;
		}
#endif

#if UNITY_3_4
		// Update the OpenGL texture directly
		if (method == AVProWindowsMediaManager.ConversionMethod.Unity34_OpenGL)
		{
			
			result = AVProWindowsMediaPlugin.UpdateTextureGL(_movieHandle, _rawTexture.GetNativeTextureID(), ref _lastFrameUploaded);
			GL.InvalidateState();
		}
#endif

//#if !UNITY_4_3 && !UNITY_4_2 && !UNITY_4_1 && !UNITY_4_0_1 && !UNITY_4_0 
#if UNITY_3_5 || UNITY_3_4 || UNITY_3_3 || UNITY_3_2 || UNITY_3_1 || UNITY_3_0
		// Update the texture using Unity scripting, this is the slowest method
		if (method == AVProWindowsMediaManager.ConversionMethod.UnityScript)
		{
			result = AVProWindowsMediaPlugin.GetFramePixels(_movieHandle, _frameHandle.AddrOfPinnedObject(), _rawTexture.width, _rawTexture.height, ref _lastFrameUploaded);
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
		
		if (_rawTexture != null)
		{			
			Texture2D.Destroy(_rawTexture);
			_rawTexture = null;
		}

//#if !UNITY_4_3 && !UNITY_4_2 && !UNITY_4_1 && !UNITY_4_0_1 && !UNITY_4_0 
#if UNITY_3_5 || UNITY_3_4 || UNITY_3_3 || UNITY_3_2 || UNITY_3_1 || UNITY_3_0
		if (_frameHandle.IsAllocated)
		{
			_frameHandle.Free();
			_frameData = null;
		}
#endif
	}
	

	private bool CreateMaterial()
	{	
		Shader shader = AVProWindowsMediaManager.Instance.GetPixelConversionShader(_sourceVideoFormat, _useBT709);
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
			}
		}
		
		return (_conversionMaterial != null);
	}	

	private void CreateTexture()
	{
		_usedTextureWidth = _width;
		_usedTextureHeight = _height;
		bool formatIs422 = (_sourceVideoFormat != AVProWindowsMediaPlugin.VideoFrameFormat.RAW_BGRA32);
		if (formatIs422)
			_usedTextureWidth /= 2;
		
		// Calculate texture size
		int textureWidth = _usedTextureWidth;
		int textureHeight = _usedTextureHeight;
		
		bool requiresPOT = true;
//#if UNITY_4_3 || UNITY_4_2 || UNITY_4_1
#if !UNITY_4_0 && !UNITY_3_5 && !UNITY_3_4 && !UNITY_3_3 && !UNITY_3_2 && !UNITY_3_1 && !UNITY_3_0
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
			if (_rawTexture.width < textureWidth || _rawTexture.height < textureHeight)
			{
				Texture2D.Destroy(_rawTexture);
				_rawTexture = null;
			}
		}
		if (_rawTexture == null)
		{
			_rawTexture = new Texture2D(textureWidth, textureHeight, TextureFormat.ARGB32, false);
			_rawTexture.hideFlags = HideFlags.HideAndDontSave;
			_rawTexture.wrapMode = TextureWrapMode.Clamp;
			_rawTexture.filterMode = FilterMode.Point;
			
			/*Color32 black = new Color32(0, 127, 127, 255);
			Color32[] blacks = new Color32[textureWidth * textureHeight];
			for (int i = 0; i < textureWidth * textureHeight; i++)
				blacks[i] = black;
			_rawTexture.SetPixels32(blacks);
			_rawTexture.Apply(false, false);*/
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
			_finalTexture.hideFlags = HideFlags.HideAndDontSave;
			_finalTexture.wrapMode = TextureWrapMode.Clamp;
			_finalTexture.filterMode = FilterMode.Bilinear;
			_finalTexture.useMipMap = false;
			_finalTexture.Create();
		}
	}
	
//#if !UNITY_4_3 && !UNITY_4_2 && !UNITY_4_1 && !UNITY_4_0_1 && !UNITY_4_0 
#if UNITY_3_5 || UNITY_3_4 || UNITY_3_3 || UNITY_3_2 || UNITY_3_1 || UNITY_3_0
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
		
		RenderTexture prev = RenderTexture.active;
		RenderTexture.active = _finalTexture;
		
		_conversionMaterial.SetPass(0);

		GL.PushMatrix();
		GL.LoadOrtho();
		DrawQuad(_uv);
		GL.PopMatrix();
		ValidPicture = true;

		RenderTexture.active = prev;
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