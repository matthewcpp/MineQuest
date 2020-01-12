using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MineQuest
{
    class ChunkBuilder
    {
        public void Build(Chunk chunk)
        {
            var worldPos = chunk.WorldPos;

            for (int z = 0; z < World.chunkSize; z++)
            {
                for (int y = 0; y < World.chunkSize; y++)
                {
                    for (int x = 0; x < World.chunkSize; x++)
                    {
                        Vector3 blockWorldPos = worldPos + new Vector3(x, y, z);
                        chunk.Blocks[x, y, z].type = DetermineBlockType(blockWorldPos);
                    }
                }
            }
        }

        public const float waterLevel = 55.0f;

        Block.Type DetermineBlockType(Vector3 blockWorldPos)
        {
            if (blockWorldPos.y == 0.0f)
                return Block.Type.Bedrock;

            int stoneHeight = StoneHeight(blockWorldPos.x, blockWorldPos.z);
            int dirtHeight = WorldHeight(blockWorldPos.x, blockWorldPos.z);
            var blockType = Block.Type.Air;

            if (blockWorldPos.y < stoneHeight) // this will be a type of stone block
            {
                if (blockWorldPos.y < 20.0f && Noise.BrownianMotion3d(blockWorldPos.x, blockWorldPos.y, blockWorldPos.z, 0.03f, 3) < 0.41f)
                    blockType = Block.Type.Redstone;
                if (blockWorldPos.y < 40.0f && Noise.BrownianMotion3d(blockWorldPos.x, blockWorldPos.y, blockWorldPos.z, 0.01f, 2) < 0.4f)
                    blockType = Block.Type.Diamond;
                else
                    blockType = Block.Type.Stone;
            }

            else if (blockWorldPos.y == dirtHeight)
                blockType = Block.Type.Grass;
            else if (blockWorldPos.y < dirtHeight)
                blockType = Block.Type.Dirt;

            //if a block is below the water line and not filled in yet then we will set it to water
            else if (blockWorldPos.y < waterLevel)
                blockType = Block.Type.Water;

            // this will allow us to create caves carved into stone and dirt
            if (blockType != Block.Type.Water && Noise.BrownianMotion3d(blockWorldPos.x, blockWorldPos.y, blockWorldPos.z, 0.1f, 3) < 0.42f)
                blockType = Block.Type.Air;

            return blockType;
        }

        public const int maxHeight = 150;
        private const float heightSmooth = 0.01f;
        private const int heightOctaves = 4;
        private const float heightPersistence = 0.5f;
        private const int stoneHeightOffset = 10;

        public int WorldHeight(float x, float z)
        {
            var val = Noise.BrownianMotion(x * heightSmooth, z * heightSmooth, heightOctaves, heightPersistence);
            float height = Util.MapValue(val, 0.0f, 1.0f, 0.0f, maxHeight);
            return (int)height;
        }

        public static int StoneHeight(float x, float z)
        {
            var val = Noise.BrownianMotion(x * heightSmooth * 2, z * heightSmooth * 2, heightOctaves + 1, heightPersistence);
            float height = Util.MapValue(val, 0, 1, 0, maxHeight - stoneHeightOffset);
            return (int)height;
        }
    }
}