using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Mixamo;

public class FacePlusShaper : MonoBehaviour {
	public TextAsset preset;

	[HideInInspector]
	[SerializeField]
	public Dictionary<string, AnimationTarget> channelMapping;

	void Update() {
		if (channelMapping == null 
		    && FaceCapture.Instance != null 
		    && FaceCapture.Instance.channelMapping != null) {
			channelMapping = FaceCapture.Instance.channelMapping;

			if (preset != null) LoadPreset (preset);
		}
	}

	public void LoadPreset(TextAsset preset) {
		if (preset == null) return;

		Hashtable presetHash = (Hashtable)JSON.JsonDecode (preset.text);
		if (presetHash != null) {
			foreach(var key in presetHash.Keys) {
				var setting = (Hashtable)presetHash[key];
				if (setting != null) {
					AnimationTarget t = null;
					if(channelMapping.TryGetValue(key.ToString (), out t)) {
						t.Scale = float.Parse (setting["Scale"].ToString ());
						t.Offset = float.Parse (setting["Offset"].ToString ());
					}
				}
			}
		}
	}

#if UNITY_EDITOR
	public void SavePreset(string path) {
		Hashtable result = new Hashtable();
		foreach(var pair in channelMapping) {
			result[pair.Key] = new Hashtable(new Dictionary<string, float> {
				{"Scale", pair.Value.Scale},
				{"Offset", pair.Value.Offset}
			});
		}

		var json = JSON.JsonEncode (result);
		File.WriteAllText(path, json);
		UnityEditor.AssetDatabase.Refresh();
	}
#endif
}
