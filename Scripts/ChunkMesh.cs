using System;
using System.Collections.Generic;
using UnityEngine;

namespace MineQuest
{
    class ChunkMesh
    {
        public class Data
        {
            public List<Vector3> Vertices { get; } = new List<Vector3>();
            public List<Vector3> Normals { get; } = new List<Vector3>();
            public List<Vector2> TexCoords { get; } = new List<Vector2>();
            public List<Vector2> OverlayTexCoords { get; } = new List<Vector2>();
            public List<int> Indices { get; } = new List<int>();
        }

        public Data Solid { get; } = new Data();
        public Data Transparent { get; } = new Data();

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

        /// <summary>
        /// Creates a new mesh and attaches it to the supplied game object.
        /// The game object should already have a MeshRenderer and MeshFilter attached.<br />
        /// <b>This method should only be called from the main thread.</b>
        /// </summary>
        /// <param name="chunkGameObject">GameObject which will receive new mesh</param>
        public void Attach()
        {
            if (Solid.Indices.Count > 0)
            {
                var chunkGameObject = world.chunkPool.GetChunkObject();
                Chunk.SolidGameObject = chunkGameObject;
                chunkGameObject.name = Chunk.ChunkPos.ToString();
                chunkGameObject.transform.position = Chunk.WorldPos;

                chunkGameObject.GetComponent<MeshRenderer>().sharedMaterial = world.textureAtlas.BlockMaterial;
                var mesh = new Mesh();
                mesh.name = Chunk.ChunkPos.ToString();

                SetMeshData(chunkGameObject, mesh, Solid);
                UpdateMeshCollider(chunkGameObject, mesh);
            }

            if (Transparent.Indices.Count > 0)
            {
                var chunkGameObject = world.chunkPool.GetChunkObject();
                Chunk.TransparentGameObject = chunkGameObject;
                chunkGameObject.name = Chunk.ChunkPos.ToString();
                chunkGameObject.transform.position = Chunk.WorldPos;

                chunkGameObject.GetComponent<MeshRenderer>().sharedMaterial = world.textureAtlas.TransparentBlockMaterial;
                var mesh = new Mesh();
                mesh.name = Chunk.ChunkPos.ToString();

                SetMeshData(chunkGameObject, mesh, Transparent);
                chunkGameObject.GetComponent<MeshCollider>().enabled = false;
            }
        }

        private void UpdateMeshCollider(GameObject chunkGameObject, Mesh mesh)
        {
            var meshCollider = chunkGameObject.GetComponent<MeshCollider>();

            // in the case that a mesh collider already exists, we will need to clear it out so it will be updated with the new mesh
            if (meshCollider.sharedMesh != null)
                meshCollider.sharedMesh = null;

            meshCollider.sharedMesh = mesh;
            meshCollider.enabled = true;
        }

        /// <summary>
        /// Updates game objects for the associated Chunk.
        /// The game object should already have a MeshRenderer and MeshFilter attached.<br />
        /// <b>This method should only be called from the main thread.</b>
        /// </summary>
        /// <param name="chunkGameObject">GameObject which will receive new mesh</param>
        public void UpdateMesh()
        {
            var mesh = Chunk.SolidGameObject.GetComponent<MeshFilter>().sharedMesh;
            mesh.Clear();
            SetMeshData(Chunk.SolidGameObject, mesh, Solid);
            UpdateMeshCollider(Chunk.SolidGameObject, mesh);
        }

        private void SetMeshData(GameObject gameObject, Mesh mesh, Data data)
        {
            mesh.SetVertices(data.Vertices);
            mesh.SetNormals(data.Normals);
            mesh.SetUVs(0, data.TexCoords);
            mesh.SetUVs(1, data.OverlayTexCoords);
            mesh.SetTriangles(data.Indices, 0);
            mesh.RecalculateBounds();

            var meshFilter = gameObject.GetComponent<MeshFilter>();

            if (meshFilter.sharedMesh == null)
                meshFilter.sharedMesh = mesh;
        }

        /// <summary>
        /// Determines if the neighboring block with the given position is solid.
        /// If the neighbor lies in another chuck, the method will attempt to fetch the chunk and check the correct block.
        /// If the neighboring chunk does not exist, this method will return false.
        /// </summary>
        /// <param name="neighborX">Block position X</param>
        /// <param name="neighborY">Block position Y</param>
        /// <param name="neighborZ">Block position Z</param>
        /// <returns></returns>
        bool BlockNeighborIsSolid(int neighborX, int neighborY, int neighborZ)
        {
            // neighboring block is actually in another chunk
            if (!Chunk.BlockPosIsInChunk(neighborX,neighborY,neighborZ))
            {
                var neighbor = Util.GetNeighboringChunkForBlock(Chunk, neighborX, neighborY, neighborZ);
                
                if (neighbor != null)
                    return neighbor.Blocks[Util.GetNeighborBlockIndex(neighborX), Util.GetNeighborBlockIndex(neighborY), Util.GetNeighborBlockIndex(neighborZ)].IsSolid;
                else // if neighbor is null then we have hit a world boundary 
                    return false;
            }
            else
                return Chunk.Blocks[neighborX, neighborY, neighborZ].IsSolid;
        }

        bool BlockNeighborIsType(int neighborX, int neighborY, int neighborZ, Block.Type blockType)
        {
            Chunk actualChunk;
            Vector3Int actualBlockPos;
            if (Util.ResolveBlock(Chunk, new Vector3Int(neighborX, neighborY, neighborZ), out actualChunk, out actualBlockPos)) 
            {
                return actualChunk.Blocks[actualBlockPos.x, actualBlockPos.y, actualBlockPos.z].type == blockType;
            }

            return false;
        }

