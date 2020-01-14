using System.Collections;
using System.Collections.Generic;
using UnityEngine;


//Perlin noise function re-written to match the compute function so noise is similar across calculation methods
//  for direct comparison
public class PerlinNoise : MonoBehaviour
{
    static Vector4 Floor(Vector4 a) 
    {
        // intrinsic function floor(x)
        return new Vector4(Floor(a.x), Floor(a.y), Floor(a.z), Floor(a.w));
    }

    static float Floor(float f)
    {
        return Mathf.Floor(f);
    }

    static Vector4 Mul(Vector4 x, Vector4 y)
    { 
        // component-wise multiplication
        return new Vector4(x.x * y.x, x.y * y.y, x.z * y.z, x.w * y.w);
    }

    static Vector4 Mod289(Vector4 v)                            
    {
        //return x - floor(x / 289.0) * 289.0;
        return v - Floor(v / 289.0f) * 289.0f; //                             
    }

    static Vector4 Permute(Vector4 v)               
    {
       //return mod289(((x * 34.0) + 1.0) * x);
        return Mod289(Mul(((v * 34.0f) + Vector4.one), v));
    }

    static Vector4 TaylorInvSqrt(Vector4 r) 
    {  
        // return (float4)1.79284291400159 - r * 0.85373472095314;
        return (Vector4.one * 1.79284291400159f) - (r * 0.85373472095314f); 
    }

    static Vector3 Fade(Vector3 t)
    {
        // t*t*t
        Vector3 t3 = Mul(Mul(t, t), t); 
        // t*(t*6.0-15.0)
        Vector4 u = Mul(t, t * 6.0f - (Vector3.one * 15.0f));
        // (t * (t * 6.0 - 15.0) + 10.0)
        Vector3 s = new Vector3(u.x, u.y, u.z) + (Vector3.one * 10.0f); 
        
        // return t * t * t * (t * (t * 6.0 - 15.0) + 10.0);
        return Mul(t3, s);

    }

    static Vector4 Frac(Vector4 v) // intrinsic functino frac(x)
    {
        return v - Floor(v);
    }

    static Vector4 Abs(Vector4 v) // intrinsic function abs(x)
    {
        return new Vector4(Abs(v.x), Abs(v.y), Abs(v.z), Abs(v.w));
    }

    static float Abs(float f)
    {
        return Mathf.Abs(f);
    }

    static Vector4 Lerp(Vector4 a, Vector4 b, float f) // intrinsic function lerp(x, y, z, w, s)
    {
        //return Vector4.Lerp(a, b, f);
        return new Vector4(Mathf.Lerp(a.x, b.x, f), Mathf.Lerp(a.y, b.y, f), Mathf.Lerp(a.z, b.z, f), Mathf.Lerp(a.w, b.w, f));
    }
    static Vector2 Lerp(Vector2 a, Vector2 b, float f) // intrinsic function lerp(x, y, s)
    {
        //return Vector2.Lerp(a, b, f);
        return new Vector2(Mathf.Lerp(a.x, b.x, f), Mathf.Lerp(a.y, b.y, f));
    }
    static float Lerp(float a, float b, float f) // intrinsic function lerp(x, y, s)
    {
        return Mathf.Lerp(a, b, f);
    }

    static Vector4 Step(Vector4 a, Vector4 b)
    {
        return new Vector4(Step(a.x, b.x), Step(a.y, b.y), Step(a.z, b.z), Step(a.w, b.w));
    }

    static float Step(float edge, float x)
    {
        return x >= edge ? 1.0f : 0.0f;
    }

    static Vector4 NewVec4(float v)
    {
        return new Vector4(v, v, v, v);
    }
    
