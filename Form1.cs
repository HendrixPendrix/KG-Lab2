using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing.Imaging;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using System.IO;

namespace KG2
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            trackBar2.Maximum = 1000;
            trackBar3.Maximum = 3000;
            trackBar3.Minimum = 500;
            
        }
        Bin bin = new Bin();
        View view = new View();
        bool loaded = false;
        int curLayer;
        int FrameCount;
        DateTime NextFPSUp = DateTime.Now.AddSeconds(1);
        bool needReload = false;


        private void открытьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                string str = dialog.FileName;
                bin.readBin(str);
                view.SetupView(glControl1.Width, glControl1.Height);
                trackBar1.Maximum = Bin.Z - 1;
                loaded = true;
                glControl1.Invalidate();

            }
        }

        private void glControl1_Paint(object sender, PaintEventArgs e)
        {
            if (loaded)
            {
                //view.DrawQuads(curLayer);
                if (radioButton1.Checked == true)
                {
                    view.DrawQuadStrip(curLayer, trackBar2.Value, trackBar3.Value);
                    glControl1.SwapBuffers();
                }
                if (radioButton3.Checked == true)
                {
                    view.DrawQuads(curLayer, trackBar2.Value, trackBar3.Value);
                    glControl1.SwapBuffers();
                }
                if (radioButton2.Checked == true)
                {
                    if (needReload)
                    {
                        view.generateTextureImage(curLayer,trackBar2.Value, trackBar3.Value);
                        view.Load2dTexture();
                        needReload = false;
                    }
                    view.DrawTexture();
                    glControl1.SwapBuffers();

                    view.DrawQuads(curLayer, trackBar2.Value, trackBar3.Value);
                    glControl1.SwapBuffers();
                }
            }
        }

        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            curLayer = trackBar1.Value;
            needReload = true;
        }

        void Application_Idle(object sender, EventArgs e)
        {
            while (glControl1.IsIdle)
            {
                displayFPS();
                glControl1.Invalidate();
            }
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            Application.Idle += Application_Idle;
        }
        void displayFPS()
        {
            if (DateTime.Now >= NextFPSUp)
            {
                this.Text = String.Format("CT Visualizer(fps={0})", FrameCount);
                NextFPSUp = DateTime.Now.AddSeconds(1);
                FrameCount = 0;
            }
            FrameCount++;
        }

        private void Form1_Load_1(object sender, EventArgs e)
        {

        }

        private void trackBar2_Scroll(object sender, EventArgs e)
        {
            //view.window = trackBar3.Value;
            needReload = true;
        }

        private void trackBar3_Scroll(object sender, EventArgs e)
        {
            //view.minimum = trackBar2.Value;
            needReload = true;
        }

        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {

        }
    }

    class Bin
    {
        public static int X, Y, Z;
        public static short[] arr;
        public Bin() { }

        public void readBin(string path)
        {
            if (File.Exists(path))
            {
                BinaryReader reader = new BinaryReader(File.Open(path, FileMode.Open));
                X = reader.ReadInt32();
                Y = reader.ReadInt32();
                Z = reader.ReadInt32();
                int ArrSize = X * Y * Z;
                arr = new short[ArrSize];
                for (int i = 0; i < ArrSize; i++)
                    arr[i] = reader.ReadInt16();
            }
        }
    }
    class View
    {
        int VBOtexture;
        Bitmap textureImage;
        public int minimum = 0;
        public int window = 2000;
        public void SetupView(int w, int h)
        {
            GL.ShadeModel(ShadingModel.Smooth);
            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadIdentity();
            GL.Ortho(0, Bin.X, 0, Bin.Y, -1, 1);
            GL.Viewport(0, 0, w, h);
        }
        public int Clamp(int val, int min, int max)
        {
            if (val < min)
                return min;
            if (val > max)
                return max;
            return val;
        }
        Color TransferFunction(short val,int min,int max)
        {
  
            int newVal = Clamp((val - min) * 255 / max, 0, 255);
            return Color.FromArgb(255, newVal, newVal, newVal);
        }

        public void DrawQuads(int NumLayer, int min, int max)
        {
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            GL.Begin(BeginMode.Quads);
            for (int x = 0; x < Bin.X - 1; x++)
                for (int y = 0; y < Bin.Y - 1; y++)
                {
                    short val;
                    //1
                    val = Bin.arr[x + y * Bin.X + NumLayer * Bin.X * Bin.Y];
                    GL.Color3(TransferFunction(val, min, max));
                    GL.Vertex2(x, y);
                    //2
                    val = Bin.arr[x + (y + 1) * Bin.X + NumLayer * Bin.X * Bin.Y];
                    GL.Color3(TransferFunction(val, min, max));
                    GL.Vertex2(x, y + 1);
                    //3
                    val = Bin.arr[x + 1 + (y + 1) * Bin.X + NumLayer * Bin.X * Bin.Y];
                    GL.Color3(TransferFunction(val, min, max));
                    GL.Vertex2(x + 1, y + 1);
                    //4
                    val = Bin.arr[x + 1 + y * Bin.X + NumLayer * Bin.X * Bin.Y];
                    GL.Color3(TransferFunction(val, min, max));
                    GL.Vertex2(x + 1, y);
                }
            GL.End();
        }
        public void Load2dTexture()
        {
            GL.BindTexture(TextureTarget.Texture2D, VBOtexture);
            BitmapData data = textureImage.LockBits(
                new System.Drawing.Rectangle(0, 0, textureImage.Width, textureImage.Height),
                ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba,
                data.Width, data.Height, 0, OpenTK.Graphics.OpenGL.PixelFormat.Bgra,
                PixelType.UnsignedByte, data.Scan0);
            textureImage.UnlockBits(data);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter,
                (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter,
               (int)TextureMagFilter.Linear);
            ErrorCode Er = GL.GetError();
            string str = Er.ToString();
        }
        public void generateTextureImage(int layerNumber,int min,int max)
        {
            textureImage = new Bitmap(Bin.X, Bin.Y);
            for (int i = 0; i < Bin.X; ++i)
                for (int j = 0; j < Bin.Y; ++j)
                {
                    int pixelNum = i + j * Bin.X + layerNumber * Bin.X * Bin.Y;
                    textureImage.SetPixel(i, j, TransferFunction(Bin.arr[pixelNum],min,max));
                }
        }
        public void DrawTexture()
        {
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            GL.Enable(EnableCap.Texture2D);
            GL.BindTexture(TextureTarget.Texture2D, VBOtexture);
            GL.Begin(BeginMode.Quads);
            GL.Color3(Color.White);
            GL.TexCoord2(0f, 0f);
            GL.Vertex2(0, 0);
            GL.TexCoord2(0f, 1f);
            GL.Vertex2(0, Bin.Y);
            GL.TexCoord2(1f, 1f);
            GL.Vertex2(Bin.X, Bin.Y);
            GL.TexCoord2(1f, 0f);
            GL.Vertex2(Bin.X, 0);
            GL.End();
            GL.Disable(EnableCap.Texture2D);
        }
        public void DrawQuadStrip(int layerNum,int min,int max)
        {
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            for (int x_coord = 0; x_coord < Bin.X - 1; x_coord++)
            {
                GL.Begin(BeginMode.QuadStrip);
                short value;

                value = Bin.arr[x_coord + 0 * Bin.X + layerNum * Bin.X * Bin.Y];
                GL.Color3(TransferFunction(value, min, max));
                GL.Vertex2(x_coord, 0);

                value = Bin.arr[x_coord + 1 + 0 * Bin.X + layerNum * Bin.X * Bin.Y];
                GL.Color3(TransferFunction(value, min, max));
                GL.Vertex2(x_coord + 1, 0);

                for (int y_coord = 1; y_coord < Bin.Y - 1; y_coord++)
                {
                    value = Bin.arr[x_coord + 1 + (y_coord + 1) * Bin.X + layerNum * Bin.X * Bin.Y];
                    GL.Color3(TransferFunction(value, min, max));
                    GL.Vertex2(x_coord + 1, y_coord + 1);

                    value = Bin.arr[x_coord + (y_coord + 1) * Bin.X + layerNum * Bin.X * Bin.Y];
                    GL.Color3(TransferFunction(value, min, max));
                    GL.Vertex2(x_coord, y_coord + 1);
                }
                GL.End();
            }
        }
    }

}




