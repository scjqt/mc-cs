using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using OpenTK.Mathematics;

namespace Minecraft_Clone
{
    struct Block
    {
        public Type Type { get; private set; }

        private readonly BitArray _visible;

        private readonly Vector3i _position;

        private List<FaceData> _faceData;

        public Block(int x, int y, int z)
        {
            _visible = new BitArray(6, true);

            _position = new Vector3i(x, y, z);

            Type = null;
            _faceData = null;
        }

        public void TypeUpdate(string type)
        {
            Type = Type.Types[type];

            if (type != "air")
            {
                _faceData = Type.Model.GetData(_position, Type.Index);
            }
        }

        public bool FaceUpdate(int face, bool visible)
        {
            if (_visible[face] != visible)
            {
                _visible[face] = visible;
                return true;
            }
            return false;
        }

        public List<float> GetData()
        {
            List<float> data = new List<float>();

            if (Type.Name != "air")
            {
                if (Type.Model.Name == "solid")
                {
                    for (int i = 0; i < 6; i++)
                    {
                        if (_visible[i])
                        {
                            data.AddRange(_faceData[i].ToVBO());
                        }
                    }
                }
                else
                {
                    foreach (FaceData face in _faceData)
                    {
                        data.AddRange(face.ToVBO());
                    }
                }
            }

            return data;
        }
    }
}
