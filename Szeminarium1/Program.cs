using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ImGui;
using Silk.NET.Windowing;
using System.Numerics;
using System.Reflection;
using Szeminarium;

namespace GrafikaSzeminarium
{
    internal class Program
    {
        private static IWindow graphicWindow;

        private static GL Gl;

        private static ImGuiController imGuiController;

        private static CameraDescriptor camera = new CameraDescriptor();

        private const string ModelMatrixVariableName = "uModel";
        private const string NormalMatrixVariableName = "uNormal";
        private const string ViewMatrixVariableName = "uView";
        private const string ProjectionMatrixVariableName = "uProjection";

        private const string LightColorVariableName = "uLightColor";
        private const string LightPositionVariableName = "uLightPos";
        private const string ViewPositionVariableName = "uViewPos";

        private const string ShinenessVariableName = "uShininess";

        private static float shininess = 50;

        private static uint program;

        private static uint phongProgram;

        private static uint gouraudProgram;

        private static int shadingMode = 0;

        private const int SegmentCount = 18;
        private const float RectangleWidth = 1f;

        private static ModelObjectDescriptor flatRectangle;
        private static ModelObjectDescriptor smoothRectangle;
        static void Main(string[] args)
        {
            WindowOptions windowOptions = WindowOptions.Default;
            windowOptions.Title = "Grafika szeminárium";
            windowOptions.Size = new Silk.NET.Maths.Vector2D<int>(500, 500);

            graphicWindow = Window.Create(windowOptions);

            graphicWindow.Load += GraphicWindow_Load;
            graphicWindow.Update += GraphicWindow_Update;
            graphicWindow.Render += GraphicWindow_Render;
            graphicWindow.Closing += GraphicWindow_Closing;

            graphicWindow.Run();
        }

        private static void GraphicWindow_Closing()
        {
            flatRectangle?.Dispose();
            smoothRectangle?.Dispose();
            Gl.DeleteProgram(phongProgram);
            Gl.DeleteProgram(gouraudProgram);
            imGuiController.Dispose();
        }

        private static void GraphicWindow_Load()
        {
            Gl = graphicWindow.CreateOpenGL();

            float halfAngle = MathF.PI / SegmentCount;

            flatRectangle = ModelObjectDescriptor.CreateRectangle(Gl, 0f);
            smoothRectangle = ModelObjectDescriptor.CreateRectangle(Gl, halfAngle);

            var inputContext = graphicWindow.CreateInput();
            foreach (var keyboard in inputContext.Keyboards)
            {
                keyboard.KeyDown += Keyboard_KeyDown;
            }

            // Handle resizes
            graphicWindow.FramebufferResize += s =>
            {
                // Adjust the viewport to the new window size
                Gl.Viewport(s);
            };



            imGuiController = new ImGuiController(Gl, graphicWindow, inputContext);


            Gl.ClearColor(System.Drawing.Color.White);
            
            //Gl.Enable(EnableCap.CullFace);
            //Gl.CullFace(TriangleFace.Back);

            Gl.Enable(EnableCap.DepthTest);
            Gl.DepthFunc(DepthFunction.Lequal);

            phongProgram = CreateShaderProgram("Shaders.VertexShader.vert", "Shaders.FragmentShader.frag");

            gouraudProgram = CreateShaderProgram("Shaders.GouraudVertexShader.vert", "Shaders.GouraudFragmentShader.frag");
            program = phongProgram;
        }

