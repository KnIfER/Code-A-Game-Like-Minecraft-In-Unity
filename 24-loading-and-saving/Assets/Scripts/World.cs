using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;
using System.IO;
using System.Collections.Concurrent;
using System;

public class World : MonoBehaviour, ModableWorld{
    public Settings settings;

    [Header("World Generation Values")]
    public BiomeAttributes[] biomes;

    [Range(0f, 1f)]
    public float globalLightLevel;
    public Color day;
    public Color night;

    public Transform player;
    public Vector3 spawnPosition;

    public Material material;
    public Material transparentMaterial;

    public BlockType[] blocktypes;

    //Chunk[,] chunks = new Chunk[VoxelData.WorldSizeInChunks, VoxelData.WorldSizeInChunks];

    List<ChunkCoord> activeChunks = new List<ChunkCoord>();
    public ChunkCoord playerChunkCoord = new ChunkCoord();
    ChunkCoord playerLastChunkCoord = new ChunkCoord();

    //List<ChunkCoord> chunksToCreate = new List<ChunkCoord>();
    //ConcurrentBag<Chunk> threadedChunks = new ConcurrentBag<Chunk>();
    public List<ChunkCoord> threadingChunks = new List<ChunkCoord>();
    List<Chunk> chunkPools = new List<Chunk>();
    public Dictionary<ChunkCoord, Chunk> chunkDict = new Dictionary<ChunkCoord, Chunk>();

    //public List<Chunk> chunksToUpdate = new List<Chunk>();
    public Queue<Chunk> chunksToDraw = new Queue<Chunk>();

    public bool applyingModifications = false;

    public Dictionary<ChunkCoord, VoxelMods> ChunkStructMods = new Dictionary<ChunkCoord, VoxelMods>();
    
    public List<ChunkCoord> ChunkStructModCoords = new List<ChunkCoord>();

    public Queue<VoxelMod> modifications = new Queue<VoxelMod>();

    private bool _inUI = false;

    public Clouds clouds;

    public GameObject debugScreen;

    public GameObject creativeInventoryWindow;
    public GameObject cursorSlot;

    Thread ChunkUpdateThread;
    public object ChunkUpdateThreadLock = new object();

    public object ChunkListThreadLock = new object();

    private static World _instance;
    public static World Instance { get { return _instance; } }

    public WorldData worldData;

    public string appPath;

    private void Awake() {
        // If the instance value is not null and not *this*, we've somehow ended up with more than one World component.
        // Since another one has already been assigned, delete this one.
        if (_instance != null && _instance != this)
            Destroy(this.gameObject);
        // Else set instance to this.
        else
            _instance = this;

        appPath = Application.persistentDataPath;
    }
    
    int CountLands;
    int landPoolsSize;

    public volatile bool  RequstingRefresh;

    private void OnValidate()
    {
        Shader.SetGlobalFloat("minGlobalLightLevel", VoxelData.minLightLevel);
        Shader.SetGlobalFloat("maxGlobalLightLevel", VoxelData.maxLightLevel);
        
        Shader.SetGlobalFloat("GlobalLightLevel", globalLightLevel);
        //Camera.main.backgroundColor = Color.Lerp(night, day, globalLightLevel);
    }


    private void Start() {
        worldData = SaveSystem.LoadWorld("Testing");

        string jsonImport = File.ReadAllText(Application.dataPath + "/settings.cfg");
        settings = JsonUtility.FromJson<Settings>(jsonImport);

        landPoolsSize = settings.GridSize*settings.GridSize;

        UnityEngine.Random.InitState(VoxelData.seed);

        Shader.SetGlobalFloat("minGlobalLightLevel", VoxelData.minLightLevel);
        Shader.SetGlobalFloat("maxGlobalLightLevel", VoxelData.maxLightLevel);

        
        Shader.SetGlobalFloat("GlobalLightLevel", globalLightLevel);
        //Camera.main.backgroundColor = Color.Lerp(night, day, globalLightLevel);

        spawnPosition = new Vector3(50, VoxelData.ChunkHeight - 50f, 50);
        spawnPosition += new Vector3(VoxelData.TileWidthUU/2, 0, VoxelData.TileWidthUU/2);

        LoadWorld();
        //ApplyModifications();
        GenerateWorld();

        GetChunkCoordFromVector3(playerLastChunkCoord);

        if (settings.enableThreading) {
            ChunkUpdateThread = new Thread(new ThreadStart(ThreadedUpdate));
            ChunkUpdateThread.Start();
        }
    }

