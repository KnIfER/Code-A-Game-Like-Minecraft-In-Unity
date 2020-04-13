using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[HideInInspector]
[System.Serializable]
public class WorldData {
    public string worldName = "Prototype"; // Will be set by player eventually.
    public int seed;
    //Illeagal Usage
    //[System.NonSerialized]
    //public Dictionary<Vector2Int, ChunkData> chunks = new Dictionary<Vector2Int, ChunkData>();

    [System.NonSerialized]
    public List<ChunkData> modifiedChunks = new List<ChunkData>();

    public WorldData (string _worldName, int _seed) {
        worldName = _worldName;
        seed = _seed;
    }

    public WorldData (WorldData wD) {
        worldName = wD.worldName;
        seed = wD.seed;
    }

    /** Hold peripheral data that could be replaced at any time. Check existing chunkdatas before do actual loading. Should avoid memory peak*/
    public Dictionary<ChunkCoord, ChunkData> PeripheralChunkDataDict = new Dictionary<ChunkCoord, ChunkData>();
    public List<ChunkData> PeripheralChunkDatas = new List<ChunkData>();
    public int PeripheralLoadingCapacity = VoxelData.ChunkWidth*4;
    public int PeripheralCapacity = VoxelData.ChunkWidth*4;//(int)(VoxelData.ChunkWidth*VoxelData.ChunkWidth*1.25);

    public int count = 0;

    /** Request A Chunk At coord position, by either creating or retrieving a ChunkData Structure to be later popu*/
    public ChunkData RequestChunk(ChunkCoord coord, bool create, bool fillInToChunk) {
        count++;
        //Debug.Log("RequestChunk @ " + coord.x+", "+ coord.z);
        ChunkData c=null;

        //if(!coord.EqualCoords(1, 1)) return null;

        //todo keep a track of all ChunkDatas.

        //lock (World.Instance.ChunkListThreadLock) {
            if (!fillInToChunk)
            {
                World.Instance.chunkDict.TryGetValue(coord, out Chunk chunk);
                if(chunk!=null) c = chunk.getFilledData();
                if (c != null) return c;
            }
            
            lock (this)
            {
                PeripheralChunkDataDict.TryGetValue(coord, out c);
            }
            //if(c==null) Debug.Log("Not !!! Found Active Chunk @ " + coord.x+", "+ coord.z);
            if (c != null) {
                if (fillInToChunk)
                {
                    lock (this)
                    {
                        PeripheralChunkDataDict.Remove(coord);
                        PeripheralChunkDatas.Remove(c);
                    }
                }
                return c;
            }
            
            if (create)
            { // LoadChunk
              // If not, we check if it is saved and if yes, get the data from there.
              //c = SaveSystem.LoadChunk(worldName, coord);
              //if (c != null) {
              //    chunks.Add(coord, c);
              //    return c;
              //}

            // If not, add it to the list and populate it's voxels.

                if (PeripheralChunkDatas.Count > (fillInToChunk?PeripheralLoadingCapacity:PeripheralCapacity))
                {
                    lock (this)
                    {
                        c = PeripheralChunkDatas[0];
                        PeripheralChunkDatas.RemoveAt(0);
                        PeripheralChunkDataDict.Remove(c.getCoord());
                        c.RepositionData(coord);
                    }
                } else {
                    c = new ChunkData(coord);
                }

                if (!fillInToChunk)
                {
                    lock (this)
                    {
                        if(!PeripheralChunkDatas.Contains(c))
                            PeripheralChunkDatas.Add(c);
                        if(!PeripheralChunkDataDict.ContainsKey(coord))
                            PeripheralChunkDataDict.Add(coord, c);
                    }
                }

                //if(!Chunk.Repositioned)
                c.Populate();
                return c;
            }
        //}
        return c;
    }

    public bool IsVoxelInWorld (Vector3 pos) {
        if (pos.y >= 0 && pos.y < VoxelData.ChunkHeight)
            return true;
        else
            return false;
    }

    public void AddToModifiedChunkList(ChunkData chunk) {
        //todo optimise
        // Only add to list if ChunkData is not already in the list.
        if (!modifiedChunks.Contains(chunk))
            modifiedChunks.Add(chunk);
    }

    public void SetVoxel (Vector3 pos, byte value) {
        // If the voxel is outside of the world we don't need to do anything with it.
        if (!IsVoxelInWorld(pos))
            return;


        // Find out the ChunkCoord value of our voxel's chunk.
        int x = Mathf.FloorToInt(pos.x / VoxelData.ChunkWidth);
        int z = Mathf.FloorToInt(pos.z / VoxelData.ChunkWidth);

        //Debug.Log("SetVoxel needs RequestChunk @ " + x+", "+ z);

        // Check if the chunk exists. If not, create it.
        ChunkData chunk = RequestChunk(new ChunkCoord(x, z), true, false);
        
        if(chunk==null) return;
        // Then reverse that to get the position of the chunk.
        x *= VoxelData.ChunkWidth;
        z *= VoxelData.ChunkWidth;

        // Then create a Vector3Int with the position of our voxel *within* the chunk.
        //Vector3Int voxel = new Vector3Int((int)(pos.x - x), (int)pos.y, (int)(pos.z - z));
        //Debug.Log(string.Format("{0}, {1}, {2}", voxel.x, voxel.y, voxel.z));
        // Then get the voxel in our chunk.

        chunk.map[(int)(pos.x - x), (int)pos.y, (int)(pos.z - z)].id = value;

        AddToModifiedChunkList(chunk);
    }

    public VoxelState GetVoxel (Vector3 pos, bool load) {
        // If the voxel is outside of the world we don't need to do anything with it.
        if (!IsVoxelInWorld(pos))
            return null;

        // Find out the ChunkCoord value of our voxel's chunk.
        int x = Mathf.FloorToInt(pos.x / VoxelData.ChunkWidth);
        int z = Mathf.FloorToInt(pos.z / VoxelData.ChunkWidth);


        // Check if the chunk exists. If not, create it.
        ChunkData chunk = RequestChunk(new ChunkCoord(x, z), load, false);
        
        // Then reverse that to get the position of the chunk.
        x *= VoxelData.ChunkWidth;
        z *= VoxelData.ChunkWidth;

        // Then create a Vector3Int with the position of our voxel *within* the chunk.
        //Vector3Int voxel = new Vector3Int();

        // Then get the voxel in our chunk.
        if(chunk==null) return null;
        return chunk.map[(int)(pos.x - x), (int)pos.y, (int)(pos.z - z)];
    }
}
