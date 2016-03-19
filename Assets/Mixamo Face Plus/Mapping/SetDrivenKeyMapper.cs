using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mixamo;

public class SetDrivenKeyMapper : IChannelMapper {

	private Transform leftEye;
	private Transform rightEye;
	private Transform headJoint;
	private List<Transform> faceJoints;
	private TextAsset SDK_Definition;

	
	private Dictionary<Transform, Vector3> initialTranslation;
	private Dictionary<Transform, Vector3> initialRotation;
	private Dictionary<Transform, Vector3> initialScale;
	private List<Transform> joints;

	public SetDrivenKeyMapper(TextAsset SDK_Definition, List<Transform> faceJoints, Transform headJoint, 
	                          Transform leftEyeJoint, Transform rightEyeJoint, FaceCapture faceCapture) {
		this.SDK_Definition = SDK_Definition;
		this.faceJoints = faceJoints; 
		this.headJoint = headJoint;
		this.leftEye = leftEyeJoint;
		this.rightEye = rightEyeJoint;

		initialTranslation = new Dictionary<Transform, Vector3>();
		initialRotation = new Dictionary<Transform, Vector3>();
		initialScale = new Dictionary<Transform, Vector3>();
		foreach(Transform j in faceJoints){
			initialTranslation.Add(j,j.localPosition);
			initialRotation.Add(j,j.localEulerAngles);
			initialScale.Add(j,j.localScale);
		}

		faceCapture.OnChannelsUpdated += SolveSetDrivenKeys;
	}
	