    int CC;
    public Dictionary<Vector3Int, Vector3Int> PreferedLocations = new Dictionary<Vector3Int, Vector3Int>();

    public List<ChunkCoord> PreferedLocArray = new List<ChunkCoord>();

    //Region Current Coord
    ChunkCoord rCurrentCoord = new ChunkCoord();
    public ChunkCoord rStartCoord = new ChunkCoord();

    public bool EditVoxel(Vector3 position, byte v)
    {
        Chunk chunk = GetChunkFromVector3(position);
        //Debug.LogError("EditVoxel "+(chunk.getFilledData()==null));
        if(chunk!=null && chunk.getFilledData()!=null)
        {
            chunk.EditVoxel(position, v);
            return true;
        }
        return false;
    }


    ChunkCoord MT_TmpCoord = new ChunkCoord();
    float init_show_time=1;
    int R0x;
    int R0z;
    int R0y;
    int rcx;
    int rcz;
    int rCurrentStillx;
    int rCurrentStillz;
    float stillx;
    float stillz;
    int GridSizeCurrent=1;
    Vector3Int baseLocation = new Vector3Int(0 , 0, 0);
    public int minX;
    public int maxX;
    public int minZ;
    public int maxZ;

    float tmpTimeDelta;
    float lastClear = 0;
    
    private void RepositionChunk(Chunk chunkToReposition, ChunkCoord coord){
        chunkToReposition.waitingForNewLoad=true;
        //ChunkCoord from = new ChunkCoord(chunkToReposition.coord);
        lock (chunkDict)
        {
            chunkDict.Remove(chunkToReposition.coord);
            chunkDict.Add(coord, chunkToReposition);
            ChunkData data = chunkToReposition.getFilledData();
            if (data != null) // 需要归还数据 回收
            {
                lock (worldData)
                {
                    if (!worldData.PeripheralChunkDataDict.ContainsKey(chunkToReposition.coord))
                    {
                        worldData.PeripheralChunkDataDict.Add(chunkToReposition.coord, data);
                        worldData.PeripheralChunkDatas.Add(data);
                    }
                }
            }
        }
        chunkToReposition.Reposition(coord);
        //Debug.Log(string.Format("RepositionChunk From::{0}, {1} To::{2}, {3}", from.x, from.z, coord.x, coord.z));
        lock (threadingChunks) {
            //threadingChunks.Insert(0, coord);
            threadingChunks.Remove(coord);
            threadingChunks.Add(coord);
        }
        //BoxDebug.DrawBox(new Vector3(coord.x*VoxelData.TileWidthUU, 40, coord.z*VoxelData.TileWidthUU), new Vector3(VoxelData.TileWidthUU, 10, VoxelData.TileWidthUU), Color.green, 6);
    }

