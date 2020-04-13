// ==++==
// 
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// 
// ==--==
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColorList : ReusableList<Color>
{
    public Vector3Int baseOffset = new Vector3Int();
    
    public ColorList(int capacity) : base(capacity)
    {
        
    }

    public void AddAsLightLevel(float wavingFlag, float lightLevel)
    {
        if (_items.Length>_size)
        {
            ref Color tmp = ref _items[_size++];
            tmp.a = lightLevel;
            tmp.r = wavingFlag;
            tmp.g = 0;
            tmp.b = 0;
        } else {
            Add(new Color(wavingFlag , 0, 0, lightLevel));
        }
    }

    public Color[] ToArray(Color[] verticesArr)
    {
        int count = Count;
        if (verticesArr!=null && verticesArr.Length>=count)
        {
            for (int i=0;i<count;i++)
            {
                verticesArr[i] = this[i];
            }
            return verticesArr;
        }
        return ToArray();
    }
}