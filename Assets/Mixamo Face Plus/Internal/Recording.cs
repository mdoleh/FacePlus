using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Mixamo {
 
public class Channel {
  	public string name;
	public List<Keyframe> keyframes;
	public AnimationCurve curve;
	public AnimationTarget metadata;
	public static bool ReduceKeyframes = false;
	public static float ReductionThreshold = 0.01f;
	public static bool UseReductionThreshold = false;

	public Channel(string name) {
		this.name = name;
		keyframes = new List<Keyframe>();
		metadata = null;
		curve = null;
	}
	
	public void AddKeyframe(float t, float val) {
		// Simplify keys
		if (ReduceKeyframes && keyframes.Count >= 2) {
			var keyStart = keyframes[keyframes.Count-2];
			var keyEnd = keyframes[keyframes.Count-1];
			
			// UseReductionThreshold: be wary, traveler. works "ok" but is not a sophisticated keyframe reduction technique 
			if (UseReductionThreshold) { 
				float deltaStart = Mathf.Abs (keyStart.value - val);
				float deltaEnd = Mathf.Abs (keyEnd.value - val);
				if (   deltaStart < ReductionThreshold
				    && deltaEnd   < ReductionThreshold) {
					keyEnd.time = t;
					keyEnd.value = val;
					return;
				}
			} else {
				if (   Mathf.Approximately (keyStart.value, val)
				    && Mathf.Approximately (keyEnd.value, val)) {
					keyEnd.time = t;
					return;
				}
			}
		}
	
		var k = new Keyframe(t, val);
		keyframes.Add (k);
	}
	
	public void Bake() {
		curve = new AnimationCurve(keyframes.ToArray());
	}
}
 
public class Recording {
	private Dictionary<string,Channel> channels;

	public float CreationTime;
	public float BakeTime;
	public AnimationClip Clip;
	public static bool SmoothTangents = false;
	public static bool ReduceKeyframes {
		set {
			Channel.ReduceKeyframes = value;
		}
		get {
			return Channel.ReduceKeyframes;
		}
	}

	public float BakeProgress = 0f;
	
	public float Length {
		get { return Clip == null? 0f : Clip.length; }	
	}
	
	
	public int ChannelCount {
		get {
			return channels.Count;
		}
	}
	
	public IEnumerable<string> ChannelNames {
		get {
			return channels.Keys;
		}
	}
	
	public Recording() {
		CreationTime = Time.time;
		channels = new Dictionary<string, Channel>();
	}
	
	public Channel GetChannel(string name) {
		Channel channel;
		if (!channels.TryGetValue(name, out channel)) {
			channels[name] = channel = new Channel(name);
		}
		return channel;
	}
		
	public void CreateChannel(string name) {
		channels[name] = new Channel(name);
	}
	
	public void AddKeyframe(string channel, float t, float val) {
		GetChannel (channel).AddKeyframe(t, val);
	}
	
	public float Evaluate(string name, float t) {
		AnimationCurve curve = GetChannel(name).curve;
		if (GetChannel (name).metadata != null && !GetChannel (name).metadata.Recordable) return 0f;
		if (curve == null) throw new System.InvalidOperationException("Curves must be baked before calling Evaluate: (Channel: "+name+")");
		return curve.Evaluate(t);
	}
	
	public void SetMetadata(string name, AnimationTarget metadata) {
		GetChannel (name).metadata = metadata;
	}

#if UNITY_EDITOR
	public IEnumerator Bake() {
		BakeProgress = 0f;
		BakeTime = Time.time;
		Clip = new AnimationClip();
			
		#if !UNITY_4_2
		AnimationUtility.SetAnimationType (Clip, ModelImporterAnimationType.Generic);
		#endif
			
		foreach(var entry in channels) {
			BakeProgress += 1f/ChannelCount;

			if (!entry.Value.metadata.Recordable)
				continue;

			Logger.Info ("Baking " + entry.Key);
			entry.Value.Bake();
			
			Logger.Info ("Path: " + entry.Value.metadata.Path);
			Logger.Info ("Type: " + entry.Value.metadata.AnimationType);
			Logger.Info ("Property Name: " + entry.Value.metadata.PropertyName);
			
			if (SmoothTangents) {
				for(var i=0; i<entry.Value.curve.length; i++) {
					entry.Value.curve.SmoothTangents (i, 0.3f);
				}
			}
			
			AnimationUtility.SetEditorCurve(Clip, 
				entry.Value.metadata.Path, 
				entry.Value.metadata.AnimationType, 
				entry.Value.metadata.PropertyName, entry.Value.curve);

			yield return null;
		}

	}

	public void SaveToClip(AnimationClip newClip) {
		newClip.ClearCurves ();
		foreach(var data in AnimationUtility.GetAllCurves(Clip)) {
			AnimationUtility.SetEditorCurve (newClip, 
			                                 data.path, 
			                                 data.type, 
			                                 data.propertyName, 
			                                 data.curve);
		}
	}
	
	public void Save(string filename) {
		Logger.Info ("Saving here: " + filename);
		AssetDatabase.CreateAsset(Clip, filename);
	}
		
	public IEnumerator Load(AnimationClip c) {
		Clip = c;
		foreach (var entry in channels) {
				if (!entry.Value.metadata.Recordable) continue;

				Debug.Log ("Loading channel from clip: " + entry.Key);
				entry.Value.curve = AnimationUtility.GetEditorCurve (Clip, 
					entry.Value.metadata.Path, 
					entry.Value.metadata.AnimationType, 
					entry.Value.metadata.PropertyName);

				yield return null;
		}
	}
#else
	public void Bake() {}
	public void Save(string name) {}
	public void SaveToClip(AnimationClip clip) {}
	public void Load(AnimationClip c) {}
#endif
		
}
}