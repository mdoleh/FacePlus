using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mixamo;

public class FacePlusExtrasSDK : MonoBehaviour {
	
	public Transform HeadJoint;
	public Transform JawJoint;
	public float MouthOpenJawValue;
	public int headJointSmoothingSteps = 1;
	public Transform PointerJoint;
	
	private Vector3 startingJaw;
	private Vector3 startingHead;
	private Queue<Quaternion> previousRotations;
	private Vector3 startingMainNodePoseTrans;
	private Quaternion startingMainNodePoseRot;
	
	// Use this for initialization
	void Start () {
		startingMainNodePoseTrans = gameObject.transform.localPosition;
		startingMainNodePoseRot = gameObject.transform.localRotation;
		
		previousRotations = new Queue<Quaternion> ();
		if (HeadJoint != null) {
			previousRotations.Enqueue (HeadJoint.localRotation);
		}
		if (JawJoint != null) {
			startingJaw = JawJoint.localEulerAngles;
		}
		if (HeadJoint != null) {
			startingHead = HeadJoint.localEulerAngles;
		}
	}
	
	// Update is called once per frame
	void LateUpdate () {
		gameObject.transform.localPosition = startingMainNodePoseTrans;
		gameObject.transform.localRotation = startingMainNodePoseRot;
		if (FaceCapture.Instance == null || FaceCapture.Instance.channelMapping == null) return;
		float mouthOpenVal = FaceCapture.Instance.channelMapping["Mix::MouthOpen"].Amount;
		float smileLeftVal = FaceCapture.Instance.channelMapping ["Mix::Smile_Left"].Amount;
		float smileRightVal = FaceCapture.Instance.channelMapping ["Mix::Smile_Right"].Amount;
		float JawRot = mouthOpenVal / 100f * MouthOpenJawValue;
		if (JawJoint != null) {
			JawJoint.localRotation = Quaternion.Euler (startingJaw + (new Vector3 (JawRot + 5, 0f, 0f)));
		}
		if (PointerJoint != null) {
			PointerJoint.localRotation = Quaternion.Euler (319.1f, 248.173f, 180f);
		}
		if (previousRotations.Count >= headJointSmoothingSteps) {
			previousRotations.Dequeue ();
		}
		if (HeadJoint != null) {
			previousRotations.Enqueue (HeadJoint.localRotation);
			HeadJoint.localRotation = previousRotations.Aggregate ((a, b) => {
				return Quaternion.Lerp (a, b, 0.5f);
			});
		}
	}
}