    /** Calculate positions to update. 
     * A grid system that starts at player's position, and flip-expand again and again to reach maximum size.
     * The grid system was first wirten for UE4's landscape. https://www.youtube.com/watch?v=xvZg9IgXLLE&t=8s
     * ... now It's used to initialise and update the voxel map. */
    public void CalculatePositionMap(bool init)
    {
        CC = 0;
        if (init) {
            PreferedLocations.Clear();
            PreferedLocArray.Clear();
        }
        rCurrentCoord.Set(0, 0);
        float relativeX = player.position.x - baseLocation.x;
        float relativeZ = player.position.z - baseLocation.z;

        rStartCoord.x = Mathf.FloorToInt(relativeX / VoxelData.ChunkWidth);
        rStartCoord.z = Mathf.FloorToInt(relativeZ / VoxelData.ChunkWidth);

        int NormalizdRelativeX = rStartCoord.x * VoxelData.ChunkWidth;
        int NormalizdRelativeZ = rStartCoord.z * VoxelData.ChunkWidth;

        R0x = NormalizdRelativeX + baseLocation.x;
        R0z = NormalizdRelativeZ + baseLocation.z;
        R0y = baseLocation.y;
        if (init) {
            BoxDebug.DrawBox(new Vector3(R0x, R0y, R0z), new Vector3(VoxelData.TileWidthUU, 10, VoxelData.TileWidthUU), Color.black, init_show_time);     
            PreferedLocArray.Add(new ChunkCoord(rStartCoord.x, rStartCoord.z));
        }
        int square_case = 0;
        if (relativeX>=NormalizdRelativeX + VoxelData.HalfTileWidth)
        {
            square_case = 1;
        }
        if (relativeZ >= NormalizdRelativeZ + VoxelData.HalfTileWidth)
        {
            square_case += 2;
        }
        
        minX = rStartCoord.x - settings.GridSize/2;
        maxX = rStartCoord.x + settings.GridSize/2;
        minZ = rStartCoord.z - settings.GridSize/2;
        maxZ = rStartCoord.z + settings.GridSize/2;
        Chunk chunkToReposition=null;
        if(!init){
            // Caculate map extensions.
            //Debug.Log(string.Format("MinMax: minX={0}, maxX={1}, minZ={2}, maxZ={3}", minX, maxX, minZ, maxZ));
            if (settings.GridSize%2==0) {
                if((square_case&1)!=0){
                    minX++;
                } else {
                    maxX--;
                }
                if((square_case&2)!=0){
                    minZ++;
                } else {
                    maxZ--;
                }
            }
            for (int i=0;i<landPoolsSize;i++) {
                Chunk c = chunkPools[i];
                ChunkCoord pos = c.coord;
                if (pos.x<minX||pos.x>maxX||pos.z<minZ||pos.z>maxZ) {
                    chunkToReposition = c;
                    break;
                }
            }
            
            if(!chunkDict.ContainsKey(rStartCoord)){ // check coord not occupied
                //Debug.Log("start not occupied!!!");
                if(chunkToReposition!=null){
                    RepositionChunk(chunkToReposition, new ChunkCoord(rStartCoord));
                }
                return;
            }
        }

        bool cornerflipped=square_case == 3||square_case == 2;
        bool typeA=square_case==0||square_case==3;
        GridSizeCurrent = 1;
        rcx = rcz = 0;
        while (GridSizeCurrent < settings.GridSize)
        {
            if (typeA)
            {
                if (!cornerflipped) {
                    rcx--;
                    rcz--;
                }
            } else {
                if (cornerflipped) {
                    rcx--;
                } else {
                    rcz--;
                }
            }
            rCurrentStillx = rcx;
            if (typeA==cornerflipped) {
                 rCurrentStillx += GridSizeCurrent;
            }
            //if(GridSizeCurrent==3)
            //rCurrentStillx-=1;
            stillx = rCurrentStillx*VoxelData.ChunkWidth + R0x;
            rCurrentStillz = rcz;
            if(cornerflipped) {
                rCurrentStillz += GridSizeCurrent;
            }
            stillz = rCurrentStillz*VoxelData.ChunkWidth + R0z;
            // Expand X.
            for(int i = 0; i <= GridSizeCurrent; i++) {
                rCurrentCoord.x = rcx + i;
                MT_TmpCoord.Set(rStartCoord.x+rCurrentCoord.x, rStartCoord.z+rCurrentStillz);
                if (init) {
                   Vector3 pos = new Vector3(R0x + rCurrentCoord.x * VoxelData.TileWidthUU, R0y, stillz);
                   BoxDebug.DrawBox(pos+new Vector3(0, 60, 0), new Vector3(VoxelData.TileWidthUU, 10, VoxelData.TileWidthUU), Color.white, init_show_time);
                   PreferedLocArray.Add(new ChunkCoord(MT_TmpCoord));
                } else if(!chunkDict.ContainsKey(MT_TmpCoord)){ // check coord not occupied
                    if(chunkToReposition!=null){
                        RepositionChunk(chunkToReposition, new ChunkCoord(MT_TmpCoord));
                    }
                    return;
                }
            }
            // Expand Z.
            int ZIndexOffset = cornerflipped?1:0;
            for(int i = 1; i <= GridSizeCurrent; i++) {
                rCurrentCoord.z = rcz + i - ZIndexOffset;
                MT_TmpCoord.Set(rStartCoord.x+rCurrentStillx, rStartCoord.z+rCurrentCoord.z);
                if (init) {
                    Vector3 pos = new Vector3(stillx, R0y, R0z + rCurrentCoord.z * VoxelData.TileWidthUU);
                    BoxDebug.DrawBox(pos+new Vector3(0, 60, 0), new Vector3(VoxelData.TileWidthUU, 10, VoxelData.TileWidthUU), Color.red, init_show_time);
                    PreferedLocArray.Add(new ChunkCoord(MT_TmpCoord));
                } else if(!chunkDict.ContainsKey(MT_TmpCoord)){ // check coord not occupied
                    if(chunkToReposition!=null){
                        RepositionChunk(chunkToReposition, new ChunkCoord(MT_TmpCoord));
                    }
                    return;
                }
            }
            GridSizeCurrent ++;
            cornerflipped = !cornerflipped;
        }
        //Debug.Log(string.Format("PreferedLocArray::{0} GridSize^2::{1}", PreferedLocArray.Count, landPoolsSize));
    }

