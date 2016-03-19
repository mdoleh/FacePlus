using UnityEngine;
using System.Collections;
using System.ComponentModel;
using System.Reflection;

namespace Mixamo {

public static class PathExtension {
	public static string GetPath(this Transform t) {
		if (t.parent == null)
			return "/"+t.name;
		else
			return t.parent.GetPath () + "/" + t.name;
	}
}

public abstract class AnimationTarget {
	public static string CommonPrefix = "";
	public static string BasePath = "";
	
	public string Name;
	public System.Type AnimationType;
	public Transform MainTransform;
	
	public AnimationTarget(string name, Transform transform) {
		Name = name;
		MainTransform = transform;
	}
	
	public string DisplayName {
		get {
			if (string.IsNullOrEmpty(CommonPrefix))
				return Name.Replace ("_", " ").Replace ("::", " - ");
			else
				return Name.Replace (CommonPrefix, "").Replace ("_", " ").Replace("::", " - ");
		}
	}
	
	public string Path { // the path to the animated object, relative to the animation controller
		get {
			string path = MainTransform.GetPath ();
			
			if (path.StartsWith (BasePath+"/"))
				path = path.Substring (BasePath.Length+1);
			
			return path;
		}
	}
	
	// Abstract interface
	public abstract bool Recordable { get; }
	public abstract string PropertyName { get; } // the property name to apply the animation data to (used when creating anim clips)
	public abstract float Amount { set; get; } // gets / sets the animated value
	public abstract float AmountAnim { set; get; } // for .anim, if different
	public abstract float Scale { get; set; } // amount to scale facePlus output
	public abstract float Offset { get; set; } // amount to add to facePlus output

//TODO: do we actually need this?	
	public abstract float Maximum { get; } // maximum value possible - just used for displaying sliders
	public abstract float Minimum { get; } // minimum value possible - "
}


}