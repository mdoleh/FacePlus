using UnityEngine;
using System.Collections;

//-----------------------------------------------------------------------------
// Copyright 2012-2013 RenderHeads Ltd.  All rights reserverd.
//-----------------------------------------------------------------------------

[AddComponentMenu("AVPro QuickTime/Mesh Apply")]
public class AVProQuickTimeMeshApply : MonoBehaviour 
{
	public MeshRenderer _mesh;
	public AVProQuickTimeMovie _movie;
	
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
