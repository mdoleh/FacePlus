using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Reflection;

//-----------------------------------------------------------------------------
// Copyright 2012 RenderHeads Ltd.  All rights reserved.
//-----------------------------------------------------------------------------

[CustomEditor(typeof(AVProQuickTimeTextureApply))]
public class AVProQuickTimeTextureApplyEditor : Editor
{
	private AVProQuickTimeTextureApply _apply;
	private Component _component = null;
	private string[] _options = new string[0];
	private int _optionIndex = -1;
	
	public override void OnInspectorGUI()
	{
		_apply = (this.target) as AVProQuickTimeTextureApply;

		DrawDefaultInspector();
		
		if (_component != _apply._component)
			Refresh();
		
		int newIndex = EditorGUILayout.Popup(_optionIndex, _options);
		if (newIndex != _optionIndex)
		{
			_optionIndex = newIndex;
			if (_optionIndex < _options.Length)
				_apply._fieldName = _options[_optionIndex];
		}
	}
	
	private void Refresh()
	{
		_component = _apply._component;
		
		_options = new string[0];
		if (_apply._component != null)
		{
        	FieldInfo[] fields = _apply._component.GetType().GetFields();
			_options = new string[fields.Length];
			int i = 0;
        	foreach (FieldInfo field in fields)
        	{
				_options[i] = field.Name;
				i++;
        	}
		}
	}
}