    private void Update() {
        //if (Time.time - lastClear > 1000) {
            Debug.ClearDeveloperConsole();
        //    lastClear = Time.time;
        //}

        GetChunkCoordFromVector3(playerChunkCoord);

        // Only update the chunks if the player has moved from the chunk they were previously on.
        //if (!playerChunkCoord.Equals(playerLastChunkCoord))
        //    CheckViewDistance();
        tmpTimeDelta += Time.deltaTime;
        //Debug.Log(string.Format("Tick CountLands::{0} GridSize^2::{1}", CountLands, landPoolsSize));
        if (CountLands<landPoolsSize) {// initilise map
            if (tmpTimeDelta > -0.1)
            {
                tmpTimeDelta = 0;
                
                ChunkCoord pos = PreferedLocArray[0];
                PreferedLocArray.RemoveAt(0);
                Chunk newChunk = new Chunk(pos);
                if (!settings.enableThreading) {
                    activeChunks.Add(pos);
                    newChunk.PullChunkDataAndUpdateChunk();
                } else {
                    lock (threadingChunks) {
                        pos.chunk = newChunk;
                        threadingChunks.Add(pos);
                    }
                }
                BoxDebug.DrawBox(new Vector3(pos.x*VoxelData.TileWidthUU, 40, pos.z*VoxelData.TileWidthUU), new Vector3(VoxelData.TileWidthUU, 10, VoxelData.TileWidthUU), Color.red, init_show_time);
                chunkPools.Add(newChunk);
                chunkDict.Add(pos, newChunk);
                CountLands ++;
            }
        } 
        else { // Lazily check and invalidate chunks.
            if (tmpTimeDelta > -0.125)
            {
                lock(unthreadingChunks){
                    int size = unthreadingChunks.Count;
                    if (size>0) {
                        for(int i=0;i<size; i++){ 
                            chunkDict.Remove(unthreadingChunks[i]);
                        }
                        unthreadingChunks.Clear();
                    }
                }
                tmpTimeDelta = 0;

                CalculatePositionMap(false);
            
                if (!settings.enableThreading) {
                    //if (!applyingModifications)
                    //    ApplyModifications();

                    //if (chunksToUpdate.Count > 0)
                    //    UpdateChunks();
                }

            }
        }

        lock (this) {
            while (chunksToDraw.Count > 0)
                chunksToDraw.Dequeue().AssignMesh();
        }

        if (Input.GetKeyDown(KeyCode.F3))
            debugScreen.SetActive(!debugScreen.activeSelf);

        if (Input.GetKeyDown(KeyCode.F1))
            SaveSystem.SaveWorld(worldData);
    }

