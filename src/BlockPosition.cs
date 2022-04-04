using System;
using System.Collections.Generic;
using System.Text;
using OpenTK.Mathematics;

namespace Minecraft_Clone
{
    class BlockPosition
    {
        public Vector2i ChunkColumn;
        public int ChunkHeight;
        public int X;
        public int Y;
        public int Z;

        public BlockPosition(int x, int y, int z)
        {
            ChunkColumn = new Vector2i(x < 0 ? (x - 15) / 16 : x / 16, z < 0 ? (z - 15) / 16 : z / 16);
            ChunkHeight = y / 16;
            X = (x % 16 + 16) % 16;
            Y = y % 16;
            Z = (z % 16 + 16) % 16;
        }
    }
}
