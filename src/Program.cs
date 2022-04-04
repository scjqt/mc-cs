using System;

namespace Minecraft_Clone
{
    class Program
    {
        static void Main(string[] args)
        {
            using var window = new Window("mc-cs");
            window.Run();
        }
    }
}
