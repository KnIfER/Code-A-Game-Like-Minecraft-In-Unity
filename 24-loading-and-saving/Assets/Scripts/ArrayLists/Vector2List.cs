// ==++==
// 
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// 
// ==--==
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Vector2List : ReusableList<Vector2>
{
    
    public Vector2List(int capacity) : base(capacity)
    {
        
    }

    public void AddWithOffset(float x, float y,ref Vector2 val)
    {
        if (_items.Length>_size)
        {
            // hate c# 
            ref Vector2 tmp = ref _items[_size++];
            tmp.x = x+val.x;
            tmp.y = y+val.y;
        } else {
            Add(val);
            _items[_size-1].x+=x;
            _items[_size-1].y+=y;
        }
    }

    public Vector2[] ToArray(Vector2[] verticesArr)
    {
        if (verticesArr!=null && verticesArr.Length==Count)
        {
            for (int i=0;i<verticesArr.Length;i++)
            {
                verticesArr[i] = this[i];
            }
            return verticesArr;
        }
        return ToArray();
    }
}