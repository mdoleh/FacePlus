using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Mixamo;

public class BlendShapeMapper : IChannelMapper {
	public string prefix = "";
	
	private SkinnedMeshRenderer face;
	private Transform leftEye;
	private Transform rightEye;
	private Transform headJoint;
	
	public BlendShapeMapper(SkinnedMeshRenderer face, Transform headJoint, Transform leftEyeJoint, Transform rightEyeJoint) {
		this.face = face;
		this.headJoint = headJoint;
		this.leftEye = leftEyeJoint;
		this.rightEye = rightEyeJoint;
	}
	#if !UNITY_4_2
	public Dictionary<string, AnimationTarget> CreateMap() {
		var channelMapping = new Dictionary<string, AnimationTarget>();
		var shapeToIndexMapping = new Dictionary<string, int>();
		//find prefix if any
		for(int i=0; i<face.sharedMesh.blendShapeCount; i++) {
			string shapeName = face.sharedMesh.GetBlendShapeName(i);
			if (shapeName.Contains("MouthOpen")){
				if (shapeName.Length > "MouthOpen".Length){
					prefix = shapeName.Substring(0,shapeName.Length-"MouthOpen".Length);
				}
			}
		}
		
		for(int i=0; i<face.sharedMesh.blendShapeCount; i++) {
			string shapeName = face.sharedMesh.GetBlendShapeName(i);
			
			string channelName;
			if(prefix != ""){
				channelName = shapeName.Replace (prefix, "Mix::")
				.Replace ("Shape", "");
			}
			else{
				channelName = "Mix::" + shapeName.Replace ("Shape", "");
			}
			
			Logger.Log ("Mapped channel to blend shape: " + channelName + " => " + shapeName);
			channelMapping.Add (channelName, 
			                    new BlendShapeTarget(shapeName, i, face)
			                    );
		}
		
		// Head rotation
		if (headJoint != null) {
			channelMapping.Add ("Head_Joint::Rotation_X", 
			                    new JointTarget("Head_Joint::Rotation_X", TransformComponent.LocalRotationX, headJoint));
			channelMapping.Add ("Head_Joint::Rotation_Y", 
			                    new JointTarget("Head_Joint::Rotation_Y", TransformComponent.LocalRotationY, headJoint, -1.0f));
			channelMapping.Add ("Head_Joint::Rotation_Z", 
			                    new JointTarget("Head_Joint::Rotation_Z", TransformComponent.LocalRotationZ, headJoint, -1.0f));
			// animation requires the w component, but we are animating with euler angles
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
		
		return channelMapping;
	}
	#else
	public Dictionary<string, AnimationTarget> CreateMap() {
		return new Dictionary<string, AnimationTarget>();
	}
	#endif

}
