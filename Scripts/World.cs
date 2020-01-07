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
        

        public int ChunkCount { get { return data.chunks.Count; } }

        private void Start()
        {
            data.textureAtlas = new TextureAtlas(blockMaterial, transparentBlockMaterial);
            data.transform = this.transform;
            data.chunkPool = new ChunkPool(this.transform);

            chunkManager = new ChunkManager(data);
            Interaction = new Interaction(data);

            SetupWorld();

            chunkManager.Start();
        }

        private void SetupWorld()
        {
            // determine the correct initial position for the player
            // todo: remove chunk builder from here.
            var playerPos = player.transform.position;
            playerPos.y = data.chunkBuilder.WorldHeight(playerPos.x, playerPos.z) + 1;

            // load the initial chunks around the player
            var initialChunkPos = GetChunkPos(playerPos);
            LoadChunks(initialChunkPos);
            chunkManager.SerialProcessChunks();
            while(InsertNextChunkMesh());

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