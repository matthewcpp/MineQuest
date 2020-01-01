using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;
using UnityEngine;

namespace MineQuest
{
    class ChunkManager
    {
        ConcurrentQueue<Chunk> chunkQueue = new ConcurrentQueue<Chunk>();
        ConcurrentQueue<ChunkBuilder> chunkBuilderQueue = new ConcurrentQueue<ChunkBuilder>();

        WorldData world;

        Thread workerThread = null;

        private bool isRunning = false;

        public ChunkManager(WorldData worldData)
        {
            this.world = worldData;
        }

        public void EnqueueChunk(Chunk chunk)
        {
            chunkQueue.Enqueue(chunk);
        }

        public bool Start()
        {
            if (workerThread == null)
            {
                isRunning = true;
                workerThread = new Thread(() => { ProcessChunks(); });
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

        public ChunkBuilder GetNextChunk()
        {
            ChunkBuilder builder = null;
            chunkBuilderQueue.TryDequeue(out builder);
            return builder;
        }

        private void ProcessChunks()
        {
            while(isRunning)
            {
                Chunk chunk = null;
                if (chunkQueue.TryDequeue(out chunk))
                {
                    var builder = new ChunkBuilder(world);
                    builder.Build(chunk);

                    chunkBuilderQueue.Enqueue(builder);
                }

                Thread.Sleep(1);
            }
        }
    }

}

