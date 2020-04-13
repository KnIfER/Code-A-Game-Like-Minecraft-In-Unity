using UnityEngine;
using UnityEditor;

#if UNITY_EDITOR
[CustomEditor(typeof(CloudAvator))]
public class CAEditor : Editor
{
    public override void OnInspectorGUI() {
        DrawDefaultInspector();

        CloudAvator mCloudAvator = (CloudAvator)target;
        if(GUILayout.Button("测试运行")) {
            mCloudAvator.UpdateClouds();
        }
        
        else if(GUILayout.Button("测试运行2")) {
            mCloudAvator.UpdateClouds2();
        }
    }
}
 #endif