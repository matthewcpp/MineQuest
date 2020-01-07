using System.IO;
using System.Threading;
using System.Collections.Generic;

using UnityEngine;

namespace MineQuest
{
    class WorldBuilder
    {
        WorldData world = new WorldData();

        public bool IsBuilding { get; private set; }
        public int TotalChunks { get { return GetTotalChunks(); } }
        public int CreatedChunks { get; private set; } = 0;

        private Vector3Int min;
        private Vector3Int max;
        private string filePath;

        public bool Build(string filePath, Vector3Int min, Vector3Int max)
        {
            if (IsBuilding) return false;

            this.min = min;
            this.max = max;
            this.filePath = filePath;
            CreatedChunks = 0;
            IsBuilding = true;
            
            world.database.Create(filePath, min, max);

            var thread = new Thread(()=> { WriteChunks(); });
            thread.Start();

            return true;
        }

        private void WriteChunks()
        {
            var chunk = new Chunk(Vector3Int.zero, world);
            for (int x = min.x; x <= max.x; x++)
            {
                for (int y = min.y; y <= max.y; y++)
                {
                    for (int z = min.z; z <= max.z; z++)
                    {
                        chunk.ChunkPos = new Vector3Int(x, y, z);

                        world.chunkBuilder.Build(chunk);
                        world.database.WriteChunk(chunk);
                        CreatedChunks += 1;
                    }
                }
            }
            
            world.database.Close();
            Debug.Log(string.Format("Done! Wrote: {0} chunks", CreatedChunks));
        }

        private int GetTotalChunks()
        {
            var worldSize = world.database.Size;
            return worldSize.x * worldSize.y * worldSize.z;
        }
    }
}