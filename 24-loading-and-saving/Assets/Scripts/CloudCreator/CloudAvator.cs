using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CloudAvator : MonoBehaviour
{
    public int MaximumCoreCount=1;
    public List<GameObject> CloudSlices;
    float tmpTimeDelta;
    int CC;


    void Start()
    {
        CC= CloudSlices.Count;
    }

    public void UpdateClouds()
    {
        CC= CloudSlices.Count;
        Vector3 translate = new Vector3(10, 0, 0);
        int step = 1;
        for(int i=0;i<CC;i++) {
            int minX = -step; 
            int maxX = step; 
            i = CloudAtlas(i, step, minX, maxX, minX, maxX, maxX, maxX); // fix y
            i = CloudAtlas(i, step, minX, minX, minX, maxX, minX, maxX); // fix y

            i = CloudAtlas(i, step, minX, minX, minX, minX, maxX, maxX); // fix x
            i = CloudAtlas(i, step, maxX, minX, minX, maxX, maxX, maxX); // fix x
            
            i = CloudAtlas(i, step, minX, minX, minX, maxX, maxX, minX); // fix z
            i = CloudAtlas(i, step, minX, minX, maxX, maxX, maxX, maxX); // fix z

            step+=1;
        }
    }
    
    public void UpdateClouds2()
    {
        gameObject.transform.Translate(0, 0 , -1);
        UpdateClouds();
    }

    private int CloudAtlas(int i, float step, int x, int y, int z, int x1, int y1, int z1)
    {
        //Debug.LogError("CloudAtlas_"+new Vector3(x,y,z));
        if(i>=CC) return i;
        for (int xx=x; xx<=x1; xx++)
        {
            for (int yy=y; yy<=y1; yy++)
            {
                for (int zz=z; zz<=z1; zz++)
                {
                    if(i>=CC) return i;
                    Vector3 parent = CloudSlices[i].gameObject.transform.parent.position;
                    CloudSlices[i].gameObject.transform.localPosition=
                    new Vector3(xx*8 + Noise.Get3DPerlin(xx+parent.x,yy+parent.y,zz+parent.z, VoxelData.seed, step) *100
                               ,yy*5 + Noise.Get3DPerlin(xx+parent.x,yy+parent.y,zz+parent.z, VoxelData.seed, step) *100
                               ,zz*8 + Noise.Get3DPerlin(xx+parent.x,yy+parent.y,zz+parent.z, VoxelData.seed, step) *100
                    )
                    ;
                    CloudSlices[i].gameObject.transform.localScale = 
                        new Vector3(
                            8/step/step * Noise.Get3DPerlin(xx +parent.x,yy +parent.y,zz +parent.z, VoxelData.seed, step)*2
                        ,5/step/step    * Noise.Get3DPerlin(xx +parent.x,yy +parent.y,zz +parent.z, VoxelData.seed, step)*2
                        ,8/step/step    * Noise.Get3DPerlin(xx +parent.x,yy +parent.y,zz +parent.z, VoxelData.seed, step)*2
                        )
                        ;
                    //CloudSlices[i].gameObject.transform.SetPositionAndRotation(new Vector3(xx,yy,zz), Quaternion.identity);
                    i++;
                }
            }
        }
        return i;
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.T))
            UpdateClouds();
        tmpTimeDelta += Time.deltaTime;
        if (tmpTimeDelta > 0.125)
        {

            tmpTimeDelta = 0;
        }
    }
}
