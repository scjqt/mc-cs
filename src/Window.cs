using System;
using System.Collections.Generic;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenTK.Graphics.OpenGL4;
using System.Linq;

namespace Minecraft_Clone
{
    class Window : GameWindow
    {
        private const bool fullscreen = true;
        private const int fps_cap = 500;

        public Window(string title) : base(
            new GameWindowSettings()
            { 
                RenderFrequency = fps_cap,
                UpdateFrequency = fps_cap,
            },
            new NativeWindowSettings()
            {
                Size = fullscreen ? new Vector2i(1920, 1080) : new Vector2i(1600, 800),
                Title = title,
                WindowBorder = WindowBorder.Hidden,
                WindowState = fullscreen ? WindowState.Fullscreen : WindowState.Normal,
            }) { }

        private const double REACH = 50;
        private const float AMBIENCE = 0.2f;

        private bool first;

        private Vector2 mouseLast;
        private Dictionary<string, bool> k;
        private Dictionary<string, bool> kl;

        private bool targeted;
        private Vector3i target;
        private Vector3i place;

        private string placeType;

        private Shader worldShader;
        private Shader highlightShader;
        private Shader uiShader;

        private Player player;
        private World world;

        private int UIVAO;
        private int UIEBOsize;

        private int highlightVAO;

        private double doubleJumpTimer;
        private bool doubleJump;

        protected override void OnLoad()
        {
            InitSettings();
            InitShaders();
            InitHighlight();
            InitUI();

            player = new Player(7.5, 66, -7.5);
            SetTime(10.5);
            world = new World();

            first = true;

            doubleJumpTimer = 0;
            doubleJump = false;

            placeType = "stone";

            base.OnLoad();
        }

        protected override void OnUpdateFrame(FrameEventArgs args)
        {
            if (!IsFocused)
            {
                return;
            }

            double deltaTime = args.Time;
            var mouse = MouseState.Position;

            if (first)
            {
                first = false;
                mouseLast = mouse;
                UpdateControls();
                return;
            }

            kl = k;
            UpdateControls();

            if (k["esc"])
            {
                Close();
            }

            if (doubleJump)
            {
                doubleJumpTimer += deltaTime;
                if (doubleJumpTimer > 0.3)
                {
                    doubleJump = false;
                    doubleJumpTimer = 0;
                }
            }

            if (player.Mode != States.Ghost && k["space"] && !kl["space"])
            {
                if (doubleJump)
                {
                    player.Mode = player.Mode == States.Walk ? States.Fly : States.Walk;
                }
                else
                {
                    doubleJump = true;
                }
            }

            if (k["f"] && !kl["f"])
            {
                player.Mode = player.Mode == States.Ghost ? States.Fly : States.Ghost;
            }

            if (k["r"])
            {
                player.Sprint = States.Sprint;
            }

            player.Look(mouse - mouseLast);
            mouseLast = mouse;

            player.Update(deltaTime, world,
                (k["w"] ? 1 : 0) - (k["s"] ? 1 : 0),
                (k["d"] ? 1 : 0) - (k["a"] ? 1 : 0),
                k["space"],
                k["shift"]);

            UpdateTargets();

            if (targeted)
            {
                if (k["m3"] && !kl["m3"])
                {
                    placeType = world.BlockType(target.X, target.Y, target.Z);
                }
                if (k["m1"] && !kl["m1"])
                {
                    world.BlockUpdate(target.X, target.Y, target.Z, "air");
                }
                if (k["m2"] && !kl["m2"])
                {
                    if (player.Mode == States.Ghost || player.ValidPlace(Type.Types[placeType].Model.AABBs.Select(x => x + place)))
                    {
                        world.BlockUpdate(place.X, place.Y, place.Z, placeType);
                    }
                }
            }

            UpdateTargets();

            base.OnUpdateFrame(args);
        }

        protected override void OnRenderFrame(FrameEventArgs args)
        {
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            Matrix4 view = Matrix4.CreateTranslation(-(Vector3)(player.Position + new Vector3d(0, player.eyeLevel, 0)))
                * Matrix4.CreateRotationY((float)player.Direction.Y)
                * Matrix4.CreateRotationX((float)player.Direction.X);

            Matrix4 projection = Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians((float)player.FOV), (float)Size.X / Size.Y, 0.1f, 400f);

            // world

            GL.Enable(EnableCap.DepthTest);
            GL.Enable(EnableCap.CullFace);

            worldShader.Use();

            GL.UniformMatrix4(1, true, ref view);
            GL.UniformMatrix4(2, true, ref projection);

            world.Render();

            GL.Disable(EnableCap.CullFace);

            // highlight

            highlightShader.Use();

