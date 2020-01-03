namespace MineQuest
{
    public struct Block
    {
        public enum Type : short
        {
            Air, Grass, Dirt, Stone, Bedrock, Redstone, Diamond
        }

        public enum Overlay: short
        {
            None, Crack1, Crack2, Crack3, Crack4
        }

        public enum Side
        {
            Front, Back, Left, Right, Bottom, Top
        }

        public Type type;
        public Overlay overlay;

        public bool IsSolid
        {
            get { return this.type != Type.Air; }
        }
    }
}