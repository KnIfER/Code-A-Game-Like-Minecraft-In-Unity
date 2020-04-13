using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class VoxelData {
	public static readonly int ChunkWidth = 16;
    public static int TileWidthUU = 16;
	public static readonly float HalfTileWidth = 16/2;
	public static readonly int ChunkHeight = 128;
    //public static readonly int WorldSizeInChunks = 100;

    // Lighting Values
    public static float minLightLevel = 0.1f;
    public static float maxLightLevel = 0.9f;
    public static float lightFalloff = 0.08f;

    public static int seed;

    public static readonly int TextureAtlasSizeInBlocks = 16;
    public static float NormalizedBlockTextureSize {
        get { return 1f / (float)TextureAtlasSizeInBlocks; }
    }

	public static readonly Vector3[] voxelVerts = new Vector3[8] {
		new Vector3(0.0f, 0.0f, 0.0f),//0
		new Vector3(1.0f, 0.0f, 0.0f),//1
		new Vector3(1.0f, 1.0f, 0.0f),//2
		new Vector3(0.0f, 1.0f, 0.0f),//3
		new Vector3(0.0f, 0.0f, 1.0f),//4
		new Vector3(1.0f, 0.0f, 1.0f),//5
		new Vector3(1.0f, 1.0f, 1.0f),//6
		new Vector3(0.0f, 1.0f, 1.0f),//7
	};

	public static readonly Vector3Int[] faceChecks = new Vector3Int[6] {
		new Vector3Int(0, 0, -1),
		new Vector3Int(0, 0, 1),
		new Vector3Int(0, 1, 0),
		new Vector3Int(0, -1, 0),
		new Vector3Int(-1, 0, 0),
		new Vector3Int(1, 0, 0)
	};

    //16*6
	public static readonly Vector3Int[] faceLightChecks = new Vector3Int[] {
        //0 3 1 2
		new Vector3Int(0, 0, -1),//
		new Vector3Int(-1, 0, -1),
		new Vector3Int(-1, -1, -1),
		new Vector3Int(0, -1, -1),
		new Vector3Int(0, 0, -1),//
		new Vector3Int(-1, 0, -1),
		new Vector3Int(-1, 1, -1),
		new Vector3Int(0, 1, -1),
		new Vector3Int(0, 0,  -1),//
		new Vector3Int(1, 0,  -1),
		new Vector3Int(1, -1, -1),
		new Vector3Int(0, -1, -1),
		new Vector3Int(0, 0, -1),//
		new Vector3Int(1, 0, -1),
		new Vector3Int(1, 1, -1),
		new Vector3Int(0, 1, -1),

        //5 6 4 7
		new Vector3Int(0, 0,  1),//
		new Vector3Int(1, 0,  1),
		new Vector3Int(1, -1, 1),
		new Vector3Int(0, -1, 1),
		new Vector3Int(0, 0, 1),//
		new Vector3Int(1, 0, 1),
		new Vector3Int(1, 1, 1),
		new Vector3Int(0, 1, 1),
		new Vector3Int(0, 0,  1),//
		new Vector3Int(-1, 0, 1),
		new Vector3Int(-1, -1,1),
		new Vector3Int(0, -1, 1),
		new Vector3Int(0, 0,  1),//
		new Vector3Int(-1, 0, 1),
		new Vector3Int(-1, 1, 1),
		new Vector3Int(0, 1,  1),
        
        //3 7 2 6
		new Vector3Int(0,  1, 0),//
		new Vector3Int(-1, 1, 0),
		new Vector3Int(-1, 1, -1),
		new Vector3Int(0,  1, -1),
		new Vector3Int(0,  1, 0),//
		new Vector3Int(-1, 1, 0),
		new Vector3Int(-1, 1, 1),
		new Vector3Int(0,  1, 1),
		new Vector3Int(0,  1, 0),//
		new Vector3Int(1,  1, 0),
		new Vector3Int(1,  1, -1),
		new Vector3Int(0,  1, -1),
		new Vector3Int(0,  1, 0),//
		new Vector3Int(1,  1, 0),
		new Vector3Int(1,  1, 1),
		new Vector3Int(0,  1, 1),

        // 1 5 0 4
		new Vector3Int(0, -1, 0),//
		new Vector3Int(1, -1, 0),
		new Vector3Int(1, -1, -1),
		new Vector3Int(0, -1, -1),
		new Vector3Int(0, -1, 0),//
		new Vector3Int(1, -1, 0),
		new Vector3Int(1, -1, 1),
		new Vector3Int(0, -1, 1),
		new Vector3Int(0,  -1, 0),//
		new Vector3Int(-1, -1, 0),
		new Vector3Int(-1, -1, -1),
		new Vector3Int(0,  -1, -1),
		new Vector3Int(0,  -1, 0),//
		new Vector3Int(-1, -1, 0),
		new Vector3Int(-1, -1, 1),
		new Vector3Int(0,  -1, 1),

        // 4 7 0 3
		new Vector3Int(-1, 0, 0),//
		new Vector3Int(-1, -1, 0),
		new Vector3Int(-1, -1, 1),
		new Vector3Int(-1, 0, 1),
		new Vector3Int(-1, 0, 0),//
		new Vector3Int(-1, 1, 0),
		new Vector3Int(-1, 1, 1),
		new Vector3Int(-1, 0, 1),
		new Vector3Int(-1, 0, 0),//
		new Vector3Int(-1, -1, 0),
		new Vector3Int(-1, -1, -1),
		new Vector3Int(-1, 0, -1),
		new Vector3Int(-1, 0, 0),//
		new Vector3Int(-1, 1, 0),
		new Vector3Int(-1, 1, -1),
		new Vector3Int(-1, 0, -1),

        // 1 2 5 6
		new Vector3Int(1, 0, 0),//
		new Vector3Int(1, -1, 0),
		new Vector3Int(1, -1, -1),
		new Vector3Int(1, 0, -1),
		new Vector3Int(1, 0, 0),//
		new Vector3Int(1, 1, 0),
		new Vector3Int(1, 1, -1),
		new Vector3Int(1, 0, -1),
		new Vector3Int(1, 0, 0),//
		new Vector3Int(1, -1, 0),
		new Vector3Int(1, -1, 1),
		new Vector3Int(1, 0, 1),
		new Vector3Int(1, 0, 0),//
		new Vector3Int(1, 1, 0),
		new Vector3Int(1, 1, 1),
		new Vector3Int(1, 0, 1),
	};

	public static readonly int[,] voxelTris = new int[6,4] {
        // Back, Front, Top, Bottom, Left, Right

		// 0 1 2 2 1 3
		{0, 3, 1, 2}, // Back Face
		{5, 6, 4, 7}, // Front Face
		{3, 7, 2, 6}, // Top Face
		{1, 5, 0, 4}, // Bottom Face
		{4, 7, 0, 3}, // Left Face
		{1, 2, 5, 6} // Right Face
	};

	public static readonly Vector2[] voxelUvs = new Vector2[4] {
		new Vector2 (0.0f, 0.0f),
		new Vector2 (0.0f, 1.0f),
		new Vector2 (1.0f, 0.0f),
		new Vector2 (1.0f, 1.0f)
	};
    
    public static bool debugChunks=false;
}
