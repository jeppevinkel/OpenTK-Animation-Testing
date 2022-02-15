using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using BOLL7708;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Windowing.Desktop;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Valve.VR;
using PixelFormat = OpenTK.Graphics.OpenGL.PixelFormat;

namespace OpenTK_Animation_Testing
{
    public class App : GameWindow
    {
        private int _textureId;
        private string _spriteSheetName;
        private int _spriteWidth;
        private int _spriteHeight;
        private Shader _shader;
        private int _animationFrames;
        private int _currentFrame;
        private double _frameInterval;
        private double _elapsedTime;
        private ulong _vrOverlayHandle;

        private int _frameBuffer;
        private int _renderedTexture;
        
        private readonly float[] _vertices =
        {
            // Position         Texture coordinates
            1.0f,  1.0f, 0.0f, 1.0f, 1.0f, // top right
            1.0f, -1.0f, 0.0f, 1.0f, 0.0f, // bottom right
            -1.0f, -1.0f, 0.0f, 0.0f, 0.0f, // bottom left
            -1.0f,  1.0f, 0.0f, 0.0f, 1.0f  // top left
        };
        private readonly uint[] _indices =
        {
            0, 1, 3,
            1, 2, 3
        };

        private int _elementBufferObject;
        private int _vertexArrayObject ;
        private int _vertexBufferObject ;

        public App(int width, int height, string title) : base(new GameWindowSettings(),
            new NativeWindowSettings
            {
                API = ContextAPI.OpenGL, Size = (width, height), Title = title, WindowBorder = WindowBorder.Fixed,
                WindowState = WindowState.Normal, StartFocused = true, StartVisible = true
            })
        {
        }

        protected override void OnLoad()
        {
            base.OnLoad();
            
            GL.ClearColor(0.2f, 0.3f, 0.3f, 1.0f);

            _vrOverlayHandle = EasyOpenVRSingleton.Instance.CreateOverlay("randomkeyiguess", "Anim Test",
                EasyOpenVRSingleton.Utils.GetEmptyTransform());

            if (_vrOverlayHandle == 0)
            {
                throw new Exception("Failed to create VR overlay");
            }

            var anchorIndex = EasyOpenVRSingleton.Instance.GetIndexesForTrackedDeviceClass(ETrackedDeviceClass.HMD)[0];
            var deviceTransform = EasyOpenVRSingleton.Instance.GetDeviceToAbsoluteTrackingPose()[anchorIndex == uint.MaxValue ? 0 : anchorIndex].mDeviceToAbsoluteTracking;
            // var hmdEuler = deviceTransform.EulerAngles();
            // hmdEuler.v2 = 0;
            // hmdEuler.v0 = 0;
            // deviceTransform = deviceTransform.FromEuler(hmdEuler);
            
            EasyOpenVRSingleton.Instance.SetOverlayTransform(_vrOverlayHandle, deviceTransform, uint.MaxValue);

            _vertexArrayObject = GL.GenVertexArray();
            GL.BindVertexArray(_vertexArrayObject);

            _vertexBufferObject = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexBufferObject);
            GL.BufferData(BufferTarget.ArrayBuffer, _vertices.Length * sizeof(float), _vertices, BufferUsageHint.StaticDraw);
            
            _elementBufferObject = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, _elementBufferObject);
            GL.BufferData(BufferTarget.ElementArrayBuffer, _indices.Length * sizeof(uint), _indices, BufferUsageHint.StaticDraw);
            
            _shader = new Shader("Shaders/shader.vert", "Shaders/shader.frag");
            _shader.Use();
            
            var vertexLocation = _shader.GetAttribLocation("aPosition");
            GL.EnableVertexAttribArray(vertexLocation);
            GL.VertexAttribPointer(vertexLocation, 3, VertexAttribPointerType.Float, false, 5 * sizeof(float), 0);
            
            var texCoordLocation = _shader.GetAttribLocation("aTexCoord");
            GL.EnableVertexAttribArray(texCoordLocation);
            GL.VertexAttribPointer(texCoordLocation, 2, VertexAttribPointerType.Float, false, 5 * sizeof(float), 3 * sizeof(float));
            
            _shader.SetInt("tex_index", _currentFrame);
            
