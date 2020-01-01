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
            GameObject chunkObject = null;

            if (chunkObjectPool.Count > 0)
            {
                chunkObject = chunkObjectPool.Dequeue();
                chunkObject.SetActive(true);
            }
            else
            {
                chunkObject = new GameObject();
                chunkObject.transform.parent = world.transform;
                chunkObject.AddComponent<MeshRenderer>().material = world.textureAtlas.BlockMaterial;
                chunkObject.AddComponent<MeshFilter>();
                chunkObject.AddComponent<MeshCollider>();
            }

            return chunkObject;
        }

        public void ReturnChunkObject(GameObject chunkObject)
        {
            var meshFilter = chunkObject.GetComponent<MeshFilter>();
            var meshCollider = chunkObject.GetComponent<MeshCollider>();

            var mesh = meshFilter.sharedMesh;

            meshFilter.sharedMesh = null;
            meshCollider.sharedMesh = null;

            GameObject.Destroy(mesh);

            chunkObject.SetActive(false);

            chunkObjectPool.Enqueue(chunkObject);
        }
    }
}
