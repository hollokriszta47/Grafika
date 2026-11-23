using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using Silk.NET.Maths;
using Silk.NET.Input;
using Silk.NET.OpenAL;
using Silk.NET.OpenGL.Extensions.ImGui;

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

        private static readonly string VertexShaderSource = @"
        #version 330 core
        layout (location = 0) in vec3 vPos;
		layout (location = 1) in vec4 vCol;
        layout (location = 2) in vec3 vNormal;
        
        uniform mat4 uModel;
        uniform mat4 uView;
        uniform mat4 uProjection;
        uniform mat4 uRotation;
        uniform mat4 uOrientation;
        uniform float uVictoryTime;
		
        out vec4 outCol;
        out vec3 fragPos;
        out vec3 fragNormal;

        void main()
        {
			outCol = vCol;
            mat4 worldMatrix = uRotation * uModel * uOrientation;
            vec4 position = worldMatrix * vec4(vPos, 1.0);
            vec3 normal  = mat3(worldMatrix) * vNormal;

            if(uVictoryTime > 0.0) {
                float scale = 1.0 + 0.08 * sin(uVictoryTime * 8.0);
                position.xyz *= scale;
                float angle = uVictoryTime * 1.8;
                float x = position.x;
                float z = position.z;
                position.x = cos(angle) * x + sin(angle) * z;
                position.z = -sin(angle) * x + cos(angle) * z;
                
                float nx = normal.x;
                float nz  = normal.z;
                normal.x = cos(angle) * nx + sin(angle) * nz;
                normal.z = -sin(angle) * nx + cos(angle) * nz;
           }
          fragPos = position.xyz;
          fragNormal = normalize(normal);
          gl_Position = uProjection * uView * position;
        }   
        ";


        private static readonly string FragmentShaderSource = @"
        #version 330 core
        out vec4 FragColor;
		
		in vec4 outCol;
        in vec3 fragPos;
        in vec3 fragNormal;
        
        uniform vec3 uLightPosition;
        uniform vec3 uLightColor;
        uniform vec3 uCameraPosition;

        void main()
        {
            vec3 normal = normalize(fragNormal);
            vec3 lightDir = normalize(uLightPosition - fragPos);
            vec3 ambient = 0.25 * uLightColor;
            
            float diff = max(dot(normal, lightDir), 0.0);
            vec3 diffuse = 0.75 * diff * uLightColor;
            
            vec3 viewDir = normalize(uCameraPosition - fragPos);
            vec3 reflectDir = reflect(-lightDir, normal);
            
            float spec = pow(max(dot(viewDir, reflectDir), 0.0), 32.0);
            vec3 specular = 0.35 * spec * uLightColor;
            
            vec3 result = (ambient + diffuse) * outCol.rgb + specular;
            FragColor = vec4(result, outCol.a);
        }
        ";
        private static uint vertexBuffer;
        private static uint indexBuffer;

        private static uint normalBuffer;

        private static uint[] cubeVaos = new uint[27];
        private static uint[] cubeColorBuffers = new uint[27];

        private static List<SmallCube> smallCubes = new List<SmallCube>();

        private static int orientationLocation;

        private static char activeAxis = 'Z';

        private static int activeLayer = 1;

        private static readonly Key[] layerKeys =
        {
            Key.Number1, Key.Number2, Key.Number3,
            Key.Number4, Key.Number5, Key.Number6,

            Key.U, Key.I, Key.O,
            Key.J, Key.K, Key.L,

            Key.Number7, Key.Number8, Key.Number9,
            Key.Z, Key.X, Key.C
        };

        private static readonly char[] layerAxes =
        {
            'Y', 'Y', 'Y',
            'Y', 'Y', 'Y',

            'X', 'X', 'X',
            'X', 'X', 'X',

            'Z', 'Z', 'Z',
            'Z', 'Z', 'Z'

        };

        private static readonly int[] layerPositions =
        {
            1,0,-1,
            1,0,-1,

            -1,0,1,
            -1,0,1,

            -1,0,1,
            -1,0,1
        };

        private static readonly int[] layerDirections =
        {
            1,1,1,
            -1,-1,-1,

            -1,-1,-1,
            1,1,1,

            1,1,1,
            -1,-1,-1
        };

        private static bool[] wasLayerKeyPressed = new bool[18];

        private static Random random = new Random();

        private static Queue<(char Axis, int Layer, int Direction)> scrambleMoves = new Queue<(char Axis, int Layer, int Direction)>();

        private static bool isScrambling = false;
        private static bool wasRPressed = false;

        private static bool wasSolved = true;

        private static bool isCelebrating = false;

        private static float victoryTime = 0f;

        private static int victoryTimeLocation;
        private class SmallCube
        {
            public int X;
            public int Y;
            public int Z;

            public int VaoIndex;

            public int[] FaceColors = new int[6];
            public Matrix4X4<float> Orientation = Matrix4X4<float>.Identity;
            public SmallCube(int x, int y, int z, int vaoIndex)
            {
                X = x;
                Y = y;
                Z = z;
                VaoIndex = vaoIndex;

                for(int i = 0; i < 6; i++)
                {
                    FaceColors[i] = -1;
                }

                if(z == 1)
                {
                    FaceColors[0] = 0;
                }

                if(z == -1)
                {
                    FaceColors[1] = 1;
                }

                if(x == -1)
                {
                    FaceColors[2] = 2;
                }

                if(x == 1)
                {
                    FaceColors[3] = 3;
                }

                if(y == 1)
                {
                    FaceColors[4] = 4;
                }

                if(y == -1)
                {
                    FaceColors[5] = 5;
                }
            }

        }


        private static float lightX = 2f;
        private static float lightY = 3f;
        private static float lightZ = 4f;

        private static float lightRed = 1f;
        private static float lightGreen = 1f;
        private static float lightBlue = 1f;

        private static int lightPositionLocation;
        private static int lightColorLocation;
        private static int cameraPositionLocation;
        private static ImGuiController imGuiController;

        static void Main(string[] args)
        {
            WindowOptions windowOptions = WindowOptions.Default;
            windowOptions.Title = "Lab3-3 - Phong arnyalas";
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

            imGuiController = new ImGuiController(Gl, graphicWindow, inputContext);


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
            orientationLocation = Gl.GetUniformLocation(program, "uOrientation");
            int projectionLocation = Gl.GetUniformLocation(program, "uProjection");
            victoryTimeLocation = Gl.GetUniformLocation(program, "uVictoryTime");


            lightPositionLocation = Gl.GetUniformLocation(program, "uLightPosition");
            lightColorLocation = Gl.GetUniformLocation(program, "uLightColor");
            cameraPositionLocation = Gl.GetUniformLocation(program, "uCameraPosition");


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


            float[] faceNormals =
            {
                0f, 0f, 1f,
                0f, 0f, -1f,
                -1f, 0f, 0f,
                1f, 0f, 0f,
                0f, 1f, 0f,
                0f, -1f, 0f
            };

            float[] normalArray = new float[6 * 4 * 3];
            for(int face = 0; face < 6; face++)
            {
                for(int corner = 0; corner < 4; corner++)
                {
                    Array.Copy(faceNormals, face * 3, normalArray, (face * 4 + corner) * 3, 3);
                }
            }

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
            

            normalBuffer = Gl.GenBuffer();
            Gl.BindBuffer(GLEnum.ArrayBuffer, normalBuffer);
            Gl.BufferData(GLEnum.ArrayBuffer, (ReadOnlySpan<float>)normalArray.AsSpan(), GLEnum.StaticDraw);
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

                        Gl.BindBuffer(GLEnum.ArrayBuffer, normalBuffer);
                        Gl.VertexAttribPointer(2, 3, VertexAttribPointerType.Float, false, 0, null);
                        Gl.EnableVertexAttribArray(2);


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
                        smallCubes.Add(new SmallCube(x, y, z, cubeIndex));
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
            imGuiController.Update((float)deltaTime);
            bool uiCapturesKeyboard = ImGuiNET.ImGui.GetIO().WantCaptureKeyboard;
            float speed = 1.0f * (float)deltaTime;
            float rotationSpeed = 1.5f * (float)deltaTime;

            if(isCelebrating)
            {
                victoryTime += (float)deltaTime;
            }

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

            bool rPressed = keyboard.IsKeyPressed(Key.R);
            if(!uiCapturesKeyboard && rPressed && !wasRPressed && !isLayerRotating && !isScrambling)
            {
                isCelebrating = false;
                victoryTime = 0f;
                StartScramble();
            }
            wasRPressed = rPressed;
            for(int i = 0; i < layerKeys.Length; i++)
            {
                bool pressed = keyboard.IsKeyPressed(layerKeys[i]);

                if (!uiCapturesKeyboard && !isCelebrating && !isScrambling && !isLayerRotating && pressed && !wasLayerKeyPressed[i])
                {
                    StartLayerRotation(layerAxes[i], layerPositions[i], layerDirections[i]);
                }
                wasLayerKeyPressed[i] = pressed;
            }

            if(isScrambling && !isLayerRotating)
            {
                if (scrambleMoves.Count > 0)
                {
                    var move = scrambleMoves.Dequeue();
                    activeAxis = move.Axis;
                    activeLayer = move.Layer;

                    layerAngle = 0f;
                    targetLayerAngle = move.Direction * MathF.PI / 2f;

                    isLayerRotating = true;
                }
                else
                {
                    isScrambling = false;
                }
            }

            if (isLayerRotating)
            {
                float step;
                if (isScrambling)
                {
                    step = MathF.PI * 4f * (float)deltaTime;
                }
                else
                {
                    step = MathF.PI * 1f * (float)deltaTime;
                }

                if (layerAngle < targetLayerAngle)
                {
                    layerAngle = MathF.Min(layerAngle + step, targetLayerAngle);
                }
                else
                {
                    layerAngle = MathF.Max(layerAngle - step, targetLayerAngle);
                }
                if (layerAngle == targetLayerAngle)
                {
                    ApplyLayerTurn(activeAxis, activeLayer, targetLayerAngle);
                    bool solvedNow = IsSolved();
                    if (solvedNow && !wasSolved && !isScrambling)
                    {
                        isCelebrating = true;
                        victoryTime = 0;
                    }

                    wasSolved = solvedNow;
                    layerAngle = 0f;
                    targetLayerAngle = 0f;
                    isLayerRotating = false;
                }
            }
        }
        private static unsafe void GraphicWindow_Render(double deltaTime)
        {
            //Console.WriteLine($"Render after {deltaTime} [s]");

            Gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            Gl.UseProgram(program);

            Gl.Uniform3(lightPositionLocation, lightX, lightY, lightZ);
            Gl.Uniform3(lightColorLocation, lightRed, lightGreen, lightBlue);
            Gl.Uniform3(cameraPositionLocation, cameraPosition.X, cameraPosition.Y, cameraPosition.Z);

            float currentVictoryTime = 0f;

            if(isCelebrating)
            {
                currentVictoryTime = victoryTime;
            }

            Gl.Uniform1(victoryTimeLocation, currentVictoryTime);

            Matrix4X4<float> view = Matrix4X4.CreateLookAt(cameraPosition, cameraPosition + cameraFront, cameraUp);
            Gl.UniformMatrix4(viewLocation, 1, false, (float*)&view);
            float distance = 0.32f;
            for(int i = 0; i < smallCubes.Count; i++)
            {
                SmallCube cube = smallCubes[i];
                Matrix4X4<float> model = Matrix4X4.CreateTranslation(cube.X * distance, cube.Y * distance, cube.Z * distance);
                Matrix4X4<float> orientation = cube.Orientation;
                Matrix4X4<float> rotation = Matrix4X4<float>.Identity;

                bool belongsToLayer =
                    (activeAxis == 'X' && cube.X == activeLayer) ||
                    (activeAxis == 'Y' && cube.Y == activeLayer) ||
                    (activeAxis == 'Z' && cube.Z == activeLayer);

                if(belongsToLayer && isLayerRotating)
                {
                    if(activeAxis == 'X')
                    {
                        rotation = Matrix4X4.CreateRotationX(layerAngle);
                    }

                    else if(activeAxis == 'Y')
                    {
                        rotation = Matrix4X4.CreateRotationY(layerAngle);
                    }
                    else
                    {
                        rotation = Matrix4X4.CreateRotationZ(layerAngle);
                    }
                }

                Gl.UniformMatrix4(modelLocation, 1, false, (float*)&model);
                Gl.UniformMatrix4(orientationLocation, 1, false, (float*)&orientation);
                Gl.UniformMatrix4(rotationLocation, 1, false, (float*)&rotation);
                Gl.BindVertexArray(cubeVaos[cube.VaoIndex]);
                Gl.DrawElements(GLEnum.Triangles, 36u, GLEnum.UnsignedInt, null);
            }

            Gl.BindVertexArray(0);
            CheckError("Drawing");

            ImGuiNET.ImGui.Begin("Lighting");
            ImGuiNET.ImGui.SliderFloat("(R)", ref lightRed, 0f, 1f);
            ImGuiNET.ImGui.SliderFloat("(G)", ref lightGreen, 0f, 1f);
            ImGuiNET.ImGui.SliderFloat("(B)", ref lightBlue, 0f, 1f);

            ImGuiNET.ImGui.Separator();
            ImGuiNET.ImGui.Text("Light Position");
            ImGuiNET.ImGui.InputFloat("X", ref lightX);
            ImGuiNET.ImGui.InputFloat("Y", ref lightY);
            ImGuiNET.ImGui.InputFloat("Z", ref lightZ);


            foreach(char axis in new[] { 'X', 'Y', 'Z'})
            {
                for(int layer = -1; layer <= 1; layer++)
                {
                    if (ImGuiNET.ImGui.Button($"{axis} {layer} + 90"))
                    {
                        StartLayerRotation(axis, layer, 1);
                    }
                    ImGuiNET.ImGui.SameLine();

                    if (ImGuiNET.ImGui.Button($"{axis} {layer} -90"))
                    {
                        StartLayerRotation(axis, layer, -1);
                    }
                }
            }

            ImGuiNET.ImGui.Separator();

            if (ImGuiNET.ImGui.Button("Scramble (R)") &&
                !isLayerRotating && !isScrambling)
            {
                isCelebrating = false;
                victoryTime = 0f;
                StartScramble();
            }

            ImGuiNET.ImGui.End();
            imGuiController.Render();
        }

        private static void GraphicWindow_Closing()
        {
            imGuiController.Dispose();
            for (int i = 0; i < 27; i++)
            {
                Gl.DeleteVertexArray(cubeVaos[i]);
                Gl.DeleteBuffer(cubeColorBuffers[i]);
            }

            Gl.DeleteBuffer(vertexBuffer);
            Gl.DeleteBuffer(normalBuffer);
            Gl.DeleteBuffer(indexBuffer); 
            Gl.DeleteProgram(program);
            CheckError("cleanup");
        }

        private static void StartLayerRotation(char axis, int layer, int direction)
        {
            if (isCelebrating || isScrambling || isLayerRotating)
            {
                return;
            }

            activeAxis = axis;
            activeLayer = layer;
            layerAngle = 0f;
            targetLayerAngle = direction * MathF.PI / 2f;
            isLayerRotating = true;
        }
        private static void ApplyLayerTurn(char axis, int layer, float angle)
        {
            int direction;
            if(angle > 0f)
            {
                direction = 1;
            }
            else
            {
                direction = -1;
            }

            Matrix4X4<float> turnRotation;
            if(axis == 'X')
            {
                turnRotation = Matrix4X4.CreateRotationX(direction * MathF.PI / 2f);
            }

            else if(axis == 'Y')
            {
                turnRotation = Matrix4X4.CreateRotationY(direction * MathF.PI / 2f);
            }
            else
            {
                turnRotation = Matrix4X4.CreateRotationZ(direction * MathF.PI / 2f);

            }

            for (int i = 0; i < smallCubes.Count; i++)
            {
                SmallCube cube = smallCubes[i];

                bool belongsToLayer =
                    (axis == 'X' && cube.X == layer) ||
                    (axis == 'Y' && cube.Y == layer) ||
                    (axis == 'Z' && cube.Z == layer);

                if(!belongsToLayer)
                {
                    continue;
                }

                int oldX = cube.X;
                int oldY = cube.Y;
                int oldZ = cube.Z;

                if(axis == 'X')
                {
                    cube.Y = -direction * oldZ;
                    cube.Z = direction * oldY;
                }
                else if(axis == 'Y')
                {
                    cube.X = direction * oldZ;
                    cube.Z = -direction * oldX;
                }
                else
                {
                    cube.X = -direction * oldY;
                    cube.Y = direction * oldX;
                }

                RotateFaceColors(cube, axis, direction);

                cube.Orientation = cube.Orientation * turnRotation;
            }
        }

        private static void RotateFaceColors(SmallCube cube, char axis, int direction)
        {
            int[] normalX = { 0, 0, -1, 1, 0, 0 };
            int[] normalY = { 0, 0, 0, 0, 1, -1 };
            int[] normalZ = { 1, -1, 0, 0, 0, 0 };

            int[] newColors = new int[6];

            for (int i = 0; i < 6; i++)
            {
                newColors[i] = -1;
            }

            for (int i = 0; i < 6; i++)
            {
                if (cube.FaceColors[i] == -1)
                {
                    continue;
                }

                int oldX = normalX[i];
                int oldY = normalY[i];
                int oldZ = normalZ[i];

                int x = oldX;
                int y = oldY;
                int z = oldZ;

                if (axis == 'X')
                {
                    y = -direction * oldZ;
                    z = direction * oldY;
                }

                else if (axis == 'Y')
                {
                    x = direction * oldZ;
                    z = -direction * oldX;
                }
                else
                {
                    x = -direction * oldY;
                    y = direction * oldX;
                }

                int newFace;

                if (z == 1)
                {
                    newFace = 0;
                }

                else if (z == -1)
                {
                    newFace = 1;
                }

                else if (x == -1)
                {
                    newFace = 2;
                }

                else if (x == 1)
                {
                    newFace = 3;
                }

                else if (y == 1)
                {
                    newFace = 4;
                }

                else
                {
                    newFace = 5;
                }
                newColors[newFace] = cube.FaceColors[i];
            }
            cube.FaceColors = newColors;
        }

        private static void StartScramble()
        {
            scrambleMoves.Clear();

            for(int i = 0; i < 30; i++)
            {
                char axis;
                int randomAxis = random.Next(3);
                if(randomAxis == 0)
                {
                    axis = 'X';
                }
                else if(randomAxis == 1)
                {
                    axis = 'Y';
                }
                else
                {
                    axis = 'Z';
                }


                int layer = random.Next(-1, 2);
                int direction;
                if(random.Next(2) == 0)
                {
                    direction = 1;
                }
                else
                {
                    direction = -1;
                }
                scrambleMoves.Enqueue((axis, layer, direction));
            }
            isScrambling = true;
        }

        private static bool IsSolved()
        {
            for(int face = 0; face < 6; face ++)
            {
                int expectedColor = -1;
                int count = 0;

                for(int i = 0; i < smallCubes.Count; i++)
                {
                    SmallCube cube = smallCubes[i];
                    bool belongsToFace =
                        (face == 0 && cube.Z == 1) ||
                        (face == 1 && cube.Z == -1) ||
                        (face == 2 && cube.X == -1) ||
                        (face == 3 && cube.X == 1) ||
                        (face == 4 && cube.Y == 1) ||
                        (face == 5 && cube.Y == -1);
                    if(!belongsToFace)
                    {
                        continue;
                    }

                    int color = cube.FaceColors[face];
                    if(color == -1)
                    {
                        return false;
                    }

                    if(expectedColor == -1)
                    {
                        expectedColor = color;
                    }

                    else if(color != expectedColor)
                    {
                        return false;
                    }
                    count++;
                }
                if(count != 9)
                {
                    return false;
                }
            }
            return true;
        }
    }

}
