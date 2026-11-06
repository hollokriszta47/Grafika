using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using Silk.NET.Maths;
using Silk.NET.Input;


namespace Szeminarium1
{
    internal static class Program
    {
        private static IWindow graphicWindow;

        private static GL Gl;

        private static uint program;

        private static int modelLocation;
        private static int viewLocation;

        private static Vector3D<float> cameraPosition = new Vector3D<float>(1.7f, 1.4f, 2.2f);
        private static Vector3D<float> cameraFront = new Vector3D<float>(-1.7f, -1.4f, -2.2f);

        private static Vector3D<float> cameraUp = new Vector3D<float>(0f, 1f, 0f);

        private static IInputContext inputContext;
        private static IKeyboard keyboard;

        private static float cameraYaw;
        private static float cameraPitch;

        private static int rotationLocation;

        private static float layerAngle = 0f;

        private static float targetLayerAngle = 0f;

        private static bool isLayerRotating = false;

        private static bool wasSpacePressed = false;

        private static bool wasBackspacePressed = false;


        private static readonly string VertexShaderSource = @"
        #version 330 core
        layout (location = 0) in vec3 vPos;
		layout (location = 1) in vec4 vCol;
        
        uniform mat4 uModel;
        uniform mat4 uView;
        uniform mat4 uProjection;
        uniform mat4 uRotation;
		out vec4 outCol;
        
        void main()
        {
			outCol = vCol;
            gl_Position = uProjection * uView * uRotation * uModel * vec4(vPos, 1.0);
        }
        ";


        private static readonly string FragmentShaderSource = @"
        #version 330 core
        out vec4 FragColor;
		
		in vec4 outCol;

        void main()
        {
            FragColor = outCol;
        }
        ";
        private static uint vertexBuffer;
        private static uint indexBuffer;
        private static uint[] cubeVaos = new uint[27];
        private static uint[] cubeColorBuffers = new uint[27];


        static void Main(string[] args)
        {
            WindowOptions windowOptions = WindowOptions.Default;
            windowOptions.Title = "Lab2 - Rubik kocka";
            windowOptions.Size = new Vector2D<int>(500, 500);

            graphicWindow = Window.Create(windowOptions);

            graphicWindow.Load += GraphicWindow_Load;
            graphicWindow.Update += GraphicWindow_Update;
            graphicWindow.Render += GraphicWindow_Render;
            graphicWindow.Closing += GraphicWindow_Closing;

            graphicWindow.Run();
        }

        private static void CheckError(string operation)
        {
            ErrorCode error = (ErrorCode)Gl.GetError();
            if (error != ErrorCode.NoError)
            {
                throw new Exception($"OpenGL error after {operation} : {error}");
            }
        }

