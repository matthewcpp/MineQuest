using System;
using System.Collections;
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
        }

        void InsertNextChunkMesh()
        {
            var chunkMesh = chunkManager.GetNextMesh();

            if (chunkMesh != null)
            {
                var chunkGameObject = chunkPool.GetChunkObject();

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
        }

        private void OnDestroy()
        {
            chunkManager.Stop();
        }

        private void LoadChunks(Vector3Int chunkPos)
        {
            LoadChunksRec(chunkPos, 4);
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
                LoadChunksRec(new Vector3Int(chunkPos.x, chunkPos.y - 1, chunkPos.z), depth - 1);
            }
        }

        public static Vector3Int GetChunkPos(Vector3 worldPos)
        {
            return new Vector3Int(Mathf.FloorToInt(worldPos.x / chunkSize), Mathf.FloorToInt(worldPos.y / chunkSize), Mathf.FloorToInt(worldPos.z / chunkSize));
        }
    }
}