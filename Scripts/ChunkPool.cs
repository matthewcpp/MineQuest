using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MineQuest
{
    class ChunkPool
    {
        Queue<GameObject> chunkObjectPool = new Queue<GameObject>();
        Transform worldTransform;

        internal ChunkPool(Transform worldTransform)
        {
            this.worldTransform = worldTransform;
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
                chunkObject.transform.parent = worldTransform;
                chunkObject.AddComponent<MeshRenderer>();
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
