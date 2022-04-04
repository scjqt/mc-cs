using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using OpenTK.Mathematics;

namespace Minecraft_Clone
{
    class Model
    {
        public static Dictionary<string, Model> Models = new Dictionary<string, Model>
        {
            { "solid", new Model("solid") }
        };

        public string Name { get; }
        private readonly List<FaceData> _faces;
        public readonly List<AABB> AABBs;

        private Model(string name)
        {
            Name = name;

            _faces = new List<FaceData>();

            string[] lines = File.ReadAllLines("Assets/Resources/Models/" + name + ".txt");
            for (int i = 0; i < lines.Length; i += 5)
            {
                string[] info = lines[i].Split(',');
                float normalIndex = int.Parse(info[0]);
                float textureOffset = int.Parse(info[1]);

                Vector3[] positions = new Vector3[4];

                for (int j = 0; j < 4; j++)
                {
                    string[] position = lines[i + j + 1].Split(',');
                    positions[j] = new Vector3(float.Parse(position[0]), float.Parse(position[1]), float.Parse(position[2]));
                }

                _faces.Add(new FaceData()
                {
                    Positions = positions,
                    TextureCoords = new Vector2[] { new Vector2(0, 0), new Vector2(0, 1), new Vector2(1, 1), new Vector2(1, 0) },
                    TextureIndex = textureOffset,
                    NormalIndex = normalIndex,
                });
            }

            AABBs = new List<AABB>
            {
                new AABB()
                {
                    Min = new Vector3d(0, 0, 0),
                    Max = new Vector3d(1, 1, 1),
                }
            };
        }

        public List<FaceData> GetData(Vector3i position, int texture)
        {
            List<FaceData> data = new List<FaceData>();

            foreach (FaceData face in _faces)
            {
                Vector3[] positions = new Vector3[4];
                face.Positions.CopyTo(positions, 0);

                for (int i = 0; i < 4; i++)
                {
                    positions[i] += position;
                }

                data.Add(new FaceData()
                {
                    Positions = positions,
                    TextureCoords = face.TextureCoords,
                    TextureIndex = face.TextureIndex + texture,
                    NormalIndex = face.NormalIndex,
                });
            }

            return data;
        }
    }

    struct FaceData
    {
        public Vector3[] Positions;
        public Vector2[] TextureCoords;
        public float TextureIndex;
        public float NormalIndex;

        public List<float> ToVBO()
        {
            List<float> data = new List<float>();

            for (int i = 0; i < 4; i++)
            {
                data.Add(Positions[i].X);
                data.Add(Positions[i].Y);
                data.Add(Positions[i].Z);
                data.Add(TextureCoords[i].X);
                data.Add(TextureCoords[i].Y);
                data.Add(TextureIndex);
                data.Add(NormalIndex);
            }

            return data;
        }
    }
}
