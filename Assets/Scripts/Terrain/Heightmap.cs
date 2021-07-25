using UnityEngine;
using Random = System.Random;

public static class Heightmap
{
    public static float[,] GenerateHeightmap(int seed, float persistance, 
        float lacunarity, float scale, int octaves, Vector2 offset, int mapChunkSize)
    {
        float[,] heightmap = new float[mapChunkSize,mapChunkSize];
        Random random = new Random(seed);
        Vector2[] octaveOffsets = new Vector2[octaves];
        for (int i = 0; i < octaves; i++)
        {
            float offsetX = random.Next(-10000, 10000) + offset.x;
            float offsetY = random.Next(-10000, 10000) + offset.y;
            octaveOffsets[i] = new Vector2(offsetX, offsetY);
        }
        
        float maxHeight = float.MinValue;
        float minHeight = float.MaxValue;

        for (int y = 0; y < mapChunkSize; y++)
        {
            for (int x = 0; x < mapChunkSize; x++)
            {
                float amplitude = 1;
                float frequency = 1;
                float noiseHeight = 0;
                
                for (int i = 0; i < octaves; i++)
                {
                    float _x = x / scale * frequency + octaveOffsets[i].x;
                    float _y = y / scale * frequency + octaveOffsets[i].y;

                    float perlin = Mathf.PerlinNoise(_x, _y);
                    noiseHeight += perlin * amplitude;
                    amplitude *= persistance;
                    frequency *= lacunarity;
                }

                if (noiseHeight > maxHeight)
                    maxHeight = noiseHeight;
                if (noiseHeight < minHeight)
                    minHeight = noiseHeight;
                heightmap[x, y] = noiseHeight;
            }
        }

        for (int y = 0; y < mapChunkSize; y++)
        {
            for (int x = 0; x < mapChunkSize; x++)
            {
                heightmap[x, y] = Mathf.InverseLerp(minHeight, maxHeight, heightmap[x, y]);
            }
        }

        return heightmap;
    }
}
