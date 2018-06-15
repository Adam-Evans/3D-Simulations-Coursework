using Labs.Utility;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using System;

namespace Labs.Lab3
{
    public class Lab3Window : GameWindow
    {
        public Lab3Window()
            : base(
                800, // Width
                600, // Height
                GraphicsMode.Default,
                "Lab 3 Lighting and Material Properties",
                GameWindowFlags.Default,
                DisplayDevice.Default,
                3, // major
                3, // minor
                GraphicsContextFlags.ForwardCompatible
                )
        {
        }

        private int[] mVBO_IDs = new int[7];
        private int[] mVAO_IDs = new int[4];
        private ShaderUtility mShader;
        private ModelUtility mSphereModelUtility, mOtherModelUtility, mCylinderModelUtility;
        private Matrix4 mView, mSphereModel, mGroundModel, mOtherModel, mCylinderModel;
        private Vector4 mEyePosition;

        protected override void OnLoad(EventArgs e)
        {
            // Set some GL state
            GL.ClearColor(Color4.LightGoldenrodYellow);
            GL.Enable(EnableCap.DepthTest);
            GL.Enable(EnableCap.CullFace);

            mShader = new ShaderUtility(@"Lab3/Shaders/vPassThrough.vert", @"Lab3/Shaders/fLighting.frag");
            GL.UseProgram(mShader.ShaderProgramID);
            int vPositionLocation = GL.GetAttribLocation(mShader.ShaderProgramID, "vPosition");
            int vNormalLocation = GL.GetAttribLocation(mShader.ShaderProgramID, "vNormal");

            GL.GenVertexArrays(mVAO_IDs.Length, mVAO_IDs);
            GL.GenBuffers(mVBO_IDs.Length, mVBO_IDs);

            float[] vertices = new float[] {-10, 0, -10,0,1,0,
                                             -10, 0, 10,0,1,0,
                                             10, 0, 10,0,1,0,
                                             10, 0, -10,0,1,0,};

            #region bind ground

            GL.BindVertexArray(mVAO_IDs[0]);
            GL.BindBuffer(BufferTarget.ArrayBuffer, mVBO_IDs[0]);
            GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(vertices.Length * sizeof(float)), vertices, BufferUsageHint.StaticDraw);

            int size;
            GL.GetBufferParameter(BufferTarget.ArrayBuffer, BufferParameterName.BufferSize, out size);
            if (vertices.Length * sizeof(float) != size)
            {
                throw new ApplicationException("Vertex data not loaded onto graphics card correctly");
            }

            GL.EnableVertexAttribArray(vPositionLocation);
            GL.VertexAttribPointer(vPositionLocation, 3, VertexAttribPointerType.Float, false, 6 * sizeof(float), 0);
            GL.EnableVertexAttribArray(vNormalLocation);
            GL.VertexAttribPointer(vNormalLocation, 3, VertexAttribPointerType.Float, true, 6 * sizeof(float), 3 * sizeof(float));

            #endregion

            #region bind Sphere

            mSphereModelUtility = ModelUtility.LoadModel(@"Utility/Models/sphere.bin"); 

