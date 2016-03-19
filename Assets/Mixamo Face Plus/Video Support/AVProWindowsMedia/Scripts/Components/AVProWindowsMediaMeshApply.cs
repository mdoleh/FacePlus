using UnityEngine;
using System.Collections;

//-----------------------------------------------------------------------------
// Copyright 2012-2013 RenderHeads Ltd.  All rights reserverd.
//-----------------------------------------------------------------------------

[AddComponentMenu("AVPro Windows Media/Mesh Apply")]
public class AVProWindowsMediaMeshApply : MonoBehaviour 
{
	public MeshRenderer _mesh;
	public AVProWindowsMediaMovie _movie;
	
	void Start()
	{
		if (_movie != null && _movie.OutputTexture != null)
		{
			ApplyMapping(_movie.OutputTexture);
		}
	}
	
	void Update()
	{
		if (_movie != null && _movie.OutputTexture != null)
		{
			ApplyMapping(_movie.OutputTexture);
		}
	}
	
	private void ApplyMapping(Texture texture)
	{
		if (_mesh != null)
		{
			foreach (Material m in _mesh.materials)
			{
				m.mainTexture = texture;
			}
		}
	}
	
	public void OnDisable()
	{
		ApplyMapping(null);
	}
}