    public static float CNoise(Vector3 P)
    {
        Vector3 Pi0 = Floor(P);
        Vector3 Pi1 = Pi0 + Vector3.one; //     float4 Pi = floor(P.xyxy) + float4(0.0, 0.0, 1.0, 1.0);
        Pi0 = Mod289(Pi0);
        Pi1 = Mod289(Pi1);
        Vector3 Pf0 = Frac(P);
        Vector3 Pf1 = Pf0 - Vector3.one;
        Vector4 ix = new Vector4(Pi0.x, Pi1.x, Pi0.x, Pi1.x);
        Vector4 iy = new Vector4(Pi0.y, Pi0.y, Pi1.y, Pi1.y);
        Vector4 iz0 = new Vector4(Pi0.z, Pi0.z, Pi0.z, Pi0.z);
        Vector4 iz1 = new Vector4(Pi1.z, Pi1.z, Pi1.z, Pi1.z);

        Vector4 ixy = Permute(Permute(ix) + iy);
        Vector4 ixy0 = Permute(ixy + iz0);
        Vector4 ixy1 = Permute(ixy + iz1);

        Vector4 gx0 = ixy0 / 7.0f;
        Vector4 gy0 = Frac(Floor(gx0) / 7.0f) - NewVec4(0.5f);
        gx0 = Frac(gx0);
        Vector4 gz0 = NewVec4(0.5f) - Abs(gx0) - Abs(gy0);

        Vector4 sz0 = Step(gz0, Vector4.zero);
        gx0 -= Mul(sz0, (Step(Vector4.zero, gx0)) - NewVec4(0.5f));
        gy0 -= Mul(sz0, (Step(Vector4.zero, gy0)) - NewVec4(0.5f));

        Vector4 gx1 = ixy1 / 7.0f;
        Vector4 gy1 = Frac(Floor(gx1) / 7.0f) - NewVec4(0.5f);
        gx1 = Frac(gx1);
        Vector4 gz1 = NewVec4(0.5f) - Abs(gx1) - Abs(gy1);

        Vector4 sz1 = Step(gz1, Vector4.zero);
        gx1 -= Mul(sz1, (Step(Vector4.zero, gx1)) - NewVec4(0.5f));
        gy1 -= Mul(sz1, (Step(Vector4.zero, gy1)) - NewVec4(0.5f));

        Vector3 g000 = new Vector3(gx0.x, gy0.x, gz0.x);
        Vector3 g100 = new Vector3(gx0.y, gy0.y, gz0.y);
        Vector3 g010 = new Vector3(gx0.z, gy0.z, gz0.z);
        Vector3 g110 = new Vector3(gx0.w, gy0.w, gz0.w);
        Vector3 g001 = new Vector3(gx1.x, gy1.x, gz1.x);
        Vector3 g101 = new Vector3(gx1.y, gy1.y, gz1.y);
        Vector3 g011 = new Vector3(gx1.z, gy1.z, gz1.z);
        Vector3 g111 = new Vector3(gx1.w, gy1.w, gz1.w);


        Vector4 norm0 = TaylorInvSqrt(new Vector4
            (
                Vector3.Dot(g000, g000),
                Vector3.Dot(g010, g010),
                Vector3.Dot(g100, g100),
                Vector3.Dot(g110, g110))
            );

        g000 *= norm0.x;
        g010 *= norm0.y;
        g100 *= norm0.z;
        g110 *= norm0.w;


        Vector4 norm1 = TaylorInvSqrt(new Vector4
    (
        Vector3.Dot(g001, g001),
        Vector3.Dot(g011, g011),
        Vector3.Dot(g101, g101),
        Vector3.Dot(g111, g111))
    );

        g001 *= norm1.x;
        g011 *= norm1.y;
        g101 *= norm1.z;
        g111 *= norm1.w;

        float n000 = Vector3.Dot(g000, Pf0);
        float n100 = Vector3.Dot(g100, new Vector3(Pf1.x, Pf0.y, Pf0.z));
        float n010 = Vector3.Dot(g010, new Vector3(Pf0.x, Pf1.y, Pf0.z));
        float n110 = Vector3.Dot(g110, new Vector3(Pf1.x, Pf1.y, Pf0.z));
        float n001 = Vector3.Dot(g001, new Vector3(Pf0.x, Pf0.y, Pf1.z));
        float n101 = Vector3.Dot(g101, new Vector3(Pf1.x, Pf0.y, Pf1.z));
        float n011 = Vector3.Dot(g011, new Vector3(Pf0.x, Pf1.y, Pf1.z));
        float n111 = Vector3.Dot(g111, Pf1);

        Vector3 fade_xyz = Fade(Pf0);
        Vector4 n_z = Lerp(new Vector4(n000, n100, n010, n110), new Vector4(n001, n101, n011, n111), fade_xyz.z);
        Vector2 n_yz = Lerp(new Vector2(n_z.x, n_z.y), new Vector2(n_z.z, n_z.w), fade_xyz.y);
        float n_xyz = Lerp(n_yz.x, n_yz.y, fade_xyz.x);

        return 2.2f * n_xyz;

    }
}
