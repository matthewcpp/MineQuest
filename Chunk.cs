using System.IO;

using UnityEngine;

namespace MineQuest
{
    class Chunk
    {
        public Block[,,] Blocks { get; set; } = null;
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

        public bool WriteBinary(BinaryWriter writer)
        {
            if (!IsPopulated) return false;

            for (int x = 0; x < World.chunkSize; x++)
            {
                for (int y = 0; y < World.chunkSize; y++)
                {
                    for (int z = 0; z < World.chunkSize; z++)
                    {
                        writer.Write((int)Blocks[x, y, z].type);
                    }
                }
            }

            return true;
        }

        public void ReadBinary(BinaryReader reader)
        {
            Populate();

            for (int x = 0; x < World.chunkSize; x++)
            {
                for (int y = 0; y < World.chunkSize; y++)
                {
                    for (int z = 0; z < World.chunkSize; z++)
                    {
                        Blocks[x, y, z].type = (Block.Type)reader.ReadInt32();
                    }
                }
            }
        }
    }
}