	public Dictionary<string, AnimationTarget> CreateMap() {
		var channelMapping = new Dictionary<string, AnimationTarget>();
		//create empty targets for all face joints
		for (int i=0; i< FacePlus.ChannelCount; i++){
			string channelName = FacePlus.GetChannelName(i);
			if (! channelName.Contains("Head_Joint::") && ! channelName.Contains("_Eye_Joint::")){
				Debug.Log ("Mapped channel to SDK: " + channelName + " => " + faceJoints);
				channelMapping.Add (channelName, new SetDrivenKeyTarget(faceJoints));
			}
		}
		
		
		//read preset if any		
		Hashtable sdkDefinition = (Hashtable)JSON.JsonDecode (SDK_Definition.text);
		if (sdkDefinition != null) {
			foreach(var sdkKey in sdkDefinition.Keys) {
				var sdk = (Hashtable)sdkDefinition[sdkKey];
				string sdkName = "Mix::" + sdkKey;
				foreach(var jointKey in sdk.Keys) {
					var joint = (Hashtable)sdk[jointKey];
					//find matching transform
					Transform jointXform = faceJoints.Where ((j) => j.name.ToString () == jointKey.ToString ())
						.DefaultIfEmpty (null)
						.LastOrDefault();
					if (jointXform == null) continue;

					float dtx = 0f;
					float dty = 0f;
					float dtz = 0f;
					float drx = 0f;
					float dry = 0f;
					float drz = 0f;
					float dsx = 0f;
					float dsy = 0f;
					float dsz = 0f;
					foreach(var attrKey in joint.Keys) {
						var val = float.Parse (joint[attrKey].ToString ());
						switch((attrKey as string)) {
						case "dtx":
							dtx = val;
							break;
						case "dty":
							dty = val;
							break;
						case "dtz":
							dtz = val;
							break;
						case "drx":
							drx = val;
							break;
						case "dry":
							dry = val;
							break;
						case "drz":
							drz = val;
							break;
						case "dsx":
							dsx = val;
							break;
						case "dsy":
							dsy = val;
							break;
						case "dsz":
							dsz = val;
							break;
						}
					}
					//set mapping
					(channelMapping[sdkName] as SetDrivenKeyTarget).deltaTranslate[jointXform] = new Vector3(dtx,dty,dtz);
					(channelMapping[sdkName] as SetDrivenKeyTarget).deltaRotate[jointXform] = new Vector3(drx,dry,drz);
					(channelMapping[sdkName] as SetDrivenKeyTarget).deltaScale[jointXform] = new Vector3(dsx,dsy,dsz);
				}
			}
		}
		
		
		// Head rotation

		if (headJoint != null) {
			channelMapping.Add ("Head_Joint::Rotation_X", 
			                    new JointTarget("Head_Joint::Rotation_X", TransformComponent.LocalRotationX, headJoint));
			channelMapping.Add ("Head_Joint::Rotation_Y", 
			                    new JointTarget("Head_Joint::Rotation_Y", TransformComponent.LocalRotationY, headJoint, -1.0f));
			channelMapping.Add ("Head_Joint::Rotation_Z", 
			                    new JointTarget("Head_Joint::Rotation_Z", TransformComponent.LocalRotationZ, headJoint, -1.0f));
			// animation requires the w component, but we are animating with euler angles, 
			// so need to add a dummy channel to capture w
			channelMapping.Add ("Head Joint W", 
			                    new JointTarget("Head Joint W", TransformComponent.LocalRotationW, headJoint));
		}
		
		// Left Eye
		if (leftEye != null) {
			channelMapping.Add ("Left_Eye_Joint::Rotation_X", 
			                    new JointTarget("Left_Eye_Joint::Rotation_X", TransformComponent.LocalRotationX, leftEye));
			channelMapping.Add ("Left_Eye_Joint::Rotation_Y", 
			                    new JointTarget("Left_Eye_Joint::Rotation_Y", TransformComponent.LocalRotationY, leftEye, -1.0f));
			channelMapping.Add ("Left Eye Z", 
			                    new JointTarget("Left Eye Z", TransformComponent.LocalRotationZ, leftEye));
			channelMapping.Add ("Left Eye W", 
			                    new JointTarget("Left Eye W", TransformComponent.LocalRotationW, leftEye));
		}
		
		// Right Eye
		if (rightEye != null) {
			channelMapping.Add ("Right_Eye_Joint::Rotation_X", 
			                    new JointTarget("Right_Eye_Joint::Rotation_X", TransformComponent.LocalRotationX, rightEye));
			channelMapping.Add ("Right_Eye_Joint::Rotation_Y", 
			                    new JointTarget("Right_Eye_Joint::Rotation_Y", TransformComponent.LocalRotationY, rightEye, -1.0f));
			channelMapping.Add ("Right Eye Z", 
			                    new JointTarget("Right Eye Z", TransformComponent.LocalRotationZ, rightEye));
			channelMapping.Add ("Right Eye W",
			                    new JointTarget("Right Eye W", TransformComponent.LocalRotationW, rightEye));
		}


		// Add all the face joints, so their values get recorded
		foreach(var joint in faceJoints) {
			channelMapping.Add (joint.name + " r.x",
			                    new JointTarget(joint.name + " r.x", TransformComponent.LocalRotationX, joint));
			channelMapping.Add (joint.name + " r.y",
			                    new JointTarget(joint.name + " r.y", TransformComponent.LocalRotationY, joint));
			channelMapping.Add (joint.name + " r.z",
			                    new JointTarget(joint.name + " r.z", TransformComponent.LocalRotationZ, joint));
			channelMapping.Add (joint.name + " r.w",
			                    new JointTarget(joint.name + " r.w", TransformComponent.LocalRotationW, joint));
			/*	
			channelMapping.Add (joint.name + " s.x",
			                    new JointTarget(joint.name + " s.x", TransformComponent.LocalScaleX, joint));
			channelMapping.Add (joint.name + " s.y",
			                    new JointTarget(joint.name + " s.y", TransformComponent.LocalScaleY, joint));
			channelMapping.Add (joint.name + " s.z",
			                    new JointTarget(joint.name + " s.z", TransformComponent.LocalScaleZ, joint));
			                    */
			channelMapping.Add (joint.name + " p.x",
			                    new JointTarget(joint.name + " p.x", TransformComponent.LocalPositionX, joint));
			channelMapping.Add (joint.name + " p.y",
			                    new JointTarget(joint.name + " p.y", TransformComponent.LocalPositionY, joint));
			channelMapping.Add (joint.name + " p.z",
			                    new JointTarget(joint.name + " p.z", TransformComponent.LocalPositionZ, joint));
            /**/
		}

		
		return channelMapping;
	}

	void SolveSetDrivenKeys() {
		foreach(Transform j in faceJoints){
			Vector3 curTranslate = initialTranslation[j];
			Vector3 curRotate = initialRotation[j];
			Vector3 curScale = initialScale[j];
			
			for (int i=0; i< FacePlus.ChannelCount; i++){
				string channelName = FacePlus.GetChannelName(i);
				if (! channelName.Contains("Head_Joint::") && ! channelName.Contains("_Eye_Joint::")){
					var map = FaceCapture.Instance.channelMapping;
					if (!map.ContainsKey (channelName)) return;
					curTranslate += Vector3.Lerp (Vector3.zero, (map[channelName] as SetDrivenKeyTarget).deltaTranslate[j], map[channelName].Amount/100);
					curRotate += Vector3.Lerp (Vector3.zero, (map[channelName] as SetDrivenKeyTarget).deltaRotate[j], map[channelName].Amount/100);
					curScale += Vector3.Lerp (Vector3.zero, (map[channelName] as SetDrivenKeyTarget).deltaScale[j], map[channelName].Amount/100);
				}
			}
			j.localPosition = curTranslate;
			j.localRotation = Quaternion.Euler(curRotate);
			j.localScale = curScale;
		}
	}
}