    public void invalidateChunkAt(ref ChunkCoord neighbourNeedRefresh, bool Immediate)
    {
        lock (chunkDict)
        {
            chunkDict.TryGetValue(neighbourNeedRefresh, out neighbourNeedRefresh.chunk);
        }
        Chunk chunk = neighbourNeedRefresh.chunk;
        if (chunk != null)
        {
            if (Immediate) {
                chunk.PullChunkDataAndUpdateChunk();
                neighbourNeedRefresh.chunk = null;
            } else{
                 lock (threadingChunks) {
                 //   //ChunkUpdateThread.Interrupt();
                 //   chunk.waitingForNewLoad = false;
                 //   threadingChunks.Insert(0, neighbourNeedRefresh);
                 //   //ChunkUpdateThread.Start();
                    new Thread(new ThreadStart(neighbourNeedRefresh.chunk.PullChunkDataAndUpdateChunk_)).Start();   
                 }
                //todo
            }
        }
        neighbourNeedRefresh = null;
    }

    void UpdateChunks () {
        //threadedChunks.TryTake(out Chunk c);
        //if (c != null) {
        //    if (!activeChunks.Contains(c.coord))
        //        activeChunks.Add(c.coord);
        //    c.PullChunkDataAndUpdateChunk();
        //}
    }

    System.Comparison<ChunkCoord> CCC = (x, y) =>
    {
        return x.getDistanceToR0(UpdateToken)-y.getDistanceToR0(UpdateToken);
    };

    class ChunkCoordComaparer : IComparer<ChunkCoord>
    {
        public int Compare(ChunkCoord x, ChunkCoord y)
        {
            return x.getDistanceToR0(UpdateToken)-y.getDistanceToR0(UpdateToken);
        }
    }

    ChunkCoordComaparer ICCC = new ChunkCoordComaparer();

    // Eagerly load chunks and calculate it's mesh data.
    static int UpdateToken = 0;
    List<ChunkCoord> unthreadingChunks = new List<ChunkCoord>();
    void ThreadedUpdate() {
        while (true) {
            //if (!applyingModifications) ApplyModifications();
            ChunkCoord c = null;
            //lock(threadingChunks){
                //todo 排序
                int size = threadingChunks.Count-1;
                if (size >= 0) {
                    if (size > VoxelData.ChunkWidth*2) { // ||RequstingRefresh
                        UpdateToken=(UpdateToken+1)%1024;
                        //Debug.Log(UpdateToken+"");
                        for (int i=0;i<=size;i++) {
                            c = threadingChunks[i];
                            Chunk chunk = threadingChunks[i].chunk;
                            if(chunk==null || c.x < minX || c.x > maxX || c.z < minZ || c.z > maxZ) {
                                lock(unthreadingChunks){
                                    unthreadingChunks.Add(c);
                                }
                                threadingChunks.RemoveAt(i--);
                                size--;
                            }
                        }
                        if (size<0) {
                            continue;
                        }
                        threadingChunks.Sort(ICCC);
                    }
                    c = threadingChunks[0];
                    threadingChunks.RemoveAt(0);
                    RequstingRefresh = false;
                }
            //}
            if (c != null) {
                //if (!activeChunks.Contains(c.coord))
                //    activeChunks.Add(c.coord);
                Chunk  chunk = c.chunk;
                //if (chunk==null || c.x<minX||c.x>maxX||c.z<minZ||c.z>maxZ) {
                //    unthreadingChunks.Add(c);
                //    continue;
                // }
                if(chunk!=null)
                    chunk.waitingForNewLoad = false;

                if(chunk==null || !chunk.PullChunkDataAndUpdateChunk())
                {
                    lock(unthreadingChunks){ 
                        unthreadingChunks.Add(c);
                    }
                    continue;
                }
            }
            Thread.Sleep(1);
        }
    }
    
    internal bool checkAbortLoad(int xx, int zz)
    {
        return xx<minX||xx>maxX||zz<minZ||zz>maxZ;
    }

