using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ModableWorld{
    void AddModAt(int x, int y, int z, int id);
}

public static class Structure {
    public static void GenerateMajorFlora (ModableWorld mw, int index, int x, int y, int z, int minTrunkHeight, int maxTrunkHeight) {
        switch (index) {
            case 0:
                MakeTree(mw, x, y, z, minTrunkHeight, maxTrunkHeight);
            break;
            case 1:
                MakeCacti(mw, x, y, z, minTrunkHeight, maxTrunkHeight);
            break;
        }
    }

    public static void MakeTree (ModableWorld mw, int x, int y, int z, int minTrunkHeight, int maxTrunkHeight) {
        int height = (int)(maxTrunkHeight * Noise.Get2DPerlin(x, z, 250f, 3f));

        if (height < minTrunkHeight)
            height = minTrunkHeight;

        for (int i = 1; i < height; i++)
            mw.AddModAt(x, y + i, z, 6);
            //queue.Enqueue(new VoxelMod(new Vector3(), 6));

        for (int i = -3; i < 4; i++) {
            for (int j = 0; j < 7; j++) {
                for (int k = -3; k < 4; k++) {
                    mw.AddModAt(x + i, y + height + j, z + k, 11);
                    //queue.Enqueue(new VoxelMod(new Vector3(), 11));
                }
            }
        }
    }

    public static void MakeCacti (ModableWorld mw, int x, int y, int z, int minTrunkHeight, int maxTrunkHeight) {
        int height = (int)(maxTrunkHeight * Noise.Get2DPerlin(x, z, 23456f, 2f));

        if (height < minTrunkHeight)
            height = minTrunkHeight;

        for (int i = 1; i <= height; i++)
            mw.AddModAt(x, y + i, z, 12);
            //queue.Enqueue(new VoxelMod(new Vector3(x, y + i, z), 12));
    }
}
