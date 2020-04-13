using System;
using System.Collections.Generic;
using UnityEngine;

public class Chunk {
    public ChunkCoord coord;

    GameObject chunkObject;
	MeshRenderer meshRenderer;
	MeshFilter meshFilter;

	int vertexIndex = 0;
	VectorList vertices = new VectorList (4096);
    VectorList normals = new VectorList(4096);
	List<int> triangles = new List<int> ();
    List<int> transparentTriangles = new List<int>();
    Material[] materials = new Material[2];
	Vector2List uvs = new Vector2List (4096);
    ColorList colors = new ColorList(4096);
    Mesh mesh;

    public Vector3 position;

    private bool _isActive;

    ChunkData chunkData;

    public volatile bool waitingForNewLoad=false;

    public ChunkData getFilledData()
    {
        return chunkData;
    }

    public Chunk (ChunkCoord _coord) {
        coord = _coord;
        
        
        chunkObject = new GameObject();

        meshFilter = chunkObject.AddComponent<MeshFilter>();
        meshFilter.mesh = mesh = new Mesh();
        mesh.MarkDynamic();
        //mesh.subMeshCount = 1;

        meshRenderer = chunkObject.AddComponent<MeshRenderer>();

        materials[0] = World.Instance.material;
        materials[1] = World.Instance.transparentMaterial;
        meshRenderer.materials = materials;

        chunkObject.transform.SetParent(World.Instance.transform);
        position = chunkObject.transform.position = new Vector3(coord.x * VoxelData.ChunkWidth, 0f, coord.z * VoxelData.ChunkWidth);
        chunkObject.name = "Chunk " + coord.x + ", " + coord.z;
    }

    public static bool Repositioned;
    private Vector3[] verticesArr;
    private Color[] colorsArr;
    private int[] trianglesArr;
    private Vector3[] normalsArr;
    private Vector2[] uvArr;

    public void Reposition (ChunkCoord _coord) {
        lock (this)
        {
            mesh.MarkDynamic();
            mesh.Clear();

            Repositioned=true;

            coord.chunk = null;

            coord = _coord;

            coord.chunk = this;

            position.Set(coord.x * VoxelData.ChunkWidth, 0f, coord.z * VoxelData.ChunkWidth);

            chunkObject.transform.SetPositionAndRotation(position, Quaternion.identity);

            chunkData = null;

            chunkObject.name = "Chunk " + coord.x + ", " + coord.z;
        }
    }

    Dictionary<ChunkCoord, ChunkData> loadingContext = new Dictionary <ChunkCoord, ChunkData>(9);

    public void PullChunkDataAndUpdateChunk_()
    {
        PullChunkDataAndUpdateChunk ();
    }

