using System;
using Labs.Utility;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;

namespace Labs.ACW
{
    public struct ball
    {
        public ball(Vector3 vel, Vector3 pos, Vector3 col, float rad, float den, bool hasPhysics = false)
        {
            velocity = vel;
            position = pos;
            prevPos = Vector3.Zero;
            color = col;
            radius = rad;
            density = den;
            mass = (float)((4 / 3) * Math.PI * Math.Pow(rad, 3)) * den;
            isStatic = !hasPhysics;
        }
        public Vector3 velocity;
        public Vector3 position;
        public Vector3 prevPos;
        public Vector3 color;
        public float radius;
        public float density;
        public float mass;
        public bool isStatic;
    }
    public struct cylinder
    {
        public cylinder(Vector3 pos, float len, float rad, float xRot = 0, float yRot = 0)
        {
            position = pos;
            length = len;
            radius = rad;
            xRotation = xRot;
            yRotation = yRot;
        }

        public Vector3 position;
        public float length;
        public float radius;
        public float xRotation;
        public float yRotation;
    }
    public struct box
    {
        public box(float Height, bool hasTop, bool hasBottom)
        {
            height = Height;
            top = hasTop;
            bottom = hasBottom;
            mOffset = new Vector3(0, height, 0);
        }

        public bool top;
        public bool bottom;
        public Vector3 mOffset;
        public float height;

    }
    public struct Light
    {
        public Vector3 position;
        public Vector3 color;
    }
    public struct camera
    {
        public camera(bool isTracking, Vector3 pos)
        {
            track = isTracking;
            position = pos;
        }
        public bool track;
        public Vector3 position;
    }

    public class ACWWindow : GameWindow
    {
        public ACWWindow()
            : base(
                600, // Width
                800, // Height
                GraphicsMode.Default,
                "Assessed Coursework",
                GameWindowFlags.Default,
                DisplayDevice.Default,
                3, // major
                3, // minor
                GraphicsContextFlags.ForwardCompatible
                )
        {
        }

        #region variables
        private int[] mVBO_IDs = new int[6];
        private int[] mVAO_IDs = new int[3];
        private ShaderUtility mShader, mTestShader, lab5Shader;
        private ModelUtility mSphereModelUtility, mCylinderModelUtility;
        private float[] planeModel;
        private ball crimsonBall, orangeBall;
        private Matrix4 mSphereModel, groundPosition, mCylinderModel, mView;
        private Matrix4[] mPlane = new Matrix4[6];
        private Vector4 mEyePosition;
        private Vector3[] boxVerts;
        private int mTexture_ID;
        private const float cameraSpeed = 2.5f;
        private const float rotatespeed = 0.1f;
        private float lastBallTime;
        private const float ballAddTime = 5.5f;
        private const float restitutionCoefficient = 0.8f;
        private Vector3 gravity = new Vector3(0, -9.81f, 0);
        private List<ball> bigListOBalls;
        private ball[] ballArray;
        private List<cylinder> ListOfCylinders;
        private List<box> boxList;
        private Timer mTimer;
        private int[] mVertexArrayObjectIDArray = new int[1];
        private int[] mVertexBufferObjectIDArray = new int[1];
        private bool debug = true;
        private bool showMomentum = false;
        private Random rnd;
        private camera[] cameras;
        private int activeCam = 1;
        #endregion

        protected override void OnLoad(EventArgs e)
        {
            // Set some GL state
            GL.ClearColor(Color4.Black);
            GL.Enable(EnableCap.DepthTest);
            //GL.Enable(EnableCap.CullFace);
            GL.Enable(EnableCap.Texture2D);

            mShader = new ShaderUtility(@"ACW/Shaders/vPassThrough.vert", @"ACW/Shaders/fLighting.frag");
            mTestShader = new ShaderUtility(@"ACW/Shaders/testVertexShader.vert", @"ACW/Shaders/testFragShader.frag");
            lab5Shader = new ShaderUtility(@"Lab5/Shaders/vTexture.vert", @"Lab5/Shaders/fTexture.frag"); 
            mTexture_ID = loadTexture(@"ACW/texture.jpg", TextureUnit.Texture0);

            GL.UseProgram(mShader.ShaderProgramID);
            int vPositionLocation = GL.GetAttribLocation(mTestShader.ShaderProgramID, "vert");
            int vTexCoord = GL.GetAttribLocation(mTestShader.ShaderProgramID, "vertTexCoord");
            int vNormalLocation = GL.GetAttribLocation(mTestShader.ShaderProgramID, "vertNormal");

            GL.GenVertexArrays(mVAO_IDs.Length, mVAO_IDs);
            GL.GenBuffers(mVBO_IDs.Length, mVBO_IDs);


            lastBallTime = -ballAddTime;

            //      X, Y, Z,     U,V,    Normal    
            planeModel = new float[] {
                    -1, -1, 0,  0,0,    0, 1, 0,
                    -1, 1, 0,   1,0,    0, 1, 0,
                    1, 1, 0,    1,1,    0, 1, 0,
                    1, -1, 0,   0, 1,   0, 1, 0,
                };

            #region bind ground

            GL.BindVertexArray(mVAO_IDs[0]);
            GL.BindBuffer(BufferTarget.ArrayBuffer, mVBO_IDs[0]);
            GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(planeModel.Length * sizeof(float)), planeModel, BufferUsageHint.StaticDraw);

            int size;
            GL.GetBufferParameter(BufferTarget.ArrayBuffer, BufferParameterName.BufferSize, out size);
            if (planeModel.Length * sizeof(float) != size)
            {
                throw new ApplicationException("Vertex data not loaded onto graphics card correctly");
            }

            GL.EnableVertexAttribArray(vPositionLocation);
            GL.VertexAttribPointer(vPositionLocation, 3, VertexAttribPointerType.Float, false, 8 * sizeof(float), 0);
            GL.EnableVertexAttribArray(vTexCoord);
            GL.VertexAttribPointer(vTexCoord, 2, VertexAttribPointerType.Float, false, 8 * sizeof(float), 3 * sizeof(float));
            GL.EnableVertexAttribArray(vNormalLocation);
            GL.VertexAttribPointer(vNormalLocation, 3, VertexAttribPointerType.Float, true, 8 * sizeof(float), 5 * sizeof(float));

            #endregion

            vPositionLocation = GL.GetAttribLocation(mShader.ShaderProgramID, "vPosition");
            vNormalLocation = GL.GetAttribLocation(mShader.ShaderProgramID, "vNormal");

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

            #region bind cylinder

            mCylinderModelUtility = ModelUtility.LoadModel(@"Utility/Models/cylinder.bin");

