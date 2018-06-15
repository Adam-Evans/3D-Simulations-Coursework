using OpenTK;
using System;
using OpenTK.Graphics;
using Labs.Utility;
using OpenTK.Graphics.OpenGL;

namespace Labs.Lab2
{
    public class Lab2_2Window : GameWindow
    {
        public Lab2_2Window()
            : base(
                800, // Width
                600, // Height
                GraphicsMode.Default,
                "Lab 2_2 Understanding the Camera",
                GameWindowFlags.Default,
                DisplayDevice.Default,
                3, // major
                3, // minor
                GraphicsContextFlags.ForwardCompatible
                )
        {
        }

        private int[] mVBO_IDs = new int[2];
        private int mVAO_ID;
        private Matrix4 mView;
        private ShaderUtility mShader;
        private ModelUtility mModel;
        private float cameraSpeed = 0.08f;
        private float rotateSpeed = 0.05f;

        protected override void OnLoad(EventArgs e)
        {
            // Set some GL state
            GL.ClearColor(Color4.DodgerBlue);
            GL.Enable(EnableCap.DepthTest);
            GL.Enable(EnableCap.CullFace); 

            mModel = ModelUtility.LoadModel(@"Utility/Models/lab22model.sjg");
            mShader = new ShaderUtility(@"Lab2/Shaders/vLab22.vert", @"Lab2/Shaders/fSimple.frag");
            GL.UseProgram(mShader.ShaderProgramID);
            int vPositionLocation = GL.GetAttribLocation(mShader.ShaderProgramID, "vPosition");
            int vColourLocation = GL.GetAttribLocation(mShader.ShaderProgramID, "vColour");

            mVAO_ID = GL.GenVertexArray();
            GL.GenBuffers(mVBO_IDs.Length, mVBO_IDs);

            GL.BindVertexArray(mVAO_ID);
            GL.BindBuffer(BufferTarget.ArrayBuffer, mVBO_IDs[0]);
            GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(mModel.Vertices.Length * sizeof(float)), mModel.Vertices, BufferUsageHint.StaticDraw);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, mVBO_IDs[1]);
            GL.BufferData(BufferTarget.ElementArrayBuffer, (IntPtr)(mModel.Indices.Length * sizeof(float)), mModel.Indices, BufferUsageHint.StaticDraw);

            int size;
            GL.GetBufferParameter(BufferTarget.ArrayBuffer, BufferParameterName.BufferSize, out size);
            if (mModel.Vertices.Length * sizeof(float) != size)
            {
                throw new ApplicationException("Vertex data not loaded onto graphics card correctly");
            }

            GL.GetBufferParameter(BufferTarget.ElementArrayBuffer, BufferParameterName.BufferSize, out size);
            if (mModel.Indices.Length * sizeof(float) != size)
            {
                throw new ApplicationException("Index data not loaded onto graphics card correctly");
            }

            GL.EnableVertexAttribArray(vPositionLocation);
            GL.VertexAttribPointer(vPositionLocation, 3, VertexAttribPointerType.Float, false, 6 * sizeof(float), 0);
            GL.EnableVertexAttribArray(vColourLocation);
            GL.VertexAttribPointer(vColourLocation, 3, VertexAttribPointerType.Float, false, 6 * sizeof(float), 3 * sizeof(float));

            GL.BindVertexArray(0);

            int uProjectionLocation = GL.GetUniformLocation(mShader.ShaderProgramID, "uProjection");
            Matrix4 projection = Matrix4.CreatePerspectiveFieldOfView(1, (float)ClientRectangle.Width / ClientRectangle.Height, 0.5f, 10);
            GL.UniformMatrix4(uProjectionLocation, true, ref projection);

            mView = Matrix4.CreateTranslation(0, 0, -2);
            int uView = GL.GetUniformLocation(mShader.ShaderProgramID, "uView");
            GL.UniformMatrix4(uView, true, ref mView);

           

