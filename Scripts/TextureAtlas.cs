using UnityEngine;
using System;
namespace MineQuest
{
    enum TextureType
    {
        GrassTop, GrassSide, Dirt, Stone, Bedrock, Redstone, Diamond
    }

    class TextureAtlas
    {
        public Material BlockMaterial { get; private set; }

        public TextureAtlas(Material blockMaterial)
        {
            BlockMaterial = blockMaterial;
        }
        
        public Vector2[] GetCoords(TextureType type)
        {
            switch (type)
            {
                case TextureType.GrassTop:
                    return grassTop;
                case TextureType.GrassSide:
                    return grassSide;
                case TextureType.Dirt:
                    return dirt;
                case TextureType.Stone:
                    return stone;
                case TextureType.Bedrock:
                    return bedrock;
                case TextureType.Redstone:
                    return redstone;
                case TextureType.Diamond:
                    return diamond;
                default:
                    throw new ArgumentException(string.Format("Unsupported Texture Type: {0}", type.ToString()));
            }
        }

        private static Vector2[] grassTop = new Vector2[] { new Vector2(0.125f, 0.375f), new Vector2(0.1875f, 0.375f), new Vector2(0.125f, 0.4375f), new Vector2(0.1875f, 0.4375f) };
        private static Vector2[] grassSide = new Vector2[] { new Vector2(0.1875f, 0.9375f), new Vector2(0.25f, 0.9375f), new Vector2(0.1875f, 1.0f), new Vector2(0.25f, 1.0f) };
        private static Vector2[] dirt = { new Vector2(0.125f, 0.9375f), new Vector2(0.1875f, 0.9375f), new Vector2(0.125f, 1.0f), new Vector2(0.1875f, 1.0f) };
        private static Vector2[] stone = { new Vector2(0, 0.875f), new Vector2(0.0625f, 0.875f), new Vector2(0, 0.9375f), new Vector2(0.0625f, 0.9375f) };
        private static Vector2[] bedrock = { new Vector2(0.3125f, 0.8125f), new Vector2(0.375f, 0.8125f), new Vector2(0.3125f, 0.875f), new Vector2(0.375f, 0.875f) };
        private static Vector2[] redstone = { new Vector2(0.1875f, 0.75f), new Vector2(0.25f, 0.75f), new Vector2(0.1875f, 0.8125f), new Vector2(0.25f, 0.8125f) };
        private static Vector2[] diamond = { new Vector2(0.125f, 0.75f), new Vector2(0.1875f, 0.75f), new Vector2(0.125f, 0.8125f), new Vector2(0.1875f, 0.8125f) };
    }
}