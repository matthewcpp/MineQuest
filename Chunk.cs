using UnityEngine;

namespace MineQuest
{
    class Chunk
    {
        public Block[,,] Blocks { get; private set; } = null;
        public Vector3 WorldPos { get
            {
                return new Vector3(ChunkPos.x * World.chunkSize, ChunkPos.y * World.chunkSize, ChunkPos.z * World.chunkSize);
            }
        }

        public bool IsPopulated { get { return Blocks != null; } }

        public GameObject GameObject {get;set;}

        public Vector3Int ChunkPos { get; set; }

        public Chunk(Vector3Int chunkPos)
        {
            ChunkPos = chunkPos;
        }

        public bool Populate()
        {
            if (IsPopulated) return false;

            Blocks = new Block[World.chunkSize, World.chunkSize, World.chunkSize];

            for (int z = 0; z < World.chunkSize; z++)
            {
                for (int y = 0; y < World.chunkSize; y++)
                {
                    for (int x = 0; x < World.chunkSize; x++)
                    {
                        Vector3 blockWorldPos = WorldPos + new Vector3(x, y, z);
                        Blocks[x, y, z].type = DetermineBlockType(blockWorldPos);
                    }
                }
            }

            return true;
        }

        Block.Type DetermineBlockType(Vector3 blockWorldPos)
        {
            if (Mathf.PerlinNoise(blockWorldPos.x, blockWorldPos.z) > 0.5)
                return Block.Type.Air;

            if (blockWorldPos.y >= World.chunkSize - 1)
                return Block.Type.Grass;
            else
                return Block.Type.Dirt;
        }
    }
}