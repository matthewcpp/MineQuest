using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MineQuest
{
    class ChunkMesh
    {
        public List<Vector3> Vertices { get; private set; } = new List<Vector3>();
        public List<Vector3> Normals { get; private set; } = new List<Vector3>();
        public List<Vector2> TexCoords { get; private set; } = new List<Vector2>();
        public List<int> Indices { get; private set; } = new List<int>();

        public Chunk Chunk { get; private set; }
        private WorldData world;

        public ChunkMesh(WorldData worldData)
        {
            world = worldData;
        }

        public void Build(Chunk chunk)
        {
            Chunk = chunk;

            for (int z = 0; z < World.chunkSize; z++)
            {
                for (int y = 0; y < World.chunkSize; y++)
                {
                    for (int x = 0; x < World.chunkSize; x++)
                    {
                        BuildBlock(x, y, z);
                    }
                }
            }
        }

        int GetNeighborChunkOffset(int value)
        {
            if (value < 0)
                return -1;
            else if (value >= World.chunkSize)
                return 1;
            else
                return 0;
        }

        Chunk GetNeighboringChunkForBlock(int x, int y, int z)
        {
            Vector3Int neighborChunkPos = Chunk.ChunkPos + new Vector3Int(GetNeighborChunkOffset(x), GetNeighborChunkOffset(y), GetNeighborChunkOffset(z));

            Chunk neighbor = null;
            world.chunks.TryGetValue(neighborChunkPos, out neighbor);

            return neighbor;
        }

        /** Gets the index of a value in the neighboring block for the given value. */
        int GetNeighborBlockIndex(int value)
        {
            if (value < 0)
                return World.chunkSize - 1;
            else if (value >= World.chunkSize)
                return 0;
            else
                return value;
        }

        /**
         * Determines if the neighboring block with the given position is solid.
         * If the neighbor lies in another chuck, the method will attempt to fetch the chunk and check the correct block.
         * If the neighboring chunk does not exist, this method will return false.
         */
        bool BlockNeighborIsSolid(int x, int y, int z)
        {
            // neighboring block is actually in another chunk
            if ((x < 0 || x >= World.chunkSize) || (y < 0 || y >= World.chunkSize) || (z < 0 || z >= World.chunkSize))
            {
                Chunk neighbor = GetNeighboringChunkForBlock(x, y, z);
                
                if (neighbor != null && neighbor.IsPopulated)
                    return neighbor.Blocks[GetNeighborBlockIndex(x), GetNeighborBlockIndex(y), GetNeighborBlockIndex(z)].IsSolid;
                else // if neighbor is null then we have hit a world boundary 
                    return false;
            }
            else
                return Chunk.Blocks[x, y, z].IsSolid;
        }

        void BuildBlock(int blockX, int blockY, int blockZ)
        {
            if (Chunk.Blocks[blockX, blockY, blockZ].type == Block.Type.Air) return;

            if (!BlockNeighborIsSolid(blockX, blockY, blockZ + 1))
                BuildBlockSide(blockX, blockY, blockZ, Block.Side.Front);
            if (!BlockNeighborIsSolid(blockX, blockY, blockZ - 1))
                BuildBlockSide(blockX, blockY, blockZ, Block.Side.Back);
            if (!BlockNeighborIsSolid(blockX, blockY + 1, blockZ))
                BuildBlockSide(blockX, blockY, blockZ, Block.Side.Top);
            if (!BlockNeighborIsSolid(blockX, blockY - 1, blockZ))
                BuildBlockSide(blockX, blockY, blockZ, Block.Side.Bottom);
            if (!BlockNeighborIsSolid(blockX + 1, blockY, blockZ))
                BuildBlockSide(blockX, blockY, blockZ, Block.Side.Right);
            if (!BlockNeighborIsSolid(blockX - 1, blockY, blockZ))
                BuildBlockSide(blockX, blockY, blockZ, Block.Side.Left);
        }

        Vector2[] GetBlockUVs(Block.Type blockType, Block.Side side)
        {
            Vector2[] uvs = null;

            switch (blockType)
            {
                case Block.Type.Grass:
                    if (side == Block.Side.Top)
                        uvs = world.textureAtlas.GetCoords(TextureType.GrassTop);
                    else if (side == Block.Side.Bottom)
                        uvs = world.textureAtlas.GetCoords(TextureType.Dirt);
                    else
                        uvs = world.textureAtlas.GetCoords(TextureType.GrassSide);
                    break;
                case Block.Type.Dirt:
                    uvs = world.textureAtlas.GetCoords(TextureType.Dirt);
                    break;
            }

            return uvs;
        }

        void AddFaceVertices(Vector3[] vertices, Vector3 normal, Vector3 blockPos)
        {
            int indexBase = Vertices.Count;

            foreach (var vertex in vertices)
            {
                Vertices.Add(blockPos + vertex);
                Normals.Add(normal);
            }

            foreach (var index in blockSideIndices)
                Indices.Add(indexBase + index);
        }

        void BuildBlockSide(int blockX, int blockY, int blockZ, Block.Side side)
        {
            var blockPos = new Vector3(blockX, blockY, blockZ);

            switch (side)
            {
                case Block.Side.Front:
                    AddFaceVertices(frontVertices, Vector3.forward, blockPos);
                    break;
                case Block.Side.Back:
                    AddFaceVertices(backVertices, Vector3.back, blockPos);
                    break;
                case Block.Side.Top:
                    AddFaceVertices(topVertices, Vector3.up, blockPos);
                    break;
                case Block.Side.Bottom:
                    AddFaceVertices(bottomVertices, Vector3.down, blockPos);
                    break;
                case Block.Side.Right:
                    AddFaceVertices(rightVertices, Vector3.right, blockPos);
                    break;
                case Block.Side.Left:
                    AddFaceVertices(leftVertices, Vector3.left, blockPos);
                    break;
            }

            var blockUvs = GetBlockUVs(Chunk.Blocks[blockX, blockY, blockZ].type, side);

            TexCoords.Add(blockUvs[3]);
            TexCoords.Add(blockUvs[2]);
            TexCoords.Add(blockUvs[0]);
            TexCoords.Add(blockUvs[1]);
        }

        static Vector3[] frontVertices = new Vector3[] { new Vector3(-0.5f, 0.5f, 0.5f), new Vector3(0.5f, 0.5f, 0.5f), new Vector3(0.5f, -0.5f, 0.5f), new Vector3(-0.5f, -0.5f, 0.5f) };
        static Vector3[] backVertices = new Vector3[] { new Vector3(0.5f, 0.5f, -0.5f), new Vector3(-0.5f, 0.5f, -0.5f), new Vector3(-0.5f, -0.5f, -0.5f), new Vector3(0.5f, -0.5f, -0.5f) };
        static Vector3[] topVertices = new Vector3[] { new Vector3(-0.5f, 0.5f, -0.5f), new Vector3(0.5f, 0.5f, -0.5f), new Vector3(0.5f, 0.5f, 0.5f), new Vector3(-0.5f, 0.5f, 0.5f) };
        static Vector3[] bottomVertices = new Vector3[] { new Vector3(-0.5f, -0.5f, 0.5f), new Vector3(0.5f, -0.5f, 0.5f), new Vector3(0.5f, -0.5f, -0.5f), new Vector3(-0.5f, -0.5f, -0.5f) };
        static Vector3[] rightVertices = new Vector3[] { new Vector3(0.5f, 0.5f, 0.5f), new Vector3(0.5f, 0.5f, -0.5f), new Vector3(0.5f, -0.5f, -0.5f), new Vector3(0.5f, -0.5f, 0.5f) };
        static Vector3[] leftVertices = new Vector3[] { new Vector3(-0.5f, 0.5f, -0.5f), new Vector3(-0.5f, 0.5f, 0.5f), new Vector3(-0.5f, -0.5f, 0.5f), new Vector3(-0.5f, -0.5f, -0.5f) };
        static int[] blockSideIndices = new int[] { 3, 1, 0, 3, 2, 1 };


        //all possible vertices
        static Vector3 p0 = new Vector3(-0.5f, -0.5f, 0.5f);
        static Vector3 p1 = new Vector3(0.5f, -0.5f, 0.5f);
        static Vector3 p2 = new Vector3(0.5f, -0.5f, -0.5f);
        static Vector3 p3 = new Vector3(-0.5f, -0.5f, -0.5f);
        static Vector3 p4 = new Vector3(-0.5f, 0.5f, 0.5f);
        static Vector3 p5 = new Vector3(0.5f, 0.5f, 0.5f);
        static Vector3 p6 = new Vector3(0.5f, 0.5f, -0.5f);
        static Vector3 p7 = new Vector3(-0.5f, 0.5f, -0.5f);
    }
}