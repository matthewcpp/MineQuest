using UnityEngine;

namespace MineQuest
{
    static class Util
    {
        public static bool ResolveBlock(Chunk sourceChunk, Vector3Int sourceBlockPos, out Chunk actualChunk, out Vector3Int actualBlockPos)
        {
            if (Chunk.BlockPosIsInChunk(sourceBlockPos.x, sourceBlockPos.y, sourceBlockPos.z))
            {
                actualChunk = sourceChunk;
                actualBlockPos = sourceBlockPos;

                return true;
            }
            else
            {
                var neighbor = GetNeighboringChunkForBlock(sourceChunk, sourceBlockPos.x, sourceBlockPos.y, sourceBlockPos.z);

                if (neighbor != null)
                {
                    actualChunk = neighbor;
                    actualBlockPos = new Vector3Int(Util.GetNeighborBlockIndex(sourceBlockPos.x), Util.GetNeighborBlockIndex(sourceBlockPos.y), Util.GetNeighborBlockIndex(sourceBlockPos.z));
                    
                    return true;
                }
                else
                {
                    actualChunk = default;
                    actualBlockPos = default;

                    return false;
                }
            }
        }

        public static Chunk GetNeighboringChunkForBlock(Chunk chunk, int x, int y, int z)
        {
            Vector3Int neighborChunkPos = chunk.ChunkPos + new Vector3Int(GetNeighborChunkOffset(x), GetNeighborChunkOffset(y), GetNeighborChunkOffset(z));

            Chunk neighbor = null;
            chunk.world.chunks.TryGetValue(neighborChunkPos, out neighbor);

            return neighbor;
        }

        /** Gets the index of a value in the neighboring block for the given value. */
        public static int GetNeighborBlockIndex(int value)
        {
            if (value < 0)
                return World.chunkSize - 1;
            else if (value >= World.chunkSize)
                return 0;
            else
                return value;
        }

        private static int GetNeighborChunkOffset(int value)
        {
            if (value < 0)
                return -1;
            else if (value >= World.chunkSize)
                return 1;
            else
                return 0;
        }
    }
}