	public bool PullChunkDataAndUpdateChunk () {
        ChunkData chunkData = this.chunkData;

        World world = World.Instance;

        int xx = coord.x;
        int zz = coord.z;
        ChunkCoord tmpCoord;
        loadingContext.Clear();
        if(world.checkAbortLoad(xx, zz)) return false;
        loadingContext.Add(tmpCoord=new ChunkCoord(coord.x+1, coord.z+1), world.worldData.RequestChunk(tmpCoord, true, false));
        if(world.checkAbortLoad(xx, zz)) return false;
        loadingContext.Add(tmpCoord=new ChunkCoord(coord.x-1, coord.z+1), world.worldData.RequestChunk(tmpCoord, true, false));
        if(world.checkAbortLoad(xx, zz)) return false;
        loadingContext.Add(tmpCoord=new ChunkCoord(coord.x+1, coord.z-1), world.worldData.RequestChunk(tmpCoord, true, false));
        if(world.checkAbortLoad(xx, zz)) return false;
        loadingContext.Add(tmpCoord=new ChunkCoord(coord.x-1, coord.z-1), world.worldData.RequestChunk(tmpCoord, true, false));
        
        ChunkData chunkDataX, chunkDataZ, chunkDataXN, chunkDataZN;
        if(world.checkAbortLoad(xx, zz)) return false;
        loadingContext.Add(tmpCoord=new ChunkCoord(coord.x+1, coord.z), chunkDataX=world.worldData.RequestChunk(tmpCoord, true, false));
        if(world.checkAbortLoad(xx, zz)) return false;
        loadingContext.Add(tmpCoord=new ChunkCoord(coord.x-1, coord.z), chunkDataXN=world.worldData.RequestChunk(tmpCoord, true, false));
        if(world.checkAbortLoad(xx, zz)) return false;
        loadingContext.Add(tmpCoord=new ChunkCoord(coord.x, coord.z+1), chunkDataZ=world.worldData.RequestChunk(tmpCoord, true, false));
        if(world.checkAbortLoad(xx, zz)) return false;
        loadingContext.Add(tmpCoord=new ChunkCoord(coord.x, coord.z-1), chunkDataZN=world.worldData.RequestChunk(tmpCoord, true, false));
        
        if (chunkData==null || !chunkData.getCoord().Equals(coord)) {
            //Debug.Log("PullChunkDataAndUpdateChunk @ "+coord.x + " "+coord.z);
            chunkData = this.chunkData = world.worldData.RequestChunk(coord, true, true);
        }
        ChunkCoord currentCoord = chunkData.getCoord();
        ChunkCoord newCoord = new ChunkCoord();

        if(world.checkAbortLoad(xx, zz)||chunkData==null) return false;
        lock (world.ChunkStructMods)
        { 
            //world.ChunkStructMods.TryRemove(coord, out VoxelMods vm);
            world.ChunkStructMods.TryGetValue(coord, out VoxelMods vm);
            if (vm != null)
            {
                for(int i = 0; i < vm.Painters.Count; i++)
                {
                    vm.Painters[i].paintToChunk(chunkData);
                }
                world.ChunkStructMods.Remove(coord);
            }
        }


        //if(Repositioned) return;

        // todo also require neighbours' chunk data!

        //lock (World.Instance.ChunkUpdateThreadLock)
         //   World.Instance.chunksToUpdate.Add(this);
        
        //if (World.Instance.settings.enableAnimatedChunks)
        //    chunkObject.AddComponent<ChunkLoadAnimation>();

        ClearMeshData();

        CalculateLight();
        
        // Polygonise

        float pad = 0;// 1.0f / 256;
        uvShift1.y = VoxelData.NormalizedBlockTextureSize-pad;
        uvShift2.x = VoxelData.NormalizedBlockTextureSize-pad;
        uvShift3.x = VoxelData.NormalizedBlockTextureSize-pad;
        uvShift3.y = VoxelData.NormalizedBlockTextureSize-pad;

        ref Vector3Int pos = ref vertices.baseOffset;
		for (int y = 0; y < VoxelData.ChunkHeight; y++) {
            pos.y = y;
			for (int x = 0; x < VoxelData.ChunkWidth; x++) {
                pos.x = x;
				for (int z = 0; z < VoxelData.ChunkWidth; z++) {
                    pos.z = z;
                    if(waitingForNewLoad) return false;
                    //if(world.checkAbortLoad(xx, zz)) return false;
                    if (World.Instance.blocktypes[chunkData.map[x, y, z].id].isSolid)
                    {
					    //UpdateMeshData (new Vector3(x, y, z));
                        byte blockID = chunkData.map[x, y, z].id;
                        // bool isTransparent = world.blocktypes[blockID].renderNeighborFaces;

		                for (int face = 0; face < 6; face++) {
                            //VoxelState neighborState = GetNeighborVoxelState(chunkData, pos + VoxelData.faceChecks[p], false);
                            VoxelState neighborState = GetNeighborVoxelState(pos, face, chunkData, chunkDataX, chunkDataXN, chunkDataZ, chunkDataZN);

			                if (neighborState != null && World.Instance.blocktypes[neighborState.id].renderNeighborFaces) {
				                vertices.AddWithOffset (ref VoxelData.voxelVerts [VoxelData.voxelTris [face, 0]]);
				                vertices.AddWithOffset (ref VoxelData.voxelVerts [VoxelData.voxelTris [face, 1]]);
				                vertices.AddWithOffset (ref VoxelData.voxelVerts [VoxelData.voxelTris [face, 2]]);
				                vertices.AddWithOffset (ref VoxelData.voxelVerts [VoxelData.voxelTris [face, 3]]);

                                for (int i = 0; i < 4; i++)
                                    normals.Add(VoxelData.faceChecks[face]);

                                AddTexture(World.Instance.blocktypes[blockID].GetTextureID(face));

                                float lightLevel = neighborState.globalLightPercent;
                                //if(lightLevel==0) lightLevel=1;
                                //lightLevel=0;

                                float wavingFlag = 0;
                                if(blockID==11)
                                    wavingFlag=1;

                                colors.AddAsLightLevel(wavingFlag, GetNeighborLight(currentCoord, newCoord, pos, face, 0, chunkData, loadingContext));
                                colors.AddAsLightLevel(wavingFlag, GetNeighborLight(currentCoord, newCoord, pos, face, 1, chunkData, loadingContext));
                                colors.AddAsLightLevel(wavingFlag, GetNeighborLight(currentCoord, newCoord, pos, face, 2, chunkData, loadingContext));
                                colors.AddAsLightLevel(wavingFlag, GetNeighborLight(currentCoord, newCoord, pos, face, 3, chunkData, loadingContext));

                                triangles.Add(vertexIndex);
                                triangles.Add(vertexIndex + 1);
                                triangles.Add(vertexIndex + 2);
                                triangles.Add(vertexIndex + 2);
                                triangles.Add(vertexIndex + 1);
                                triangles.Add(vertexIndex + 3);

                                vertexIndex += 4;
			                }
		                }
                    }
				}
			}
		}
        
        if(waitingForNewLoad||world.checkAbortLoad(xx, zz)) return false;

        CreateMesh();

        lock (world)
        {
            world.chunksToDraw.Enqueue(this);
        }
        

        return true;
	}

