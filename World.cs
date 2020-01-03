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
        

        public int ChunkCount { get { return data.chunks.Count; } }

        private void Start()
        {
            data.textureAtlas = new TextureAtlas(blockMaterial);
            data.transform = this.transform;
            data.dataDir = "D:/temp/minecraft";

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

        public bool RaycastBlock(Ray ray, out RaycastHit hit, ref Chunk chunk, ref Vector3Int blockPos)
        {
            if (Physics.Raycast(ray, out hit))
            {
                var chunkPos = Vector3Int.FloorToInt(hit.point / World.chunkSize);
                data.chunks.TryGetValue(chunkPos, out chunk);

                var hitBlock = hit.point - (hit.normal / 2.0f); // move to  "center" of the hit block
                hitBlock = new Vector3(Mathf.Floor(hitBlock.x), Mathf.Floor(hitBlock.y), Mathf.Floor(hitBlock.z));
                blockPos = Vector3Int.FloorToInt(hitBlock - chunk.GameObject.transform.position);

                return true;
            }

            return false;
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

        private void UpdateDirtyChunks()
        {
            foreach (var chunk in data.dirtyChunks)
            {
                if (!chunk.IsPopulated) continue;

                var chunkMesh = new ChunkMesh(data);
                chunkMesh.Build(chunk);

                var mesh = chunk.GameObject.GetComponent<MeshFilter>().sharedMesh;
                mesh.Clear();

                mesh.SetVertices(chunkMesh.Vertices);
                mesh.SetNormals(chunkMesh.Normals);
                mesh.SetUVs(0, chunkMesh.TexCoords);
                mesh.SetTriangles(chunkMesh.Indices, 0);
                mesh.RecalculateBounds();

                var meshCollider = chunk.GameObject.GetComponent<MeshCollider>();
                meshCollider.sharedMesh = null;
                meshCollider.sharedMesh = mesh;
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