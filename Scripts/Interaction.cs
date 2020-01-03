using UnityEngine;

namespace MineQuest
{
    public class Interaction
    {
        private WorldData world;

        internal Interaction(WorldData worldData)
        {
            world = worldData;
        }

        public bool RaycastBlock(Ray ray, out RaycastHit hit, ref Chunk chunk, ref Vector3Int blockPos)
        {
            if (Physics.Raycast(ray, out hit))
            {
                var chunkPos = Vector3Int.FloorToInt(hit.point / World.chunkSize);
                world.chunks.TryGetValue(chunkPos, out chunk);

                var hitBlock = hit.point - (hit.normal / 2.0f); // move to  "center" of the hit block
                hitBlock = new Vector3(Mathf.Floor(hitBlock.x), Mathf.Floor(hitBlock.y), Mathf.Floor(hitBlock.z));
                blockPos = Vector3Int.FloorToInt(hitBlock - chunk.GameObject.transform.position);

                return true;
            }

            return false;
        }

        public bool HitBlock(Ray ray)
        {
            RaycastHit hit;
            Chunk chunk = null;
            Vector3Int blockPos = default;
            if (RaycastBlock(ray, out hit, ref chunk, ref blockPos))
            {
                var block = chunk.Blocks[blockPos.x, blockPos.y, blockPos.z];
                bool destroyBlock = false;

                switch(block.type)
                {
                    case Block.Type.Dirt:
                    case Block.Type.Grass:
                        destroyBlock = (block.overlay == Block.Overlay.Crack2);
                        break;

                    case Block.Type.Stone:
                    case Block.Type.Diamond:
                    case Block.Type.Redstone:
                        destroyBlock = (block.overlay == Block.Overlay.Crack3);
                        break;
                }

                if (destroyBlock)
                    chunk.UpdateBlockType(blockPos, Block.Type.Air);
                else
                    chunk.UpdateBlockOverlay(blockPos, block.overlay + 1);

                return true;
            }

            return false;
        }

        public bool InsertBlock(Ray ray, Block.Type blockType)
        {
            RaycastHit hit;
            Chunk chunk = null;
            Vector3Int blockPos = default;
            if (RaycastBlock(ray, out hit, ref chunk, ref blockPos))
            {
                var chunkPos = Vector3Int.FloorToInt(hit.point / World.chunkSize);
                world.chunks.TryGetValue(chunkPos, out chunk);

                var hitBlock = hit.point + (hit.normal / 2.0f); // move to  "center" of the neighbor block
                hitBlock = new Vector3(Mathf.Floor(hitBlock.x), Mathf.Floor(hitBlock.y), Mathf.Floor(hitBlock.z));
                blockPos = Vector3Int.FloorToInt(hitBlock - chunk.GameObject.transform.position);

                Chunk targetChunk = null;
                Vector3Int targetBlockPos = default;
                if (Util.ResolveBlock(chunk, blockPos, out targetChunk, out targetBlockPos))
                {
                    targetChunk.UpdateBlockType(targetBlockPos, blockType);
                    return true;
                }
            }

            return false;
        }
    }
}