        private static uint CreateShaderProgram(string vertexPath, string fragmentPath)
        {
            uint vshader = Gl.CreateShader(ShaderType.VertexShader);
            uint fshader = Gl.CreateShader(ShaderType.FragmentShader);

            Gl.ShaderSource(vshader, GetEmbeddedResourceAsString(vertexPath));
            Gl.CompileShader(vshader);

            Gl.GetShader(vshader, ShaderParameterName.CompileStatus, out int vStatus);
            if(vStatus != (int)GLEnum.True)
            {
                throw new Exception("Vertex shader failed to compile: " + Gl.GetShaderInfoLog(vshader));
            }

            Gl.ShaderSource(fshader, GetEmbeddedResourceAsString(fragmentPath));
            Gl.CompileShader(fshader);
            Gl.GetShader(fshader, ShaderParameterName.CompileStatus, out int fStatus);
            if (fStatus != (int)GLEnum.True)
            {
                throw new Exception("Vertex shader failed to compile: " + Gl.GetShaderInfoLog(fshader));
            }

            uint shaderProgram = Gl.CreateProgram();
            Gl.AttachShader(shaderProgram, vshader);
            Gl.AttachShader(shaderProgram, fshader);
            Gl.LinkProgram(shaderProgram);

            Gl.GetProgram(shaderProgram, GLEnum.LinkStatus, out int status);
            if(status == 0)
            {
                throw new Exception("Shader linking failed: " + Gl.GetProgramInfoLog(shaderProgram));
            }

            Gl.DetachShader(shaderProgram, vshader);
            Gl.DetachShader(shaderProgram, fshader);
            Gl.DeleteShader(vshader);
            Gl.DeleteShader(fshader);

            return shaderProgram;
        }

        private static string GetEmbeddedResourceAsString(string resourceRelativePath)
        {
            string resourceFullPath = Assembly.GetExecutingAssembly().GetName().Name + "." + resourceRelativePath;

            using (var resStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceFullPath))
            using (var resStreamReader = new StreamReader(resStream))
            {
                var text = resStreamReader.ReadToEnd();
                return text;
            }
        }

        private static void Keyboard_KeyDown(IKeyboard keyboard, Key key, int arg3)
        {
            switch (key)
            {
                case Key.Left:
                    camera.DecreaseZYAngle();
                    break;
                case Key.Right:
                    camera.IncreaseZYAngle();
                    break;
                case Key.Down:
                    camera.IncreaseDistance();
                    break;
                case Key.Up:
                    camera.DecreaseDistance();
                    break;
                case Key.U:
                    camera.IncreaseZXAngle();
                    break;
                case Key.D:
                    camera.DecreaseZXAngle();
                    break;
            }
        }

        private static void GraphicWindow_Update(double deltaTime)
        {
            imGuiController.Update((float)deltaTime);
        }

        private static unsafe void GraphicWindow_Render(double deltaTime)
        {
            Gl.Clear(ClearBufferMask.ColorBufferBit);
            Gl.Clear(ClearBufferMask.DepthBufferBit);

            if(shadingMode == 0)
            {
                program = phongProgram;
            } else
            {
                program = gouraudProgram;
            }

            Gl.UseProgram(program);

            SetUniform3(LightColorVariableName, new Vector3(1f, 1f, 1f));
            SetUniform3(LightPositionVariableName, new Vector3(camera.Position.X, camera.Position.Y, camera.Position.Z));
            SetUniform3(ViewPositionVariableName, new Vector3(camera.Position.X, camera.Position.Y, camera.Position.Z));
            SetUniform1(ShinenessVariableName, shininess);

            var viewMatrix = Matrix4X4.CreateLookAt(camera.Position, camera.Target, camera.UpVector);
            SetMatrix(viewMatrix, ViewMatrixVariableName);

            var projectionMatrix = Matrix4X4.CreatePerspectiveFieldOfView<float>((float)(Math.PI / 3), 500f / 500f, 0.1f, 100f);
            SetMatrix(projectionMatrix, ProjectionMatrixVariableName);


            float radius = GetTubRadius();
            float gap = 2f;

            float centerDistance = 2f * (radius + RectangleWidth / 2f) + gap;

            DrawTub(flatRectangle, -centerDistance / 2f);
            DrawTub(smoothRectangle, centerDistance / 2f);

            //ImGuiNET.ImGui.ShowDemoWindow();
            ImGuiNET.ImGui.Begin("Lighting", ImGuiNET.ImGuiWindowFlags.AlwaysAutoResize | ImGuiNET.ImGuiWindowFlags.NoCollapse);
            ImGuiNET.ImGui.SliderFloat("Shininess", ref shininess, 5, 100);

            ImGuiNET.ImGui.Separator();
            ImGuiNET.ImGui.Text("Shading Model");

            ImGuiNET.ImGui.RadioButton("Phong", ref shadingMode, 0);
            ImGuiNET.ImGui.RadioButton("Gouraud", ref shadingMode, 1);


            ImGuiNET.ImGui.End();

            imGuiController.Render();
        }