    void CalculateLight () {
        Queue<Vector3Int> litVoxels = new Queue<Vector3Int>();
        ChunkData chunkData = this.chunkData;
        if(chunkData==null) return;

        for (int x = 0; x < VoxelData.ChunkWidth; x++) {
            for (int z = 0; z < VoxelData.ChunkWidth; z++) {
                float lightRay = 1f;
                for (int y = VoxelData.ChunkHeight - 1; y >= 0; y--) {
                    if(waitingForNewLoad) return;
                    VoxelState thisVoxel = chunkData.map[x, y, z];

                    if (thisVoxel.id > 0 && World.Instance.blocktypes[thisVoxel.id].transparency < lightRay)
                        lightRay = World.Instance.blocktypes[thisVoxel.id].transparency;

                    thisVoxel.globalLightPercent = lightRay;

                    chunkData.map[x, y, z] = thisVoxel;

                    if (lightRay > VoxelData.lightFalloff)
                        litVoxels.Enqueue(new Vector3Int(x, y, z));
                }
            }
        }
        
        //if(false)
        while (litVoxels.Count > 0) {
            Vector3Int v = litVoxels.Dequeue();

            for (int p = 0; p < 6; p++) {
                if(waitingForNewLoad) return;
                Vector3 currentVoxel = v + VoxelData.faceChecks[p];
                Vector3Int neighbor = new Vector3Int((int)currentVoxel.x, (int)currentVoxel.y, (int)currentVoxel.z);

                if (IsVoxelInChunk(neighbor.x, neighbor.y, neighbor.z)) {
                    if (chunkData.map[neighbor.x, neighbor.y, neighbor.z].globalLightPercent < chunkData.map[v.x, v.y, v.z].globalLightPercent - VoxelData.lightFalloff) {
                        chunkData.map[neighbor.x, neighbor.y, neighbor.z].globalLightPercent = chunkData.map[v.x, v.y, v.z].globalLightPercent - VoxelData.lightFalloff;

                        if (chunkData.map[neighbor.x, neighbor.y, neighbor.z].globalLightPercent > VoxelData.lightFalloff)
                            litVoxels.Enqueue(neighbor);
                    }
                }

            }

        }

    }

