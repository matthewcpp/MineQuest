namespace MineQuest
{
    public struct Block
    {
        public enum Type
        {
            Air, Grass, Dirt, Stone, Bedrock, Redstone, Diamond
        }

        public enum Side
        {
            Front, Back, Left, Right, Bottom, Top
        }

        public Type type;

        public bool IsSolid
        {
            get { return this.type != Type.Air; }
        }
    }
}