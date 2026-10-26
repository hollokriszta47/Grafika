using Silk.NET.OpenGL;
using Silk.NET.Windowing;

namespace Szeminarium1
{
    internal static class Program
    {
        private static IWindow graphicWindow;

        private static GL Gl;

        private static uint program;

        private static readonly string VertexShaderSource = @"
        #version 330 core
        layout (location = 0) in vec3 vPos;
		layout (location = 1) in vec4 vCol;

		out vec4 outCol;
        
        void main()
        {
			outCol = vCol;
            gl_Position = vec4(vPos.x, vPos.y, vPos.z, 1.0);
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

        static void Main(string[] args)
        {
            WindowOptions windowOptions = WindowOptions.Default;
            windowOptions.Title = "1. szeminárium - háromszög";
            windowOptions.Size = new Silk.NET.Maths.Vector2D<int>(500, 500);

            graphicWindow = Window.Create(windowOptions);

            graphicWindow.Load += GraphicWindow_Load;
            graphicWindow.Update += GraphicWindow_Update;
            graphicWindow.Render += GraphicWindow_Render;

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

        private static void GraphicWindow_Load()
        {
            // egszeri beallitasokat
            //Console.WriteLine("Loaded");

            Gl = graphicWindow.CreateOpenGL();

            Gl.ClearColor(System.Drawing.Color.White);

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

        }

        private static void GraphicWindow_Update(double deltaTime)
        {
            // NO GL
            // make it threadsave
            //Console.WriteLine($"Update after {deltaTime} [s]");
        }
        private static unsafe void GraphicWindow_Render(double deltaTime)
        {
            //Console.WriteLine($"Render after {deltaTime} [s]");

            Gl.Clear(ClearBufferMask.ColorBufferBit);

            uint vao = Gl.GenVertexArray();
            Gl.BindVertexArray(vao);
            CheckError("VAO initialization");

            List<float> v = new List<float>();
            for(int i = 0; i < 4; i++)
            {
                for(int j = 0; j < 4; j++)
                {
                    float x = -0.5f + i / 3.0f;
                    float y = -0.5f + j / 3.0f;
                    v.Add(x);
                    v.Add(y);
                    v.Add(0.0f);
                }
            }

            for(int i = 0; i < 4; i++)
            {
                float t = i / 3.0f;

                float x = -0.5f - 0.2f * t;
                float bottomY = -0.5f + 0.3f * t;
                float topY = 0.5f + 0.2f * t;

                for(int j = 0; j < 4; j++)
                {
                    float s = j / 3.0f;
                    float y = bottomY + (topY - bottomY) * s;

                    v.Add(x);
                    v.Add(y);
                    v.Add(0.0f);
                }
            }

            for (int i = 0; i < 4; i++) {
                float t = i / 3.0f;

                float leftX = -0.5f - 0.2f * t;
                float rightX = 0.5f - 0.2f * t;
                float y = 0.5f + 0.2f * t;

                for(int j = 0; j < 4; j++)
                {
                    float s = j / 3.0f;
                    float x = leftX + (rightX - leftX) * s;

                    v.Add(x);
                    v.Add(y);
                    v.Add(0.0f);
                }
            }

            float[] vertexArray = v.ToArray();

            List<float> colorValues = new List<float>();
            
            for(int i = 0; i < 16; i++)
            {
                colorValues.Add(0.0f);
                colorValues.Add(0.0f);
                colorValues.Add(1.0f);
                colorValues.Add(1.0f);
            }
            
            for(int i = 0; i < 16; i++)
            {
                colorValues.Add(1.0f);
                colorValues.Add(1.0f);
                colorValues.Add(0.0f);
                colorValues.Add(1.0f);
            }
            
            for(int i = 0; i < 16; i++)
            {
                colorValues.Add(1.0f);
                colorValues.Add(0.0f);
                colorValues.Add(0.0f);
                colorValues.Add(1.0f);
            }

            float[] colorArray = colorValues.ToArray();

            List<uint> idx = new List<uint>();
            for(uint i = 0; i < 3; i++)
            {
                for(uint j = 0; j < 3; j++)
                {
                    uint vertexIndex = i * 4 + j;
                    idx.Add(vertexIndex);
                    idx.Add(vertexIndex + 1);
                    idx.Add(vertexIndex + 5);

                    idx.Add(vertexIndex);
                    idx.Add(vertexIndex + 5);
                    idx.Add(vertexIndex + 4);

                }
            }

            for(uint i = 0; i < 3; i++)
            {
                for(uint j = 0; j < 3; j++)
                {
                    uint vertexIndex = 16 + i * 4 + j;
                    idx.Add(vertexIndex);
                    idx.Add(vertexIndex + 1);
                    idx.Add(vertexIndex + 5);

                    idx.Add(vertexIndex);
                    idx.Add(vertexIndex + 5);
                    idx.Add(vertexIndex + 4);

                }
            }
            
            for(uint i = 0; i < 3; i++)
            {
                for(uint j = 0; j < 3; j++)
                {
                    uint vertexIndex = 32 + i * 4 + j;
                    idx.Add(vertexIndex);
                    idx.Add(vertexIndex + 1);
                    idx.Add(vertexIndex + 5);

                    idx.Add(vertexIndex);
                    idx.Add(vertexIndex + 5);
                    idx.Add(vertexIndex + 4);

                }
            }

            uint[] indexArray = idx.ToArray();

            uint vertices = Gl.GenBuffer();
            Gl.BindBuffer(GLEnum.ArrayBuffer, vertices);
            Gl.BufferData(GLEnum.ArrayBuffer, (ReadOnlySpan<float>)vertexArray.AsSpan(), GLEnum.StaticDraw);
            Gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 0, null);
            Gl.EnableVertexAttribArray(0);
            CheckError("Vertex buffer");

            uint colors = Gl.GenBuffer();
            Gl.BindBuffer(GLEnum.ArrayBuffer, colors);
            Gl.BufferData(GLEnum.ArrayBuffer, (ReadOnlySpan<float>)colorArray.AsSpan(), GLEnum.StaticDraw);
            Gl.VertexAttribPointer(1, 4, VertexAttribPointerType.Float, false, 0, null);
            Gl.EnableVertexAttribArray(1);
            CheckError("Color buffer");

            uint indices = Gl.GenBuffer();
            Gl.BindBuffer(GLEnum.ElementArrayBuffer, indices);
            Gl.BufferData(GLEnum.ElementArrayBuffer, (ReadOnlySpan<uint>)indexArray.AsSpan(), GLEnum.StaticDraw);
            CheckError("Index buffer");

            Gl.BindBuffer(GLEnum.ArrayBuffer, 0);

            Gl.UseProgram(program);
            
            Gl.DrawElements(GLEnum.Triangles, (uint)indexArray.Length, GLEnum.UnsignedInt, null); // we used element buffer
            CheckError("Drawing");

            List<uint> lineIndices = new List<uint>();
            for(uint startIdx = 0; startIdx < 48; startIdx += 16)
            {
                for(uint i = 1; i < 3; i++)
                {
                    for(uint j = 0; j < 3; j++)
                    {
                        lineIndices.Add(startIdx + i * 4 + j);
                        lineIndices.Add(startIdx + i * 4 + j + 1);

                        lineIndices.Add(startIdx + j * 4 + i);
                        lineIndices.Add(startIdx + (j + 1) * 4 + i);
                    }
                }
            }

            uint[] lineIndexArray = lineIndices.ToArray();
            uint lineBuffer = Gl.GenBuffer();
            Gl.BindBuffer(GLEnum.ElementArrayBuffer, lineBuffer);
            Gl.BufferData(GLEnum.ElementArrayBuffer, (ReadOnlySpan<uint>)lineIndexArray.AsSpan(), GLEnum.StaticDraw);
            Gl.DisableVertexAttribArray(1);
            Gl.VertexAttrib4(1, new System.Numerics.Vector4(0f, 0f, 0f, 1f));
            Gl.DrawElements(GLEnum.Lines, (uint)lineIndexArray.Length, GLEnum.UnsignedInt, null);
            Gl.BindBuffer(GLEnum.ElementArrayBuffer, 0);
            Gl.BindVertexArray(0);

            // always unbound the vertex buffer first, so no halfway results are displayed by accident
            Gl.DeleteBuffer(vertices);
            Gl.DeleteBuffer(colors);
            Gl.DeleteBuffer(indices);
            Gl.DeleteBuffer(lineBuffer);
            Gl.DeleteVertexArray(vao);
            CheckError("Buffer and VAO cleanup");
        }
    }

}