    void ClearMeshData () {
        vertexIndex = 0;
        vertices.Clear();
        triangles.Clear();
        transparentTriangles.Clear();
        uvs.Clear();
        colors.Clear();
        normals.Clear();
    }

    public bool isActive {
        get { return _isActive; }
        set {
            _isActive = value;
            if (chunkObject != null)
                chunkObject.SetActive(value);
        }
    }

    bool IsVoxelInChunk (int x, int y, int z) {
        if (x < 0 || x >= VoxelData.ChunkWidth || y < 0 || y >= VoxelData.ChunkHeight || z < 0 || z >= VoxelData.ChunkWidth)
            return false;
        else
            return true;
    }

    public void EditVoxel (Vector3 pos, byte newID) {
        int xCheck = Mathf.FloorToInt(pos.x);
        int yCheck = Mathf.FloorToInt(pos.y);
        int zCheck = Mathf.FloorToInt(pos.z);

        xCheck -= Mathf.FloorToInt(chunkObject.transform.position.x);
        zCheck -= Mathf.FloorToInt(chunkObject.transform.position.z);

        chunkData.map[xCheck, yCheck, zCheck].id = newID;

        World world = World.Instance;

        world.worldData.AddToModifiedChunkList(chunkData);

        lock (world.threadingChunks) {
            //World.Instance.chunksToUpdate.Add(this);
            //UpdateSurroundingVoxels(xCheck, yCheck, zCheck);
            //World.Instance.threadingChunks.Add(coord);

            ChunkCoord neighbourNeedRefresh=null;
            if (xCheck==0)
            {
                neighbourNeedRefresh=new ChunkCoord(coord.x-1, coord.z);
            } else if (xCheck==VoxelData.ChunkWidth-1) {
                neighbourNeedRefresh=new ChunkCoord(coord.x+1, coord.z);
            }
            if(neighbourNeedRefresh!=null)
                world.invalidateChunkAt(ref neighbourNeedRefresh, false);
            if (zCheck==0)
            {
                neighbourNeedRefresh=new ChunkCoord(coord.x, coord.z-1);
            } else if (zCheck==VoxelData.ChunkWidth-1) {
                neighbourNeedRefresh=new ChunkCoord(coord.x, coord.z+1);
            }
            if(neighbourNeedRefresh!=null)
                world.invalidateChunkAt(ref neighbourNeedRefresh, false);

            PullChunkDataAndUpdateChunk();

            world.RequstingRefresh = true;

        }
    }

    void UpdateSurroundingVoxels (int x, int y, int z) {
        Vector3 thisVoxel = new Vector3(x, y, z);

        for (int p = 0; p < 6; p++) {
            Vector3 currentVoxel = thisVoxel + VoxelData.faceChecks[p];

            //if (!IsVoxelInChunk((int)currentVoxel.x, (int)currentVoxel.y, (int)currentVoxel.z)) {
            //    World.Instance.chunksToUpdate.Insert(0, World.Instance.GetChunkFromVector3(currentVoxel + position));
            //}
        }

    }
    