    void LoadWorld () {
        //for (int x = (VoxelData.WorldSizeInChunks / 2) - settings.loadDistance; x < (VoxelData.WorldSizeInChunks / 2) + settings.loadDistance; x++) {
        //    for (int z = (VoxelData.WorldSizeInChunks / 2) - settings.loadDistance; z < (VoxelData.WorldSizeInChunks / 2) + settings.loadDistance; z++) {
        //        worldData.LoadChunk(new Vector2Int(x, z));
        //    }
        //}
    }


    void GenerateWorld () {
        CountLands = 0;

        player.position = spawnPosition;

        CalculatePositionMap(true);
    }

    private void OnDisable() {
        if (settings.enableThreading) {
            ChunkUpdateThread.Abort();
        }
    }

    //tree pa
    void ApplyModifications () {
        lock (modifications)
        {
            applyingModifications = true;
            int count=0;
            while (modifications.Count > 0 && count<100) {
                count++;

                VoxelMod v = modifications.Dequeue();
                worldData.SetVoxel(v.position, v.id);
            }

            applyingModifications = false;
        }
    }

    void GetChunkCoordFromVector3 (ChunkCoord pos) {
        pos.x = (int)(player.position.x / VoxelData.ChunkWidth);
        pos.z = (int)(player.position.z / VoxelData.ChunkWidth);
    }

    public Chunk GetChunkFromVector3 (Vector3 pos) {
        int x = Mathf.FloorToInt(pos.x / VoxelData.ChunkWidth);
        int z = Mathf.FloorToInt(pos.z / VoxelData.ChunkWidth);
        lock (chunkDict)
        {
            chunkDict.TryGetValue(new ChunkCoord(x, z), out Chunk ret);
            return ret;
        }
    }

    public bool CheckForVoxelAt (Vector3 pos) {
        VoxelState voxel = worldData.GetVoxel(pos, true);
        if(voxel==null) return false;

        if (blocktypes[voxel.id].isSolid)
            return true;
        else
            return false;

    }

    //todo optimise
    public VoxelState GetNeighborVoxelStateAt (Vector3 pos) {
        return worldData.GetVoxel(pos, false);
    }

    public bool inUI {
        get { return _inUI; }

        set {
            _inUI = value;
            if (_inUI) {
                //Cursor.lockState = CursorLockMode.None;
                //Cursor.visible = true;
                creativeInventoryWindow.SetActive(true);
                cursorSlot.SetActive(true);
            } else {
                //Cursor.lockState = CursorLockMode.Locked;
                //Cursor.visible = false;
                creativeInventoryWindow.SetActive(false);
                cursorSlot.SetActive(false);
            }
        }
    }

    public void AddModAt(int x, int y, int z, int id)
    {
        
        //Vector3 pos = new Vector3(x, y, z);
        //modifications.Enqueue(new VoxelMod(ref pos,(byte)id));
        
        //ChunkStructMods.GetOrAdd
    }

