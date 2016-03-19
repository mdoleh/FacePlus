using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mixamo;

/* 
This script:
	Listens to one meshes blendshapes and applies the same value to more meshes.
*/

public class FacePlusExtras_MoreBlendshapeMeshes : MonoBehaviour {

	public SkinnedMeshRenderer FaceMesh;
	public List<SkinnedMeshRenderer> ExtraBlendshapeMeshes;
	private List<List<int>> indexMap;

#if !UNITY_4_2
	
	string FindCommonPrefix(List<string> stringList){
		bool stillValid = true;
		string currentPrefix = "";
		int checkIndex = 0;
		while (stillValid){
			if (checkIndex < stringList[0].Length){
				string testPrefix = currentPrefix + stringList[0].Substring(checkIndex,1);
				for(int i=0; i<stringList.Count; i++){
					if (testPrefix != stringList[i].Substring (0,checkIndex+1)){
						stillValid = false;
					}
				}
				if (stillValid){
					currentPrefix += stringList[0].Substring(checkIndex,1);
				}
				checkIndex++;
			}
			else{
				stillValid = false;
			}
		}
		return currentPrefix;
	}


	void Start () {
		List<string> BlendshapeNameList = new List<string>();
		indexMap = new List<List<int>>();
		for(int i=0; i<FaceMesh.sharedMesh.blendShapeCount; i++) {
			BlendshapeNameList.Add(FaceMesh.sharedMesh.GetBlendShapeName(i));
		}
		string BlendshapePrefixMain = FindCommonPrefix(BlendshapeNameList);
		
		for(int i=0; i<ExtraBlendshapeMeshes.Count; i++){
			indexMap.Add (new List<int>());
			List<string> nameList = new List<string>();
			for(int j=0; j<ExtraBlendshapeMeshes[i].sharedMesh.blendShapeCount; j++) {
				nameList.Add ( ExtraBlendshapeMeshes[i].sharedMesh.GetBlendShapeName(j));
			}
			string shapesPrefix = FindCommonPrefix(nameList);
			
			for(int j=0; j<nameList.Count; j++) {
				for(int k=0; k<BlendshapeNameList.Count; k++) {
					if(nameList[j].Remove(0,shapesPrefix.Length-1) == BlendshapeNameList[k].Remove(0,BlendshapePrefixMain.Length-1)){
						indexMap[i].Add(k);
					}
				}
			}
			
		}
	}
	
	
	void LateUpdate () {
		for(int i=0; i<indexMap.Count; i++){
			for(int j=0; j<indexMap[i].Count; j++){
				float curBlendshapeValue = FaceMesh.GetBlendShapeWeight (indexMap[i][j]);
				ExtraBlendshapeMeshes[i].SetBlendShapeWeight (j, curBlendshapeValue);
			}
		}
	}

#endif

}
