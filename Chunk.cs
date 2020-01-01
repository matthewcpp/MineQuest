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

            return true;
        }


    }
}