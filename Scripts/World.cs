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
        public ChunkPool chunkPool;
        public Database database = new Database();
    }

    public class World : MonoBehaviour
    {
        public const int chunkSize = 16;
        internal WorldData data = new WorldData();
        private ChunkManager chunkManager;

        public GameObject player;
        public Material blockMaterial;
        public Material transparentBlockMaterial;

        private Vector3Int previousChunkPos = Vector3Int.zero;

        public int buildDistance = 4;
        public int unloadDistance = 5;
        private List<Chunk> pruneList = new List<Chunk>();

        public Interaction Interaction { get; private set; }
        public bool WorldLoaded { get { return data.database.IsOpen; } }
        

        public int ChunkCount { get { return data.chunks.Count; } }

        private void Awake()
        {
            data.textureAtlas = new TextureAtlas(blockMaterial, transparentBlockMaterial);
            data.transform = this.transform;
            data.chunkPool = new ChunkPool(this.transform);

            chunkManager = new ChunkManager(data);
        }

        public float GetWorldHeight(float worldX, float worldY)
        {
            // todo: remove chunk builder from here.
            return data.chunkBuilder.WorldHeight(worldX, worldY) + 1;
        }

        public void Open(string filePath)
        {
            data.database.Open(filePath);
        }

        public void LoadInitial()
        {
            var initialChunkPos = GetChunkPos(player.transform.position);
            LoadChunks(initialChunkPos);
            chunkManager.SerialProcessChunks();
            while (InsertNextChunkMesh());

            Interaction = new Interaction(data);

            chunkManager.Start();
        }

        private void Update()
        {
            if (!WorldLoaded) return;

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
            if (WorldLoaded)
                UpdateDirtyChunks();
        }

        bool InsertNextChunkMesh()
        {
            // find the next viable mesh to build
            ChunkMesh chunkMesh = null;

            do {
                chunkMesh = chunkManager.GetNextMesh();

                if (chunkMesh == null)
                    return false;

                // check that the chunk we are about to build has not been pruned.
                if (data.chunks.ContainsKey(chunkMesh.Chunk.ChunkPos))
                    break;
            } while (true);

            chunkMesh.Attach();

            return true;
        }

        private void UpdateDirtyChunks()
        {
            foreach (var chunk in data.dirtyChunks)
            {
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

                if (chunk.SolidGameObject)
                    data.chunkPool.ReturnChunkObject(chunk.SolidGameObject);

                if (chunk.TransparentGameObject)
                    data.chunkPool.ReturnChunkObject(chunk.TransparentGameObject);
            }

            pruneList.Clear();

        }

        private void OnDestroy()
        {
            chunkManager.Stop();
            data.database.Close();
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