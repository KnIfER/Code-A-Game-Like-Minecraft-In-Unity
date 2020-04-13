using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Noise  {
    public static float Get2DPerlin (float x, float y, float offset, float scale) {
        x += (offset + VoxelData.seed + 0.1f);
        y += (offset + VoxelData.seed + 0.1f);

        return Mathf.PerlinNoise(x/ VoxelData.ChunkWidth * scale, y / VoxelData.ChunkWidth * scale);
    }

    public static bool Get3DPerlin (float x, float y, float z, float offset, float scale, float threshold) {
        // https://www.youtube.com/watch?v=Aga0TBJkchM Carpilot on YouTube

        x = (x + offset + VoxelData.seed + 0.1f) * scale;
        y = (y + offset + VoxelData.seed + 0.1f) * scale;
        z = (z + offset + VoxelData.seed + 0.1f) * scale;

        float AB = Mathf.PerlinNoise(x, y);
        float BC = Mathf.PerlinNoise(y, z);
        float AC = Mathf.PerlinNoise(x, z);
        float BA = Mathf.PerlinNoise(y, x);
        float CB = Mathf.PerlinNoise(z, y);
        float CA = Mathf.PerlinNoise(z, x);

        if ((AB + BC + AC + BA + CB + CA) / 6f > threshold)
            return true;
        else
            return false;
    }

    
    public static float Get3DPerlin (float x, float y, float z, float offset, float scale) {
        // https://www.youtube.com/watch?v=Aga0TBJkchM Carpilot on YouTube

        x = (x + offset + VoxelData.seed + 0.1f) * scale;
        y = (y + offset + VoxelData.seed + 0.1f) * scale;
        z = (z + offset + VoxelData.seed + 0.1f) * scale;

        float AB = Mathf.PerlinNoise(x, y);
        float BC = Mathf.PerlinNoise(y, z);
        float AC = Mathf.PerlinNoise(x, z);
        float BA = Mathf.PerlinNoise(y, x);
        float CB = Mathf.PerlinNoise(z, y);
        float CA = Mathf.PerlinNoise(z, x);

        return ((AB + BC + AC + BA + CB + CA) / 6f);
    }
}
