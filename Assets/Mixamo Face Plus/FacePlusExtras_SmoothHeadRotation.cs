using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mixamo;

/* 
This script:
	Smooths out head rotation over a few cycles.
*/

public class FacePlusExtras_SmoothHeadRotation : MonoBehaviour {

	public Transform HeadJoint;
	public int headJointSmoothingSteps = 4;

	private Vector3 startingHead;

	private Queue<Quaternion> previousRotations;

#if !UNITY_4_2

	void Start () {
		previousRotations = new Queue<Quaternion> ();
		previousRotations.Enqueue (HeadJoint.localRotation);
		startingHead = HeadJoint.localEulerAngles;
	}
	
	float clampRotation(float value, float clampValue = 20f){
		float newValue = value;
		if (value <= -180f && value >= -360f+clampValue){
			newValue = -360f+clampValue;
		}
		else if (value > -180f && value <= -clampValue){
			newValue = -1f*clampValue;
		}
		else if (value >= clampValue && value <= 180f){
			newValue = clampValue;
		}
		else if (value <= 360f-clampValue && value >= 180f){
			newValue = 360f-clampValue;
		}
		return newValue;
	}
	
	// Update is called once per frame
	void LateUpdate () {

		if (previousRotations.Count >= headJointSmoothingSteps) {
			previousRotations.Dequeue ();
		}
		
		float x = clampRotation(HeadJoint.localRotation.eulerAngles.x);
		float y = clampRotation(HeadJoint.localRotation.eulerAngles.y);
		float z = clampRotation(HeadJoint.localRotation.eulerAngles.z);
		HeadJoint.eulerAngles = new Vector3(x, (-1*y)+180f, -1*z);
		
		previousRotations.Enqueue (HeadJoint.localRotation);

		HeadJoint.localRotation = previousRotations.Aggregate ((a, b) => {
			return Quaternion.Lerp (a, b, 0.5f);
		});

	}

#endif
}