    private VoxelState GetNeighborVoxelState(Vector3Int pos, int p, ChunkData chunkData, ChunkData chunkDataX, ChunkData chunkDataXN, ChunkData chunkDataZ, ChunkData chunkDataZN)
    {
        int x = pos.x;
        int y = pos.y;
        int z = pos.z;
        switch (p)
        {
            case 0:
                if (--z < 0)
                {
                    chunkData = chunkDataZN;
                    z = VoxelData.ChunkWidth-1;
                }
            break;
            case 1:
                if (++z >= VoxelData.ChunkWidth)
                {
                    chunkData = chunkDataZ;
                    z = 0;
                }
            break;
            case 2:
                if (++y >= VoxelData.ChunkHeight) return null;
            break;
            case 3:
                if (--y < 0) return null;
            break;
            case 4:
                if (--x < 0)
                {
                    x = VoxelData.ChunkWidth-1;
                    chunkData = chunkDataXN;
                }
            break;
            case 5:
                if (++x >= VoxelData.ChunkWidth)
                {
                    chunkData = chunkDataX;
                    x = 0;
                }
            break;
        }
        return chunkData.map [x, y, z];
    }

    
    private float GetNeighborLight(ChunkCoord currentCoord, ChunkCoord newCoord, Vector3Int pos, int p, int vertice, ChunkData chunkData, Dictionary <ChunkCoord, ChunkData> loadingContext)
    {
        VoxelState N0 = GetNeighborVoxelState(currentCoord, newCoord, chunkData, pos+VoxelData.faceLightChecks[p*16+4*vertice], loadingContext);
        VoxelState N1 = GetNeighborVoxelState(currentCoord, newCoord, chunkData, pos+VoxelData.faceLightChecks[p*16+4*vertice+1], loadingContext);
        VoxelState N2 = GetNeighborVoxelState(currentCoord, newCoord, chunkData, pos+VoxelData.faceLightChecks[p*16+4*vertice+2], loadingContext);
        VoxelState N3 = GetNeighborVoxelState(currentCoord, newCoord, chunkData, pos+VoxelData.faceLightChecks[p*16+4*vertice+3], loadingContext);
        if(N0==null) return 0;
        if(N1==null) return 0;
        if(N2==null) return 0;
        if(N3==null) return 0;
        float ret = N0.globalLightPercent*((N0.id==0?1:0.6f)
            +N1.globalLightPercent*(N1.id==0?1:0.6f)
            +N2.globalLightPercent*(N2.id==0?1:0.6f)
            +N3.globalLightPercent*(N3.id==0?1:0.6f)
            )/4;
        return ret;

    }

    //deprecating
	VoxelState GetNeighborVoxelState (ChunkData chunkData, Vector3Int pos, bool cxch) {
        if (!IsVoxelInChunk(pos.x, pos.y, pos.z))
            return cxch?World.Instance.GetNeighborVoxelStateAt(pos + position):null;

		return chunkData.map [pos.x, pos.y, pos.z];
	}


	VoxelState GetNeighborVoxelState (ChunkCoord currentCoord, ChunkCoord newCoord, ChunkData chunkData, Vector3Int pos, Dictionary <ChunkCoord, ChunkData> loadingContext) {
        int x = pos.x;
        int z = pos.z;
        int y = pos.y;
        if(x >= 0 && x < VoxelData.ChunkWidth && z >= 0 && z < VoxelData.ChunkWidth){
            if(y < 0 || y >= VoxelData.ChunkHeight) return null;
		    return chunkData.map [x, y, z];
        }
        if(VoxelData.debugChunks) return null;
        x = currentCoord.x*VoxelData.ChunkWidth+x;
        z = currentCoord.z*VoxelData.ChunkWidth+z;
        newCoord.Set(
            Mathf.FloorToInt(x/(float)VoxelData.ChunkWidth),
            Mathf.FloorToInt(z/(float)VoxelData.ChunkWidth)
            );
        
        loadingContext.TryGetValue(newCoord, out ChunkData data);
        if (data != null)
        {
            if(!data.getCoord().Equals(newCoord)) 
                return null;
            return data.getVoxel(x-newCoord.x*VoxelData.ChunkWidth, y, z-newCoord.z*VoxelData.ChunkWidth);
        
        }
        return null;
	}

    public VoxelState GetVoxelFromGlobalVector3 (Vector3 pos) {
        int xCheck = Mathf.FloorToInt(pos.x);
        int yCheck = Mathf.FloorToInt(pos.y);
        int zCheck = Mathf.FloorToInt(pos.z);

        xCheck -= Mathf.FloorToInt(position.x);
        zCheck -= Mathf.FloorToInt(position.z);

        return chunkData.map[xCheck, yCheck, zCheck];
    }

	public void CreateMesh () {
        lock (this)
        {
            //if(Repositioned) return;
            //mesh.Clear();
            //mesh = new Mesh();
            //todo manage memory?
            //Debug.LogError("Length="+(verticesArr==null?0:verticesArr.Length)+"_"+vertices.Count);
		    verticesArr = vertices.ToArray ();
            colorsArr = colors.ToArray();
            normalsArr = normals.ToArray();
            uvArr = uvs.ToArray();
            trianglesArr = triangles.ToArray();
        }
	}

