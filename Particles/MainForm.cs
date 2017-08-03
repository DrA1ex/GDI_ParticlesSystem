using System;
using System.Collections.Concurrent;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Particles
{
    public partial class MainForm : Form
    {
        private readonly uint _particleColor = (uint)Color.LightSalmon.ToArgb();
        private Bitmap _canvas;
        private bool _init;
        private LockBitmap _lck;
        private Particle[] _particles;

        private OrderablePartitioner<Tuple<int, int>> _partitioner;

        private ulong _pass;

        public MainForm()
        {
            InitializeComponent();
            CheckForIllegalCrossThreadCalls = false;

            SetStyle(ControlStyles.OptimizedDoubleBuffer
                     | ControlStyles.AllPaintingInWmPaint
                     | ControlStyles.UserPaint
                     | ControlStyles.Opaque
                     | ControlStyles.AllPaintingInWmPaint,
                true);
            SetStyle(ControlStyles.DoubleBuffer, false);
        }

        public int AreaHeight { get; set; }

        public int AreaWidth { get; set; }

        [DllImport("user32.dll")]
        public static extern int PeekMessage(out NativeMessage message, IntPtr window, uint filterMin, uint filterMax, uint remove);

        private bool IsApplicationIdle()
        {
            NativeMessage result;
            return PeekMessage(out result, IntPtr.Zero, 0, 0, 0) == 0;
        }

        private void HandleApplicationIdle(object sender, EventArgs e)
        {
            while(IsApplicationIdle())
            {
                Animate();
                DrawParticles();
            }
        }

        private void Animate()
        {
            var point = PointToClient(Cursor.Position);
            var x = point.X;
            var y = point.Y;

            Parallel.ForEach(_partitioner, range =>
            {
                for(var i = range.Item1; i < range.Item2; i++)
                {
                    AnimateImpl(ref _particles[i], x, y);
                }
            });
        }

        private void AnimateImpl(ref Particle particle, int x, int y)
        {
            double num3;
            double num = particle.X;
            double num2 = particle.Y;
            var num4 = num - x;
            var num5 = num2 - y;
            var num6 = num4 * num4 + num5 * num5;
            if(num6 >= 400.0)
            {
                num3 = 16.673847198486328 / num6;
            }
            else
            {
                num3 = 0.0;
            }
            var num7 = (x - num) * num3;
            var num8 = (y - num2) * num3;
            particle.Xv *= 0.99750000238418579;
            particle.Yv *= 0.99750000238418579;
            particle.Xv += num7;
            particle.Yv += num8;
            particle.X += (int)particle.Xv;
            particle.Y += (int)particle.Yv;
            var flag = false;
            var flag2 = false;

            if(particle.X >= AreaWidth)
            {
                flag = true;
                particle.X = AreaWidth - (particle.X - AreaWidth) - 1;
            }
            else if(particle.X < 0)
            {
                flag = true;
                particle.X = -particle.X;
            }
            if(particle.Y >= AreaHeight)
            {
                flag2 = true;
                particle.Y = AreaHeight - (particle.Y - AreaHeight) - 1;
            }
            else if(particle.Y < 0)
            {
                flag2 = true;
                particle.Y = -particle.Y;
            }

            if(flag)
            {
                particle.Xv = -particle.Xv * 0.5;
            }
            if(flag2)
            {
                particle.Yv = -particle.Yv * 0.5;
            }
        }

        private void DrawParticles()
        {
            _lck.LockBits();
            _lck.Fill(0xff000000);

            if(_pass++ % 5000 == 0)
            {
                Array.Sort(_particles, (a, b) =>
                {
                    var yDiff = a.Y - b.Y;
                    return yDiff == 0 ? a.X - b.X : yDiff;
                });
            }

            Parallel.ForEach(_partitioner, range =>
            {
                for(var j = range.Item1; j < range.Item2; j++)
                {
                    RenderParticle(j);
                }
            });

            _lck.UnlockBits();
            Invalidate();
        }

        private void Form1Paint(object sender, PaintEventArgs e)
        {
            try
            {
                e.Graphics.DrawImageUnscaled(_canvas, 0, 0);
            }
            catch
            {
                //ignore
            }
        }

        private void Form1Shown(object sender, EventArgs e)
        {
            if(!_init)
            {
                AreaWidth = Width;
                AreaHeight = Height;
                _init = true;
                _canvas = new Bitmap(AreaWidth, AreaHeight);
                Init();
                DrawParticles();
            }
        }

        private void Init()
        {
            var random = new Random();
            _particles = new Particle[1000000];
            for(var i = 0; i < _particles.Length; i++)
            {
                _particles[i] = new Particle
                {
                    X = random.Next(AreaWidth),
                    Y = random.Next(AreaHeight)
                };
            }

            _lck = new LockBitmap(_canvas);
            _partitioner = Partitioner.Create(0, _particles.Length, Math.Max(_particles.Length / Environment.ProcessorCount / 2, 100));

            Application.Idle += HandleApplicationIdle;
        }

        private void RenderParticle(int i)
        {
            ref var particle = ref _particles[i];
            _lck.SetPixel(particle.X, particle.Y, _particleColor);
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct NativeMessage
        {
            public IntPtr Handle;
            public uint Message;
            public IntPtr WParameter;
            public IntPtr LParameter;
            public uint Time;
            public Point Location;
        }
    }
}