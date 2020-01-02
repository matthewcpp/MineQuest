using System.IO;
using System.Threading;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

using UnityEngine;

namespace MineQuest
{
    class ChunkManager
    {
        ConcurrentQueue<Chunk> persistQueue = new ConcurrentQueue<Chunk>();
        ConcurrentQueue<Chunk> populateQueue = new ConcurrentQueue<Chunk>();
        ConcurrentQueue<Chunk> buildQueue = new ConcurrentQueue<Chunk>();
        ConcurrentQueue<ChunkMesh> chunkMeshQueue = new ConcurrentQueue<ChunkMesh>();

        private const int cacheSize = (1024 * 1024 * 1) / (World.chunkSize * World.chunkSize * World.chunkSize * sizeof(int));

        LruCache<Vector3Int, Chunk> chunkCache = new LruCache<Vector3Int, Chunk>(cacheSize);

        WorldData world;

        Thread workerThread = null;

        private bool isRunning = false;

        public int LoadedTotal { get; private set; } = 0;
        public int LoadedFromCache { get; private set; } = 0;
        public int LoadedFromDisk { get; private set; } = 0;
        public int LoadedByBuilding { get; private set; } = 0;

        public ChunkManager(WorldData worldData)
        {
            this.world = worldData;

            chunkCache.OnEvict += WriteChunkToFile;
        }

        public void EnqueueChunk(Chunk chunk)
        {
            if (chunk.IsPopulated)
                buildQueue.Enqueue(chunk);
            else
                populateQueue.Enqueue(chunk);
        }

        public void PersistChunkData(Chunk chunk)
        {
            persistQueue.Enqueue(chunk);
        }

        public bool Start()
        {
            if (workerThread == null)
            {
                isRunning = true;
                workerThread = new Thread(() => { Run(); });
                workerThread.Start();

                return true;
            }

            return false;
        }

        public bool Stop()
        {
            if (workerThread != null && workerThread.ThreadState == ThreadState.Running)
            {
                isRunning = false;
                workerThread.Join();
                workerThread = null;
                return true;
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private string ChunkFileName(Vector3Int chunkPos)
        {
            return string.Format("{0}_{1}_{2}.chunk", chunkPos.x, chunkPos.y, chunkPos.z);
        }

        private void WriteChunkToFile(Vector3Int chunkPos, Chunk chunk)
        {
            var chunkDataPath = Path.Combine(world.dataDir, ChunkFileName(chunkPos));
            
            using (var fileStream = new FileStream(chunkDataPath, FileMode.Create))
            {
                var binaryWriter = new BinaryWriter(fileStream);
                chunk.WriteBinary(binaryWriter);
            }
        }

        public ChunkMesh GetNextMesh()
        {
            ChunkMesh chunkMesh = null;
            chunkMeshQueue.TryDequeue(out chunkMesh);
            return chunkMesh;
        }

        private void Run()
        {
            while(isRunning)
            {
                ProcessChunks();
                Thread.Sleep(1);
            }
        }

        public bool SerialProcessChunks()
        {
            if (!isRunning)
                ProcessChunks();

            return isRunning;
        }

        private void ProcessChunks()
        {
            do
            {
                // note this could trigger writing to disk if cache is full
                if (!persistQueue.IsEmpty)
                {
                    Chunk chunk = null;
                    persistQueue.TryDequeue(out chunk);
                    chunkCache.Add(chunk.ChunkPos, chunk); 

                    continue;
                }

                if (!populateQueue.IsEmpty)
                {
                    Chunk chunk = null;
                    populateQueue.TryDequeue(out chunk);
                    LoadedTotal += 1;

                    // check the cache
                    Chunk cachedChunk = null;
                    if (chunkCache.TryGetValue(chunk.ChunkPos, out cachedChunk)) {
                        chunk.Blocks = cachedChunk.Blocks;
                        LoadedFromCache += 1;
                    }

                    // check file on disk
                    if (!chunk.IsPopulated)
                    {
                        var filePath = ChunkFileName(chunk.ChunkPos);
                        if (File.Exists(filePath))
                        {
                            using (var fileStream = new FileStream(filePath, FileMode.Open))
                            {
                                var binaryReader = new BinaryReader(fileStream);
                                chunk.ReadBinary(binaryReader);
                            }

                            LoadedFromDisk += 1;
                        }
                    }
                    
                    // in this case it is new, generate it 
                    if (!chunk.IsPopulated)
                    {
                        chunk.Populate();
                        world.chunkBuilder.Build(chunk);

                        LoadedByBuilding += 1;
                    }

                    buildQueue.Enqueue(chunk);

                    continue;
                }

                if (!buildQueue.IsEmpty)
                {
                    Chunk chunk = null;
                    buildQueue.TryDequeue(out chunk);

                    var chunkMesh = new ChunkMesh(world);
                    chunkMesh.Build(chunk);
                    chunkMeshQueue.Enqueue(chunkMesh);

                    continue;
                }

            } while (false);
        }
    }

}

