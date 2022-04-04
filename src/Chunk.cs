using System;
using System.Collections.Generic;
using System.Text;
using OpenTK.Mathematics;
using OpenTK.Graphics.OpenGL4;

namespace Minecraft_Clone
{
    class Chunk
    {
        private readonly Block[,,] _blocks;

        private bool _rebuild;

        private Matrix4 _model;

        private readonly int _VAO;
        private readonly int _VBO;
        
        private int _VBOsize;

        public Chunk(int x, int y, int z, int EBO, int chunkY)
        {
            _model = Matrix4.CreateTranslation(x * 16, y * 16, z * 16);

            _blocks = new Block[16, 16, 16];

            _rebuild = true;

            for (z = 0; z < 16; z++)
            {
                for (y = 0; y < 16; y++)
                {
                    for (x = 0; x < 16; x++)
                    {
                        _blocks[x, y, z] = new Block(x, y, z);

                        string type;

                        if (chunkY > 3)
                        {
                            type = "air";
                        }
                        else if (chunkY > 2)
                        {
                            if (y > 14)
                            {
                                type = "grass";
                            }
                            else if (y > 10)
                            {
                                type = "dirt";
                            }
                            else
                            {
                                type = "stone";
                            }
                        }
                        else
                        {
                            type = "stone";
                        }

                        _blocks[x, y, z].TypeUpdate(type);
                    }
                }
            }

            _VAO = GL.GenVertexArray();
            GL.BindVertexArray(_VAO);

            _VBO = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, _VBO);

            GL.BindBuffer(BufferTarget.ElementArrayBuffer, EBO);

            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 7 * sizeof(float), 0);

            GL.EnableVertexAttribArray(1);
            GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 7 * sizeof(float), 3 * sizeof(float));

            GL.EnableVertexAttribArray(2);
            GL.VertexAttribPointer(2, 1, VertexAttribPointerType.Float, false, 7 * sizeof(float), 5 * sizeof(float));

            GL.EnableVertexAttribArray(3);
            GL.VertexAttribPointer(3, 1, VertexAttribPointerType.Float, false, 7 * sizeof(float), 6 * sizeof(float));
        }

        public void TypeUpdate(BlockPosition position, string type)
        {
            _blocks[position.X, position.Y, position.Z].TypeUpdate(type);
            _rebuild = true;
        }

        public void FaceUpdate(BlockPosition position, int face, bool visible)
        {
            if (_blocks[position.X, position.Y, position.Z].FaceUpdate(face, visible) && _blocks[position.X, position.Y, position.Z].Type.Name != "air")
            {
                _rebuild = true;
            }
        }

        public string BlockType(BlockPosition position)
        {
            return _blocks[position.X, position.Y, position.Z].Type.Name;
        }

        public List<AABB> AABBs(BlockPosition position)
        {
            return _blocks[position.X, position.Y, position.Z].Type.Model.AABBs;
        }

        public void Render()
        {
            GL.UniformMatrix4(0, true, ref _model);

            GL.BindVertexArray(_VAO);
            GL.BindBuffer(BufferTarget.ArrayBuffer, _VBO);

            if (_rebuild)
            {
                _rebuild = false;

                List<float> data = new List<float>();
                for (int z = 0; z < 16; z++)
                {
                    for (int y = 0; y < 16; y++)
                    {
                        for (int x = 0; x < 16; x++)
                        {
                            data.AddRange(_blocks[x, y, z].GetData());
                        }
                    }
                }

                float[] vertices = data.ToArray();
                _VBOsize = vertices.Length;

                GL.BufferData(BufferTarget.ArrayBuffer, _VBOsize * sizeof(float), IntPtr.Zero, BufferUsageHint.DynamicDraw);
                GL.BufferSubData(BufferTarget.ArrayBuffer, IntPtr.Zero, _VBOsize * sizeof(float), vertices);
            }

            int count = _VBOsize * 3 / 14;
            GL.DrawElements(PrimitiveType.Triangles, count, DrawElementsType.UnsignedInt, 0);
        }

        public void Dispose()
        {
            GL.DeleteBuffer(_VBO);
            GL.DeleteVertexArray(_VAO);
        }
    }
}