        private static unsafe void GraphicWindow_Load()
        {
            // egszeri beallitasokat
            //Console.WriteLine("Loaded");

            Gl = graphicWindow.CreateOpenGL();

            inputContext = graphicWindow.CreateInput();
            keyboard = inputContext.Keyboards[0];

            cameraYaw = MathF.Atan2(cameraFront.Z, cameraFront.X);
            cameraPitch = MathF.Atan2(cameraFront.Y, MathF.Sqrt(cameraFront.X * cameraFront.X + cameraFront.Z * cameraFront.Z));

            Gl.ClearColor(System.Drawing.Color.White);
            Gl.Enable(EnableCap.DepthTest);

            CheckError("OpenGL initialization");

            uint vshader = Gl.CreateShader(ShaderType.VertexShader);
            uint fshader = Gl.CreateShader(ShaderType.FragmentShader);

            Gl.ShaderSource(vshader, VertexShaderSource);
            Gl.CompileShader(vshader);


            Gl.GetShader(vshader, ShaderParameterName.CompileStatus, out int vStatus);
            if (vStatus != (int)GLEnum.True)
                throw new Exception("Vertex shader failed to compile: " + Gl.GetShaderInfoLog(vshader));

            Gl.ShaderSource(fshader, FragmentShaderSource);
            Gl.CompileShader(fshader);

            CheckError("Shader compilation");
            Gl.GetShader(fshader, ShaderParameterName.CompileStatus, out int fStatus);
            if (fStatus != (int)GLEnum.True)
                throw new Exception("Fragment shader failed to compile: " + Gl.GetShaderInfoLog(fshader));

            program = Gl.CreateProgram();
            Gl.AttachShader(program, vshader);
            Gl.AttachShader(program, fshader);
            CheckError("Shader attachment");

            Gl.LinkProgram(program);
            CheckError("Shader program link");

            Gl.UseProgram(program);
            
            modelLocation = Gl.GetUniformLocation(program, "uModel");
            viewLocation = Gl.GetUniformLocation(program, "uView");
            rotationLocation = Gl.GetUniformLocation(program, "uRotation");
            int projectionLocation = Gl.GetUniformLocation(program, "uProjection");

            Matrix4X4<float> projection = Matrix4X4.CreatePerspectiveFieldOfView<float>((float)(Math.PI / 4), 1f, 0.1f, 100f);
            Gl.UniformMatrix4(projectionLocation, 1, false, (float*)&projection);
                    

            Gl.DetachShader(program, vshader);
            Gl.DetachShader(program, fshader);
            Gl.DeleteShader(vshader);
            Gl.DeleteShader(fshader);

            CheckError("Shader cleanup");

            Gl.GetProgram(program, GLEnum.LinkStatus, out var status);
            if (status == 0)
            {
                Console.WriteLine($"Error linking shader {Gl.GetProgramInfoLog(program)}");
            }


            float[] vertexArray =
            {
                -0.15f, -0.15f, 0.15f,
                -0.15f, 0.15f, 0.15f,
                0.15f, -0.15f, 0.15f,
                0.15f, 0.15f, 0.15f,

                0.15f, -0.15f, -0.15f,
                0.15f, 0.15f, -0.15f,
                -0.15f, -0.15f, -0.15f,
                -0.15f, 0.15f, -0.15f,

                 -0.15f, -0.15f, -0.15f,
                -0.15f, 0.15f, -0.15f,
                -0.15f, -0.15f, 0.15f,
                -0.15f, 0.15f, 0.15f,

                 0.15f, -0.15f, 0.15f,
                 0.15f, 0.15f, 0.15f,
                 0.15f, -0.15f, -0.15f,
                 0.15f, 0.15f, -0.15f,

                -0.15f, 0.15f, 0.15f,
                -0.15f, 0.15f, -0.15f,
                0.15f, 0.15f, 0.15f,
                0.15f, 0.15f, -0.15f,

                 -0.15f, -0.15f, -0.15f,
                -0.15f, -0.15f, 0.15f,
                0.15f, -0.15f, -0.15f,
                0.15f, -0.15f, 0.15f,
            };

            float[][] colorsCube =
            {
                new float[] {0f, 0f, 1f, 1f},
                new float[] {0f, 1f, 0f, 1f},
                new float[] {1f, 1f, 0f, 1f},
                new float[] {1f, 0f, 0f, 1f},
                new float[] {0f, 1f, 1f, 1f},
                new float[] {1f, 0.5f, 0.7f, 1f}
            };

            float[] innerColor = { 0f, 0f, 0f, 1f };

            List<uint> idx = new List<uint>();
            for (uint i = 0; i < 6; i++)
            {
                uint startIdx = i * 4;

                idx.Add(startIdx);
                idx.Add(startIdx + 2);
                idx.Add(startIdx + 1);

                idx.Add(startIdx + 1);
                idx.Add(startIdx + 2); 
                idx.Add(startIdx + 3);

            }

            uint[] indexArray = idx.ToArray();
           
            vertexBuffer = Gl.GenBuffer();
            Gl.BindBuffer(GLEnum.ArrayBuffer, vertexBuffer);
            Gl.BufferData(GLEnum.ArrayBuffer, (ReadOnlySpan<float>)vertexArray.AsSpan(), GLEnum.StaticDraw);
            
            indexBuffer = Gl.GenBuffer();

            int cubeIndex = 0;
            for(int x = -1; x <= 1; x++)
            {
                for(int y = -1; y <= 1; y++)
                {
                    for(int z = -1; z <= 1; z++)
                    {
                        List<float> colorValues = new List<float>();
                        for(int i = 0; i < 6; i++)
                        {
                            bool isOuter =
                                (i == 0 && z == 1) ||
                                (i == 1 && z == -1) ||
                                (i == 2 && x == -1) ||
                                (i == 3 && x == 1) ||
                                (i == 4 && y == 1) ||
                                (i == 5 && y == -1);

                            float[] color;
                            if(isOuter)
                            {
                                color = colorsCube[i];
                            }
                            else
                            {
                                color = innerColor;
                            }
                           
                            for(int j = 0; j < 4; j++)
                            {
                                colorValues.AddRange(color);
                            }
                        }
                        float[] colorArray = colorValues.ToArray();

                        cubeVaos[cubeIndex] = Gl.GenVertexArray();
                        Gl.BindVertexArray(cubeVaos[cubeIndex]);

                        Gl.BindBuffer(GLEnum.ArrayBuffer, vertexBuffer);
                        Gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 0, null);
                        Gl.EnableVertexAttribArray(0);

                        cubeColorBuffers[cubeIndex] = Gl.GenBuffer();
                        Gl.BindBuffer(GLEnum.ArrayBuffer, cubeColorBuffers[cubeIndex]);

                        Gl.BufferData(GLEnum.ArrayBuffer, (ReadOnlySpan<float>)colorArray.AsSpan(), GLEnum.StaticDraw);
                        Gl.VertexAttribPointer(1, 4, VertexAttribPointerType.Float, false, 0, null);
                        Gl.EnableVertexAttribArray(1);
                        Gl.BindBuffer(GLEnum.ElementArrayBuffer, indexBuffer);

                        if(cubeIndex == 0)
                        {
                            Gl.BufferData(GLEnum.ElementArrayBuffer, (ReadOnlySpan<uint>)indexArray.AsSpan(), GLEnum.StaticDraw);
                        }
                        cubeIndex++;
                    }
                }
            }

