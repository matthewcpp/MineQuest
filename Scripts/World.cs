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
        public HashSet<Chunk> dirtyChunks = new HashSet<Chunk>();
        public string dataDir;
    }

    public class World : MonoBehaviour
    {
        public const int chunkSize = 16;
        internal WorldData data = new WorldData();
        private ChunkManager chunkManager;
        private ChunkPool chunkPool;

        public GameObject player;
        public Material blockMaterial;

        private Vector3Int previousChunkPos = Vector3Int.zero;

        public int buildDistance = 4;
        public int unloadDistance = 5;
        private List<Chunk> pruneList = new List<Chunk>();

        public Interaction Interaction { get; private set; }
        

        public int ChunkCount { get { return data.chunks.Count; } }

        private void Start()
        {
            data.textureAtlas = new TextureAtlas(blockMaterial);
            data.transform = this.transform;
            data.dataDir = "D:/temp/minecraft";

            chunkManager = new ChunkManager(data);
            chunkPool = new ChunkPool(data);
            Interaction = new Interaction(data);

            SetupWorld();

            chunkManager.Start();
        }

        private void SetupWorld()
        {
            // determine the correct initial position for the player
            var playerPos = player.transform.position;
            playerPos.y = data.chunkBuilder.WorldHeight(playerPos.x, playerPos.z) + 1;

            // load the initial player chunk
            var initialChunkPos = GetChunkPos(playerPos);
            LoadChunks(initialChunkPos);
            chunkManager.SerialProcessChunks();
            InsertNextChunkMesh();

            // temp...use of FPS controller
            var characterController = player.GetComponent<CharacterController>();
            characterController.enabled = false;
            player.transform.position = playerPos;
            characterController.enabled = true;
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

        private void LateUpdate()
        {
            UpdateDirtyChunks();
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

            chunkMesh.Chunk.GameObject = chunkGameObject;
            chunkGameObject.name = chunkMesh.Chunk.ChunkPos.ToString();
            chunkMesh.AttachMesh(chunkGameObject);
            chunkGameObject.transform.position = chunkMesh.Chunk.WorldPos;
        }

        private void UpdateDirtyChunks()
        {
            foreach (var chunk in data.dirtyChunks)
            {
                if (!chunk.IsPopulated) continue;

                var chunkMesh = new ChunkMesh(data);
                chunkMesh.Build(chunk);
                chunkMesh.UpdateMesh();
            }

            data.dirtyChunks.Clear();
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
                chunkManager.PersistChunkData(chunk);
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
                var chunk = new Chunk(chunkPos, data);
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

                if (chunkPos.y <= ChunkBuilder.maxHeight / World.chunkSize)
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