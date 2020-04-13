// ==++==
// 
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// 
// ==--==
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VectorList : ReusableList<Vector3>
{
    public Vector3Int baseOffset = new Vector3Int();

    public VectorList(int capacity) : base(capacity)
    {
        
    }

    public void AddWithOffset(ref Vector3 val)
    {
        if (_items.Length>_size)
        {
            ref Vector3 tmp = ref _items[_size++];
            tmp.x = baseOffset.x+val.x;
            tmp.y = baseOffset.y+val.y;
            tmp.z = baseOffset.z+val.z;
        } else {
            Add(baseOffset+val);
        }
    }

    public Vector3[] ToArray(Vector3[] verticesArr)
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