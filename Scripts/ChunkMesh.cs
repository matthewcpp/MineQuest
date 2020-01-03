using System;
using System.Collections.Generic;
using UnityEngine;

namespace MineQuest
{
    class ChunkMesh
    {
        public List<Vector3> Vertices { get;} = new List<Vector3>();
        public List<Vector3> Normals { get;} = new List<Vector3>();
        public List<Vector2> TexCoords { get;} = new List<Vector2>();
        public List<Vector2> OverlayTexCoords { get;} = new List<Vector2>();
        public List<int> Indices { get;} = new List<int>();

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

        // Note: this can only be called from the main thread
        public void AttachMesh(GameObject chunkGameObject)
        {
            var mesh = new Mesh();
            mesh.name = Chunk.ChunkPos.ToString();
            SetMeshData(mesh);
        }

        public void UpdateMesh()
        {
            var mesh = Chunk.GameObject.GetComponent<MeshFilter>().sharedMesh;
            mesh.Clear();
            SetMeshData(mesh);
        }

        private void SetMeshData(Mesh mesh)
        {
            mesh.SetVertices(Vertices);
            mesh.SetNormals(Normals);
            mesh.SetUVs(0, TexCoords);
            mesh.SetUVs(1, OverlayTexCoords);
            mesh.SetTriangles(Indices, 0);
            mesh.RecalculateBounds();

            var meshCollider = Chunk.GameObject.GetComponent<MeshCollider>();
            var meshFilter = Chunk.GameObject.GetComponent<MeshFilter>();

            if (meshFilter.sharedMesh == null)
                meshFilter.sharedMesh = mesh;

            if (meshCollider.sharedMesh != null)
                meshCollider.sharedMesh = null;

            meshCollider.sharedMesh = mesh;
        }



        /**
         * Determines if the neighboring block with the given position is solid.
         * If the neighbor lies in another chuck, the method will attempt to fetch the chunk and check the correct block.
         * If the neighboring chunk does not exist, this method will return false.
         */
        bool BlockNeighborIsSolid(int x, int y, int z)
        {
            // neighboring block is actually in another chunk
            if (!Chunk.BlockPosIsInChunk(x,y,z))
            {
                var neighbor = Util.GetNeighboringChunkForBlock(Chunk, x, y, z);
                
                if (neighbor != null && neighbor.IsPopulated)
                    return neighbor.Blocks[Util.GetNeighborBlockIndex(x), Util.GetNeighborBlockIndex(y), Util.GetNeighborBlockIndex(z)].IsSolid;
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
            switch (blockType)
            {
                case Block.Type.Grass:
                    if (side == Block.Side.Top)
                        return world.textureAtlas.GetCoords(TextureType.GrassTop);
                    else if (side == Block.Side.Bottom)
                        return world.textureAtlas.GetCoords(TextureType.Dirt);
                    else
                        return world.textureAtlas.GetCoords(TextureType.GrassSide);
                case Block.Type.Dirt:
                    return world.textureAtlas.GetCoords(TextureType.Dirt);
                case Block.Type.Stone:
                    return world.textureAtlas.GetCoords(TextureType.Stone);
                case Block.Type.Bedrock:
                    return world.textureAtlas.GetCoords(TextureType.Bedrock);
                case Block.Type.Redstone:
                    return world.textureAtlas.GetCoords(TextureType.Redstone);
                case Block.Type.Diamond:
                    return world.textureAtlas.GetCoords(TextureType.Diamond);
                default:
                    throw new ArgumentException(string.Format("Unsupported Block Type: {0}", blockType.ToString()));
            }
        }

        Vector2[] GetOverlayUVs(Block.Overlay overlayType)
        {
            switch(overlayType)
            {
                case Block.Overlay.None:
                    return world.textureAtlas.GetCoords(TextureType.Crack0);
                case Block.Overlay.Crack1:
                    return world.textureAtlas.GetCoords(TextureType.Crack1);
                case Block.Overlay.Crack2:
                    return world.textureAtlas.GetCoords(TextureType.Crack2);
                case Block.Overlay.Crack3:
                    return world.textureAtlas.GetCoords(TextureType.Crack3);
                case Block.Overlay.Crack4:
                    return world.textureAtlas.GetCoords(TextureType.Crack4);
                default:
                    throw new ArgumentException(string.Format("Unsupported Overlay Type: {0}", overlayType.ToString()));
            }
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

            var overlayUvs = GetOverlayUVs(Chunk.Blocks[blockX, blockY, blockZ].overlay);
            OverlayTexCoords.Add(overlayUvs[3]);
            OverlayTexCoords.Add(overlayUvs[2]);
            OverlayTexCoords.Add(overlayUvs[0]);
            OverlayTexCoords.Add(overlayUvs[1]);
        }

        static Vector3[] frontVertices = new Vector3[] { new Vector3(0.0f, 1.0f, 1.0f), new Vector3(1.0f, 1.0f, 1.0f), new Vector3(1.0f, 0.0f, 1.0f), new Vector3(0.0f, 0.0f, 1.0f) };
        static Vector3[] backVertices = new Vector3[] { new Vector3(1.0f, 1.0f, 0.0f), new Vector3(0.0f, 1.0f, 0.0f), new Vector3(0.0f, 0.0f, 0.0f), new Vector3(1.0f, 0.0f, 0.0f) };
        static Vector3[] topVertices = new Vector3[] { new Vector3(0.0f, 1.0f, 0.0f), new Vector3(1.0f, 1.0f, 0.0f), new Vector3(1.0f, 1.0f, 1.0f), new Vector3(0.0f, 1.0f, 1.0f) };
        static Vector3[] bottomVertices = new Vector3[] { new Vector3(0.0f, 0.0f, 1.0f), new Vector3(1.0f, 0.0f, 1.0f), new Vector3(1.0f, 0.0f, 0.0f), new Vector3(0.0f, 0.0f, 0.0f) };
        static Vector3[] rightVertices = new Vector3[] { new Vector3(1.0f, 1.0f, 1.0f), new Vector3(1.0f, 1.0f, 0.0f), new Vector3(1.0f, 0.0f, 0.0f), new Vector3(1.0f, 0.0f, 1.0f) };
        static Vector3[] leftVertices = new Vector3[] { new Vector3(0.0f, 1.0f, 0.0f), new Vector3(0.0f, 1.0f, 1.0f), new Vector3(0.0f, 0.0f, 1.0f), new Vector3(0.0f, 0.0f, 0.0f) };
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