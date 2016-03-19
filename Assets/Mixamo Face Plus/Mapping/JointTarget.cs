using UnityEngine;
using System.Collections;
using System.ComponentModel;
using System.Reflection;
using Mixamo;

public enum TransformComponent {
	[Description("localRotation.x")]
	LocalRotationX,
	
	[Description("localRotation.y")]
	LocalRotationY,
	
	[Description("localRotation.z")]
	LocalRotationZ,
	
	[Description("localRotation.w")]
	LocalRotationW,

	[Description("localScale.x")]
	LocalScaleX,

	[Description("localScale.y")]
	LocalScaleY,

	[Description("localScale.z")]
	LocalScaleZ,

	[Description("localPosition.x")]
	LocalPositionX,

	[Description("localPosition.y")]
	LocalPositionY,

	[Description("localPosition.z")]
	LocalPositionZ,
}

public class JointTarget : AnimationTarget {
	public Transform TargetJoint;
	public override bool Recordable { get { return true; } }
	public override float Maximum { get { return 360f; } }
	public override float Minimum { get { return -360f; } }
	public override float Scale { 
		get { return _scale; } 
		set { _scale = value; } 
	}
	private float _scale = 1f;
	public override float Offset { 
		get { return _offset; } 
		set { _offset = value; } 
	}
	private float _offset = 0f;
	private float flipModifier = 1.0f;
	
	public TransformComponent Subcomponent;
	
	public JointTarget(string name, TransformComponent transComponent, Transform targetTransform, float flip_) : base(name, targetTransform) {
		Subcomponent = transComponent;
		TargetJoint = targetTransform;
		AnimationType = typeof(Transform);
		flipModifier = flip_;
	}
	
	public JointTarget(string name, TransformComponent transComponent, Transform targetTransform) : base(name, targetTransform) {
		Subcomponent = transComponent;
		TargetJoint = targetTransform;
		AnimationType = typeof(Transform);
		flipModifier = 1.0f;
	}

	private Quaternion lastQuaternion = Quaternion.identity;
	private Quaternion q;
	public override float AmountAnim {
		set {
			switch(Subcomponent) {
			case TransformComponent.LocalRotationX:
				q = TargetJoint.localRotation;
				q.x = value;// * flipModifier;
				TargetJoint.localRotation = q;
				break;
			case TransformComponent.LocalRotationY:
				q = TargetJoint.localRotation;
				q.y = value;// * flipModifier;
				TargetJoint.localRotation = q;
				break;
			case TransformComponent.LocalRotationZ:
				q = TargetJoint.localRotation;
				q.z = value;// * flipModifier;
				TargetJoint.localRotation = q;
				break;
			case TransformComponent.LocalRotationW:
				q = TargetJoint.localRotation;
				q.w = value;// * flipModifier;
				TargetJoint.localRotation = q;
				break;
			default:
				Amount = value;// * flipModifier;
				break;
			}


		}

		get {
			switch(Subcomponent) {
			case TransformComponent.LocalRotationX:
				// prevent axis flipping
				if (Quaternion.Dot (TargetJoint.localRotation, lastQuaternion) < 0) {
					Quaternion tmp = TargetJoint.localRotation;
					tmp.x *= -1f;
					tmp.y *= -1f;
					tmp.z *= -1f;
					tmp.w *= -1f;
					TargetJoint.localRotation = tmp;
				}
				lastQuaternion = TargetJoint.localRotation;
				return TargetJoint.localRotation.x;
			case TransformComponent.LocalRotationY:
				return TargetJoint.localRotation.y;
			case TransformComponent.LocalRotationZ:
				return TargetJoint.localRotation.z;
			case TransformComponent.LocalRotationW:
				return TargetJoint.localRotation.w;
			default:
				return Amount;
			}
			return 0f;
		}
	}
	
	public override float Amount {
		set {
			Vector3 tmp = TargetJoint.localRotation.eulerAngles;
			switch(Subcomponent) {
			case TransformComponent.LocalRotationX:
				tmp.x = value* flipModifier;
				break;
			case TransformComponent.LocalRotationY:
				tmp.y = value * flipModifier;
				break;
			case TransformComponent.LocalRotationZ:
				tmp.z = value * flipModifier;
				break;
			case TransformComponent.LocalPositionX:
				var tPosX = TargetJoint.localPosition;
				tPosX.x = value;
				TargetJoint.localPosition = tPosX;
				break;
			case TransformComponent.LocalPositionY:
				var tPosY = TargetJoint.localPosition;
				tPosY.y = value;
				TargetJoint.localPosition = tPosY;
				break;
			case TransformComponent.LocalPositionZ:
				var tPosZ = TargetJoint.localPosition;
				tPosZ.z = value;
				TargetJoint.localPosition = tPosZ;
				break;
			case TransformComponent.LocalScaleX:
				var tScaleX = TargetJoint.localScale;
				tScaleX.x = value;
				TargetJoint.localScale = tScaleX;
				break;
			case TransformComponent.LocalScaleY:
				var tScaleY = TargetJoint.localScale;
				tScaleY.y = value;
				TargetJoint.localScale = tScaleY;
				break;
			case TransformComponent.LocalScaleZ:
				var tScaleZ = TargetJoint.localScale;
				tScaleZ.z = value;
				TargetJoint.localScale = tScaleZ;
				break;
			}
			
			TargetJoint.localEulerAngles = tmp;
		}
		
		get {
			switch(Subcomponent) {
			case TransformComponent.LocalRotationX:
				return TargetJoint.localRotation.eulerAngles.x;
			case TransformComponent.LocalRotationY:
				return TargetJoint.localRotation.eulerAngles.y;
			case TransformComponent.LocalRotationZ:
				return TargetJoint.localRotation.eulerAngles.z;
			case TransformComponent.LocalRotationW:
				return 0f;
			case TransformComponent.LocalPositionX:
				return TargetJoint.localPosition.x;
			case TransformComponent.LocalPositionY:
				return TargetJoint.localPosition.y;
			case TransformComponent.LocalPositionZ:
				return TargetJoint.localPosition.z;
			case TransformComponent.LocalScaleX:
				return TargetJoint.localScale.x;
			case TransformComponent.LocalScaleY:
				return TargetJoint.localScale.y;
			case TransformComponent.LocalScaleZ:
				return TargetJoint.localScale.z;
			}
			return 0f;
		}
	}
	
	public static string GetEnumDescription(TransformComponent value) {
		FieldInfo fi = value.GetType().GetField(value.ToString());
		
		DescriptionAttribute[] attributes =
			(DescriptionAttribute[])fi.GetCustomAttributes(
				typeof(DescriptionAttribute),
				false);
		
		if (attributes != null &&
		    attributes.Length > 0)
			return attributes[0].Description;
		else
			return value.ToString();
	}
	
	
	public override string PropertyName {
		get {
			return GetEnumDescription(Subcomponent);
		}
	}
}

