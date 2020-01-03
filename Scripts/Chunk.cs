﻿using System.IO;

using UnityEngine;

namespace MineQuest
{
    public class Chunk
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

        public bool IsDirty { 
            get { return world.dirtyChunks.Contains(this); } 
            set { SetDirty(value); } 
        }

        private WorldData world;

        internal  Chunk(Vector3Int chunkPos, WorldData worldData)
        {
            ChunkPos = chunkPos;
            world = worldData;
        }

        public void UpdateBlockType(Vector3Int blockPos, Block.Type type)
        {
            Blocks[blockPos.x, blockPos.y, blockPos.z].type = type;
            IsDirty = true;

            MarkNeighborsDirty(blockPos);
        }

        void MarkNeighborsDirty(Vector3Int blockPos)
        {
            Chunk neighbor;

            if (blockPos.x == 0 && world.chunks.TryGetValue(ChunkPos + Vector3Int.left, out neighbor))
                neighbor.IsDirty = true;
            if (blockPos.x == World.chunkSize - 1 && world.chunks.TryGetValue(ChunkPos + Vector3Int.right, out neighbor))
                neighbor.IsDirty = true;

            if (blockPos.y == 0 && world.chunks.TryGetValue(ChunkPos + Vector3Int.down, out neighbor))
                neighbor.IsDirty = true;
            if (blockPos.y == World.chunkSize - 1 && world.chunks.TryGetValue(ChunkPos + Vector3Int.up, out neighbor))
                neighbor.IsDirty = true;

            if (blockPos.z == 0 && world.chunks.TryGetValue(ChunkPos + new Vector3Int(0, 0, -1), out neighbor))
                neighbor.IsDirty = true;
            if (blockPos.z == World.chunkSize - 1 && world.chunks.TryGetValue(ChunkPos + new Vector3Int(0, 0, 1), out neighbor))
                neighbor.IsDirty = true;
        }

        public bool Populate()
        {
            if (IsPopulated) return false;

            Blocks = new Block[World.chunkSize, World.chunkSize, World.chunkSize];

            return true;
        }

        private void SetDirty(bool dirty)
        {
            if (dirty)
                world.dirtyChunks.Add(this);
            else
                world.dirtyChunks.Remove(this);
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