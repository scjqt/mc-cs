using System;
using System.Collections.Generic;
using System.Text;

namespace Minecraft_Clone
{
    class ChunkColumn
    {
        private readonly Chunk[] _chunks;

        public ChunkColumn(int x, int z, int EBO)
        {
            _chunks = new Chunk[16];
            for (int y = 0; y < 16; y++)
            {
                _chunks[y] = new Chunk(x, y, z, EBO, y);
            }
        }

        public void TypeUpdate(BlockPosition position, string type)
        {
            _chunks[position.ChunkHeight].TypeUpdate(position, type);
        }

        public void FaceUpdate(BlockPosition position, int face, bool visible)
        {
            _chunks[position.ChunkHeight].FaceUpdate(position, face, visible);
        }

        public string BlockType(BlockPosition position)
        {
            return _chunks[position.ChunkHeight].BlockType(position);
        }

        public List<AABB> AABBs(BlockPosition position)
        {
            return _chunks[position.ChunkHeight].AABBs(position);
        }

        public void Render()
        {
            for (int y = 0; y < 16; y++)
            {
                _chunks[y].Render();
            }
        }

        public void Dispose()
        {
            for (int y = 0; y < 16; y++)
            {
                _chunks[y].Dispose();
            }
        }
    }
}
