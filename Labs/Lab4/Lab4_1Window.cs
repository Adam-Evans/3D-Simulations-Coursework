using System;
using Labs.Utility;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace Labs.Lab4
{
    public class Lab4_1Window : GameWindow
    {
        private int[] mVertexArrayObjectIDArray = new int[2];
        private int[] mVertexBufferObjectIDArray = new int[2];
        private ShaderUtility mShader;
        private Matrix4 mSquareMatrix;
        private Matrix4 mSquareMatrix2;
        private Vector3 mCirclePosition;
        private Vector3 mStaticCirclePosition;
        private Vector3 mCirclePreviousPos;
        private Vector3 mCircleVelocity;
        private float mCircleRadius = 0.1f;
        private float mStaticCircleRadius = 0.2f;
        private Timer mTimer;

        public Lab4_1Window()
            : base(
                800, // Width
                600, // Height
                GraphicsMode.Default,
                "Lab 4_1 Simple Animation and Collision Detection",
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

            mSquareMatrix = Matrix4.CreateScale(3f, 5f, 1f);
            mSquareMatrix2 = Matrix4.CreateScale(5f, 5, 1) * Matrix4.CreateRotationZ((float)Math.PI /2) *
Matrix4.CreateTranslation(0.0f, 0.0f, 0);
            mCirclePosition = new Vector3(-2, -2, 0);
            mStaticCirclePosition = new Vector3(-3.25f, -1, 0);
            mCirclePreviousPos = mCirclePosition;
            mCircleVelocity = new Vector3(1f, 1f, 0);
            


            mTimer = new Timer();
            mTimer.Start();

            base.OnLoad(e);
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

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            GL.Viewport(this.ClientRectangle);
            SetCamera();
        }

        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            base.OnUpdateFrame(e);
            float timestep = mTimer.GetElapsedSeconds();
            mCirclePosition = mCirclePosition + mCircleVelocity * timestep;
            Vector3 circleInSquareSpace = Vector3.Transform(mCirclePosition, mSquareMatrix.Inverted());
            if (circleInSquareSpace.X + (mCircleRadius / mSquareMatrix.ExtractScale().X) > 1)
            {
                Vector3 normal = Vector3.Transform(new Vector3(-1, 0, 0), mSquareMatrix.ExtractRotation());
                mCircleVelocity = mCircleVelocity - 2 * Vector3.Dot(normal, mCircleVelocity) * normal;
                mCirclePosition = mCirclePreviousPos;
            }
            if (-1 > circleInSquareSpace.X + (-mCircleRadius / mSquareMatrix.ExtractScale().X))
            {
                Vector3 normal = Vector3.Transform(new Vector3(1, 0, 0), mSquareMatrix.ExtractRotation());
                mCircleVelocity = mCircleVelocity - 2 * Vector3.Dot(normal, mCircleVelocity) * normal;
                mCirclePosition = mCirclePreviousPos;
            }
            if (circleInSquareSpace.Y + (mCircleRadius / mSquareMatrix.ExtractScale().Y) > 1)
            {
                Vector3 normal = Vector3.Transform(new Vector3(0, -1, 0), mSquareMatrix.ExtractRotation());
                mCircleVelocity = mCircleVelocity - 2 * Vector3.Dot(mCircleVelocity, normal) * normal;
                mCirclePosition = mCirclePreviousPos;
            }
            if (-1 > circleInSquareSpace.Y + (-mCircleRadius / mSquareMatrix.ExtractScale().Y))
            {
                Vector3 normal = Vector3.Transform(new Vector3(0, 1, 0), mSquareMatrix.ExtractRotation());
                mCircleVelocity = mCircleVelocity - 2 * Vector3.Dot(mCircleVelocity, normal) * normal;
                mCirclePosition = mCirclePreviousPos;
            }
            if ((mCirclePosition - mStaticCirclePosition).Length < mCircleRadius + mStaticCircleRadius)
            {
                Vector3 normal = (mCirclePosition - mStaticCirclePosition).Normalized();
                mCircleVelocity = mCircleVelocity - 2 * Vector3.Dot(normal, mCircleVelocity) * normal;

                mCirclePosition = mCirclePreviousPos;
            }
            Vector3 squareEdge = Vector3.Transform(mCirclePosition, mSquareMatrix.Inverted());
            Vector3 l1, l2, l3, l4;
            l1 = new Vector3(-1, 1, 0);
            l2 = new Vector3(-1, -1, 0);
            l3 = new Vector3(1, -1, 0);
            l4 = new Vector3(1, 1, 0);
            Vector3 A = (mCirclePosition - l2);
            float AScalar = Vector3.Dot(A, (l1 - l2).Normalized());
            if (AScalar > 0)
            {
                A = Vector3.Multiply((l1 - l2).Normalized(), AScalar);
                if (A.Length < (l1 - l2).Length + mCircleRadius / 2)
                {
                    if ((l2 + A - mCirclePosition).Length < mCircleRadius)
                    {
                        mCircleVelocity = Vector3.Zero;
                        mCirclePosition = mCirclePreviousPos;
                    }
                }
            }
            A = (mCirclePosition - l3);
            AScalar = Vector3.Dot(A, (l2 - l3).Normalized());
            if (AScalar > 0)
            {
                A = Vector3.Multiply((l2 - l3).Normalized(), AScalar);
                if (A.Length < (l2 - l3).Length + mCircleRadius / 2)
                {
                    if ((l3 + A - mCirclePosition).Length < mCircleRadius)
                    {
                        mCircleVelocity = Vector3.Zero;
                        mCirclePosition = mCirclePreviousPos;
                    }
                }
            }
            A = (mCirclePosition - l4);
            AScalar = Vector3.Dot(A, (l3 - l4).Normalized());
            if (AScalar > 0)
            {
                A = Vector3.Multiply((l4 - l3).Normalized(), AScalar);
                if (A.Length < (l4 - l3).Length + mCircleRadius / 2)
                {
                    if ((l4 - A - mCirclePosition).Length < mCircleRadius)
                    {
                        mCircleVelocity = Vector3.Zero;
                        mCirclePosition = mCirclePreviousPos;
                    }
                }
            }
            A = (mCirclePosition - l1);
            AScalar = Vector3.Dot(A, (l4 - l1).Normalized());
            if (AScalar > 0)
            {
                A = Vector3.Multiply((l1 - l4).Normalized(), AScalar);
                if (A.Length < (l4 - l1).Length + mCircleRadius / 2)
                {
                    if ((l1 - A - mCirclePosition).Length < mCircleRadius)
                    {
                        mCircleVelocity = Vector3.Zero;
                        mCirclePosition = mCirclePreviousPos;
                    }
                }
            }
            mCirclePreviousPos = mCirclePosition;
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

            GL.UniformMatrix4(uModelMatrixLocation, true, ref mSquareMatrix2);
            GL.BindVertexArray(mVertexArrayObjectIDArray[0]);
            GL.DrawArrays(PrimitiveType.LineLoop, 0, 4);

            Matrix4 circleMatrix = Matrix4.CreateScale(mCircleRadius) * Matrix4.CreateTranslation(mCirclePosition);
            GL.UniformMatrix4(uModelMatrixLocation, true, ref circleMatrix);
            GL.BindVertexArray(mVertexArrayObjectIDArray[1]);
            GL.DrawArrays(PrimitiveType.LineLoop, 0, 100);

            circleMatrix = Matrix4.CreateScale(mStaticCircleRadius) * Matrix4.CreateTranslation(mStaticCirclePosition);
            GL.UniformMatrix4(uModelMatrixLocation, true, ref circleMatrix);
            GL.BindVertexArray(mVertexArrayObjectIDArray[1]);
            GL.DrawArrays(PrimitiveType.LineLoop, 0, 100);

            circleMatrix = Matrix4.CreateScale(new Vector3(3, 1.5f, 1) * mStaticCircleRadius) * Matrix4.CreateTranslation(mStaticCirclePosition); 
            GL.UniformMatrix4(uModelMatrixLocation, true, ref circleMatrix);
            GL.BindVertexArray(mVertexArrayObjectIDArray[1]);
            GL.DrawArrays(PrimitiveType.LineLoop, 0, 100);


            GL.Uniform4(uColourLocation, Color4.Red);

            Matrix4 m = mSquareMatrix * mSquareMatrix.Inverted();
            GL.UniformMatrix4(uModelMatrixLocation, true, ref m);
            GL.BindVertexArray(mVertexArrayObjectIDArray[0]);
            GL.DrawArrays(PrimitiveType.LineLoop, 0, 4);

            m = mSquareMatrix2 * mSquareMatrix.Inverted();
            GL.UniformMatrix4(uModelMatrixLocation, true, ref m);
            GL.BindVertexArray(mVertexArrayObjectIDArray[0]);
            GL.DrawArrays(PrimitiveType.LineLoop, 0, 4);

            m = (Matrix4.CreateScale(mCircleRadius) * Matrix4.CreateTranslation(mCirclePosition)) *
            mSquareMatrix.Inverted();
            GL.UniformMatrix4(uModelMatrixLocation, true, ref m);
            GL.BindVertexArray(mVertexArrayObjectIDArray[1]);
            GL.DrawArrays(PrimitiveType.LineLoop, 0, 100);


            m = (Matrix4.CreateScale(mStaticCircleRadius) * Matrix4.CreateTranslation(mStaticCirclePosition)) *
            mSquareMatrix.Inverted();
            GL.UniformMatrix4(uModelMatrixLocation, true, ref m);
            GL.BindVertexArray(mVertexArrayObjectIDArray[1]);
            GL.DrawArrays(PrimitiveType.LineLoop, 0, 100);

    
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