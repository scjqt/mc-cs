using System;
using System.Collections.Generic;
using System.Text;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace Minecraft_Clone
{
    class World
    {
        private readonly Dictionary<Vector2i, ChunkColumn> _map;

        private Texture _texture;

        private int _EBO;

        public World()
        {
            InitTextures();
            InitEBO();

            _map = new Dictionary<Vector2i, ChunkColumn>();

            LoadChunkColumn(0, -1);
            LoadChunkColumn(1, -1);
            LoadChunkColumn(0, -2);
            LoadChunkColumn(1, -2);
        }

        private void InitEBO()
        {
            int maxSize = 16 * 16 * 16 * 6 / 2;
            List<uint> indicesList = new List<uint>();
            for (uint i = 0; i < maxSize; i++)
            {
                uint j = i * 4;
                indicesList.AddRange(new uint[]
                {
                    j, j + 1, j + 2,
                    j, j + 2, j + 3,
                });
            }

            uint[] indices = indicesList.ToArray();

            _EBO = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, _EBO);
            GL.BufferData(BufferTarget.ArrayBuffer, indices.Length * sizeof(uint), indices, BufferUsageHint.StaticDraw);
        }

        public void BlockUpdate(int x, int y, int z, string type)
        {
            BlockPosition position = new BlockPosition(x, y, z);
            _map[position.ChunkColumn].TypeUpdate(position, type);

            FacesUpdate(x, y, z, type);
        }

        private void FacesUpdate(int x, int y, int z, string type)
        {
            bool visible = type == "air";

            FaceUpdate(x, y - 1, z, 0, visible);
            FaceUpdate(x, y + 1, z, 1, visible);
            FaceUpdate(x, y, z - 1, 2, visible);
            FaceUpdate(x, y, z + 1, 3, visible);
            FaceUpdate(x - 1, y, z, 4, visible);
            FaceUpdate(x + 1, y, z, 5, visible);
        }

        private void FaceUpdate(int x, int y, int z, int face, bool visible)
        {
            BlockPosition position = new BlockPosition(x, y, z);
            if (y >= 0 && y < 256 &&_map.ContainsKey(position.ChunkColumn))
            {
                _map[position.ChunkColumn].FaceUpdate(position, face, visible);
            }
        }

        public string BlockType(int x, int y, int z)
        {
            BlockPosition position = new BlockPosition(x, y, z);
            if (y >= 0 && y < 256 && _map.ContainsKey(position.ChunkColumn))
            {
                return _map[position.ChunkColumn].BlockType(position);
            }
            return null;
        }

        public List<AABB> BlockAABBs(int x, int y, int z)
        {
            BlockPosition position = new BlockPosition(x, y, z);
            if (y >= 0 && y < 256 && _map.ContainsKey(position.ChunkColumn) && _map[position.ChunkColumn].BlockType(position) != "air")
            {
                return _map[position.ChunkColumn].AABBs(position);
            }
            return new List<AABB>();
        }

        private void LoadChunkColumn(int cx, int cz)
        {
            _map[new Vector2i(cx, cz)] = new ChunkColumn(cx, cz, _EBO);

            for (int z = -1; z < 17; z++)
            {
                for (int y = 0; y < 256; y++)
                {
                    for (int x = -1; x < 17; x++)
                    {
                        int X = x + cx * 16;
                        int Z = z + cz * 16;
                        BlockPosition position = new BlockPosition(X, y, Z);
                        if (_map.ContainsKey(position.ChunkColumn))
                        {
                            FacesUpdate(X, y, Z, _map[position.ChunkColumn].BlockType(position));
                        }
                    }
                }
            }
        }

        public List<AABB> SurroundingAABBs(Vector3d start, Vector3d end, double width, double height)
        {
            double extra = 0.1;

            Vector3i min = new Vector3i(
                (int)Math.Floor(Math.Min(start.X, end.X) - width / 2 - extra),
                (int)Math.Floor(Math.Min(start.Y, end.Y) - extra),
                (int)Math.Floor(Math.Min(start.Z, end.Z) - width / 2 - extra));
            Vector3i max = new Vector3i(
                (int)Math.Floor(Math.Max(start.X, end.X) + width / 2 + extra),
                (int)Math.Floor(Math.Max(start.Y, end.Y) + height + extra),
                (int)Math.Floor(Math.Max(start.Z, end.Z) + width / 2 + extra));

            List<AABB> AABBs = new List<AABB>();

            for (int z = min.Z; z <= max.Z; z++)
            {
                for (int y = min.Y; y <= max.Y; y++)
                {
                    for (int x = min.X; x <= max.X; x++)
                    {
                        foreach (var AABB in BlockAABBs(x, y, z))
                        {
                            AABBs.Add(AABB + new Vector3i(x, y, z));
                            AABBs[^1].Round(10); // necessary?
                        }
                    }
                }
            }

            return AABBs;
        }

        private void InitTextures()
        {
            _texture = new Texture();

            foreach (KeyValuePair<string, Type> type in Type.Types)
            {
                if (type.Key == "air")
                {
                    Type.Types[type.Key].Index = -1;
                }
                else
                {
                    Type.Types[type.Key].Index = _texture.AddTexture("Assets/Resources/Textures/" + type.Value.Name + "-top.png");
                    _texture.AddTexture("Assets/Resources/Textures/" + type.Value.Name + "-bottom.png");
                    _texture.AddTexture("Assets/Resources/Textures/" + type.Value.Name + "-side.png");
                }
            }

            _texture.GenerateMipmaps();
        }

        public void Render()
        {
            foreach (ChunkColumn chunkColumn in _map.Values)
            {
                chunkColumn.Render();
            }
        }

        public void Dispose()
        {
            _texture.Delete();

            foreach (ChunkColumn chunkColumn in _map.Values)
            {
                chunkColumn.Dispose();
            }

            GL.DeleteBuffer(_EBO);
        }
    }
}
