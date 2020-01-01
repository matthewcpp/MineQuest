using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MineQuest
{
    class ChunkBuilder
    {
        public void Build(Chunk chunk)
        {
            var woldPos = chunk.WorldPos;

            for (int z = 0; z < World.chunkSize; z++)
            {
                for (int y = 0; y < World.chunkSize; y++)
                {
                    for (int x = 0; x < World.chunkSize; x++)
                    {
                        Vector3 blockWorldPos = woldPos + new Vector3(x, y, z);
                        chunk.Blocks[x, y, z].type = DetermineBlockType(blockWorldPos);
                    }
                }
            }
        }

        Block.Type DetermineBlockType(Vector3 blockWorldPos)
        {
            if (blockWorldPos.y == 0.0f)
                return Block.Type.Bedrock;

            if (Noise.BrownianMotion3d(blockWorldPos.x, blockWorldPos.y, blockWorldPos.z, 0.1f, 3) < 0.42f)
                return Block.Type.Air;

            int stoneHeight = StoneHeight(blockWorldPos.x, blockWorldPos.z);
            if (blockWorldPos.y < stoneHeight)
            {
                if (blockWorldPos.y < 20.0f && Noise.BrownianMotion3d(blockWorldPos.x, blockWorldPos.y, blockWorldPos.z, 0.03f, 3) < 0.41f)
                    return Block.Type.Redstone;
                if (blockWorldPos.y < 40.0f && Noise.BrownianMotion3d(blockWorldPos.x, blockWorldPos.y, blockWorldPos.z, 0.01f, 2) < 0.4f)
                    return Block.Type.Diamond;
                else
                    return Block.Type.Stone;
            }

            int dirtHeight = WorldHeight(blockWorldPos.x, blockWorldPos.z);

            if (blockWorldPos.y == dirtHeight)
                return Block.Type.Grass;
            else if (blockWorldPos.y < dirtHeight)
                return Block.Type.Dirt;

            return Block.Type.Air;
        }

        private const int maxHeight = 150;
        private const float heightSmooth = 0.01f;
        private const int heightOctaves = 4;
        private const float heightPersistence = 0.5f;

        public int WorldHeight(float x, float z)
        {
            float height = Map(0.0f, 1.0f, 0.0f, maxHeight, Noise.BrownianMotion(x * heightSmooth, z * heightSmooth, heightOctaves, heightPersistence));
            return (int)height;
        }

        public static int StoneHeight(float x, float z)
        {
            float height = Map(0, 1, 0, maxHeight - 5, Noise.BrownianMotion(x * heightSmooth * 2, z * heightSmooth * 2, heightOctaves + 1, heightPersistence));
            return (int)height;
        }

        static float Map(float origmin, float origmax, float newmin, float newmax, float value)
        {
            return Mathf.Lerp(newmin, newmax, Mathf.InverseLerp(origmin, origmax, value));
        }
    }
}