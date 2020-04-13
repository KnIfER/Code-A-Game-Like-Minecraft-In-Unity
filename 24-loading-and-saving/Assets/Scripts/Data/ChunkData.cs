using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ChunkData {
    // The global position of the chunk. ie, (16, 16) NOT (1, 1). We want to be able to
    // access it as a Vector2Int, but Vector2Int's are not serialized so we won't be able
    // to save them. So we'll store them as ints.
    ChunkCoord coord = new ChunkCoord();
    Queue<Vector3Int> litVoxels = new Queue<Vector3Int>();

    public ChunkCoord getCoord(){ 
        return coord;
    }

    public int pos_x;
    public int pos_y;

    [HideInInspector] // Displaying lots of data in the inspector slows it down even more so hide this one.
    public VoxelState[,,] map = new VoxelState[VoxelData.ChunkWidth, VoxelData.ChunkHeight, VoxelData.ChunkWidth];

    // Constructors take in position data to ensure we never have ChunkData without a position.
    public ChunkData (ChunkCoord pos) {
        RepositionData(pos);
    }
    
    bool IsVoxelInChunk (int x, int y, int z) {
        if (x < 0 || x >= VoxelData.ChunkWidth || y < 0 || y >= VoxelData.ChunkHeight || z < 0 || z >= VoxelData.ChunkWidth)
            return false;
        else
            return true;
    }

    public void RepositionData(ChunkCoord pos){
        coord.Set(pos);
        pos_x = coord.x * VoxelData.ChunkWidth;
        pos_y = coord.z * VoxelData.ChunkWidth;
    }

	public void Populate () {
        VoxelState thisVoxel;
        litVoxels.Clear();
		for (int x = 0; x < VoxelData.ChunkWidth; x++) {
			for (int z = 0; z < VoxelData.ChunkWidth; z++) {
                float lightRay = 1f;
                for (int y = VoxelData.ChunkHeight - 1; y >= 0; y--) {
                    //if(Chunk.Repositioned) {
                        //World.Instance.GetVoxel(x + pos_x, y, z + pos_y);
                        //VoxelState voxelstate = new VoxelState(World.Instance.GetVoxel(x + position.x, y, z + position.y));
                        //Vector3 v3  = new Vector3();
                        //VoxelState voxelstate = new VoxelState();
                        //map[x, y, z] = new VoxelState();
                    //} else
                    thisVoxel = map[x, y, z] = new VoxelState(World.Instance.GenerateVoxel(x + pos_x, y, z + pos_y));

                    if (thisVoxel.id > 0 && World.Instance.blocktypes[thisVoxel.id].transparency < lightRay)
                        lightRay = World.Instance.blocktypes[thisVoxel.id].transparency;

                    thisVoxel.globalLightPercent = lightRay;

                    if (lightRay > VoxelData.lightFalloff)
                        litVoxels.Enqueue(new Vector3Int(x, y, z));

				}
			}
		}
        
        while (litVoxels.Count > 0) {
            Vector3Int v = litVoxels.Dequeue();

            for (int p = 0; p < 6; p++) {
                Vector3 currentVoxel = v + VoxelData.faceChecks[p];
                Vector3Int neighbor = new Vector3Int((int)currentVoxel.x, (int)currentVoxel.y, (int)currentVoxel.z);

                if (IsVoxelInChunk(neighbor.x, neighbor.y, neighbor.z)) {
                    if (map[neighbor.x, neighbor.y, neighbor.z].globalLightPercent < map[v.x, v.y, v.z].globalLightPercent - VoxelData.lightFalloff) {
                        map[neighbor.x, neighbor.y, neighbor.z].globalLightPercent = map[v.x, v.y, v.z].globalLightPercent - VoxelData.lightFalloff;

                        if (map[neighbor.x, neighbor.y, neighbor.z].globalLightPercent > VoxelData.lightFalloff)
                            litVoxels.Enqueue(neighbor);
                    }
                }
            }
        }
        // todo better infrastructures
        
        //World.Instance.applyingModifications = true;

        //if (World.Instance.modifications.Count > 0) {
        //    Queue<VoxelMod> queue = World.Instance.modifications.Peek();
        //
        //    VoxelMod v = queue.Peek();
        //    if (v.position.x>=pos_x&&v.position.x<pos_x+VoxelData.ChunkWidth&&v.position.z>=pos_y&&v.position.z<pos_y+VoxelData.ChunkWidth) {
        //        while (queue.Count>0)
        //        {
        //            v = queue.Dequeue();
        //            map[(int)v.position.x-pos_x, (int)v.position.y, (int)v.position.z-pos_y] = new VoxelState(v.id);
        //        }
        //        World.Instance.modifications.Dequeue();
        //    }
        //}
        //
        //World.Instance.applyingModifications = false;

        World.Instance.worldData.AddToModifiedChunkList(this);
    }

    internal VoxelState getVoxel(int x, int y, int z)
    {
        if(y<0 || y>=VoxelData.ChunkHeight)
            return null;
        return map[x, y, z];
    }
}