            Gl.BindVertexArray(0);
            Gl.BindBuffer(GLEnum.ArrayBuffer, 0);
            CheckError("Cube initialization");

        }

        private static void GraphicWindow_Update(double deltaTime)
        {
            float speed = 1.0f * (float)deltaTime;
            float rotationSpeed = 1.5f * (float)deltaTime;

            if(keyboard.IsKeyPressed(Key.Left))
            {
                cameraYaw -= rotationSpeed;
            }

            if (keyboard.IsKeyPressed(Key.Right))
            {
                cameraYaw += rotationSpeed;
            }

            if(keyboard.IsKeyPressed(Key.Up))
            {
                cameraPitch += rotationSpeed;
            }

            if(keyboard.IsKeyPressed(Key.Down))
            {
                cameraPitch -= rotationSpeed;
            }
            cameraPitch = Math.Clamp(cameraPitch, -1.55f, 1.55f);

            cameraFront = new Vector3D<float>(MathF.Cos(cameraPitch) * MathF.Cos(cameraYaw), MathF.Sin(cameraPitch), MathF.Cos(cameraPitch) * MathF.Sin(cameraYaw));

            Vector3D<float> direction = cameraFront / MathF.Sqrt(cameraFront.X * cameraFront.X + cameraFront.Y * cameraFront.Y + cameraFront.Z * cameraFront.Z);

            Vector3D<float> right = new Vector3D<float>(-direction.Z, 0f, direction.X);
            right = right / MathF.Sqrt(right.X * right.X + right.Z * right.Z);
            if (keyboard.IsKeyPressed(Key.W))
            {
                cameraPosition += direction * speed;
            }

            if(keyboard.IsKeyPressed(Key.S))
            {
                cameraPosition -= direction * speed;
            }

            if(keyboard.IsKeyPressed(Key.A))
            {
               cameraPosition -= right * speed;
            }

            if(keyboard.IsKeyPressed(Key.D))
            {
                cameraPosition += right * speed;
            }

            bool spacePressed = keyboard.IsKeyPressed(Key.Space);
            bool backspacePressed = keyboard.IsKeyPressed(Key.Backspace);

            if(!isLayerRotating)
            {
                if(spacePressed && !wasSpacePressed)
                {
                    targetLayerAngle = layerAngle + MathF.PI / 2f;
                    isLayerRotating = true;
                }
                else if(backspacePressed && !wasBackspacePressed)
                {
                    targetLayerAngle = layerAngle - MathF.PI / 2f;
                    isLayerRotating = true;
                }
            }

            if(isLayerRotating)
            {
                float step = MathF.PI * (float)deltaTime;

                if(layerAngle < targetLayerAngle)
                {
                    layerAngle = MathF.Min(layerAngle + step, targetLayerAngle);
                }
                else
                {
                    layerAngle = MathF.Max(layerAngle - step, targetLayerAngle);
                }
                if(layerAngle == targetLayerAngle)
                {
                    isLayerRotating = false;
                }
            }

            wasSpacePressed = spacePressed;
            wasBackspacePressed = backspacePressed;
        }
        private static unsafe void GraphicWindow_Render(double deltaTime)
        {
            //Console.WriteLine($"Render after {deltaTime} [s]");

            Gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            Gl.UseProgram(program);

            Matrix4X4<float> view = Matrix4X4.CreateLookAt(cameraPosition, cameraPosition + cameraFront, cameraUp);
            Gl.UniformMatrix4(viewLocation, 1, false, (float*)&view);
            int cubeIndex = 0;
            float distance = 0.32f;
            for(int x = -1; x <= 1; x++)
            {
                for(int y = -1; y <= 1; y++)
                {
                    for(int z = -1; z <= 1; z++)
                    {
                        Matrix4X4<float> model = Matrix4X4.CreateTranslation(x * distance, y * distance, z * distance);
                        Gl.UniformMatrix4(modelLocation, 1, false, (float*)&model);

                        Matrix4X4<float> rotation;
                        if(z == 1)
                        {
                            rotation = Matrix4X4.CreateRotationZ(layerAngle);
                        }
                        else
                        {
                            rotation = Matrix4X4<float>.Identity;
                        }
                        Gl.UniformMatrix4(rotationLocation, 1, false, (float*)&rotation);
                        Gl.BindVertexArray(cubeVaos[cubeIndex]);
                        Gl.DrawElements(GLEnum.Triangles, 36u, GLEnum.UnsignedInt, null);
                        cubeIndex++;
                    }
                }
            }

            Gl.BindVertexArray(0);
            CheckError("Drawing");
        }

        private static void GraphicWindow_Closing()
        {
            for(int i = 0; i < 27; i++)
            {
                Gl.DeleteVertexArray(cubeVaos[i]);
                Gl.DeleteBuffer(cubeColorBuffers[i]);
            }

            Gl.DeleteBuffer(vertexBuffer);
            Gl.DeleteBuffer(indexBuffer); 
            Gl.DeleteProgram(program);
            CheckError("cleanup");
        }
    }

}