	public void AssignMesh () {
        lock (this)
        {
            //CreateMesh();
            //todo 
            //if(Repositioned) return;
            try{
                if (coord.x*VoxelData.ChunkWidth!=position.x||coord.z*VoxelData.ChunkWidth!=position.z)
                {
                    throw new InvalidOperationException();
                }
                mesh.Clear();
                mesh.vertices = verticesArr;
                mesh.normals = normalsArr;
                mesh.uv = uvArr;
                mesh.colors = colorsArr;
                mesh.SetTriangles(trianglesArr, 0);
            } catch (Exception e) {
                Debug.LogError(e);
                mesh.Clear();
            }

            //mesh.MarkModified();

            //mesh.OptimizeIndexBuffers();

            //meshFilter.mesh = mesh;
        }
	}

    void AddTexture (int textureID) {
        float y = textureID / VoxelData.TextureAtlasSizeInBlocks;
        float x = textureID - (y * VoxelData.TextureAtlasSizeInBlocks);

        x *= VoxelData.NormalizedBlockTextureSize;
        y *= VoxelData.NormalizedBlockTextureSize;

        y = 1f - y - VoxelData.NormalizedBlockTextureSize;

        float pad = 0;// 1.0f / 256;
        x += pad;
        y += pad;

        uvs.AddWithOffset(x, y, ref uvShift0);
        uvs.AddWithOffset(x, y, ref uvShift1);
        uvs.AddWithOffset(x, y, ref uvShift2);
        uvs.AddWithOffset(x, y, ref uvShift3);
    }

    Vector2 uvShift0 = new Vector2();
    Vector2 uvShift1 = new Vector2();
    Vector2 uvShift2 = new Vector2();
    Vector2 uvShift3 = new Vector2();

}

public class ChunkCoord {
    public int x;
    public int z;
    internal Chunk chunk;

    public ChunkCoord () {
        x = 0;
        z = 0;
    }

    public ChunkCoord (ChunkCoord other) {
        Set(other);
    }

    public ChunkCoord (int _x, int _z) {
        x = _x;
        z = _z;
    }

    public ChunkCoord(Vector3 pos)
    {
        x = Mathf.FloorToInt(pos.x) / VoxelData.ChunkWidth;
        z = Mathf.FloorToInt(pos.z) / VoxelData.ChunkWidth;
    }
    
    public void Set (int _x, int _z) {
        x = _x;
        z = _z;
    }

    internal void Set(ChunkCoord other)
    {
        x = other.x;
        z = other.z;
    }

    public bool Equals (ChunkCoord other) {
        //if (other == null) return false;
        return other.x == x && other.z == z;
    }

    public override bool Equals(object other) {
        if (other == null)
            return false;
        // Don't want a Is-ChunkCoord check. 
        ChunkCoord oc = (ChunkCoord)other;
        return oc.x == x && oc.z == z;
    }

    public override string ToString() {
        return x+"_"+z;
    }

    public override int GetHashCode () {
        return ((x<<16)|z)^(x+z);
    }
    int mUpdateToken=-1;
    int dist=0;
    public int getDistanceToR0(int updateToken)
    {
        if(mUpdateToken!=updateToken) {
            int val = x-World.Instance.rStartCoord.x;
            dist = val*val;
            val = z-World.Instance.rStartCoord.z;
            dist += val*val;
            mUpdateToken = updateToken;
        }
        return dist;
    }

    internal bool EqualCoords(int _x, int _z)
    {
        return _x == x && _z == z;
    }
}

[HideInInspector]
[System.Serializable]
public class VoxelState {
    public byte id;
    public float globalLightPercent;

    public VoxelState () {
        id = 0;
        globalLightPercent = 0f;
    }

    public VoxelState (byte _id) {
        id = _id;
        globalLightPercent = 0f;
    }
}
