using System.IO;

using UnityEngine;

namespace MineQuest
{
    internal class Database
    {
        private FileStream fileStream;
        private BinaryReader reader;
        private BinaryWriter writer;

        // a chunk is size * size * size blocks, each of which is 4 bytes
        private const long chunkDataSize = World.chunkSize * World.chunkSize * World.chunkSize * 4;
        private const long baseDataSize = 24;

        public string FilePath { get; private set; }

        public bool IsOpen
        {
            get { return fileStream != null; }
        }
        
        public Vector3Int Size
        {
            get { return (Max - Min) + Vector3Int.one; }
        }

        public Vector3Int Min { get; private set; }
        public Vector3Int Max { get; private set; }

        public void Open(string path)
        {
            Close();

            if (File.Exists(path))
            {
                fileStream = new FileStream(path, FileMode.Open);
                reader = new BinaryReader(fileStream);
                writer = new BinaryWriter(fileStream);
                FilePath = path;

                Min = ReadVector3Int();
                Max = ReadVector3Int();
            }
            else
            {
                throw new FileNotFoundException();
            }
        }

        public void Close()
        {
            if (IsOpen)
            {
                reader = null;
                writer = null;
                fileStream.Close();
                FilePath = null;
                
                Min = Vector3Int.zero;
                Max = Vector3Int.zero;
            }
        }

        public void Create(string path, Vector3Int min, Vector3Int max)
        {
            Close();

            fileStream = new FileStream(path, FileMode.Create);
            reader = new BinaryReader(fileStream);
            writer = new BinaryWriter(fileStream);

            Min = min;
            Max = max;

            var regionSize = Size;
            long dataSize = baseDataSize + (regionSize.x * regionSize.y * regionSize.z * chunkDataSize);
            fileStream.SetLength(dataSize);
            
            WriteVector3Int(Min);
            WriteVector3Int(Max);
        }

        public bool WriteChunk(Chunk chunk)
        {
            SeekToChunk(chunk);
            
            for (int x = 0; x < World.chunkSize; x++)
            {
                for (int y = 0; y < World.chunkSize; y++)
                {
                    for (int z = 0; z < World.chunkSize; z++)
                    {
                        writer.Write((short)chunk.Blocks[x, y, z].type);
                        writer.Write((short)chunk.Blocks[x, y, z].overlay);
                    }
                }
            }

            return true;
        }

        public void ReadChunk(Chunk chunk)
        {
            SeekToChunk(chunk);
            
            for (int x = 0; x < World.chunkSize; x++)
            {
                for (int y = 0; y < World.chunkSize; y++)
                {
                    for (int z = 0; z < World.chunkSize; z++)
                    {
                        chunk.Blocks[x, y, z].type = (Block.Type)reader.ReadInt16();
                        chunk.Blocks[x, y, z].overlay = (Block.Overlay)reader.ReadInt16();
                    }
                }
            }
        }

        void SeekToChunk(Chunk chunk)
        {
            long x = (long) Util.MapValue(chunk.ChunkPos.x, Min.x, Max.x, 0, Max.x - Min.x);
            long y = (long) Util.MapValue(chunk.ChunkPos.y, Min.y, Max.y, 0, Max.y - Min.y);
            long z = (long) Util.MapValue(chunk.ChunkPos.z, Min.z, Max.z, 0, Max.z - Min.z);

            var regionSize = Size;
            long chunkIndex = (x * regionSize.y * regionSize.z + y * regionSize.x + z);

            fileStream.Seek(baseDataSize + (chunkIndex * chunkDataSize), SeekOrigin.Begin);
        }
        
        private void WriteVector3Int(Vector3Int vec)
        {
            writer.Write(vec.x);
            writer.Write(vec.y);
            writer.Write(vec.z);
        }

        private Vector3Int ReadVector3Int()
        {
            return new Vector3Int(reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32());
        }
        
    }
}