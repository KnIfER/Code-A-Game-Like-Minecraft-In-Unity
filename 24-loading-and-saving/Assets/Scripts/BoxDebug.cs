using UnityEngine;
  
 public static class BoxDebug
 {
     public static void DrawBox(Vector3 origin, Vector3 fullExtents, Color color, float du)
     {
        Vector3 origin1 = origin + fullExtents;
        
        Vector3 X = new Vector3(fullExtents.x, 0, 0);
        Vector3 Y = new Vector3(0, fullExtents.y, 0);
        Vector3 Z = new Vector3(0, 0, fullExtents.z);
        Vector3 A = origin + X;
        Vector3 B = origin + Y;
        Vector3 C = origin + Z;
        Vector3 D = origin1 - X;
        Vector3 E = origin1 - Y;
        Vector3 F = origin1 - Z;

        Debug.DrawLine(origin, A, color, du);
        Debug.DrawLine(origin, B, color, du);
        Debug.DrawLine(origin, C, color, du);

        Debug.DrawLine(origin1, D, color, du);
        Debug.DrawLine(origin1, E, color, du);
        Debug.DrawLine(origin1, F, color, du);
        
        Debug.DrawLine(A, F, color, du);
        Debug.DrawLine(F, B, color, du);
        Debug.DrawLine(B, D, color, du);
        Debug.DrawLine(D, C, color, du);
        Debug.DrawLine(C, E, color, du);
        Debug.DrawLine(E, A, color, du);

    }
 }