        private static unsafe void SetModelMatrix(Matrix4X4<float> modelMatrix)
        {
            SetMatrix(modelMatrix, ModelMatrixVariableName);

            // set also the normal matrix
            int location = Gl.GetUniformLocation(program, NormalMatrixVariableName);
            if (location == -1)
            {
                throw new Exception($"{NormalMatrixVariableName} uniform not found on shader.");
            }

            // G = (M^-1)^T
            var modelMatrixWithoutTranslation = new Matrix4X4<float>(modelMatrix.Row1, modelMatrix.Row2, modelMatrix.Row3, modelMatrix.Row4);
            modelMatrixWithoutTranslation.M41 = 0;
            modelMatrixWithoutTranslation.M42 = 0;
            modelMatrixWithoutTranslation.M43 = 0;
            modelMatrixWithoutTranslation.M44 = 1;

            Matrix4X4<float> modelInvers;
            Matrix4X4.Invert<float>(modelMatrixWithoutTranslation, out modelInvers);
            Matrix3X3<float> normalMatrix = new Matrix3X3<float>(Matrix4X4.Transpose(modelInvers));

            Gl.UniformMatrix3(location, 1, false, (float*)&normalMatrix);
            CheckError();
        }

        private static unsafe void SetUniform1(string uniformName, float uniformValue)
        {
            int location = Gl.GetUniformLocation(program, uniformName);
            if (location == -1)
            {
                throw new Exception($"{uniformName} uniform not found on shader.");
            }

            Gl.Uniform1(location, uniformValue);
            CheckError();
        }

        private static unsafe void SetUniform3(string uniformName, Vector3 uniformValue)
        {
            int location = Gl.GetUniformLocation(program, uniformName);
            if (location == -1)
            {
                throw new Exception($"{uniformName} uniform not found on shader.");
            }

            Gl.Uniform3(location, uniformValue);
            CheckError();
        }

        private static unsafe void DrawModelObject(ModelObjectDescriptor modelObject)
        {
            Gl.BindVertexArray(modelObject.Vao);
            Gl.BindBuffer(GLEnum.ElementArrayBuffer, modelObject.Indices);
            Gl.DrawElements(PrimitiveType.Triangles, modelObject.IndexArrayLength, DrawElementsType.UnsignedInt, null);
            Gl.BindBuffer(GLEnum.ElementArrayBuffer, 0);
            Gl.BindVertexArray(0);
        }

        private static unsafe void SetMatrix(Matrix4X4<float> mx, string uniformName)
        {
            int location = Gl.GetUniformLocation(program, uniformName);
            if (location == -1)
            {
                throw new Exception($"{uniformName} uniform not found on shader.");
            }

            Gl.UniformMatrix4(location, 1, false, (float*)&mx);
            CheckError();
        }

        public static void CheckError()
        {
            var error = (ErrorCode)Gl.GetError();
            if (error != ErrorCode.NoError)
                throw new Exception("GL.GetError() returned " + error.ToString());
        }

        private static float GetTubRadius()
        {
            float halfAngle = MathF.PI / SegmentCount;
            return (RectangleWidth / 2f) / MathF.Tan(halfAngle);
        }
        private static void DrawTub(ModelObjectDescriptor rectangle, float centerX)
        {
            float angleStep = 2f * MathF.PI / SegmentCount;
            float radius = GetTubRadius();
            for(int i = 0; i < SegmentCount; i++)
            {
                float angle = i * angleStep;
                Matrix4X4<float> translation = Matrix4X4.CreateTranslation(0f, 0f, radius);
                Matrix4X4<float> rotation = Matrix4X4.CreateRotationY(angle);
                Matrix4X4<float> positioning = Matrix4X4.CreateTranslation(centerX, 0f, 0f);
                Matrix4X4<float> modelMatrix = translation * rotation * positioning;
                SetModelMatrix(modelMatrix);
                DrawModelObject(rectangle);
            }
        }
    }
}