            Matrix4 highlightModel = Matrix4.CreateTranslation(target);
            GL.UniformMatrix4(0, true, ref highlightModel);
            GL.UniformMatrix4(1, true, ref view);
            GL.UniformMatrix4(2, true, ref projection);

            RenderHighlight();

            GL.Disable(EnableCap.DepthTest);

            // ui

            uiShader.Use();

            RenderUI();

            //

            SwapBuffers();

            base.OnRenderFrame(args);
        }

        private void SetTime(double time) // 0 midnight, 12 noon
        {
            worldShader.Use();
            double angle = time * Math.PI / 12;
            var sun = new Vector3(-(float)Math.Sin(angle), (float)Math.Cos(angle), 0.1f);
            GL.Uniform3(3, ref sun);
        }

        private void RenderHighlight()
        {
            if (targeted)
            {
                GL.BindVertexArray(highlightVAO);
                GL.DrawElements(PrimitiveType.Lines, 24, DrawElementsType.UnsignedInt, 0);
            }
        }

        private void RenderUI()
        {
            GL.BindVertexArray(UIVAO);
            GL.DrawElements(PrimitiveType.Triangles, UIEBOsize * 6, DrawElementsType.UnsignedInt, 0);
        }

        private void UpdateTargets()
        {
            double weight = 1;
            targeted = false;

            Vector3d start = player.Position + new Vector3d(0, player.eyeLevel, 0);
            Vector3d finish = player.Position + new Vector3d(0, player.eyeLevel, 0) + 
                ( Matrix3d.CreateRotationY(player.Direction.Y)
                * Matrix3d.CreateRotationX(player.Direction.X)
                * new Vector3d(0, 0, -REACH));

            foreach (var interception in Interception.Get(new double[] { start.X, start.Y, start.Z }, new double[] { finish.X, finish.Y, finish.Z }))
            {
                Vector3i candidate = new Vector3i(interception.Position[0], interception.Position[2], interception.Position[3]);
                if (interception.Weight < weight && BlockAt(candidate))
                {
                    weight = interception.Weight;
                    target = candidate;
                    place = new Vector3i(interception.Position[1], interception.Position[2], interception.Position[3]);
                    targeted = true;
                    break;
                }
            }

            foreach (var interception in Interception.Get(new double[] { start.Y, start.X, start.Z }, new double[] { finish.Y, finish.X, finish.Z }))
            {
                Vector3i candidate = new Vector3i(interception.Position[2], interception.Position[0], interception.Position[3]);
                if (interception.Weight < weight && BlockAt(candidate))
                {
                    weight = interception.Weight;
                    target = candidate;
                    place = new Vector3i(interception.Position[2], interception.Position[1], interception.Position[3]);
                    targeted = true;
                    break;
                }
            }

            foreach (var interception in Interception.Get(new double[] { start.Z, start.X, start.Y }, new double[] { finish.Z, finish.X, finish.Y }))
            {
                Vector3i candidate = new Vector3i(interception.Position[2], interception.Position[3], interception.Position[0]);
                if (interception.Weight < weight && BlockAt(candidate))
                {
                    weight = interception.Weight;
                    target = candidate;
                    place = new Vector3i(interception.Position[2], interception.Position[3], interception.Position[1]);
                    targeted = true;
                    break;
                }
            }
        }

        private bool BlockAt(Vector3i position)
        {
            string type = world.BlockType(position.X, position.Y, position.Z);
            return type != null && type != "air";
        }

        private void InitSettings()
        {
            CenterWindow();
            VSync = VSyncMode.Off;
            GL.ClearColor(0.3f, 0.8f, 1.0f, 1.0f);

            GL.Enable(EnableCap.FramebufferSrgb);
            //GL.Enable(EnableCap.Multisample);

            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

            GL.LineWidth(2);

            CursorVisible = false;
            CursorGrabbed = true;

            Texture.InitAnisotropy();
        }

        private void InitShaders()
        {
            worldShader = new Shader("Assets/Shaders/world.vert", "Assets/Shaders/world.frag");
            worldShader.Use();

            GL.Uniform1(4, AMBIENCE);

            highlightShader = new Shader("Assets/Shaders/highlight.vert", "Assets/Shaders/highlight.frag");

            uiShader = new Shader("Assets/Shaders/ui.vert", "Assets/Shaders/ui.frag");
        }

        private void InitHighlight()
        {
            highlightShader.Use();

            highlightVAO = GL.GenVertexArray();
            GL.BindVertexArray(highlightVAO);

            int VBO = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, VBO);

