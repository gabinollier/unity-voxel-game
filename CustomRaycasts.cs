using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class CustomRaycasts
{
    static ChunkLoader _chunkLoader;

    [RuntimeInitializeOnLoadMethod]
    static void Initialize()
    {
        _chunkLoader = GameObject.FindObjectOfType<ChunkLoader>();
    }

    public static (bool success, Block block, int rotation, Vector3Int? position) BlockRaycast(Vector3 origin, Vector3 normalizedDirection, float range, Func<Block, Vector3Int, bool> condition)
    {
        // From "A Fast Voxel Traversal Algorithm for Ray Tracing"
        // by John Amanatides and Andrew Woo, 1987
        // <http://www.cse.yorku.ca/~amana/research/grid.pdf>
        // Implemented in JS by github.com/kpreid/cubes/
        // Translated in c#-unity by Gabin Ollier

        int x = Mathf.FloorToInt(origin.x);
        int y = Mathf.FloorToInt(origin.y);
        int z = Mathf.FloorToInt(origin.z);

        float dx = normalizedDirection.x;
        float dy = normalizedDirection.y;
        float dz = normalizedDirection.z;

        int stepX = Signum(dx);
        int stepY = Signum(dy);
        int stepZ = Signum(dz);

        float tDeltaX = stepX / dx;
        float tDeltaY = stepY / dy;
        float tDeltaZ = stepZ / dz;

        float tMaxX = DistanceToIntAlongDir(origin.x, dx);
        float tMaxY = DistanceToIntAlongDir(origin.y, dy);
        float tMaxZ = DistanceToIntAlongDir(origin.z, dz);


        while (true)
        {
            Vector3Int position = new Vector3Int(x, y, z);
            (Block block, int rotation) = _chunkLoader.GetBlock(position);
            if (condition(block, position))
            {
                return (true, block, rotation, position);
            }

            if (tMaxX < tMaxY)
            {
                if (tMaxX < tMaxZ)
                {
                    if (tMaxX > range) break;
                    x += stepX;
                    tMaxX += tDeltaX;
                }
                else
                {
                    if (tMaxZ > range) break;
                    z += stepZ;
                    tMaxZ += tDeltaZ;
                }
            }
            else
            {
                if (tMaxY < tMaxZ)
                {
                    if (tMaxY > range) break;
                    y += stepY;
                    tMaxY += tDeltaY;
                }
                else
                {
                    if (tMaxZ > range) break;
                    z += stepZ;
                    tMaxZ += tDeltaZ;
                }
            }
        }

        return (false, null, 0, null);


        int Signum(float x)
        {
            return x > 0 ? 1 : x < 0 ? -1 : 0;
        }

        // calculate the distance from origin to the nearest integrer along direction
        float DistanceToIntAlongDir(float origin, float direction)
        {
            if (direction < 0)
            {
                return DistanceToIntAlongDir(-origin, -direction);
            }
            else
            {
                origin = Mod(origin, 1);
                return (1 - origin) / direction;
            }
        }

        float Mod(float value, int modulus)
        {
            return (value % modulus + modulus) % modulus;
        }
    }

}
