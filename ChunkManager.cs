using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;
using UnityEngine;

namespace MineQuest
{
    class ChunkManager
    {
        ConcurrentQueue<Chunk> populateQueue = new ConcurrentQueue<Chunk>();
        ConcurrentQueue<Chunk> buildQueue = new ConcurrentQueue<Chunk>();
        ConcurrentQueue<ChunkMesh> chunkMeshQueue = new ConcurrentQueue<ChunkMesh>();

        WorldData world;

        Thread workerThread = null;

        private bool isRunning = false;

        public ChunkManager(WorldData worldData)
        {
            this.world = worldData;
        }

        public void EnqueueChunk(Chunk chunk)
        {
            if (chunk.IsPopulated)
                buildQueue.Enqueue(chunk);
            else
                populateQueue.Enqueue(chunk);
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
            while (!populateQueue.IsEmpty)
            {
                Chunk chunk = null;
                if (populateQueue.TryDequeue(out chunk))
                {
                    chunk.Populate();
                    world.chunkBuilder.Build(chunk);
                    buildQueue.Enqueue(chunk);
                }
            }

            while (!buildQueue.IsEmpty)
            {
                Chunk chunk = null;
                if (buildQueue.TryDequeue(out chunk))
                {

                    var chunkMesh = new ChunkMesh(world);
                    chunkMesh.Build(chunk);

                    chunkMeshQueue.Enqueue(chunkMesh);
                }
            }
        }
    }

}