    public byte GenerateVoxel (int x, int y, int z) {
        /* IMMUTABLE PASS */
        //if(y==0) return 1;
        //return 0;

        // If outside world, return air.
        //IsVoxelInWorld(pos)
        if (y < 0 || y >= VoxelData.ChunkHeight)
            return 0;

        // If bottom block of chunk, return bedrock.
        if (y == 0)
            return 1;

        /* BIOME SELECTION PASS*/

        int solidGroundHeight = 42;
        float sumOfHeights = 0f;
        int count = 0;
        float strongestHeight = 0f;
        float strongestWeight = 0f;
        int strongestBiomeIndex = 0;

        for (int i = 0; i < biomes.Length; i++) {
            float weight = Noise.Get2DPerlin(x, z, biomes[i].offset, biomes[i].scale);

            // Get the height of the terrain (for the current biome) and multiply it by its weight.
            float height = biomes[i].terrainHeight * Noise.Get2DPerlin(x, z, 0, biomes[i].terrainScale) * weight;
            
            // Keep track of which weight is strongest.
            if (weight > strongestWeight) {
                strongestWeight = weight;
                strongestBiomeIndex = i;
                strongestHeight = height;
            }

            // If the height value is greater 0 add it to the sum of heights.
            if (height > 0) {
                sumOfHeights += height;
                count++;
            }
        }

        // Set biome to the one with the strongest weight.
        BiomeAttributes biome = biomes[strongestBiomeIndex];

        // Get the average of the heights.
        sumOfHeights /= count;

        sumOfHeights = sumOfHeights*3/4 + strongestHeight*1/4;

        int terrainHeight = (int)(sumOfHeights + solidGroundHeight);

        //terrainHeight = (int)(sumOfHeights/4 + solidGroundHeight);

        //BiomeAttributes biome = biomes[index];

        /* BASIC TERRAIN PASS */

        byte voxelValue = 0;

        if (y == terrainHeight)
            voxelValue = biome.surfaceBlock;
        else if (y < terrainHeight && y > terrainHeight - 4)
            voxelValue = biome.subSurfaceBlock;
        else if (y > terrainHeight)
            return 0;
        else
            voxelValue = 2;

        /* SECOND PASS */

        if (voxelValue == 2) {
            foreach (Lode lode in biome.lodes) {
                if (y > lode.minHeight && y < lode.maxHeight)
                    if (Noise.Get3DPerlin(x, y, z, lode.noiseOffset, lode.scale, lode.threshold))
                        voxelValue = lode.blockID;
            }
        }

        /* TREE PASS */
        //if(false)
        if (y == terrainHeight && biome.placeMajorFlora) {
            if (Noise.Get2DPerlin(x, z, 0, biome.majorFloraZoneScale) > biome.majorFloraZoneThreshold) {
                //voxelValue = 6;
                if (Noise.Get2DPerlin(x, z, 0, biome.majorFloraPlacementScale) > biome.majorFloraPlacementThreshold) {
                    //voxelValue = 12;
                    lock (modifications)
                    {
                        //Structure.GenerateMajorFlora(this, biome.majorFloraIndex, x, y, z, biome.minHeight, biome.maxHeight);
                        
                        switch (biome.majorFloraIndex) {
                            //default:
                            case 0:{ 
                                voxelValue = 10;

                                int height = (int)(biome.maxHeight * Noise.Get2DPerlin(x, z, 250f, 3f));
                        
                                if (height < biome.minHeight)
                                    height = biome.minHeight;

                                //height = 50;

                                int baseY = y+height-2;
                                new BoxPainter(this, x-2, baseY, z-2, 4, 0, 4, 11);
                                new BoxPainter(this, x-2, baseY+1, z-2, 4, 0, 4, 11);
                                new BoxPainter(this, x-1, baseY+2, z-1, 2, 0, 2, 11);
                                new BoxPainter(this, x-1, baseY+3, z-1, 2, 0, 2, 11, 1);
                                BoxPainter trunk = new BoxPainter(this, x, y+1, z, 0, height-1, 0, 6);
                                //BoxPainter MainLeaves = new BoxPainter(this, x-1, y+height, z-1, 3, 4, 3, 11);
                            } break;
                            case 1:{ 
                                int height = (int)(biome.maxHeight * Noise.Get2DPerlin(x, z, 23456f, 2f));
                        
                                if (height < biome.minHeight)
                                    height = biome.minHeight;
                                
                                BoxPainter trunk = new BoxPainter(this, x, y, z, 0, height, 0, 12);
                            } break;
                        }
                    }

                }
            }
        }

        return voxelValue;
    }

    bool IsChunkInWorld (ChunkCoord coord) {
        return true;
    }
}

[System.Serializable]
public class BlockType {
    public string blockName;
    public bool isSolid;
    public bool renderNeighborFaces;
    public float transparency;
    public Sprite icon;

    [Header("Texture Values")]
    public int backFaceTexture;
    public int frontFaceTexture;
    public int topFaceTexture;
    public int bottomFaceTexture;
    public int leftFaceTexture;
    public int rightFaceTexture;

    // Back, Front, Top, Bottom, Left, Right

    public int GetTextureID (int faceIndex) {
        switch (faceIndex) {
            case 0:
                return backFaceTexture;
            case 1:
                return frontFaceTexture;
            case 2:
                return topFaceTexture;
            case 3:
                return bottomFaceTexture;
            case 4:
                return leftFaceTexture;
            case 5:
                return rightFaceTexture;
            default:
                Debug.Log("Error in GetTextureID; invalid face index");
                return 0;
        }

    }

}


