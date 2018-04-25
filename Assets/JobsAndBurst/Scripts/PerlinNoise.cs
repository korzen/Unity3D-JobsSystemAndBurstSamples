// Converted to Unity.Mathematics - https://github.com/korzen/Unity3D-JobsSystemAndBurstSamples

// Noise Shader Library for Unity - https://github.com/keijiro/NoiseShader
//
// Original work (webgl-noise) Copyright (C) 2011 Stefan Gustavson
// Translation and modification was made by Keijiro Takahashi.
//
// This shader is based on the webgl-noise GLSL shader. For further details
// of the original shader, please see the following description from the
// original source code.
//

//
// GLSL textureless classic 2D noise "cnoise",
// with an RSL-style periodic variant "pnoise".
// Author:  Stefan Gustavson (stefan.gustavson@liu.se)
// Version: 2011-08-22
//
// Many thanks to Ian McEwan of Ashima Arts for the
// ideas for permutation and gradient selection.
//
// Copyright (c) 2011 Stefan Gustavson. All rights reserved.
// Distributed under the MIT license. See LICENSE file.
// https://github.com/ashima/webgl-noise
//

using Unity.Mathematics;

public class PerlinNoise
{

    // Classic Perlin noise
    public static float cnoise(float2 P)
    {
        float4 Pi = math.floor(P.xyxy) + new float4(0.0f, 0.0f, 1.0f, 1.0f);
        float4 Pf = math.frac(P.xyxy) - new float4(0.0f, 0.0f, 1.0f, 1.0f);
        Pi = mod289(Pi); // To avoid truncation effects in permutation
        float4 ix = Pi.xzxz;
        float4 iy = Pi.yyww;
        float4 fx = Pf.xzxz;
        float4 fy = Pf.yyww;

        float4 i = permute(permute(ix) + iy);

        float4 gx = math.frac(i / 41.0f) * 2.0f - 1.0f;
        float4 gy = math.abs(gx) - 0.5f;
        float4 tx = math.floor(gx + 0.5f);
        gx = gx - tx;

        float2 g00 = new  float2(gx.x, gy.x);
        float2 g10 = new float2(gx.y, gy.y);
        float2 g01 = new float2(gx.z, gy.z);
        float2 g11 = new float2(gx.w, gy.w);

        float4 norm = taylorInvSqrt(new float4(math.dot(g00, g00), math.dot(g01, g01), math.dot(g10, g10), math.dot(g11, g11)));
        g00 *= norm.x;
        g01 *= norm.y;
        g10 *= norm.z;
        g11 *= norm.w;

        float n00 = math.dot(g00, new float2(fx.x, fy.x));
        float n10 = math.dot(g10, new float2(fx.y, fy.y));
        float n01 = math.dot(g01, new float2(fx.z, fy.z));
        float n11 = math.dot(g11, new float2(fx.w, fy.w));

        float2 fade_xy = fade(Pf.xy);
        float2 n_x = math.lerp(new float2(n00, n01), new float2(n10, n11), fade_xy.x);
        float n_xy = math.lerp(n_x.x, n_x.y, fade_xy.y);
        return 2.3f * n_xy;
    }

    // Classic Perlin noise, periodic variant
    float pnoise(float2 P, float2 rep)
    {
        float4 Pi = math.floor(P.xyxy) + new float4(0.0f, 0.0f, 1.0f, 1.0f);
        float4 Pf = math.frac(P.xyxy) - new float4(0.0f, 0.0f, 1.0f, 1.0f);
        Pi = mod(Pi, rep.xyxy); // To create noise with explicit period
        Pi = mod289(Pi);        // To avoid truncation effects in permutation
        float4 ix = Pi.xzxz;
        float4 iy = Pi.yyww;
        float4 fx = Pf.xzxz;
        float4 fy = Pf.yyww;

        float4 i = permute(permute(ix) + iy);

        float4 gx = math.frac(i / 41.0f) * 2.0f - 1.0f;
        float4 gy = math.abs(gx) - 0.5f;
        float4 tx = math.floor(gx + 0.5f);
        gx = gx - tx;

        float2 g00 = new float2(gx.x, gy.x);
        float2 g10 = new float2(gx.y, gy.y);
        float2 g01 = new float2(gx.z, gy.z);
        float2 g11 = new float2(gx.w, gy.w);

        float4 norm = taylorInvSqrt(new float4(math.dot(g00, g00), math.dot(g01, g01), math.dot(g10, g10), math.dot(g11, g11)));
        g00 *= norm.x;
        g01 *= norm.y;
        g10 *= norm.z;
        g11 *= norm.w;

        float n00 = math.dot(g00, new float2(fx.x, fy.x));
        float n10 = math.dot(g10, new float2(fx.y, fy.y));
        float n01 = math.dot(g01, new float2(fx.z, fy.z));
        float n11 = math.dot(g11, new float2(fx.w, fy.w));

        float2 fade_xy = fade(Pf.xy);
        float2 n_x = math.lerp(new float2(n00, n01), new float2(n10, n11), fade_xy.x);
        float n_xy = math.lerp(n_x.x, n_x.y, fade_xy.y);
        return 2.3f * n_xy;
    }


    public static float4 mod(float4 x, float4 y)
    {
        return x - y * math.floor(x / y);
    }

    public static float4 mod289(float4 x)
    {
        return x - math.floor(x / 289.0f) * 289.0f;
    }

    public static float4 permute(float4 x)
    {
        return mod289(((x * 34.0f) + 1.0f) * x);
    }

    public static float4 taylorInvSqrt(float4 r)
    {
        return (float4)1.79284291400159f - r * 0.85373472095314f;
    }

    public static float2 fade(float2 t)
    {
        return t * t * t * (t * (t * 6.0f - 15.0f) + 10.0f);
    }
}