        void BuildBlock(int blockX, int blockY, int blockZ)
        {
            if (Chunk.Blocks[blockX, blockY, blockZ].type == Block.Type.Air) return;

            if (Chunk.Blocks[blockX, blockY, blockZ].type == Block.Type.Water)
            {
                if (!BlockNeighborIsType(blockX, blockY + 1, blockZ, Block.Type.Water))
                    BuildBlockSide(Transparent, blockX, blockY, blockZ, Block.Side.Top);
            }
            else
            {
                if (!BlockNeighborIsSolid(blockX, blockY, blockZ + 1))
                    BuildBlockSide(Solid, blockX, blockY, blockZ, Block.Side.Front);
                if (!BlockNeighborIsSolid(blockX, blockY, blockZ - 1))
                    BuildBlockSide(Solid, blockX, blockY, blockZ, Block.Side.Back);
                if (!BlockNeighborIsSolid(blockX, blockY + 1, blockZ))
                    BuildBlockSide(Solid, blockX, blockY, blockZ, Block.Side.Top);
                if (!BlockNeighborIsSolid(blockX, blockY - 1, blockZ))
                    BuildBlockSide(Solid, blockX, blockY, blockZ, Block.Side.Bottom);
                if (!BlockNeighborIsSolid(blockX + 1, blockY, blockZ))
                    BuildBlockSide(Solid, blockX, blockY, blockZ, Block.Side.Right);
                if (!BlockNeighborIsSolid(blockX - 1, blockY, blockZ))
                    BuildBlockSide(Solid, blockX, blockY, blockZ, Block.Side.Left);
            }
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
                case Block.Type.Water:
                    return world.textureAtlas.GetCoords(TextureType.Water);
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

        void AddFaceVertices(Data data, Vector3[] vertices, Vector3 normal, Vector3 blockPos)
        {
            int indexBase = data.Vertices.Count;

            foreach (var vertex in vertices)
            {
                data.Vertices.Add(blockPos + vertex);
                data.Normals.Add(normal);
            }

            foreach (var index in blockSideIndices)
                data.Indices.Add(indexBase + index);
        }

        void BuildBlockSide(Data data, int blockX, int blockY, int blockZ, Block.Side side)
        {
            var blockPos = new Vector3(blockX, blockY, blockZ);

            switch (side)
            {
                case Block.Side.Front:
                    AddFaceVertices(data, frontVertices, Vector3.forward, blockPos);
                    break;
                case Block.Side.Back:
                    AddFaceVertices(data, backVertices, Vector3.back, blockPos);
                    break;
                case Block.Side.Top:
                    AddFaceVertices(data, topVertices, Vector3.up, blockPos);
                    break;
                case Block.Side.Bottom:
                    AddFaceVertices(data, bottomVertices, Vector3.down, blockPos);
                    break;
                case Block.Side.Right:
                    AddFaceVertices(data, rightVertices, Vector3.right, blockPos);
                    break;
                case Block.Side.Left:
                    AddFaceVertices(data, leftVertices, Vector3.left, blockPos);
                    break;
            }

            var blockUvs = GetBlockUVs(Chunk.Blocks[blockX, blockY, blockZ].type, side);

            data.TexCoords.Add(blockUvs[3]);
            data.TexCoords.Add(blockUvs[2]);
            data.TexCoords.Add(blockUvs[0]);
            data.TexCoords.Add(blockUvs[1]);

            var overlayUvs = GetOverlayUVs(Chunk.Blocks[blockX, blockY, blockZ].overlay);
            data.OverlayTexCoords.Add(overlayUvs[3]);
            data.OverlayTexCoords.Add(overlayUvs[2]);
            data.OverlayTexCoords.Add(overlayUvs[0]);
            data.OverlayTexCoords.Add(overlayUvs[1]);
        }

        static Vector3[] frontVertices = new Vector3[] { new Vector3(0.0f, 1.0f, 1.0f), new Vector3(1.0f, 1.0f, 1.0f), new Vector3(1.0f, 0.0f, 1.0f), new Vector3(0.0f, 0.0f, 1.0f) };
        static Vector3[] backVertices = new Vector3[] { new Vector3(1.0f, 1.0f, 0.0f), new Vector3(0.0f, 1.0f, 0.0f), new Vector3(0.0f, 0.0f, 0.0f), new Vector3(1.0f, 0.0f, 0.0f) };
        static Vector3[] topVertices = new Vector3[] { new Vector3(0.0f, 1.0f, 0.0f), new Vector3(1.0f, 1.0f, 0.0f), new Vector3(1.0f, 1.0f, 1.0f), new Vector3(0.0f, 1.0f, 1.0f) };
        static Vector3[] bottomVertices = new Vector3[] { new Vector3(0.0f, 0.0f, 1.0f), new Vector3(1.0f, 0.0f, 1.0f), new Vector3(1.0f, 0.0f, 0.0f), new Vector3(0.0f, 0.0f, 0.0f) };
        static Vector3[] rightVertices = new Vector3[] { new Vector3(1.0f, 1.0f, 1.0f), new Vector3(1.0f, 1.0f, 0.0f), new Vector3(1.0f, 0.0f, 0.0f), new Vector3(1.0f, 0.0f, 1.0f) };
        static Vector3[] leftVertices = new Vector3[] { new Vector3(0.0f, 1.0f, 0.0f), new Vector3(0.0f, 1.0f, 1.0f), new Vector3(0.0f, 0.0f, 1.0f), new Vector3(0.0f, 0.0f, 0.0f) };
        static int[] blockSideIndices = new int[] { 3, 1, 0, 3, 2, 1 };
    }
}