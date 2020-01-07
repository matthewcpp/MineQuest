using System.IO;
using System.Runtime.CompilerServices;

using UnityEngine;

namespace MineQuest
{
    public class Chunk
    {
        public Block[,,] Blocks { get; set; } = new Block[World.chunkSize, World.chunkSize, World.chunkSize];
        public Vector3 WorldPos { get
            {
                return new Vector3(ChunkPos.x * World.chunkSize, ChunkPos.y * World.chunkSize, ChunkPos.z * World.chunkSize);
            }
        }

        public GameObject SolidGameObject {get;set;}
        public GameObject TransparentGameObject { get; set; }

        public Vector3Int ChunkPos { get; set; }

        public bool IsDirty { 
            get { return world.dirtyChunks.Contains(this); } 
            set { SetDirty(value); } 
        }

        internal WorldData world;

        internal  Chunk(Vector3Int chunkPos, WorldData worldData)
        {
            ChunkPos = chunkPos;
            world = worldData;
        }

        /// <summary>
        /// Updates the type for the block at the given position, resets the overlay value, and marks the chunk as dirty.
        /// </summary>
        /// <param name="blockPos">Block position to update</param>
        /// <param name="type">New block type</param>
        public void UpdateBlockType(Vector3Int blockPos, Block.Type type)
        {
            Blocks[blockPos.x, blockPos.y, blockPos.z].type = type;
            Blocks[blockPos.x, blockPos.y, blockPos.z].overlay = Block.Overlay.None;

            IsDirty = true;

            MarkNeighborsDirty(blockPos);
        }

        public void UpdateBlockOverlay(Vector3Int blockPos, Block.Overlay overlay)
        {
            Blocks[blockPos.x, blockPos.y, blockPos.z].overlay = overlay;
            IsDirty = true;
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

        private void SetDirty(bool dirty)
        {
            if (dirty)
                world.dirtyChunks.Add(this);
            else
                world.dirtyChunks.Remove(this);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool BlockPosIsInChunk(int x, int y, int z)
        {
            return (x >= 0 && x < World.chunkSize) && (y >= 0 && y < World.chunkSize) && (z >= 0 && z < World.chunkSize);
        }
    }
}