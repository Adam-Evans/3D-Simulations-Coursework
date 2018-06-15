using System;
using System.Collections.Generic;
using Labs.Utility;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace Labs.Lab4
{
    public struct circle
    {
        public Vector3 position, velocity;
        public float radius, density, mass;
        public Vector4 color;
    }

    public class Lab4_2Window : GameWindow
    {
        private int[] mVertexArrayObjectIDArray = new int[2];
        private int[] mVertexBufferObjectIDArray = new int[2];
        private ShaderUtility mShader;
        private Matrix4 mSquareMatrix;
        private const float restitutionCoefficient = 1f;
        private circle[] Circles;
        private Timer mTimer;
        private Random rnd;
        Vector3 accelerationDueToGravity = new Vector3(0, -9.81f, 0);

        public Lab4_2Window()
            : base(
                800, // Width
                600, // Height
                GraphicsMode.Default,
                "Lab 4_2 Physically Based Simulation",
                GameWindowFlags.Default,
                DisplayDevice.Default,
                3, // major
                3, // minor
                GraphicsContextFlags.ForwardCompatible
                )
        {
        }

        protected override void OnLoad(EventArgs e)
        {
            GL.ClearColor(Color4.AliceBlue);
            rnd = new Random();
            mShader = new ShaderUtility(@"Lab4/Shaders/vLab4.vert", @"Lab4/Shaders/fLab4.frag");
            int vPositionLocation = GL.GetAttribLocation(mShader.ShaderProgramID, "vPosition");
            GL.UseProgram(mShader.ShaderProgramID);

            float[] vertices = new float[] {
                   -1f, -1f,
                   1f, -1f,
                   1f, 1f,
                   -1f, 1f
            };

            GL.GenVertexArrays(mVertexArrayObjectIDArray.Length, mVertexArrayObjectIDArray);
            GL.GenBuffers(mVertexBufferObjectIDArray.Length, mVertexBufferObjectIDArray);

            GL.BindVertexArray(mVertexArrayObjectIDArray[0]);
            GL.BindBuffer(BufferTarget.ArrayBuffer, mVertexBufferObjectIDArray[0]);
            GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(vertices.Length * sizeof(float)), vertices, BufferUsageHint.StaticDraw);

            int size;
            GL.GetBufferParameter(BufferTarget.ArrayBuffer, BufferParameterName.BufferSize, out size);

            if (vertices.Length * sizeof(float) != size)
            {
                throw new ApplicationException("Vertex data not loaded onto graphics card correctly");
            }

            GL.EnableVertexAttribArray(vPositionLocation);
            GL.VertexAttribPointer(vPositionLocation, 2, VertexAttribPointerType.Float, false, 2 * sizeof(float), 0);

            vertices = new float[200];

            for (int i = 0; i < 100; ++i)
            {
                vertices[2 * i] = (float)Math.Cos(MathHelper.DegreesToRadians(i * 360.0 / 100));
                vertices[2 * i + 1] = (float)Math.Cos(MathHelper.DegreesToRadians(90.0 + i * 360.0 / 100));
            }

            GL.BindVertexArray(mVertexArrayObjectIDArray[1]);
            GL.BindBuffer(BufferTarget.ArrayBuffer, mVertexBufferObjectIDArray[1]);

            GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(vertices.Length * sizeof(float)), vertices, BufferUsageHint.StaticDraw);

            GL.GetBufferParameter(BufferTarget.ArrayBuffer, BufferParameterName.BufferSize, out size);

            if (vertices.Length * sizeof(float) != size)
            {
                throw new ApplicationException("Vertex data not loaded onto graphics card correctly");
            }

            GL.EnableVertexAttribArray(vPositionLocation);
            GL.VertexAttribPointer(vPositionLocation, 2, VertexAttribPointerType.Float, false, 2 * sizeof(float), 0);
            int uViewLocation = GL.GetUniformLocation(mShader.ShaderProgramID, "uView");
            Matrix4 m = Matrix4.CreateTranslation(0, 0, 0);
            GL.UniformMatrix4(uViewLocation, true, ref m);
            Circles = new circle[2];
            Circles[0].radius = 0.245f;
            Circles[1].radius = 0.2f;
            Circles[0].position = new Vector3(-2, 2.3f, 0);
            Circles[1].position = new Vector3(2, 2, 0);
            Circles[0].velocity = new Vector3(2, 0, 0);
            Circles[1].velocity = new Vector3(0, 0, 0);
            Circles[0].density = 3;
            Circles[1].density = 3f;
            Circles[0].mass = ((4 / 3) * (float)Math.PI * (float)Math.Pow(Circles[0].radius, 3) * Circles[0].density);
            Circles[1].mass = ((4 / 3) * (float)Math.PI * (float)Math.Pow(Circles[1].radius, 3) * Circles[1].density);
            Circles[0].color = new Vector4(0, 0, 1, 1);
            Circles[1].color = new Vector4(0, 0.9f, 0.1f, 1);
            mSquareMatrix = Matrix4.CreateScale(4f) * Matrix4.CreateRotationZ(0.0f) * Matrix4.CreateTranslation(0, 0, 0);

            base.OnLoad(e);

            mTimer = new Timer();
            mTimer.Start();


        }
        private void SetCamera()
        {
            float height = ClientRectangle.Height;
            float width = ClientRectangle.Width;
            if (mShader != null)
            {
                Matrix4 proj;
                if (height > width)
                {
                    if (width == 0)
                    {
                        width = 1;
                    }
                    proj = Matrix4.CreateOrthographic(10, 10 * height / width, 0, 10);
                }
                else
                {
                    if (height == 0)
                    {
                        height = 1;
                    }
                    proj = Matrix4.CreateOrthographic(10 * width / height, 10, 0, 10);
                }
                int uProjectionLocation = GL.GetUniformLocation(mShader.ShaderProgramID, "uProjection");
                GL.UniformMatrix4(uProjectionLocation, true, ref proj);
            }
        }

        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            float timestep = mTimer.GetElapsedSeconds();

            Circles[0].velocity = Circles[0].velocity + accelerationDueToGravity * timestep;
            Circles[0].position = Circles[0].position + Circles[0].velocity * timestep;

            Circles[1].velocity = Circles[1].velocity + accelerationDueToGravity * timestep;
            Circles[1].position = Circles[1].position + Circles[1].velocity * timestep;

            //DEBUG MOMENTUM
            float magnitude0 = (float)Math.Sqrt((Circles[0].velocity.X * Circles[0].velocity.X) +
                (Circles[0].velocity.Y * Circles[0].velocity.Y) +
                (Circles[0].velocity.Z * Circles[0].velocity.Z));
            float magnitude1 = (float)Math.Sqrt((Circles[1].velocity.X * Circles[1].velocity.X) +
                (Circles[1].velocity.Y * Circles[1].velocity.Y) +
                (Circles[1].velocity.Z * Circles[1].velocity.Z));
            float p = (Circles[0].mass * magnitude0) + (Circles[1].mass * magnitude1);
            Console.WriteLine("Momentum : " + p);

            //END DEBUG


            checkCircleCollisions(timestep);


            base.OnUpdateFrame(e);
        }

        private void checkCircleCollisions(float timestep)
        {
            List<int> collisions = new List<int>();
            for (int i = 0; i < 2; i++)
            {
                Vector3 oldPosition = Circles[i].position;
                Circles[i].position += Circles[i].velocity * timestep;
                Vector3 circleInSquareSpace = Vector3.Transform(Circles[i].position, mSquareMatrix.Inverted());
                if (circleInSquareSpace.X + Circles[i].radius / mSquareMatrix.ExtractScale().X > 1)
                {
                    Vector3 normal = Vector3.Transform(new Vector3(-1, 0, 0), mSquareMatrix.ExtractRotation());
                    Circles[i].velocity = Circles[i].velocity - 2 * Vector3.Dot(normal, Circles[i].velocity) * normal;
                    Circles[i].position = oldPosition;
                }
                if (-1 > circleInSquareSpace.X - Circles[i].radius / mSquareMatrix.ExtractScale().X)
                {
                    Vector3 normal = Vector3.Transform(new Vector3(1, 0, 0), mSquareMatrix.ExtractRotation());
                    Circles[i].velocity = Circles[i].velocity - 2 * Vector3.Dot(normal, Circles[i].velocity) * normal;
                    Circles[i].position = oldPosition;
                }
                if (circleInSquareSpace.Y + Circles[i].radius / mSquareMatrix.ExtractScale().Y > 1)
                {
                    Vector3 normal = Vector3.Transform(new Vector3(0, -1, 0), mSquareMatrix.ExtractRotation());
                    Circles[i].velocity = Circles[i].velocity - 2 * Vector3.Dot(Circles[i].velocity, normal) * normal;
                    Circles[i].position = oldPosition;
                }
                if (-1 > circleInSquareSpace.Y - Circles[i].radius / mSquareMatrix.ExtractScale().Y)
                {
                    Vector3 normal = Vector3.Transform(new Vector3(0, 1, 0), mSquareMatrix.ExtractRotation());
                    Circles[i].velocity = Circles[i].velocity - 2 * Vector3.Dot(Circles[i].velocity, normal) * normal;
                    Circles[i].position = oldPosition;
                }
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            GL.Viewport(this.ClientRectangle);
            SetCamera();
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            base.OnRenderFrame(e);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            int uModelMatrixLocation = GL.GetUniformLocation(mShader.ShaderProgramID, "uModel");
            int uColourLocation = GL.GetUniformLocation(mShader.ShaderProgramID, "uColour");

            GL.Uniform4(uColourLocation, Color4.DodgerBlue);

            GL.UniformMatrix4(uModelMatrixLocation, true, ref mSquareMatrix);
            GL.BindVertexArray(mVertexArrayObjectIDArray[0]);
            GL.DrawArrays(PrimitiveType.LineLoop, 0, 4);

            for (int i = 0; i < Circles.Length; i++)
            {
                GL.Uniform4(uColourLocation, Circles[i].color);
                Matrix4 circleMatrix = Matrix4.CreateScale(Circles[i].radius) * Matrix4.CreateTranslation(Circles[i].position);

                GL.UniformMatrix4(uModelMatrixLocation, true, ref circleMatrix);
                GL.BindVertexArray(mVertexArrayObjectIDArray[1]);
                GL.DrawArrays(PrimitiveType.LineLoop, 0, 100);
            }


            this.SwapBuffers();
        }

        protected override void OnUnload(EventArgs e)
        {
            base.OnUnload(e);
            GL.DeleteBuffers(mVertexBufferObjectIDArray.Length, mVertexBufferObjectIDArray);
            GL.DeleteVertexArrays(mVertexArrayObjectIDArray.Length, mVertexArrayObjectIDArray);
            GL.UseProgram(0);
            mShader.Delete();
        }
    }
}