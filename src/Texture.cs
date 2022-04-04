using System;
using System.Collections.Generic;
using System.Text;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System.Drawing;
using System.Drawing.Imaging;

namespace Minecraft_Clone
{
    class Texture
    {
        private static float anisotropicLevel;

        public static void InitAnisotropy()
        {
            float maxAnisotropy = GL.GetFloat((GetPName)All.MaxTextureMaxAnisotropy);
            anisotropicLevel = MathHelper.Clamp(16, 1f, maxAnisotropy);
        }

        private readonly int _handle;
        private int _count;
        
        public Texture()
        {
            _handle = GL.GenTexture();

            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2DArray, _handle);
            GL.TexStorage3D(TextureTarget3d.Texture2DArray, 4, (SizedInternalFormat)All.Srgb8Alpha8, 16, 16, 256);

            GL.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);

            GL.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);

            GL.TexParameter(TextureTarget.Texture2DArray, (TextureParameterName)All.TextureMaxAnisotropy, anisotropicLevel);

            _count = 0;
        }

        public int AddTexture(string path)
        {
            using var image = new Bitmap(path);
            var data = image.LockBits(new Rectangle(0, 0, 16, 16), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            GL.TexSubImage3D(TextureTarget.Texture2DArray, 0, 0, 0, _count, 16, 16, 1, OpenTK.Graphics.OpenGL4.PixelFormat.Bgra, PixelType.UnsignedByte, data.Scan0);

            _count++;

            return _count - 1;
        }

        public void GenerateMipmaps()
        {
            GL.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);
            GL.GenerateMipmap(GenerateMipmapTarget.Texture2DArray);
        }

        public void Delete()
        {
            GL.DeleteTexture(_handle);
        }
    }
}
