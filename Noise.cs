using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MineQuest
{
    static class Noise
    {
        private const float brownianOffset = 32000f; // hack to prevent negative numbers from being fed into Perlin noise

        public static float BrownianMotion(float x, float z, int octaves, float persistence)
        {
            float total = 0;
            float frequency = 1;
            float amplitude = 1;
            float maxValue = 0;
            
            for (int i = 0; i < octaves; i++)
            {
                total += Mathf.PerlinNoise((x + brownianOffset) * frequency, (z + brownianOffset) * frequency) * amplitude;

                maxValue += amplitude;

                amplitude *= persistence;
                frequency *= 2;
            }

            return total / maxValue;
        }

        public static float BrownianMotion3d(float x, float y, float z, float smoothFactor, int octaves)
        {
            float xy = BrownianMotion(x * smoothFactor, y * smoothFactor, octaves, 0.5f);
            float yz = BrownianMotion(y * smoothFactor, z * smoothFactor, octaves, 0.5f);
            float xz = BrownianMotion(x * smoothFactor, z * smoothFactor, octaves, 0.5f);

            float yx = BrownianMotion(y * smoothFactor, x * smoothFactor, octaves, 0.5f);
            float zy = BrownianMotion(z * smoothFactor, y * smoothFactor, octaves, 0.5f);
            float zx = BrownianMotion(z * smoothFactor, x * smoothFactor, octaves, 0.5f);

            return (xy + yz + xz + yx + zy + zx) / 6.0f;
        }
    }
}