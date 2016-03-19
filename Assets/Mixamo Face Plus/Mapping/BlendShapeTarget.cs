using UnityEngine;
using System.Collections;
using System.ComponentModel;
using System.Reflection;
using Mixamo;

public class BlendShapeTarget : AnimationTarget {
	public int Index;
	public override bool Recordable { get { return true; } }
	public override float Scale { 
		get { return _scale; } 
		set { _scale = value; } 
	}
	private float _scale = 100f;
	public override float Offset { 
		get { return _offset; } 
		set { _offset = value; } 
	}
	private float _offset = 0f;
	
	public override float Maximum { get { return 100f; } }
	public override float Minimum { get { return 0f; } }
	public SkinnedMeshRenderer TargetMesh;
	
	public BlendShapeTarget(string name, int shapeIndex, SkinnedMeshRenderer targetMesh) : base(name, targetMesh.transform) {
		Index = shapeIndex;
		TargetMesh = targetMesh;
		AnimationType = typeof(SkinnedMeshRenderer);
	}

	public override float AmountAnim {
		get {
			return Amount;
		}
		set {
			Amount = value;
		}
	}
	
	public override float Amount {
		get {
#if !UNITY_4_2
			return TargetMesh.GetBlendShapeWeight(Index);
#else
			return 0f;
#endif
		}
		set {
#if !UNITY_4_2
			TargetMesh.SetBlendShapeWeight (Index, value);
#endif
		}
	}
	
	public override string PropertyName {
		get {
			return "blendShape." + Name;
		}
	}
}
