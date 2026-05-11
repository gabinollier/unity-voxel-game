using UnityEngine;

public static class Noise
{
    static Vector3UInt ui3 = new Vector3UInt(1597334673U, 3812015801U, 2798796415U);
    static float uif = 1.0f / 0xffffffffU;

    public static Vector3 Hash32(Vector2 v, int seed1, int seed2) // returns value 0..1
    {
        v = new Vector2(v.x + seed1 + seed2, v.y + seed1 + seed2);
        Vector3UInt n = new Vector3UInt((uint)v.x * ui3.x, (uint)v.y * ui3.y, (uint)v.x * ui3.z);
        n = ui3 * (n.x ^ n.y ^ n.z);
        return new Vector3(n.x, n.y, n.z) * uif;
    }

    static Vector2 Fract(Vector2 v)
    {
        return new Vector2(
            v.x - Mathf.Floor(v.x),
            v.y - Mathf.Floor(v.y));
    }

    

    static Vector2 Fract(Vector3 v)
    {
        return new Vector3(
            v.x - Mathf.Floor(v.x),
            v.y - Mathf.Floor(v.y),
            v.z - Mathf.Floor(v.z));
    }

    struct Vector3UInt
    {
        public uint x;
        public uint y;
        public uint z;


        public Vector3UInt(uint x, uint y, uint z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public static Vector3UInt operator * (Vector3UInt left, uint right)
        {
            return new Vector3UInt(
                left.x * right,
                left.y * right,
                left.z * right);
        }
    }


    public static float Hash12(Vector2 x)
    {
        Vector2 i = new Vector2(Mathf.Floor(x.x), Mathf.Floor(x.y));
        Vector2 f = new Vector2(Mathf.Repeat(x.x, 1f), Mathf.Repeat(x.y, 1f));

        // Four corners in 2D of a tile
        float a = hash(i);
        float b = hash(i + new Vector2(1f, 0f));
        float c = hash(i + new Vector2(0f, 1f));
        float d = hash(i + new Vector2(1f, 1f));

        // Simple 2D lerp using smoothstep envelope between the values.
        // return vec3(mix(mix(a, b, smoothstep(0.0, 1.0, f.x)),
        //			mix(c, d, smoothstep(0.0, 1.0, f.x)),
        //			smoothstep(0.0, 1.0, f.y)));

        // Same code, with the clamps in smoothstep and common subexpressions
        // optimized away.
        Vector2 u = f * f * new Vector2(3f - 2f * f.x, 3f - 2f * f.y);
        return Mathf.Lerp(a, b, u.x) + (c - a) * u.y * (1f - u.x) + (d - b) * u.x * u.y;

        float hash(Vector2 p)
        {
            return Fract(1e4f * Mathf.Sin(17f * p.x + p.y * 0.1f) * (0.1f + Mathf.Abs(Mathf.Sin(p.y * 13f + p.x))));
        }

    }

    static float Fract(float f)
    {
        return f - Mathf.Floor(f);
    }

}