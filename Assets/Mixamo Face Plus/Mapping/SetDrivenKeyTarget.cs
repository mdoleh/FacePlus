using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Mixamo;

public class SetDrivenKeyTarget : AnimationTarget {
	public override bool Recordable {
		get { return false; }
	}

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

	private float _amount = 0f;
	public override float Amount {
		get {
			return _amount;
		}
		set {
			_amount = value;
		}
	}
	
	public override string PropertyName {
		get {
			return "";
		}
	}
	
	public override float AmountAnim {
		set {
			_amount = value;
		}
		get {
			return _amount;
		}
	}
	
	public Dictionary<Transform, Vector3> deltaTranslate;
	public Dictionary<Transform, Vector3> deltaRotate;
	public Dictionary<Transform, Vector3> deltaScale;
	
	public SetDrivenKeyTarget(List<Transform> faceJoints) : base("", faceJoints[0]) {
		deltaTranslate = new Dictionary<Transform, Vector3>();
		deltaRotate = new Dictionary<Transform, Vector3>();
		deltaScale = new Dictionary<Transform, Vector3>();
		foreach(Transform faceJoint in faceJoints){
			deltaTranslate.Add(faceJoint,Vector3.zero);
			deltaRotate.Add(faceJoint,Vector3.zero);
			deltaScale.Add(faceJoint,Vector3.zero);
		}
	}

}
