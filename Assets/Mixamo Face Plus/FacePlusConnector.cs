using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Mixamo;



public class FacePlusConnector : MonoBehaviour {
	public string CommonPrefix = "";

	public Transform HeadJoint;
	public Transform LeftEyeTransform;
	public Transform RightEyeTransform;
	public IFacePlusVideo Movie;


	public enum RigType {
		Joint_SDK,
		BlendShape
	}



	#if !UNITY_4_2
	[SerializeField]
	public RigType Type = RigType.BlendShape;
	#else
	public RigType Type = RigType.Joint_SDK;
	#endif





	[HideInInspector]
	[SerializeField]
	public List<Transform> FaceJoints;


	[HideInInspector]
	[SerializeField]
	public TextAsset SDK_Definition;

	[HideInInspector]
	[SerializeField]
	public SkinnedMeshRenderer FaceMesh;

	void Start () {
		var faceCapture = gameObject.AddComponent<FaceCapture>();

		faceCapture.CommonPrefix = CommonPrefix;

//#if UNITY_EDITOR_WIN
		// implement a class which adheres to IFaceVideo if you'd like to add support for other video types.
		faceCapture.Movie = Object.FindObjectOfType(typeof(FaceVideoAVProWMV)) as Mixamo.IFacePlusVideo;
//#endif
	}

	void Update () {
		// Wait for Face Plus initialization to attach a mapper, in case mapper depends on channel names...
		if (FacePlus.IsInitComplete && FaceCapture.Instance.Mapper == null){
			Debug.Log ("Creating Mapping");
			// If you'd like to experiment with a different mapping, implement an 
			// IChannelMapper and attach it similarly.
			if (Type == RigType.BlendShape) {
				FaceCapture.Instance.Mapper = new BlendShapeMapper(FaceMesh, HeadJoint, LeftEyeTransform, RightEyeTransform);
			} else if (Type == RigType.Joint_SDK){
				// Quality settings:
				// need blend weights to allow four bones for best joint-driven rig
				QualitySettings.blendWeights = BlendWeights.FourBones;
				FaceCapture.Instance.Mapper = new SetDrivenKeyMapper(SDK_Definition, 
				                                                     FaceJoints, 
				                                                     HeadJoint, 
				                                                     LeftEyeTransform, 
				                                                     RightEyeTransform,
				                                                     FaceCapture.Instance);
			}
		}
	}
}
