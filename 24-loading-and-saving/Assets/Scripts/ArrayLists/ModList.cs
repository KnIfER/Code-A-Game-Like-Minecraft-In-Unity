// ==++==
// 
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// 
// ==--==
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ModList : ReusableList<Vector4>
{
    public Vector3Int baseOffset = new Vector3Int();
    
    public ModList(int capacity) : base(capacity)
    {
        
    }

    public void AddWithOffset(ref Vector4 val)
    {
        if (_items.Length>_size)
        {
            ref Vector4 tmp = ref _items[_size++];
            tmp.x = baseOffset.x+val.x;
            tmp.y = baseOffset.y+val.y;
            tmp.z = baseOffset.z+val.z;
        } else {
            Add(val);
        }
    }
}