            int EBO = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, EBO);

            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);

            float[] vertices = new float[]
            {
                -0.002f, -0.002f, -0.002f,
                -0.002f, -0.002f,  1.002f,
                -0.002f,  1.002f, -0.002f,
                -0.002f,  1.002f,  1.002f,
                 1.002f, -0.002f, -0.002f,
                 1.002f, -0.002f,  1.002f,
                 1.002f,  1.002f, -0.002f,
                 1.002f,  1.002f,  1.002f,
            };

            uint[] indices = new uint[]
            {
                0, 1,
                0, 2,
                0, 4,
                1, 3,
                1, 5,
                2, 3,
                2, 6,
                3, 7,
                4, 5,
                4, 6,
                5, 7,
                6, 7,
            };

            GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StaticDraw);
            GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * sizeof(uint), indices, BufferUsageHint.StaticDraw);
        }

        private void InitUI()
        {
            uiShader.Use();

            UIVAO = GL.GenVertexArray();
            GL.BindVertexArray(UIVAO);

            int VBO = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, VBO);

            int EBO = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, EBO);

            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 6 * sizeof(float), 0);

            GL.EnableVertexAttribArray(1);
            GL.VertexAttribPointer(1, 4, VertexAttribPointerType.Float, false, 6 * sizeof(float), 2 * sizeof(float));

            UIEBOsize = 0;

            List<float> data = new List<float>();

            data.AddRange(Rectangle(Size.X / 2 - 1, Size.Y / 2 - 10, 2, 20, 0.0f, 0.0f, 0.0f, 1.0f));
            data.AddRange(Rectangle(Size.X / 2 - 10, Size.Y / 2 - 1, 20, 2, 0.0f, 0.0f, 0.0f, 1.0f));

            float[] vertices = data.ToArray();

            uint[] indices = new uint[UIEBOsize * 6];
            for (uint i = 0; i < UIEBOsize; i++)
            {
                uint j = i * 4;
                indices[i * 6] = j;
                indices[i * 6 + 1] = j + 1;
                indices[i * 6 + 2] = j + 2;
                indices[i * 6 + 3] = j;
                indices[i * 6 + 4] = j + 2;
                indices[i * 6 + 5] = j + 3;
            }

            GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StaticDraw);
            GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * sizeof(uint), indices, BufferUsageHint.StaticDraw);
        }

        private float[] Rectangle(int x, int y, int width, int height, float r, float g, float b, float a)
        {
            UIEBOsize++;
            float[] vertices = new float[]
            {
                ConvertX(x), ConvertY(y), r, g, b, a,
                ConvertX(x + width), ConvertY(y), r, g, b, a,
                ConvertX(x + width), ConvertY(y + height), r, g, b, a,
                ConvertX(x), ConvertY(y + height), r, g, b, a,
            };
            return vertices;
        }

        private float ConvertX(int pixels)
        {
            return 2 * (float)pixels / Size.X - 1;
        }
        private float ConvertY(int pixels)
        {
            return 1 - 2 * (float)pixels / Size.Y;
        }

        protected override void OnUnload()
        {
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.BindVertexArray(0);
            GL.UseProgram(0);
            worldShader.Delete();

            world.Dispose();

            base.OnUnload();
        }

        private void UpdateControls()
        {
            var input = KeyboardState;
            var mouse = MouseState;
            k = new Dictionary<string, bool>
            {
                { "w", input.IsKeyDown(Keys.W) },
                { "a", input.IsKeyDown(Keys.A) },
                { "s", input.IsKeyDown(Keys.S) },
                { "d", input.IsKeyDown(Keys.D) },
                { "space", input.IsKeyDown(Keys.Space) },
                { "shift", input.IsKeyDown(Keys.LeftShift) },
                { "esc", input.IsKeyDown(Keys.Escape) },

                { "r", input.IsKeyDown(Keys.R) },
                { "f", input.IsKeyDown(Keys.F) },

                { "m1", mouse.IsButtonDown(MouseButton.Button1) },
                { "m2", mouse.IsButtonDown(MouseButton.Button2) },
                { "m3", mouse.IsButtonDown(MouseButton.Button3) },
            };
        }
    }

    class Interception
    {
        public double Weight { get; }
        public int[] Position { get; }

        private Interception(double[] start, double[] finish, int i, bool positive)
        {
            Weight = (i - start[0]) / (finish[0] - start[0]);
            Position = new int[]
            {
                i - (positive ? 0 : 1),
                i - (positive ? 1 : 0),
                (int)Math.Floor((1 - Weight) * start[1] + Weight * finish[1]),
                (int)Math.Floor((1 - Weight) * start[2] + Weight * finish[2]),
            };
        }

        public static IEnumerable<Interception> Get(double[] start, double[] finish)
        {
            if (finish[0] >= start[0])
            {
                for (int i = (int)Math.Ceiling(start[0]); i < finish[0]; i++)
                {
                    yield return new Interception(start, finish, i, true);
                }
            }
            else
            {
                for (int i = (int)Math.Floor(start[0]); i > finish[0]; i--)
                {
                    yield return new Interception(start, finish, i, false);
                }
            }
        }
    }
}
