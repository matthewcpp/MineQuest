using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MineQuest
{
    class ChunkPool
    {
        Queue<GameObject> chunkObjectPool = new Queue<GameObject>();

        WorldData world;

        public ChunkPool(WorldData worldData)
        {
            world = worldData;
        }

        public GameObject GetChunkObject()
        {
            if (chunkObjectPool.Count > 0)
                return chunkObjectPool.Dequeue();

            GameObject chunkObject = new GameObject();
            chunkObject.transform.parent = world.transform;
            chunkObject.AddComponent<MeshRenderer>().material = world.textureAtlas.BlockMaterial;
            chunkObject.AddComponent<MeshFilter>();
            chunkObject.AddComponent<MeshCollider>();

            return chunkObject;
        }
    }
}