            // Render Texture Preparation
            this._frameBuffer = GL.GenFramebuffer();
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, this._frameBuffer);
            
            this._renderedTexture = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, this._renderedTexture);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, this._spriteWidth, this._spriteHeight, 0, PixelFormat.Rgba, PixelType.UnsignedByte, IntPtr.Zero);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            
            GL.FramebufferTexture(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, this._renderedTexture, 0);
            GL.DrawBuffer(DrawBufferMode.ColorAttachment0);
            
            Texture_t tex = new Texture_t
            {
                handle = (IntPtr)_renderedTexture,
                eType = ETextureType.OpenGL,
                eColorSpace = EColorSpace.Auto
            };
            
            var error = OpenVR.Overlay.SetOverlayTexture(_vrOverlayHandle, ref tex);
            // var error = OpenVR.Overlay.SetOverlayFromFile(_vrOverlayHandle, @"C:\Users\Jeppe\Documents\RiderProjects\OpenTK Gif Testing\OpenTK Gif Testing\Tiles\sheet1.png");
            
            if (error != EVROverlayError.None)
            {
                throw new Exception("Failed to set overlay texture, error: " + error);
            }

            OpenVR.Overlay.ShowOverlay(_vrOverlayHandle);
        }

        protected override void OnUpdateFrame(FrameEventArgs args)
        {
            base.OnUpdateFrame(args);

            if (KeyboardState.IsKeyPressed(Keys.Escape))
            {
                Close();
            }
        }

        protected override void OnRenderFrame(FrameEventArgs args)
        {
            base.OnRenderFrame(args);
            _elapsedTime += args.Time;

            if (_elapsedTime > this._frameInterval)
            {
                _elapsedTime = 0;
                _currentFrame = (_currentFrame + 1) % _animationFrames;
                _shader.SetInt("tex_index", _currentFrame);
            }

            GL.Clear(ClearBufferMask.ColorBufferBit);
            
            // GL.BindVertexArray(_vertexArrayObject);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, this._frameBuffer);
            GL.Viewport(0, 0, this._spriteWidth, this._spriteHeight);
            
            _shader.Use();
            
            // GL.DrawBuffer(DrawBufferMode.ColorAttachment0);
            GL.DrawElements(PrimitiveType.Triangles, _indices.Length, DrawElementsType.UnsignedInt, 0);
            
            // Console.WriteLine(OpenVR.Overlay.IsOverlayVisible(_vrOverlayHandle));
            
            SwapBuffers();
            
            Texture_t tex = new Texture_t
            {
                handle = (IntPtr)_renderedTexture,
                eType = ETextureType.OpenGL,
                eColorSpace = EColorSpace.Auto
            };
            
            var error = OpenVR.Overlay.SetOverlayTexture(_vrOverlayHandle, ref tex);
            
            if (error != EVROverlayError.None)
            {
                throw new Exception("Failed to set overlay texture, error: " + error);
            }

            OpenVR.Overlay.ShowOverlay(_vrOverlayHandle);
        }

        public void IncrementAnimation()
        {
            // _vertices[5] += 0.1f % _animationFrames;
            // _vertices[11] += 0.1f % _animationFrames;
            // _vertices[17] += 0.1f % _animationFrames;
            // _vertices[23] += 0.1f % _animationFrames;
        }

        public void SpriteSheet(string filename, int spriteWidth, int spriteHeight, double frameInterval)
        {
            Console.WriteLine("Loading sprite sheet...");
            // Assign ID and get name
            this._textureId = GL.GenTexture();
            this._spriteSheetName = Path.GetFileNameWithoutExtension(filename);
            this._frameInterval = frameInterval;
            this._spriteWidth = spriteWidth;
            this._spriteHeight = spriteHeight;

            // Bind the Texture Array and set appropriate parameters
            GL.BindTexture(TextureTarget.Texture2DArray, _textureId);
            GL.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureMinFilter,
                (int) TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureMagFilter,
                (int) TextureMagFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureWrapS,
                (int) TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureWrapT,
                (int) TextureWrapMode.ClampToEdge);

            // Load the image file
            Bitmap image = new Bitmap(@"Tiles/" + filename);
            image.RotateFlip(RotateFlipType.RotateNoneFlipY);
            BitmapData data = image.LockBits(new System.Drawing.Rectangle(0, 0, image.Width, image.Height),
                ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            
            // Determine columns and rows
            int spriteSheetwidth = image.Width;
            int spriteSheetheight = image.Height;
            int columns = spriteSheetwidth / spriteWidth;
            int rows = spriteSheetheight / spriteHeight;

            _animationFrames = columns * rows;
            
            // Allocate storage
            GL.TexStorage3D(TextureTarget3d.Texture2DArray, 1, SizedInternalFormat.Rgba8, spriteWidth, spriteHeight,
                rows * columns);
            
            // Split the loaded image into individual Texture2D slices
            GL.PixelStore(PixelStoreParameter.UnpackRowLength, spriteSheetwidth);
            for (int i = 0; i < columns * rows; i++)
            {
                GL.TexSubImage3D(TextureTarget.Texture2DArray,
                    0, 0, 0, i, spriteWidth, spriteHeight, 1,
                    OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte,
                    data.Scan0 + (spriteWidth * 4 * (i % columns)) +
                    (spriteSheetwidth * 4 * spriteHeight * (i / columns))); // 4 bytes in an Bgra value.
            }
            
            image.UnlockBits(data);
        }
    }
}