using System;
using System.Collections.Generic;
using System.Text;

namespace Minecraft_Clone
{
    class Type
    {
        public static Dictionary<string, Type> Types = new Dictionary<string, Type>
        {
            { "air", new Type("air") },
            { "grass", new Type("grass", "solid") },
            { "dirt", new Type("dirt", "solid") },
            { "stone", new Type("stone", "solid") },
        };

        public string Name { get; }
        public int Index { get; set; }
        public Model Model { get; }

        private Type(string name, string model)
        {
            Name = name;
            Model = Model.Models[model];
        }

        private Type(string name)
        {
            Name = name;
        }
    }
}