            GL.BindVertexArray(mVAO_IDs[2]);
            GL.BindBuffer(BufferTarget.ArrayBuffer, mVBO_IDs[3]);
            GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(mCylinderModelUtility.Vertices.Length * sizeof(float)), mCylinderModelUtility.Vertices, BufferUsageHint.StaticDraw);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, mVBO_IDs[4]);
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

            #region bindSquare for debugging physics
            float[] verts = new float[] {
                   -1f, -1f,
                   1f, -1f,
                   1f, 1f,
                   -1f, 1f
            };

            GL.GenVertexArrays(mVertexArrayObjectIDArray.Length, mVertexArrayObjectIDArray);
            GL.GenBuffers(mVertexBufferObjectIDArray.Length, mVertexBufferObjectIDArray);

            GL.BindVertexArray(mVertexArrayObjectIDArray[0]);
            GL.BindBuffer(BufferTarget.ArrayBuffer, mVertexBufferObjectIDArray[0]);
            GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(verts.Length * sizeof(float)), verts, BufferUsageHint.StaticDraw);

            int _size = 0;
            GL.GetBufferParameter(BufferTarget.ArrayBuffer, BufferParameterName.BufferSize, out _size);

            if (verts.Length * sizeof(float) != _size)
            {
                throw new ApplicationException("Vertex data not loaded onto graphics card correctly");
            }

            GL.EnableVertexAttribArray(vPositionLocation);
            GL.VertexAttribPointer(vPositionLocation, 2, VertexAttribPointerType.Float, false, 2 * sizeof(float), 0);
            #endregion

            GL.BindVertexArray(0);

            mView = Matrix4.CreateTranslation(0, -20, -42);
            int uView = GL.GetUniformLocation(mShader.ShaderProgramID, "uView");
            GL.UniformMatrix4(uView, true, ref mView);
            mEyePosition = new Vector4(0, 20, 42, 1);
            int uEyePosition = GL.GetUniformLocation(mShader.ShaderProgramID, "uEyePosition");
            GL.Uniform4(uEyePosition, mEyePosition);
            int cam = GL.GetUniformLocation(mTestShader.ShaderProgramID, "camera");
            GL.UniformMatrix4(cam, true, ref mView);





            groundPosition = Matrix4.CreateTranslation(0, 0, 0) * Matrix4.CreateRotationY(-0.785398f);
            mSphereModel = Matrix4.CreateTranslation(0, 0.5f, 0);
            mCylinderModel = Matrix4.CreateTranslation(0, 0.5f, 0);
            #region setting up planes for physics visual
            mPlane[0] = Matrix4.CreateScale(5, 5, 5) * Matrix4.CreateRotationY(1.5708f) * groundPosition * Matrix4.CreateTranslation(new Vector3(-3.5355f, 15, -3.5355f));
            mPlane[1] = Matrix4.CreateScale(5, 5, 5) * Matrix4.CreateRotationY(0) * groundPosition * Matrix4.CreateTranslation(new Vector3(3.5355f, 15, -3.5355f));
            mPlane[2] = Matrix4.CreateScale(5, 5, 5) * Matrix4.CreateRotationY(1.5708f) * groundPosition * Matrix4.CreateTranslation(new Vector3(3.5355f, 15, 3.5355f));
            mPlane[3] = Matrix4.CreateScale(5, 5, 5) * Matrix4.CreateRotationY(0) * groundPosition * Matrix4.CreateTranslation(new Vector3(-3.5355f, 15, 3.5355f));
            mPlane[4] = Matrix4.CreateScale(5, 5, 5) * Matrix4.CreateRotationX(1.5708f) * groundPosition * Matrix4.CreateTranslation(new Vector3(0, 20, 0));
            mPlane[5] = Matrix4.CreateScale(5, 5, 5) * Matrix4.CreateRotationX(1.5708f) * groundPosition * Matrix4.CreateTranslation(new Vector3(0, 10, 0));
            #endregion


            #region Lighting

            int uLightDirectionLocation = GL.GetUniformLocation(mShader.ShaderProgramID, "uLightPosition");
            Vector4 uLightPosition = Vector4.Transform(new Vector4(-3, 20, 7, 1), mView);
            GL.Uniform4(uLightDirectionLocation, uLightPosition);


            #endregion

            #region configure boxes
            box spawnBox = new ACW.box(40, true, false);
            //box moEmptySpaceBox = new ACW.box(50, false, false);
            box level1Box = new ACW.box(30, false, false);
            //box emptySpaceBox = new ACW.box(30, false, false);
            box level2Box = new ACW.box(20, false, false);
            box sphereOfDoomBox = new ACW.box(10, false, false);
            box portalBox = new ACW.box(0, false, true);
            boxList = new List<box>() { spawnBox, level1Box, level2Box, sphereOfDoomBox, portalBox };
        
            #endregion

            #region configure Cyclinders
            ListOfCylinders = new List<cylinder>();
            cylinder temp = new cylinder(new Vector3(0, level1Box.height + 7.5f, -0.75f), 5, 1.5f, (float)Math.PI / 2, 0);
            ListOfCylinders.Add(temp);
            temp = new cylinder(new Vector3(0, level1Box.height + 2.5f, -0.75f), 5, 1.5f, (float)Math.PI / 2, 0);
            ListOfCylinders.Add(temp);
            temp = new cylinder(new Vector3(0, level1Box.height + 7.5f, -0.75f), 5, 0.75f, (float)Math.PI / 2, (float)Math.PI / 2);
            ListOfCylinders.Add(temp); //
            temp = new cylinder(new Vector3(0, level1Box.height + 2.5f, -0.75f), 5, 0.75f, (float)Math.PI / 2, (float)Math.PI / 2);
            ListOfCylinders.Add(temp);
            //rotated
            temp = new cylinder(new Vector3(0, level2Box.height + 5, -0.75f), 7.07f, 1, (float)Math.PI / 2, (float)Math.PI / 4);
            ListOfCylinders.Add(temp);
            temp = new cylinder(new Vector3(0, level2Box.height + 5, -0.75f), 7.07f, 1.5f, (float)Math.PI / 2, -(float)Math.PI / 4);
            ListOfCylinders.Add(temp);
            #endregion

            #region configure balls
            ballArray = new ball[2];
            crimsonBall = new ball(new Vector3(10, 0, 7), new Vector3(-3, spawnBox.height + 2.5f, 0), new Vector3(0.875f, 0.075f, 0.234f), 0.6f, 0.0014f, true);
            orangeBall = new ball(new Vector3(6, 0, 8), new Vector3(3, spawnBox.height + 2.5f, 0), new Vector3(1.0f, 0.55f, 0.0f), 0.8f, 0.001f, true);
            ball SphereOfDoom = new ball(new Vector3(0, 0, -4), new Vector3(0, sphereOfDoomBox.height + 5f, 0), new Vector3(0.12f, 0.65f, 1), 3.5f, 0, false);
            bigListOBalls = new List<ball>() { SphereOfDoom, crimsonBall, orangeBall };
            #endregion

            #region configure Cameras


            cameras = new camera[7] {
               new camera (true, new Vector3(0, 0, -45)), //track far 0
               new camera (true, new Vector3(0, 0, -15)), //track close 1
               new camera (false, new Vector3(0, -25, -50)), //front 2
               new camera (false, new Vector3(50, -25, 0)), //left 3
               new camera (false, new Vector3(-50, -25, 0)), //right 4
               new camera (false, new Vector3(0, -(boxList[0].height + 10), 0)), //top 5
               new camera (false, new Vector3(0, 0, 0)) //manual 6
            };

            #endregion

            if (cameras[activeCam].track)
            {
                int index;
                if (bigListOBalls.Count > 1)
                    index = 2;
                else
                    index = 0;
                mView = Matrix4.CreateTranslation(-bigListOBalls[index].position.X, -bigListOBalls[index].position.Y, -42);
                uView = GL.GetUniformLocation(mShader.ShaderProgramID, "uView");
                GL.UniformMatrix4(uView, true, ref mView);
                mEyePosition = new Vector4(bigListOBalls[index].position.X, bigListOBalls[index].position.Y, 42, 1);
                uEyePosition = GL.GetUniformLocation(mShader.ShaderProgramID, "uEyePosition");
                GL.Uniform4(uEyePosition, mEyePosition);
            }

            prepLights();
            mTimer = new Timer();
            mTimer.Start();
            rnd = new Random();
            base.OnLoad(e);

        }

        private void prepLights()
        {
            float[] light_ambient = new float[4] { 0.0f, 0.0f, 0.0f, 1.0f };
            float[] light_diffuse = new float[4] { 0.5f, 0.5f, 0.5f, 1.0f };
            float[] light_specular = new float[4] { 0.5f, 0.5f, 0.5f, 1.0f };
    

            GL.Light(LightName.Light0, LightParameter.Position, new Vector4(0, 20, 42, 1));
            GL.Light(LightName.Light0, LightParameter.Ambient, light_ambient);
            GL.Light(LightName.Light0, LightParameter.Diffuse, light_diffuse);
            GL.Light(LightName.Light0, LightParameter.Specular, light_specular);

            GL.Light(LightName.Light1, LightParameter.Position, new Vector4(20, 20, 42, 1));
            GL.Light(LightName.Light1, LightParameter.Ambient, light_ambient);
            GL.Light(LightName.Light1, LightParameter.Diffuse, light_diffuse);
            GL.Light(LightName.Light1, LightParameter.Specular, light_specular);

            GL.Enable(EnableCap.Lighting);
            GL.Enable(EnableCap.Light0);
            GL.Enable(EnableCap.Light1);
            GL.Enable(EnableCap.ColorMaterial);
            
            GL.Enable(EnableCap.Normalize);
        }

        private void updateBall(int index, ball b)
        {
            bigListOBalls.Remove(bigListOBalls[index]);
            bigListOBalls.Insert(index, b);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            GL.Viewport(this.ClientRectangle);
            if (mShader != null)
            {
                int uProjectionLocation = GL.GetUniformLocation(mShader.ShaderProgramID, "uProjection");
                Matrix4 projection = Matrix4.CreatePerspectiveFieldOfView(1, (float)ClientRectangle.Width / ClientRectangle.Height, 0.1f, 200); //edit for view distance etc
                GL.UniformMatrix4(uProjectionLocation, true, ref projection);
                uProjectionLocation = GL.GetUniformLocation(mTestShader.ShaderProgramID, "projection");
                GL.UniformMatrix4(uProjectionLocation, true, ref projection);
            }
        }

        protected override void OnKeyPress(KeyPressEventArgs e)
        {
            base.OnKeyPress(e);
            if (e.KeyChar == 'w')
            {
                mView = mView * Matrix4.CreateTranslation(0.0f, 0.0f, cameraSpeed);
                mEyePosition.Z += cameraSpeed;
                moveCamera();
            }
            if (e.KeyChar == 'a')
            {
                mView = mView * Matrix4.CreateRotationY(-0.025f);
                moveCamera();
            }
            if (e.KeyChar == 's')
            {
                mView = mView * Matrix4.CreateTranslation(0.0f, 0.0f, -cameraSpeed);
                mEyePosition.Z += -cameraSpeed;
                moveCamera();
            }
            if (e.KeyChar == 'd')
            {
                mView = mView * Matrix4.CreateRotationY(+0.025f);
                moveCamera();
            }
            if (e.KeyChar == 'i')
            {
                mView = mView * Matrix4.CreateTranslation(0.0f, -cameraSpeed, 0.0f);
                mEyePosition.Y += cameraSpeed;
                moveCamera();
            }
            if (e.KeyChar == 'k')
            {
                mView = mView * Matrix4.CreateTranslation(0.0f, cameraSpeed, 0.0f);
                mEyePosition.Y += -cameraSpeed;
                moveCamera();
            }
            if (e.KeyChar == 'j')
            {
                mView = mView * Matrix4.CreateTranslation(cameraSpeed, 0.0f, 0.0f);
                mEyePosition.X += -cameraSpeed;
                moveCamera();
            }
            if (e.KeyChar == 'l')
            {
                mView = mView * Matrix4.CreateTranslation(-cameraSpeed, 0.0f, 0.0f);
                mEyePosition.X += cameraSpeed;
                moveCamera();
            }
            if (e.KeyChar == 'r')
            {
                for (int i = 1; i < bigListOBalls.Count; i++)
                {
                    Teleporter(i, bigListOBalls[i]);
                }
            }
            if (e.KeyChar == '0')
            {
                activeCam = 0;      
            }
            if (e.KeyChar == '1')
            {
                activeCam = 1;
            }
            if (e.KeyChar == '2') //front
            {
                activeCam = 2;
                updateCamera(0, 0);
            }
            if (e.KeyChar == '3') //left
            {
                activeCam = 3;
                updateCamera((float)Math.PI / 2, 0);
            }
            if (e.KeyChar == '4') //right
            {
                activeCam = 4;
                updateCamera(-(float)Math.PI / 2, 0);
            }
            if (e.KeyChar == '5') //top
            {
                activeCam = 5;
                updateCamera((float)Math.PI / 4, (float)Math.PI / 2);
            }
        }

        private void updateCamera(float yRot, float xRot)
        {
            mView = Matrix4.CreateTranslation(cameras[activeCam].position) * Matrix4.CreateRotationY(yRot) * Matrix4.CreateRotationX(xRot);
            int uView = GL.GetUniformLocation(mShader.ShaderProgramID, "uView");
            GL.UniformMatrix4(uView, true, ref mView);
            mEyePosition = new Vector4(-cameras[activeCam].position, 1);
            int uEyePosition = GL.GetUniformLocation(mShader.ShaderProgramID, "uEyePosition");
            GL.Uniform4(uEyePosition, mEyePosition);
            Vector4 lightPosition = Vector4.Transform(new Vector4(-3, 20, 7, 1), mView);
            GL.Uniform4(GL.GetUniformLocation(mShader.ShaderProgramID, "uLightPosition"), ref lightPosition);
        }

        private void autoCamera()
        {
            int index = 0;
            if (bigListOBalls.Count > 1)
                index = 1;
            mEyePosition = new Vector4(bigListOBalls[index].position, 1);
            mEyePosition.Z -=cameras[activeCam].position.Z;
            mView = Matrix4.CreateTranslation(new Vector3(-mEyePosition.X, -mEyePosition.Y, -mEyePosition.Z));    
                  
            moveCamera();
        }

        private void moveCamera()
        {
            int uView = GL.GetUniformLocation(mShader.ShaderProgramID, "uView");
            GL.UniformMatrix4(uView, true, ref mView);
            int uEyePosition = GL.GetUniformLocation(mShader.ShaderProgramID, "uEyePosition");
            GL.Uniform4(uEyePosition, ref mEyePosition);
            Vector4 lightPosition = Vector4.Transform(new Vector4(-3, 20, 7, 1), mView);
            GL.Uniform4(GL.GetUniformLocation(mShader.ShaderProgramID, "uLightPosition"), ref lightPosition);
            Vector3 pos = new Vector3(lightPosition.X, lightPosition.Y, lightPosition.Z);
            GL.Uniform3(GL.GetUniformLocation(mTestShader.ShaderProgramID, "light.position"), ref pos);
            Vector3 intensities = new Vector3(0.7f, 0.7f, 1);
            GL.Uniform3(GL.GetUniformLocation(mTestShader.ShaderProgramID, "light.intensities"), ref intensities);
            int cam = GL.GetUniformLocation(mTestShader.ShaderProgramID, "camera");
            GL.UniformMatrix4(cam, true, ref mView);
        }

        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            float timeElapsed = mTimer.GetElapsedSeconds();
            updateBallPhysics(timeElapsed);
            if (cameras[activeCam].track)
                autoCamera();
            if (showMomentum)
            {
                float p = 0;
                for (int i = 0; i < bigListOBalls.Count; i++)
                {
                    float m = (float)Math.Sqrt((bigListOBalls[i].velocity.X * bigListOBalls[i].velocity.X) +
                (bigListOBalls[i].velocity.Y * bigListOBalls[i].velocity.Y) +
                (bigListOBalls[i].velocity.Z * bigListOBalls[i].velocity.Z));
                    p += m * bigListOBalls[i].mass;
                }

                Console.WriteLine("Total Momentum : " + p);
                Console.WriteLine();
            }
            base.OnUpdateFrame(e);
        }

        private void updateBallPhysics(float timeElapsed)
        {
            lastBallTime += timeElapsed;
            if (lastBallTime > 0)
            {
                bigListOBalls.Add(crimsonBall);
                bigListOBalls.Add(orangeBall);
                lastBallTime = -ballAddTime;
            }

            for (int i = 0; i < bigListOBalls.Count; i++)
            {
                if (!bigListOBalls[i].isStatic)
                {
                    ball temp = bigListOBalls[i];
                    temp.prevPos = bigListOBalls[i].position;
                    temp.velocity = temp.velocity + gravity * timeElapsed;
                    temp.position = temp.position + temp.velocity * timeElapsed;
                    updateBall(i, temp);
                    if (bigListOBalls[i].position.Y < -10 || Math.Abs(bigListOBalls[i].position.X) > 20 || Math.Abs(bigListOBalls[i].position.Z) > 20)
                        Teleporter(i, bigListOBalls[i]);
                }
            }
            checkCollisionBoxes(timeElapsed);
            checkCollisionCylinders(timeElapsed);
            checkCollisionSpheres(timeElapsed);
        }

        private void checkCollisionBoxes(float timestep)
        {
            for (int i = 0; i < bigListOBalls.Count; i++)
            {
                bool collided = false;
                ball temp = bigListOBalls[i];
                Vector3 ballInPlaneSpace;
                Vector4 tempvec4;
                if (!bigListOBalls[i].isStatic)
                {
                    for (int j = 0; j < boxList.Count; j++)
                    {
                        for (int k = 0; k < 6; k++) //loop through each side wall
                        {
                            if (!collided)
                            {
                                Vector3 pos = Vector3.Zero;
                                float rot = 0;
                                switch (k)
                                {
                                    case (0):
                                        if (temp.position.Y < boxList[j].height + 10 && temp.position.Y > boxList[j].height)
                                        {
                                            pos = new Vector3(0, boxList[j].height + 5, -5);
                                            rot = 0;
                                            mPlane[k] = Matrix4.CreateScale(5, 5, 5) * Matrix4.CreateRotationY(rot) * Matrix4.CreateTranslation(pos) * groundPosition;
                                            tempvec4 = Vector4.Transform(new Vector4(temp.position, 1), mPlane[k].Inverted());
                                            ballInPlaneSpace.X = tempvec4.X; ballInPlaneSpace.Y = tempvec4.Y; ballInPlaneSpace.Z = tempvec4.Z;
                                            //ballInPlaneSpace = Vector3.Transform(temp.position, mPlane[k].Inverted());

                                            if ((temp.radius - 0.5f) / 2 > (ballInPlaneSpace.Z + temp.radius / mPlane[k].ExtractScale().Z) + (ballInPlaneSpace.X + temp.radius / mPlane[k].ExtractScale().X))
                                            {
                                                if (debug)
                                                {
                                                    Console.WriteLine("collision with ball i: " + i + " at wall -Z (case 0)" + "At position: " + temp.position);
                                                    Vector3 normal = Vector3.Transform(new Vector3(-0.707f, 0, -0.707f), mPlane[k].ExtractRotation()); //45 degree at any unit circle will give a value 0.707, 0.707 - thanks to sqrt 2/2 (best number, fight me)
                                                    Console.WriteLine("Previous Vel: " + temp.velocity);
                                                    temp.velocity = temp.velocity - 2 * Vector3.Dot(normal, temp.velocity) * normal * restitutionCoefficient;
                                                    Console.WriteLine("New vel: " + temp.velocity);
                                                    Console.WriteLine();
                                                    temp.position = temp.prevPos;
                                                    collided = true;
                                                }
                                                else
                                                {
                                                    Vector3 normal = Vector3.Transform(new Vector3(-0.707f, 0, -0.707f), mPlane[k].ExtractRotation()); //45 degree at any unit circle will give a value 0.707, 0.707 - thanks to sqrt 2/2 (best number, fight me)
                                                    temp.velocity = temp.velocity - 2 * Vector3.Dot(normal, temp.velocity) * normal * restitutionCoefficient;
                                                    temp.position = temp.prevPos;
                                                    collided = true;
                                                }
                                                updateBall(i, temp);
                                            }
                                        }
                                        break;
                                    case (1):
                                        if (temp.position.Y < boxList[j].height + 10 && temp.position.Y > boxList[j].height)
                                        {
                                            pos = new Vector3(-5, boxList[j].height + 5, 0);
                                            rot = 1.5708f;
                                            mPlane[k] = Matrix4.CreateScale(5, 5, 5) * Matrix4.CreateRotationY(rot) * Matrix4.CreateTranslation(pos) * groundPosition;
                                            tempvec4 = Vector4.Transform(new Vector4(temp.position, 1), mPlane[k].Inverted());
                                            ballInPlaneSpace.X = tempvec4.X; ballInPlaneSpace.Y = tempvec4.Y; ballInPlaneSpace.Z = tempvec4.Z;

                                            if ((temp.radius - 0.5f) / 2 > (ballInPlaneSpace.Z + temp.radius / mPlane[k].ExtractScale().Z) + (ballInPlaneSpace.X + temp.radius / mPlane[k].ExtractScale().X))
                                            {
                                                if (debug)
                                                {
                                                    Console.WriteLine("collision with ball i: " + i + " at wall -X (case 1)" + "At position: " + temp.position);
                                                    Vector3 normal = Vector3.Transform(new Vector3(0.707f, 0, 0.707f), mPlane[k].ExtractRotation()); //45 degree at any unit circle will give a value 0.707, 0.707 - thanks to sqrt 2/2 (best number, fight me)
                                                    Console.WriteLine("Previous Vel: " + temp.velocity);
                                                    temp.velocity = temp.velocity - 2 * Vector3.Dot(normal, temp.velocity) * normal * restitutionCoefficient;
                                                    Console.WriteLine("New vel: " + temp.velocity);
                                                    Console.WriteLine();
                                                    temp.position = temp.prevPos;
                                                    collided = true;
                                                }
                                                else
                                                {
                                                    Vector3 normal = Vector3.Transform(new Vector3(0.707f, 0, 0.707f), mPlane[k].ExtractRotation()); //45 degree at any unit circle will give a value 0.707, 0.707 - thanks to sqrt 2/2 (best number, fight me)
                                                    temp.velocity = temp.velocity - 2 * Vector3.Dot(normal, temp.velocity) * normal * restitutionCoefficient;
                                                    temp.position = temp.prevPos;
                                                    collided = true;
                                                }
                                                updateBall(i, temp);
                                            }

                                        }
                                        break;
                                    case (2):
                                        if (temp.position.Y < boxList[j].height + 10 && temp.position.Y > boxList[j].height)
                                        {
                                            pos = new Vector3(0, boxList[j].height + 5, 5);
                                            rot = 0;
                                            mPlane[k] = Matrix4.CreateScale(5, 5, 5) * Matrix4.CreateRotationY(rot) * Matrix4.CreateTranslation(pos) * groundPosition;
                                            tempvec4 = Vector4.Transform(new Vector4(temp.position, 1), mPlane[k].Inverted());
                                            ballInPlaneSpace.X = tempvec4.X; ballInPlaneSpace.Y = tempvec4.Y; ballInPlaneSpace.Z = tempvec4.Z;

                                            if (0.5f < (ballInPlaneSpace.Z + temp.radius / mPlane[k].ExtractScale().Z) + (ballInPlaneSpace.X + temp.radius / mPlane[k].ExtractScale().X))
                                            {
                                                if (debug)
                                                {
                                                    Console.WriteLine("collision with ball i: " + i + " at wall +Z (case 2)" + "At position: " + temp.position);
                                                    Vector3 normal = Vector3.Transform(new Vector3(0.707f, 0, 0.707f), mPlane[k].ExtractRotation()); //45 degree at any unit circle will give a value 0.707, 0.707 - thanks to sqrt 2/2 (best number, fight me)
                                                    Console.WriteLine("Previous Vel: " + temp.velocity);
                                                    temp.velocity = temp.velocity - 2 * Vector3.Dot(normal, temp.velocity) * normal * restitutionCoefficient;
                                                    Console.WriteLine("New vel: " + temp.velocity);
                                                    Console.WriteLine();
                                                    temp.position = temp.prevPos;
                                                    collided = true;
                                                }
                                                else
                                                {
                                                    Vector3 normal = Vector3.Transform(new Vector3(0.707f, 0, 0.707f), mPlane[k].ExtractRotation()); //45 degree at any unit circle will give a value 0.707, 0.707 - thanks to sqrt 2/2 (best number, fight me)
                                                    temp.velocity = temp.velocity - 2 * Vector3.Dot(normal, temp.velocity) * normal * restitutionCoefficient;
                                                    temp.position = temp.prevPos;
                                                    collided = true;
                                                }
                                                updateBall(i, temp);
                                            }
                                        }
                                        break;
                                    case (3):
                                        if (temp.position.Y < boxList[j].height + 10 && temp.position.Y > boxList[j].height)
                                        {
                                            pos = new Vector3(5, boxList[j].height + 5, 0);
                                            rot = 1.5708f;
                                            mPlane[k] = Matrix4.CreateScale(5, 5, 5) * Matrix4.CreateRotationY(rot) * Matrix4.CreateTranslation(pos) * groundPosition;
                                            tempvec4 = Vector4.Transform(new Vector4(temp.position, 1), mPlane[k].Inverted());
                                            ballInPlaneSpace.X = tempvec4.X; ballInPlaneSpace.Y = tempvec4.Y; ballInPlaneSpace.Z = tempvec4.Z;

                                            if (0.5f < (ballInPlaneSpace.Z + temp.radius / mPlane[k].ExtractScale().Z) + (ballInPlaneSpace.X + temp.radius / mPlane[k].ExtractScale().X))
                                            {
                                                if (debug)
                                                {
                                                    Console.WriteLine("collision with ball i: " + i + " at wall +X (case 3)" + "At position: " + temp.position);
                                                    Vector3 normal = Vector3.Transform(new Vector3(-0.707f, 0, -0.707f), mPlane[k].ExtractRotation()); //45 degree at any unit circle will give a value 0.707, 0.707 - thanks to sqrt 2/2 (best number, fight me)
                                                    Console.WriteLine("Previous Vel: " + temp.velocity);
                                                    temp.velocity = temp.velocity - 2 * Vector3.Dot(normal, temp.velocity) * normal * restitutionCoefficient;
                                                    Console.WriteLine("New vel: " + temp.velocity);
                                                    Console.WriteLine();
                                                    temp.position = temp.prevPos;
                                                    collided = true;
                                                }
                                                else
                                                {

                                                    Vector3 normal = Vector3.Transform(new Vector3(-0.707f, 0, -0.707f), mPlane[k].ExtractRotation()); //45 degree at any unit circle will give a value 0.707, 0.707 - thanks to sqrt 2/2 (best number, fight me)                       
                                                    temp.velocity = temp.velocity - 2 * Vector3.Dot(normal, temp.velocity) * normal * restitutionCoefficient;
                                                    temp.position = temp.prevPos;
                                                    collided = true;
                                                }
                                                updateBall(i, temp);
                                            }

                                        }
                                        break;
                                    case (4):
                                        if (boxList[j].top)
                                        {
                                            pos = new Vector3(0, boxList[j].height + 10, 0);
                                            rot = 1.5708f;
                                            mPlane[k] = Matrix4.CreateScale(5, 5, 5) * Matrix4.CreateRotationX(rot) * Matrix4.CreateTranslation(pos) * groundPosition;
                                            if (temp.position.Y + temp.radius > boxList[j].height + 10)
                                            {
                                                if (debug)
                                                {
                                                    Console.WriteLine("collision with ball i: " + i + " at wall +Y (case 4)" + "At position: " + temp.position);
                                                    Vector3 normal = new Vector3(-0, -0.707f, 0);
                                                    Console.WriteLine("Previous Vel: " + temp.velocity);
                                                    temp.velocity = temp.velocity - 2 * Vector3.Dot(normal, temp.velocity) * normal * restitutionCoefficient;
                                                    Console.WriteLine("New vel: " + temp.velocity);
                                                    Console.WriteLine();
                                                    temp.position = temp.prevPos;
                                                    collided = true;
                                                }
                                                else
                                                {
                                                    Vector3 normal = new Vector3(-0, -0.707f, 0);
                                                    temp.velocity = temp.velocity - 2 * Vector3.Dot(normal, temp.velocity) * normal * restitutionCoefficient;
                                                    temp.position = temp.prevPos;
                                                    collided = true;
                                                }
                                                updateBall(i, temp);
                                            }
                                        }
                                        else
                                            continue;
                                        break;
                                    case (5):
                                        if (boxList[j].bottom)
                                        {
                                            pos = new Vector3(0, boxList[j].height, 0);
                                            rot = 1.5708f;
                                            mPlane[k] = Matrix4.CreateScale(5, 5, 5) * Matrix4.CreateRotationX(rot) * Matrix4.CreateTranslation(pos) * groundPosition;

                                            if (temp.position.Y - temp.radius < boxList[j].height)
                                            {                                            
                                                if (debug)
                                                {
                                                    Console.WriteLine("collision with ball i: " + i + " at wall -Y (case 5)" + "At position: " + temp.position);
                                                    Console.WriteLine("Previous Vel: " + temp.velocity);
                                                    //temp.velocity.Y = -temp.velocity.Y * restitutionCoefficient; no need in changing the velocity here, the only bottom is teleporter!
                                                    Console.WriteLine("New vel: " + temp.velocity);
                                                    Console.WriteLine();
                                                    temp.position = temp.prevPos;
                                                    collided = true;
                                                }
                                                else
                                                {
                                                    //temp.velocity.Y = -temp.velocity.Y * restitutionCoefficient; same as above :<
                                                    temp.position = temp.prevPos;
                                                    collided = true;
                                                }
                                                Teleporter(i, temp);
                                            }
                                        }
                                        else
                                            continue;
                                        break;
                                    default: break;
                                }

                                //note for checks we only really need one matrix but for visual purposeeeessse its nice to see them all, dont'ya think?               
                            }
                        }
                    }
                }
            }
        }
        private void checkCollisionCylinders(float timestep)
        {
            for (int i = 0; i < bigListOBalls.Count; i++)
            {
                bool collided = false;
                ball temp = bigListOBalls[i];
                //temp.position = temp.velocity * timestep;
                for (int j = 0; j < ListOfCylinders.Count; j++)
                {
                    if (!collided)
                    {
                        Vector3 Pa, Pb; //end points of cylinder, Pa, Pb
                        Pa = Pb = ListOfCylinders[j].position;
                        Pa.Z += ListOfCylinders[j].length;
                        Pb.Z -= ListOfCylinders[j].length;



                        //Matrix4 m = Matrix4.CreateRotationX(xRot) * Matrix4.CreateRotationY(yRot);
                        Vector4 tempVec4 = Vector4.Transform(new Vector4(Pa, 1), Matrix4.Mult(groundPosition, Matrix4.CreateRotationY(ListOfCylinders[j].yRotation)));
                        tempVec4 = Vector4.Transform(tempVec4, Matrix4.Mult(groundPosition, Matrix4.CreateRotationY(ListOfCylinders[j].xRotation)));
                        //Pa = Vector3.Transform(Pa, Matrix4.Mult(groundPosition, Matrix4.CreateRotationY(ListOfCylinders[j].yRotation)));
                        //so thanks to the power of magic after the first transform the y axis becomes the z axis and for some reason rotating by z axis again swaps it with the x axis
                        //but rotating by y axis twice has the right effect and places y back where it should be  ¯\_(ツ)_/¯
                        //Pa = Vector3.Transform(Pa, Matrix4.Mult(groundPosition, Matrix4.CreateRotationY(ListOfCylinders[j].xRotation)));
                        Pa.X = tempVec4.X; Pa.Y = tempVec4.Y; Pa.Z = tempVec4.Z;
                        tempVec4 = Vector4.Transform(new Vector4(Pb, 1), Matrix4.Mult(groundPosition, Matrix4.CreateRotationY(ListOfCylinders[j].yRotation)));
                        tempVec4 = Vector4.Transform(tempVec4, Matrix4.Mult(groundPosition, Matrix4.CreateRotationY(ListOfCylinders[j].xRotation)));
                        //Pb = Vector3.Transform(Pb, Matrix4.Mult(groundPosition, Matrix4.CreateRotationY(ListOfCylinders[j].yRotation)));
                        //Pb = Vector3.Transform(Pb, Matrix4.Mult(groundPosition, Matrix4.CreateRotationY(ListOfCylinders[j].xRotation)));
                        Pb.X = tempVec4.X; Pb.Y = tempVec4.Y; Pb.Z = tempVec4.Z;
                        //now that points are relative to ground, we need a triangle, PaPbSo where So is Sphere centre point (or origin)
                        float PaPb, PaSo, PbSo, Perimiter, area, h, adj; //where h = height, adj = adjacent, Pa and Pb are points on cylinder and So is Sphere origin
                        PaPb = (float)Math.Sqrt(Math.Pow(Pa.X - Pb.X, 2) + Math.Pow(Pa.Y - Pb.Y, 2) + Math.Pow(Pa.Z - Pb.Z, 2));
                        PaSo = (float)Math.Sqrt(Math.Pow(Pa.X - temp.position.X, 2) + Math.Pow(Pa.Y - temp.position.Y, 2) + Math.Pow(Pa.Z - temp.position.Z, 2));
                        PbSo = (float)Math.Sqrt(Math.Pow(Pb.X - temp.position.X, 2) + Math.Pow(Pb.Y - temp.position.Y, 2) + Math.Pow(Pb.Z - temp.position.Z, 2));
                        Perimiter = (PaPb + PaSo + PbSo) / 2;
                        area = (float)Math.Sqrt(Perimiter * (Perimiter - PaPb) * (Perimiter - PbSo) * (Perimiter - PaSo));
                        h = (2 * area) / PaPb;
                        float rad = temp.radius + ListOfCylinders[j].radius;

                        if (h < temp.radius + ListOfCylinders[j].radius)
                        {
                            adj = (float)Math.Sqrt(Math.Pow(PaSo, 2) - Math.Pow(h, 2));
                            Vector3 ColPoint = ListOfCylinders[j].position;
                            ColPoint.Z += ListOfCylinders[j].length - adj;
                            Vector4 converter;
                            //ColPoint = Vector3.Transform(ColPoint, Matrix4.Mult(groundPosition, Matrix4.CreateRotationY(ListOfCylinders[j].yRotation)));
                            //ColPoint = Vector3.Transform(ColPoint, Matrix4.Mult(groundPosition, Matrix4.CreateRotationY(ListOfCylinders[j].xRotation)));
                            converter = Vector4.Transform(new Vector4(ColPoint, 1), Matrix4.Mult(groundPosition, Matrix4.CreateRotationY(ListOfCylinders[j].yRotation)));
                            converter = Vector4.Transform(converter, Matrix4.Mult(groundPosition, Matrix4.CreateRotationY(ListOfCylinders[j].xRotation)));
                            ColPoint.X = converter.X;
                            ColPoint.Y = converter.Y;
                            ColPoint.Z = converter.Z;
                            if (debug)
                            {
                                Console.WriteLine(string.Format("Ball: {0} collided with cylinder: {1} at position {2}", i, j, ColPoint));
                                Console.WriteLine();
                            }
                            Vector3 normal = (ColPoint - temp.position).Normalized();
                            temp.velocity = restitutionCoefficient * (temp.velocity - 2 * Vector3.Dot(normal, temp.velocity) * normal);
                            temp.position = temp.prevPos;
                            //temp.velocity = Vector3.Zero;
                            collided = true;
                        }
                    }
                }
                updateBall(i, temp);
            }
        }
        private void checkCollisionSpheres(float timestep)
        {
            List<int> collisions = new List<int>();
            for (int i = 0; i < bigListOBalls.Count; i++)
            {
                if (!bigListOBalls[i].isStatic)
                {
                    ball temp = bigListOBalls[i];
                    temp.position += temp.velocity * timestep;
                    for (int j = 0; j < bigListOBalls.Count; j++)
                    {
                        if (j != i)
                        {
                            if (!collisions.Contains(j))
                            {
                                if ((bigListOBalls[j].position - temp.position).Length < bigListOBalls[i].radius + bigListOBalls[j].radius)
                                {
                                    Vector3 normal = (bigListOBalls[j].position - bigListOBalls[i].position).Normalized();
                                    Vector3 u1 = bigListOBalls[i].velocity;
                                    Vector3 u2 = bigListOBalls[j].velocity;
                                    float m1 = bigListOBalls[i].mass;
                                    float m2 = bigListOBalls[j].mass;
                                    float dotI = Vector3.Dot(bigListOBalls[i].velocity, normal);
                                    float dotJ = Vector3.Dot(bigListOBalls[j].velocity, -normal);



                                    //Keep this method for elastic none momentum based collision incase we cant get it working.
                                    //p = mv
                                    //v = p/m
                                    ball temp2 = bigListOBalls[j];
                                    Vector3 v1, v2;
                                    float newX, newY, newZ;
                                    newX = (temp.velocity.X * (temp.mass - temp2.mass) + (2 * temp2.mass * temp2.velocity.X)) / (temp.mass + temp2.mass);
                                    newY = (temp.velocity.Y * (temp.mass - temp2.mass) + (2 * temp2.mass * temp2.velocity.Y)) / (temp.mass + temp2.mass);
                                    newZ = (temp.velocity.Z * (temp.mass - temp2.mass) + (2 * temp2.mass * temp2.velocity.Z)) / (temp.mass + temp2.mass);
                                    v1 = new Vector3(newX, newY, newZ);
                                    newX = (temp2.velocity.X * (temp2.mass - temp.mass) + (2 * temp.mass * temp.velocity.X)) / (temp.mass + temp2.mass);
                                    newY = (temp2.velocity.Y * (temp2.mass - temp.mass) + (2 * temp.mass * temp.velocity.Y)) / (temp.mass + temp2.mass);
                                    newZ = (temp2.velocity.Z * (temp2.mass - temp.mass) + (2 * temp.mass * temp.velocity.Z)) / (temp.mass + temp2.mass);
                                    v2 = new Vector3(newX, newY, newZ);
                                    //this method is elastic, if all elese fails use this....
                                    //v1 = restitutionCoefficient * (u1 - dotI * normal + dotJ * -normal);
                                    //v2 = restitutionCoefficient * (dotI * normal + u2 - dotJ * -normal);



                                    if (!bigListOBalls[j].isStatic)
                                    {

                                        temp.velocity = v1;
                                        //temp.velocity = temp.velocity + v1;
                                        temp.position = temp.prevPos;
                                        temp2.velocity = v2;
                                        //temp2.velocity = temp2.velocity + v2
                                        temp2.position = temp2.prevPos;
                                        updateBall(i, temp);
                                        updateBall(j, temp2);
                                        break;
                                    }
                                    else
                                    {
                                        if (j == 0)
                                        {
                                            sphereOfDOOOOOOOOM(i, temp);
                                            break;
                                        }
                                        else
                                        {
                                            temp.velocity = -v2;
                                            updateBall(i, temp);
                                        }
                                    }
                                    collisions.Add(i);
                                    collisions.Add(j);
                                }
                            }
                        }
                    }
                }
            }
        }

        void Teleporter(int index, ball b)
        {
            bigListOBalls.Remove(bigListOBalls[index]);
            Vector3 randPos = new Vector3(rnd.Next(-3, 3), boxList[0].height + 8, rnd.Next(-3, 3));
            b.position = randPos;
            bigListOBalls.Insert(index, b);
        }

        void sphereOfDOOOOOOOOM(int index, ball b)
        {

            ball s = bigListOBalls[0]; //Sphere of doom is always index 0.. for reasons
            bigListOBalls.Remove(bigListOBalls[index]);
            float distanceCenter = (float)Math.Sqrt(Math.Pow(b.position.X - s.position.X, 2) + Math.Pow(b.position.Y - s.position.Y, 2) + Math.Pow(b.position.Z - s.position.Z, 2));
            distanceCenter -= s.radius;
            b.radius = distanceCenter;
            if (b.radius > 0)
            {
                b.mass = (float)((4 / 3) * Math.PI * Math.Pow(b.radius, 3)) * b.density;
                bigListOBalls.Insert(index, b);
            }
        }


        protected override void OnRenderFrame(FrameEventArgs e)
        {
            base.OnRenderFrame(e);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            Matrix4 m;
            int uModel = GL.GetUniformLocation(mShader.ShaderProgramID, "uModel");
            int uColor = GL.GetUniformLocation(mShader.ShaderProgramID, "uColor");

           

            /*
            m = Matrix4.CreateScale(10) * Matrix4.CreateRotationX(1.5708f) * Matrix4.CreateTranslation(new Vector3(0, -0.05f, 0));
            uModel = GL.GetUniformLocation(mTestShader.ShaderProgramID, "model");
            int uTextureSamplerLocation = GL.GetUniformLocation(mTestShader.ShaderProgramID, "tex");
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, mTexture_ID);
            GL.Uniform1(uTextureSamplerLocation, 0);
            GL.UniformMatrix4(uModel, true, ref m);
            GL.BindVertexArray(mVAO_IDs[0]); // ground 
            GL.DrawArrays(PrimitiveType.TriangleFan, 0, 4);
            //OKAY no idea what is going on here but the texture is not drawing, only green color, the Matrix m is not applying and Buh... Texture is loaded properly, enabled, index points to testShader coords and active texture is set
            //but still nothing.  ¯\_(ツ)_/¯ 
            */

            for (int i = 0; i < bigListOBalls.Count; i++)
            {
                m = Matrix4.CreateScale(bigListOBalls[i].radius) * Matrix4.CreateTranslation(bigListOBalls[i].position) * groundPosition;
                uModel = GL.GetUniformLocation(mShader.ShaderProgramID, "uModel");
                GL.UniformMatrix4(uModel, true, ref m);
                uColor = GL.GetUniformLocation(mShader.ShaderProgramID, "uColor");
                GL.Uniform3(uColor, bigListOBalls[i].color);
                GL.BindVertexArray(mVAO_IDs[1]); // Sphere 
                GL.DrawElements(PrimitiveType.Triangles, mSphereModelUtility.Indices.Length, DrawElementsType.UnsignedInt, 0);
            }

            GL.Uniform3(uColor, Vector3.One);

            for (int i = 0; i < ListOfCylinders.Count; i++)
            {
                Vector3 scale = new Vector3(ListOfCylinders[i].radius, ListOfCylinders[i].length, ListOfCylinders[i].radius);
                m = Matrix4.CreateScale(scale) * mCylinderModel * Matrix4.CreateRotationX(ListOfCylinders[i].xRotation) * Matrix4.CreateRotationY(ListOfCylinders[i].yRotation) * Matrix4.CreateTranslation(ListOfCylinders[i].position) * groundPosition;
                uModel = GL.GetUniformLocation(mShader.ShaderProgramID, "uModel");
                GL.UniformMatrix4(uModel, true, ref m);
                GL.BindVertexArray(mVAO_IDs[2]); // cylinder
                GL.DrawElements(PrimitiveType.Triangles, mCylinderModelUtility.Indices.Length, DrawElementsType.UnsignedInt, 0);
            }

            #region render squares to debug phsyics

            for (int i = 0; i < mPlane.Length; i++)
            {
                uModel = GL.GetUniformLocation(mShader.ShaderProgramID, "uModel");
                uColor = GL.GetUniformLocation(mShader.ShaderProgramID, "uColor");
                GL.Uniform3(uColor, new Vector3(1, 0, 0));
                GL.UniformMatrix4(uModel, true, ref mPlane[i]);
                GL.BindVertexArray(mVertexArrayObjectIDArray[0]);
                GL.DrawArrays(PrimitiveType.LineLoop, 0, 4);
                GL.Uniform3(uColor, new Vector3(0.3f, 1, 0.65f));

            }

            #endregion

            uModel = GL.GetUniformLocation(mShader.ShaderProgramID, "uModel");

            GL.UniformMatrix4(uModel, true, ref groundPosition);

            for (int i = 0; i < boxList.Count; i++)
            {
                GL.Color3(Color.White);
                drawBox(10, boxList[i].mOffset, boxList[i].top, boxList[i].bottom);
            }
            GL.BindVertexArray(0);
            this.SwapBuffers();

        }

        private void drawBox(float size, Vector3 offset, bool top, bool bottom)
        {
            GL.Enable(EnableCap.CullFace);
            GL.Enable(EnableCap.Texture2D);
        

            int uModel = GL.GetUniformLocation(mShader.ShaderProgramID, "uModel");
            int uColor = GL.GetUniformLocation(mShader.ShaderProgramID, "uColor");
            GL.Uniform3(uColor, new Vector3(1, 1, 1));
            boxVerts = new Vector3[] {
                //front 
                new Vector3 (-1.0f * (size / 2), 0 , 1.0f * (size / 2)),
                new Vector3 (1.0f * (size / 2), 0, 1.0f * (size / 2)),
                new Vector3 (1.0f * (size / 2), size, 1.0f * (size / 2)),
                new Vector3 (-1.0f * (size / 2), size, 1.0f * (size / 2)),
                //back
                new Vector3 (-1.0f * (size / 2), 0, -1.0f * (size / 2)),
                new Vector3 (1.0f * (size / 2), 0, -1.0f * (size / 2)),
                new Vector3 (1.0f * (size / 2), size, -1.0f * (size / 2)),
                new Vector3 (-1.0f * (size / 2), size, -1.0f * (size / 2))
            };
            //top
            GL.PushMatrix();
            if (top)
            {
                GL.BindTexture(TextureTarget.Texture2D, mTexture_ID);
                GL.Begin(PrimitiveType.Quads);

                //GL.Color4(0.8f, 0.8f, 0.8f, 1);
                GL.Color3(Color.White);
                GL.TexCoord2(0.0f, 1.0f - 0.0f); GL.Vertex3(boxVerts[3] + offset);
                GL.TexCoord2(1.0f, 1.0f - 0.0f); GL.Vertex3(boxVerts[7] + offset);
                GL.TexCoord2(1.0f, 1.0f - 1.0f); GL.Vertex3(boxVerts[6] + offset);
                GL.TexCoord2(0.0f, 1.0f - 1.0f); GL.Vertex3(boxVerts[2] + offset);
                
                GL.End();
            }
            if (bottom)
            {
                GL.BindTexture(TextureTarget.Texture2D, mTexture_ID);
                GL.Begin(PrimitiveType.Quads);
                //GL.Color4(0.8f, 0.8f, 0.8f, 1);
                GL.Color3(Color.White);
                GL.TexCoord2(0.0f, 1.0f - 0.0f); GL.Vertex3(boxVerts[4] + offset);
                GL.TexCoord2(1.0f, 1.0f - 0.0f); GL.Vertex3(boxVerts[0] + offset);
                GL.TexCoord2(1.0f, 1.0f - 1.0f); GL.Vertex3(boxVerts[1] + offset);
                GL.TexCoord2(0.0f, 1.0f - 1.0f); GL.Vertex3(boxVerts[5] + offset);
                GL.End();
            }

            //left
            GL.BindTexture(TextureTarget.Texture2D, mTexture_ID);
            GL.Begin(PrimitiveType.Quads);
            //GL.Color4(0.8f, 0.8f, 0.8f, 1);
            GL.Color3(Color.White);
            GL.TexCoord2(0.0f, 1.0f - 0.0f); GL.Vertex3(boxVerts[3] + offset);
            GL.TexCoord2(1.0f, 1.0f - 0.0f); GL.Vertex3(boxVerts[0] + offset);
            GL.TexCoord2(1.0f, 1.0f - 1.0f); GL.Vertex3(boxVerts[4] + offset);
            GL.TexCoord2(0.0f, 1.0f - 1.0f); GL.Vertex3(boxVerts[7] + offset);
            GL.End();

            //back
            GL.BindTexture(TextureTarget.Texture2D, mTexture_ID);
            GL.Begin(PrimitiveType.Quads);
            //GL.Color4(0.8f, 0.8f, 0.8f, 1);
            GL.Color3(Color.White);
            GL.TexCoord2(0.0f, 1.0f - 0.0f); GL.Vertex3(boxVerts[7] + offset);
            GL.TexCoord2(1.0f, 1.0f - 0.0f); GL.Vertex3(boxVerts[4] + offset);
            GL.TexCoord2(1.0f, 1.0f - 1.0f); GL.Vertex3(boxVerts[5] + offset);
            GL.TexCoord2(0.0f, 1.0f - 1.0f); GL.Vertex3(boxVerts[6] + offset);
            GL.End();
            
            /*
            //front - make transparent so we can see
            GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.One);
            GL.Enable(EnableCap.Blend);

            GL.Begin(PrimitiveType.Quads);
            GL.Color4(0.8f, 0.8f, 0.8f, 0.1f);
            GL.Vertex3(boxVerts[0] + offset);
            GL.Vertex3(boxVerts[1] + offset);
            GL.Vertex3(boxVerts[2] + offset);
            GL.Vertex3(boxVerts[3] + offset);
            GL.End();
            GL.Disable(EnableCap.Blend);

            //right - also transparent to see in
            GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.One);
            GL.Enable(EnableCap.Blend);

            GL.Begin(PrimitiveType.Quads);
            GL.Color4(0.8f, 0.8f, 0.8f, 0.1f);
            GL.Vertex3(boxVerts[6] + offset);
            GL.Vertex3(boxVerts[2] + offset);
            GL.Vertex3(boxVerts[1] + offset);
            GL.Vertex3(boxVerts[5] + offset);
            GL.End();
            */
            GL.Disable(EnableCap.Blend);
            GL.PopMatrix();

        }

        private int loadTexture(string filepath, TextureUnit unit)
        {
            int id = 0;
            if (System.IO.File.Exists(filepath))
            {
                GL.Enable(EnableCap.Texture2D);
                id = GL.GenTexture();

                GL.ActiveTexture(TextureUnit.Texture0);
                GL.BindTexture(TextureTarget.Texture2D, id);

                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureBaseLevel, 0);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMaxLevel, 0);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

                Bitmap bmp = new Bitmap(filepath);
                BitmapData bmp_data = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

                GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, bmp_data.Width, bmp_data.Height, 0,
                    OpenTK.Graphics.OpenGL.PixelFormat.Rgba, PixelType.UnsignedByte, bmp_data.Scan0);

                bmp.UnlockBits(bmp_data);
                bmp.Dispose();
            }
            else
            {
                throw new Exception("Could not find file " + filepath);
            }
            return id;
        }

        protected override void OnUnload(EventArgs e)
        {
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
            GL.BindVertexArray(0);
            GL.DeleteBuffers(mVBO_IDs.Length, mVBO_IDs);
            GL.DeleteVertexArrays(mVAO_IDs.Length, mVAO_IDs);
            mShader.Delete();
            mTestShader.Delete();
            base.OnUnload(e);
        }
    }
}