public class VoxelMods {
    ChunkCoord coord;
    public List<BoxPainter> Painters;
    public VoxelMods (ChunkCoord _coord) {
        coord = _coord;
        Painters = new List<BoxPainter>();
    }
}

public class VoxelMod {
    public Vector3 position;
    public byte id;

    public VoxelMod (ref Vector3 _position, byte _id) {
        position = _position;
        id = _id;
    }
}

public class BoxPainter{
    int x;
    int y;
    int z;
    int x1;
    int y1;
    int z1;
    int id;
    float CornerEraseChance;
    public BoxPainter(World World, int x, int y, int z, int x1, int y1, int z1, int id, float CornerEraseChance=0)
    {
        this.x  = x;
        this.y  = y;
        this.z  = z;
        this.x1 = x1;
        this.y1 = y1;
        this.z1 = z1;
        this.id = id;
        this.CornerEraseChance = CornerEraseChance;
        
        int CCMinX = x<0?(x+1)/VoxelData.ChunkWidth-1:(x/VoxelData.ChunkWidth);
        int CCMinZ = z<0?(z+1)/VoxelData.ChunkWidth-1:(z/VoxelData.ChunkWidth);
        x+=x1; z+=z1;
        int CCMaxX = x<0?(x+1)/VoxelData.ChunkWidth-1:(x/VoxelData.ChunkWidth);
        int CCMaxZ = z<0?(z+1)/VoxelData.ChunkWidth-1:(z/VoxelData.ChunkWidth);
        lock (World.ChunkStructMods)
        { 
            for (int i=CCMinX; i<=CCMaxX; i++) {
                for (int j=CCMinZ; j<=CCMaxZ; j++) {
                    ChunkCoord coord = new ChunkCoord(i, j);
                
                    World.ChunkStructMods.TryGetValue(coord, out VoxelMods vm);
                    if (vm == null)
                    {
                        vm = new VoxelMods(coord);
                        World.ChunkStructMods.Add(coord, vm);
                    }
                    vm.Painters.Add(this);
                }
            }
        }

    }

    public void paintToChunk(ChunkData chunkData)
    {
        VoxelState[,,] map = chunkData.map;
        int x=this.x-chunkData.pos_x;
        int y=this.y;
        int z=this.z-chunkData.pos_y;
        int x1=x+this.x1;
        int y1=y+this.y1;
        int z1=z+this.z1;
        if(x<0) x=0;
        if(y<0) y=0;
        if(z<0) z=0;
        if(x1>=VoxelData.ChunkWidth) x1=VoxelData.ChunkWidth-1;
        if(y1>=VoxelData.ChunkHeight) y1=VoxelData.ChunkHeight-1;
        if(z1>=VoxelData.ChunkWidth) z1=VoxelData.ChunkWidth-1;
        //Debug.Log(string.Format("paintToChunk {0} {1} {2} x1_{3} {4} {5} {6} {7}", x,y,z,x1,y1,z1,VoxelData.ChunkWidth,VoxelData.ChunkHeight));
        for (int i=x;i<=x1;i++)
        {
            for (int k=z;k<=z1;k++)
            {
                for (int j=y;j<=y1;j++)
                {
                    if (CornerEraseChance == 1 && (i==x||i==x1)&&(k==z||k==z1))
                    {
                        continue;
                    }
                    map[i,j,k] = new VoxelState((byte)id);
                }
            }
        }
    }
} 

[System.Serializable]
public class Settings {
    [Header("Game Data")]
    public string version = "0.0.0.01";

    [Header("Performance")]
    public int GridSize = 8;
    public int loadDistance = 16; // Cannot be lower than viewDistance, validation in Settings Menu to come...
    public bool enableThreading = true;
    public CloudStyle clouds = CloudStyle.Fancy;
    public bool enableAnimatedChunks = false;

    [Header("Controls")]
    [Range(0.1f, 10f)]
    public float mouseSensitivity = 2.0f;
}
