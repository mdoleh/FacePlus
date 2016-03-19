using UnityEngine;
using System.Collections;
using System;
using System.Text;
using System.Reflection;

//-----------------------------------------------------------------------------
// Copyright 2012-2013 RenderHeads Ltd.  All rights reserverd.
//-----------------------------------------------------------------------------

[AddComponentMenu("AVPro QuickTime/Texture Apply")]
public class AVProQuickTimeTextureApply : MonoBehaviour 
{
	public AVProQuickTimeMovie _movie;
	public Component _component;
	public string _fieldName = "texture";
	
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
		if (_component != null)
		{
        	FieldInfo[] fields = _component.GetType().GetFields();
        	foreach (FieldInfo field in fields)
        	{
				if (field.Name == _fieldName)
				{
					field.SetValue(_component, texture);
					break;
				}
        	}
		}
	}
	
	public void OnDisable()
	{
		ApplyMapping(null);
	}
}
