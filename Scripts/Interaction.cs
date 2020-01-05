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

        private Vector3Int DetermineBlockPosFromHit(Chunk chunk, Vector3 point, Vector3 normal)
        {
            var hitBlock = point + (normal / 2.0f); // adjust the point to be in the center of a block
            hitBlock = new Vector3(Mathf.Floor(hitBlock.x), Mathf.Floor(hitBlock.y), Mathf.Floor(hitBlock.z));
            return  Vector3Int.FloorToInt(hitBlock - chunk.SolidGameObject.transform.position);
        }

        public bool HitBlock(Ray ray)
        {
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit))
            {
                Chunk chunk = null;
                var chunkPos = Vector3Int.FloorToInt(hit.point / World.chunkSize);
                world.chunks.TryGetValue(chunkPos, out chunk);

                // move us in towards the block we clicked.
                var blockPos = DetermineBlockPosFromHit(chunk, hit.point, -hit.normal);
                var block = chunk.Blocks[blockPos.x, blockPos.y, blockPos.z];
                bool destroyBlock = false;

                switch(block.type)
                {
                    case Block.Type.Water:
                        return false;

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
            if (Physics.Raycast(ray, out hit))
            {
                Chunk chunk = null;
                var chunkPos = Vector3Int.FloorToInt(hit.point / World.chunkSize);
                world.chunks.TryGetValue(chunkPos, out chunk);

                // move us to the adjacent block that we clicked.
                var blockPos = DetermineBlockPosFromHit(chunk, hit.point, hit.normal);

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