            GL.BindVertexArray(mVAO_IDs[1]);
            GL.BindBuffer(BufferTarget.ArrayBuffer, mVBO_IDs[1]);
            GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(mSphereModelUtility.Vertices.Length * sizeof(float)), mSphereModelUtility.Vertices, BufferUsageHint.StaticDraw);           
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, mVBO_IDs[2]);
            GL.BufferData(BufferTarget.ElementArrayBuffer, (IntPtr)(mSphereModelUtility.Indices.Length * sizeof(float)), mSphereModelUtility.Indices, BufferUsageHint.StaticDraw);

            GL.GetBufferParameter(BufferTarget.ArrayBuffer, BufferParameterName.BufferSize, out size);
            if (mSphereModelUtility.Vertices.Length * sizeof(float) != size)
            {
                throw new ApplicationException("Vertex data not loaded onto graphics card correctly");
            }

            GL.GetBufferParameter(BufferTarget.ElementArrayBuffer, BufferParameterName.BufferSize, out size);
            if (mSphereModelUtility.Indices.Length * sizeof(float) != size)
            {
                throw new ApplicationException("Index data not loaded onto graphics card correctly");
            }

            GL.EnableVertexAttribArray(vPositionLocation);
            GL.VertexAttribPointer(vPositionLocation, 3, VertexAttribPointerType.Float, false, 6 * sizeof(float), 0);
            GL.EnableVertexAttribArray(vNormalLocation);
            GL.VertexAttribPointer(vNormalLocation, 3, VertexAttribPointerType.Float, true, 6 * sizeof(float), 3 * sizeof(float));

            #endregion

            #region bind other model (dragon)

            mOtherModelUtility = ModelUtility.LoadModel(@"Utility/Models/model.bin");

            GL.BindVertexArray(mVAO_IDs[2]);
            GL.BindBuffer(BufferTarget.ArrayBuffer, mVBO_IDs[3]);
            GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(mOtherModelUtility.Vertices.Length * sizeof(float)), mOtherModelUtility.Vertices, BufferUsageHint.StaticDraw);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, mVBO_IDs[4]);
            GL.BufferData(BufferTarget.ElementArrayBuffer, (IntPtr)(mOtherModelUtility.Indices.Length * sizeof(float)), mOtherModelUtility.Indices, BufferUsageHint.StaticDraw);

            GL.GetBufferParameter(BufferTarget.ArrayBuffer, BufferParameterName.BufferSize, out size);
            if (mOtherModelUtility.Vertices.Length * sizeof(float) != size)
            {
                throw new ApplicationException("Vertex data not loaded onto graphics card correctly");
            }

            GL.GetBufferParameter(BufferTarget.ElementArrayBuffer, BufferParameterName.BufferSize, out size);
            if (mOtherModelUtility.Indices.Length * sizeof(float) != size)
            {
                throw new ApplicationException("Index data not loaded onto graphics card correctly");
            }

            GL.EnableVertexAttribArray(vPositionLocation);
            GL.VertexAttribPointer(vPositionLocation, 3, VertexAttribPointerType.Float, false, 6 * sizeof(float), 0);
            GL.EnableVertexAttribArray(vNormalLocation);
            GL.VertexAttribPointer(vNormalLocation, 3, VertexAttribPointerType.Float, true, 6 * sizeof(float), 3 * sizeof(float));

            #endregion

            #region bind cylinder

            mCylinderModelUtility = ModelUtility.LoadModel(@"Utility/Models/cylinder.bin");

            GL.BindVertexArray(mVAO_IDs[3]);
            GL.BindBuffer(BufferTarget.ArrayBuffer, mVBO_IDs[5]);
            GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(mCylinderModelUtility.Vertices.Length * sizeof(float)), mCylinderModelUtility.Vertices, BufferUsageHint.StaticDraw);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, mVBO_IDs[6]);
            GL.BufferData(BufferTarget.ElementArrayBuffer, (IntPtr)(mCylinderModelUtility.Indices.Length * sizeof(float)), mCylinderModelUtility.Indices, BufferUsageHint.StaticDraw);

            GL.GetBufferParameter(BufferTarget.ArrayBuffer, BufferParameterName.BufferSize, out size);
            if (mCylinderModelUtility.Vertices.Length * sizeof(float) != size)
            {
                throw new ApplicationException("Vertex data not loaded onto graphics card correctly");
            }

            GL.GetBufferParameter(BufferTarget.ElementArrayBuffer, BufferParameterName.BufferSize, out size);
            if (mCylinderModelUtility.Indices.Length * sizeof(float) != size)
            {
                throw new ApplicationException("Index data not loaded onto graphics card correctly");
            }
            

            GL.EnableVertexAttribArray(vPositionLocation);
            GL.VertexAttribPointer(vPositionLocation, 3, VertexAttribPointerType.Float, false, 6 * sizeof(float), 0);
            GL.EnableVertexAttribArray(vNormalLocation);
            GL.VertexAttribPointer(vNormalLocation, 3, VertexAttribPointerType.Float, true, 6 * sizeof(float), 3 * sizeof(float));

            #endregion
            GL.BindVertexArray(0);

            mView = Matrix4.CreateTranslation(0, -1.5f, 0);
            int uView = GL.GetUniformLocation(mShader.ShaderProgramID, "uView");
            GL.UniformMatrix4(uView, true, ref mView);
            mEyePosition = new Vector4(0, 1.5f, 0, 1);
            int uEyePosition = GL.GetUniformLocation(mShader.ShaderProgramID, "uEyePosition");
            GL.Uniform4(uEyePosition, mEyePosition);



            mGroundModel = Matrix4.CreateTranslation(0, 0, -5f);
            mSphereModel = Matrix4.CreateTranslation(0, 1, -5f);
            mOtherModel = Matrix4.CreateTranslation(0, 0.75f, -5f);
            mCylinderModel = Matrix4.CreateTranslation(0, 0.5f, -5f);

            #region Lighting

            int uLightDirectionLocation = GL.GetUniformLocation(mShader.ShaderProgramID, "uLightPosition");
            Vector4 uLightPosition = Vector4.Transform(new Vector4(2, 2, -9.5f, 1), mView);
            GL.Uniform4(uLightDirectionLocation, uLightPosition);


            #endregion

            base.OnLoad(e);
            
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            GL.Viewport(this.ClientRectangle);
            if (mShader != null)
            {
                int uProjectionLocation = GL.GetUniformLocation(mShader.ShaderProgramID, "uProjection");
                Matrix4 projection = Matrix4.CreatePerspectiveFieldOfView(1, (float)ClientRectangle.Width / ClientRectangle.Height, 0.5f, 25);
                GL.UniformMatrix4(uProjectionLocation, true, ref projection);
            }
        }

        protected override void OnKeyPress(KeyPressEventArgs e)
        {
            base.OnKeyPress(e);
            if (e.KeyChar == 'w')
            {
                mView = mView * Matrix4.CreateTranslation(0.0f, 0.0f, 0.05f);
                mEyePosition.Z += 0.05f;
                moveCamera();
            }
            if (e.KeyChar == 'a')
            {
                mView = mView * Matrix4.CreateRotationY(-0.025f);
                moveCamera();       
            }
            if (e.KeyChar == 's')
            {
                mView = mView * Matrix4.CreateTranslation(0.0f, 0.0f, -0.05f);
                mEyePosition.Z += -0.05f;
                moveCamera();
            }
            if (e.KeyChar == 'd')
            {
                mView = mView * Matrix4.CreateRotationY(+0.025f);
                moveCamera();
            }
            if (e.KeyChar == 'i')
            {
                mView = mView * Matrix4.CreateTranslation(0.0f, -0.05f, 0.0f);
                mEyePosition.Y += 0.05f;
                moveCamera();
            }
            if (e.KeyChar == 'k')
            {
                mView = mView * Matrix4.CreateTranslation(0.0f, 0.05f, 0.0f);
                mEyePosition.Y += -0.05f;
                moveCamera();
            }
            if (e.KeyChar == 'j')
            {
                mView = mView * Matrix4.CreateTranslation(0.05f, 0.0f, 0.0f);
                mEyePosition.X += -0.05f;
                moveCamera();
            }
            if (e.KeyChar == 'l')
            {
                mView = mView * Matrix4.CreateTranslation(-0.05f, 0.0f, 0.0f);
                mEyePosition.X += 0.05f;
                moveCamera();
            }
            if (e.KeyChar == 'z')
            {
                Vector3 t = mGroundModel.ExtractTranslation();
                Matrix4 translation = Matrix4.CreateTranslation(t);
                Matrix4 inverseTranslation = Matrix4.CreateTranslation(-t);
                mGroundModel = mGroundModel * inverseTranslation * Matrix4.CreateRotationY(-0.025f) *
                translation;
            }
            if (e.KeyChar == 'x')
            {
                Vector3 t = mGroundModel.ExtractTranslation();
                Matrix4 translation = Matrix4.CreateTranslation(t);
                Matrix4 inverseTranslation = Matrix4.CreateTranslation(-t);
                mGroundModel = mGroundModel * inverseTranslation * Matrix4.CreateRotationY(0.025f) *
                translation;
            }
            if (e.KeyChar == 'c')
            {
                Vector3 t = mSphereModel.ExtractTranslation();
                Matrix4 translation = Matrix4.CreateTranslation(t);
                Matrix4 inverseTranslation = Matrix4.CreateTranslation(-t);
                mSphereModel = mSphereModel * inverseTranslation * Matrix4.CreateRotationY(-0.025f) *
                translation;
            }
            if (e.KeyChar == 'v')
            {
                Vector3 t = mSphereModel.ExtractTranslation();
                Matrix4 translation = Matrix4.CreateTranslation(t);
                Matrix4 inverseTranslation = Matrix4.CreateTranslation(-t);
                mSphereModel = mSphereModel * inverseTranslation * Matrix4.CreateRotationY(0.025f) *
                translation;
            }
        }

        private void moveCamera()
        {
            int uView = GL.GetUniformLocation(mShader.ShaderProgramID, "uView");
            GL.UniformMatrix4(uView, true, ref mView);
            int uEyePosition = GL.GetUniformLocation(mShader.ShaderProgramID, "uEyePosition");
            GL.Uniform4(uEyePosition, ref mEyePosition);
            Vector4 lightPosition = Vector4.Transform(new Vector4(2, 2, -9.5f, 1), mView);
            GL.Uniform4(GL.GetUniformLocation(mShader.ShaderProgramID, "uLightPosition"), ref lightPosition);
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            base.OnRenderFrame(e);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);


            int uModel = GL.GetUniformLocation(mShader.ShaderProgramID, "uModel");
            GL.UniformMatrix4(uModel, true, ref mGroundModel);  

            GL.BindVertexArray(mVAO_IDs[0]);
            GL.DrawArrays(PrimitiveType.TriangleFan, 0, 4);

            Matrix4 m = mCylinderModel * mGroundModel;
            uModel = GL.GetUniformLocation(mShader.ShaderProgramID, "uModel");
            GL.UniformMatrix4(uModel, true, ref m);

            GL.BindVertexArray(mVAO_IDs[3]); // cylinder
            GL.DrawElements(PrimitiveType.Triangles, mCylinderModelUtility.Indices.Length, DrawElementsType.UnsignedInt, 0);

            m = mOtherModel * mGroundModel;
            uModel = GL.GetUniformLocation(mShader.ShaderProgramID, "uModel");
            GL.UniformMatrix4(uModel, true, ref m);

            GL.BindVertexArray(mVAO_IDs[2]); // other model (dragon)
            GL.DrawElements(PrimitiveType.Triangles, mOtherModelUtility.Indices.Length, DrawElementsType.UnsignedInt, 0);

            GL.BindVertexArray(0);
            this.SwapBuffers();
        }

        protected override void OnUnload(EventArgs e)
        {
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
            GL.BindVertexArray(0);
            GL.DeleteBuffers(mVBO_IDs.Length, mVBO_IDs);
            GL.DeleteVertexArrays(mVAO_IDs.Length, mVAO_IDs);
            mShader.Delete();
            base.OnUnload(e);
        }
    }
}
