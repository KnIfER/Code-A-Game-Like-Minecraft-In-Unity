using System.Collections.Generic;
using UnityEngine;

public class CCEditor : MonoBehaviour
{
    public int MaximumCoreCount=1;
    public int TotalCount=100;
    public Material cloudMat;
    
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
    }
#if UNITY_EDITOR
    private void OnValidate()
    {
        CloudAvator CA = gameObject.GetComponent<CloudAvator>();
        if (enabled && CA!=null)
        {
            int totalCount = TotalCount;
            if (CA.CloudSlices == null)
            {
                CA.CloudSlices = new List<GameObject>(totalCount);
            } else if(CA.CloudSlices.Capacity < totalCount){
                CA.CloudSlices.Capacity = totalCount;
            }
            CA.CloudSlices.Clear();
            int SC = 0;
            int CC = gameObject.transform.childCount;
            for(int i=0;i<CC;i++){
                GameObject childAt = gameObject.transform.GetChild(i).gameObject;
                List<GameObject> toDelete = new List<GameObject>(totalCount);
                if (childAt.name.Contains("slice")) {
                    if(SC<totalCount)
                    {
                        if (cloudMat!=null && !childAt.name.Contains("mat")){
                            MeshRenderer MR = childAt.GetComponent<MeshRenderer>();
                            MR.material = cloudMat;
                        }
                        CA.CloudSlices.Add(childAt);
                        SC++;
                    } else if(!childAt.name.Contains("keep")){
                        toDelete.Add(childAt);
                    }
                }
                int deletCount = toDelete.Count;
                if(CC>0)
                UnityEditor.EditorApplication.delayCall += () =>
                {
                    for(int j=0;j<deletCount;j++){
                        DestroyImmediate(toDelete[j]);
                    }
                }; 
            }
            for(int i=SC;i<totalCount;i++){
                GameObject childAt = new GameObject("slice");
                childAt.transform.parent = CA.transform;
                
                MeshFilter meshFilter = childAt.AddComponent<MeshFilter>();
                meshFilter.mesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
                MeshRenderer meshRenderer = childAt.AddComponent<MeshRenderer>();
                meshRenderer.material = cloudMat;

            }
            CA.MaximumCoreCount = MaximumCoreCount;
        }
    }
#endif
}
