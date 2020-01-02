using System;
using System.Collections.Generic;
using UnityEngine;

namespace MineQuest
{
    class WorldData
    {
        public Dictionary<Vector3Int, Chunk> chunks = new Dictionary<Vector3Int, Chunk>();
        public TextureAtlas textureAtlas;
        public ChunkBuilder chunkBuilder = new ChunkBuilder();
        public Transform transform;
    }

    public class World : MonoBehaviour
    {
        public const int chunkSize = 16;
        private WorldData data = new WorldData();
        private ChunkManager chunkManager;
        private ChunkPool chunkPool;

        public GameObject player;
        public Material blockMaterial;

        private Vector3Int previousChunkPos = Vector3Int.zero;

        public int buildDistance = 4;
        public int unloadDistance = 5;
        private List<Chunk> pruneList = new List<Chunk>();

        public int ChunkCount { get { return data.chunks.Count; } }

        private void Start()
        {
            data.textureAtlas = new TextureAtlas(blockMaterial);
            data.transform = this.transform;

            chunkManager = new ChunkManager(data);
            chunkPool = new ChunkPool(data);

            SetupWorld();

            chunkManager.Start();
        }

        private void SetupWorld()
        {
            // determine the correct initial position for the player
            var playerPos = player.transform.position;
            playerPos.y = data.chunkBuilder.WorldHeight(playerPos.x, playerPos.z) + 1;

            // temp...use of FPS controller
            var characterController = player.GetComponent<CharacterController>();
            characterController.enabled = false;
            player.transform.position = playerPos;
            characterController.enabled = true;


            // load the initial player chunk
            var initialChunkPos = GetChunkPos(playerPos);
            EnqueueChunkPos(initialChunkPos);
            chunkManager.SerialProcessChunks();
            InsertNextChunkMesh();
        }

        private void Update()
        {
            var playerChunkPos = GetChunkPos(player.transform.position);

            if (playerChunkPos != previousChunkPos)
            {
                previousChunkPos = playerChunkPos;
                LoadChunks(playerChunkPos);
            }
            
            InsertNextChunkMesh();
            PruneChunks(playerChunkPos);
        }

        void InsertNextChunkMesh()
        {
            // find the next viable mesh to build
            ChunkMesh chunkMesh = null;

            do {
                chunkMesh = chunkManager.GetNextMesh();

                if (chunkMesh == null)
                    return;

                // check that the chunk we are about to build has not been pruned.
                if (data.chunks.ContainsKey(chunkMesh.Chunk.ChunkPos))
                    break;
            } while (true);

            var chunkGameObject = chunkPool.GetChunkObject();
            chunkGameObject.name = chunkMesh.Chunk.ChunkPos.ToString();

            chunkMesh.Chunk.GameObject = chunkGameObject;

            var mesh = new Mesh();
            mesh.name = chunkMesh.Chunk.ChunkPos.ToString();
            mesh.SetVertices(chunkMesh.Vertices);
            mesh.SetNormals(chunkMesh.Normals);
            mesh.SetUVs(0, chunkMesh.TexCoords);
            mesh.SetTriangles(chunkMesh.Indices, 0);
            mesh.RecalculateBounds();

            chunkGameObject.transform.position = chunkMesh.Chunk.WorldPos;
            chunkGameObject.GetComponent<MeshFilter>().sharedMesh = mesh;
            chunkGameObject.GetComponent<MeshCollider>().sharedMesh = mesh;
        }

        void PruneChunks(Vector3Int referencePoint)
        {
            foreach(var chunk in data.chunks)
            {
                if (Vector3Int.Distance(referencePoint, chunk.Value.ChunkPos) >= unloadDistance)
                    pruneList.Add(chunk.Value);
            }

            foreach(var chunk in pruneList)
            {
                data.chunks.Remove(chunk.ChunkPos);
                chunkPool.ReturnChunkObject(chunk.GameObject);
            }

            pruneList.Clear();

        }

        private void OnDestroy()
        {
            chunkManager.Stop();
        }

        private void LoadChunks(Vector3Int chunkPos)
        {
            LoadChunksRec(chunkPos, buildDistance);
        }

        private void EnqueueChunkPos(Vector3Int chunkPos)
        {
            if (!data.chunks.ContainsKey(chunkPos))
            {
                var chunk = new Chunk(chunkPos);
                data.chunks[chunkPos] = chunk;
                chunkManager.EnqueueChunk(chunk);
            }
        }

        private void LoadChunksRec(Vector3Int chunkPos, int depth)
        {
            EnqueueChunkPos(chunkPos);

            if (depth > 0)
            {
                LoadChunksRec(new Vector3Int(chunkPos.x + 1, chunkPos.y, chunkPos.z), depth - 1);
                LoadChunksRec(new Vector3Int(chunkPos.x - 1, chunkPos.y, chunkPos.z), depth - 1);

                LoadChunksRec(new Vector3Int(chunkPos.x, chunkPos.y, chunkPos.z + 1), depth - 1);
                LoadChunksRec(new Vector3Int(chunkPos.x, chunkPos.y, chunkPos.z - 1), depth - 1);

                LoadChunksRec(new Vector3Int(chunkPos.x, chunkPos.y + 1, chunkPos.z), depth - 1);

                if (chunkPos.y > 0)
                    LoadChunksRec(new Vector3Int(chunkPos.x, chunkPos.y - 1, chunkPos.z), depth - 1);
            }
        }

        public static Vector3Int GetChunkPos(Vector3 worldPos)
        {
            return new Vector3Int(Mathf.FloorToInt(worldPos.x / chunkSize), Mathf.FloorToInt(worldPos.y / chunkSize), Mathf.FloorToInt(worldPos.z / chunkSize));
        }
    }
}