            base.OnLoad(e);

        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (mShader != null)
            {
                int uProjectionLocation = GL.GetUniformLocation(mShader.ShaderProgramID, "uProjection");
                int windowHeight = this.ClientRectangle.Height;
                int windowWidth = this.ClientRectangle.Width;
                if (windowHeight > windowWidth)
                {
                    
                    if (windowWidth < 1)
                    {
                        windowWidth = 1;
                    }
                    

                    float ratio = windowHeight / windowWidth;
                    Matrix4 projection = Matrix4.CreatePerspectiveFieldOfView(1, (float)ClientRectangle.Width / ClientRectangle.Height, 0.5f, 10);
                    GL.UniformMatrix4(uProjectionLocation, true, ref projection);
                }
                else
                {
                    if (windowHeight < 1)
                    {
                        windowHeight = 1;
                    }

                    float ratio = windowWidth / windowHeight;
                    Matrix4 projection = Matrix4.CreatePerspectiveFieldOfView(1, (float)ClientRectangle.Width / ClientRectangle.Height, 0.5f, 10);
                    GL.UniformMatrix4(uProjectionLocation, true, ref projection);
                }
            }
            GL.Viewport(this.ClientRectangle);
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            base.OnRenderFrame(e);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            int uModelLocation;
            
            GL.BindVertexArray(mVAO_ID);

            for (int x = 0; x < 10; ++x)
            {
                for (int y = 0; y < 10; ++y)
                {
                    for (int z = 0; z < 10; ++z)
                    {
                        uModelLocation = GL.GetUniformLocation(mShader.ShaderProgramID, "uModel");
                        Matrix4 m1 = Matrix4.CreateTranslation(x * 0.55f - 2.5f, y * 0.55f - 2.5f, 5 - (z * 0.55f) - 7);
                        GL.UniformMatrix4(uModelLocation, true, ref m1);
                        GL.DrawElements(BeginMode.Triangles, mModel.Indices.Length, DrawElementsType.UnsignedInt, 0);
                    }
                }
            }

            GL.BindVertexArray(0);
            this.SwapBuffers();
        }

        protected override void OnKeyPress(KeyPressEventArgs e) {
            base.OnKeyPress(e);
            if (e.KeyChar == 'a')
            {
                mView = mView * Matrix4.CreateTranslation(cameraSpeed, 0, 0);
                MoveCamera();
            }
            if (e.KeyChar == 'd')
            {
                mView = mView * Matrix4.CreateTranslation(-cameraSpeed, 0, 0);
                MoveCamera();
            }
            if (e.KeyChar == 'w')
            {
                mView = mView * Matrix4.CreateTranslation(0, 0, cameraSpeed);
                MoveCamera();
            }
            if (e.KeyChar == 's')
            {
                mView = mView * Matrix4.CreateTranslation(0, 0, -cameraSpeed);
                MoveCamera();
            }
            if (e.KeyChar == 'z')
            {
                mView = mView * Matrix4.CreateRotationZ(rotateSpeed);
                MoveCamera();
            }
            if (e.KeyChar == 'c')
            {
                mView = mView * Matrix4.CreateRotationZ(-rotateSpeed);
                MoveCamera();
            }
            if (e.KeyChar == 'q')
            {
                mView = mView * Matrix4.CreateRotationY(rotateSpeed);
                MoveCamera();
            }
            if (e.KeyChar == 'e')
            {
                mView = mView * Matrix4.CreateRotationY(-rotateSpeed);
                MoveCamera();
            }
        }

        private void MoveCamera()
        {
            int uView = GL.GetUniformLocation(mShader.ShaderProgramID, "uView");
            GL.UniformMatrix4(uView, true, ref mView);
        }

        protected override void OnUnload(EventArgs e)
        {
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
            GL.BindVertexArray(0);
            GL.DeleteBuffers(mVBO_IDs.Length, mVBO_IDs);
            GL.DeleteVertexArray(mVAO_ID);
            mShader.Delete();
            base.OnUnload(e);
        }
    }
}
