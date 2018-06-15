using Labs.Utility;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using System;
using System.Drawing;
using System.Drawing.Imaging;

namespace Labs.Lab5
{
    public class Lab5Window : GameWindow
    {
        public Lab5Window()
            : base(
                800, // Width
                600, // Height
                GraphicsMode.Default,
                "Lab 5 Textures",
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
        private ShaderUtility mShader;
        private Matrix4 mView;
        private Vector4 mEyePosition;
        private int mTexture_ID, mNoise_ID;

        protected override void OnLoad(EventArgs e)
        {
            // Set some GL state
            GL.ClearColor(Color4.Firebrick);

            float[] vertices = {-0.5f, -0.5f, 0, 0,
                                -0.25f, -0.5f, 0.25f, 0,
                                0.0f, -0.5f, 0.5f, 0f,
                                0.25f, -0.5f, 0.75f, 0f,
                                0.5f, -0.5f, 1f, 0,
                                -0.5f, 0.0f, 0f, 0.5f,
                                -0.25f, 0.0f, 0.25f, 0.5f,
                                0.0f, 0.0f,  0.5f, 0.5f,
                                0.25f, 0.0f, 0.75f, 0.5f,
                                0.5f, 0.0f, 1f, 0.5f,
                               -0.5f, 0.5f, 0f, 1f,
                                -0.25f, 0.5f, 0.25f, 1f,
                                0.0f, 0.5f, 0.5f, 1f,
                                0.25f, 0.5f, 0.75f, 1f,
                                0.5f, 0.5f, 1f, 1f
                                };

            uint[] indices = { 5, 0, 1,
                               5, 1, 6,
                               6, 1, 2,
                               6, 2, 7,
                               7, 2, 3,
                               7, 3, 8,
                               8, 3, 4,
                               8, 4, 9,
                               10, 5, 6,
                               10, 6, 11,
                               11, 6, 7,
                               11, 7, 12,
                               12, 7, 8,
                               12, 8, 13,
                               13, 8, 9,
                               13, 9, 14
                             };

            GL.Enable(EnableCap.CullFace);
            GL.Enable(EnableCap.Texture2D);

            #region Texture loading
            mTexture_ID = loadTexture(@"Lab5/texture.jpg", TextureUnit.Texture0);
            mNoise_ID = loadTexture(@"Lab5/cloudNoise.jpg", TextureUnit.Texture1);

            #endregion

            mShader = new ShaderUtility(@"Lab5/Shaders/vTexture.vert", @"Lab5/Shaders/fTexture.frag");
            GL.UseProgram(mShader.ShaderProgramID);
            int vPositionLocation = GL.GetAttribLocation(mShader.ShaderProgramID, "vPosition");
            int vTexLocation = GL.GetAttribLocation(mShader.ShaderProgramID, "vTexCoords"); 

            int uTextureSamplerLocation = GL.GetUniformLocation(mShader.ShaderProgramID, "uTextureSampler");
            GL.Uniform1(uTextureSamplerLocation, 0);
            int uTextureSamplerLocation2 = GL.GetUniformLocation(mShader.ShaderProgramID, "uTextureSampler2");
            GL.Uniform1(uTextureSamplerLocation, 1);

            mVAO_ID = GL.GenVertexArray();
            GL.GenBuffers(mVBO_IDs.Length, mVBO_IDs);

            GL.BindVertexArray(mVAO_ID);
            GL.BindBuffer(BufferTarget.ArrayBuffer, mVBO_IDs[0]);
            GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(vertices.Length * sizeof(float)), vertices, BufferUsageHint.StaticDraw);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, mVBO_IDs[1]);
            GL.BufferData(BufferTarget.ElementArrayBuffer, (IntPtr)(indices.Length * sizeof(uint)), indices, BufferUsageHint.StaticDraw);

            int size;
            GL.GetBufferParameter(BufferTarget.ArrayBuffer, BufferParameterName.BufferSize, out size);
            if (vertices.Length * sizeof(float) != size)
            {
                throw new ApplicationException("Vertex data not loaded onto graphics card correctly");
            }

            GL.GetBufferParameter(BufferTarget.ElementArrayBuffer, BufferParameterName.BufferSize, out size);
            if (indices.Length * sizeof(uint) != size)
            {
                throw new ApplicationException("Index data not loaded onto graphics card correctly");
            }

            GL.EnableVertexAttribArray(vPositionLocation);
            GL.VertexAttribPointer(vPositionLocation, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 0);
            GL.EnableVertexAttribArray(vTexLocation);
            GL.VertexAttribPointer(vTexLocation, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 2 * sizeof(float));


            mView = Matrix4.CreateTranslation(0, -1.5f, 0);
            int uView = GL.GetUniformLocation(mShader.ShaderProgramID, "uView");
            GL.UniformMatrix4(uView, true, ref mView);
            mEyePosition = new Vector4(0, 1.5f, 0, 1);
            int uEyePosition = GL.GetUniformLocation(mShader.ShaderProgramID, "uEyePosition");
            GL.Uniform4(uEyePosition, mEyePosition);

            GL.BindVertexArray(0);

            base.OnLoad(e);

        }

        private int loadTexture(string filepath, TextureUnit unit)
        {
            int texID = 0;
            if (System.IO.File.Exists(filepath))
            {
                Bitmap TextureBitmap = new Bitmap(filepath);
                TextureBitmap.RotateFlip(RotateFlipType.RotateNoneFlipY);
                BitmapData TextureData = TextureBitmap.LockBits(
                new Rectangle(0, 0, TextureBitmap.Width,
                TextureBitmap.Height), ImageLockMode.ReadOnly,
                System.Drawing.Imaging.PixelFormat.Format32bppRgb);
                GL.ActiveTexture(unit);
                GL.GenTextures(1, out texID);
                GL.BindTexture(TextureTarget.Texture2D, texID);
                GL.TexImage2D(TextureTarget.Texture2D,
                0, PixelInternalFormat.Rgba, TextureData.Width, TextureData.Height,
                0, OpenTK.Graphics.OpenGL.PixelFormat.Bgra,
                PixelType.UnsignedByte, TextureData.Scan0);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter,
                (int)TextureMinFilter.Linear);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter,
                (int)TextureMagFilter.Linear);
                TextureBitmap.UnlockBits(TextureData);
            }
            else
            {
                throw new Exception("Could not find file " + filepath);
            }
            return texID;
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

            GL.BindVertexArray(mVAO_ID);
            GL.DrawElements(PrimitiveType.Triangles, 48, DrawElementsType.UnsignedInt, 0);

            GL.BindVertexArray(0);
            this.SwapBuffers();
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
