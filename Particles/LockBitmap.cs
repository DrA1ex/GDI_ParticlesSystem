using System;
using System.Drawing;
using System.Drawing.Imaging;

namespace Particles
{
    public class LockBitmap
    {
        private static BitmapData _bitmapData;
        private readonly int _dataLength;
        private readonly Bitmap _source;
        private unsafe uint* _data = (uint*)IntPtr.Zero;

        private IntPtr _ptr = IntPtr.Zero;

        public LockBitmap(Bitmap source)
        {
            _source = source;

            Width = _source.Width;
            Height = _source.Height;
            _dataLength = Width * Height;
        }

        public int Height { get; }

        public int Width { get; }

        private int Blend(int bottom, int top)
        {
            int r = (bottom >> 16) & 0xff;
            int g = (bottom >> 8) & 0xff;
            int b = bottom & 0xff;
            int num4 = (top >> 16) & 0xff;
            int num5 = (top >> 8) & 0xff;
            int num6 = top >> 0xff;
            var num7 = num4 < 0x80 ? r + 2 * (num4 - 0x80) : r + 2 * num4 - 0xff;
            var num8 = num5 < 0x80 ? g + 2 * (num5 - 0x80) : g + 2 * num5 - 0xff;
            var num9 = num6 < 0x80 ? b + 2 * (num6 - 0x80) : b + 2 * num6 - 0xff;
            if(num7 > 0xff)
            {
                num7 = 0xff;
            }
            else if(num7 < 0)
            {
                num7 = 0;
            }
            if(num8 > 0xff)
            {
                num8 = 0xff;
            }
            else if(num8 < 0)
            {
                num8 = 0;
            }
            if(num9 > 0xff)
            {
                num9 = 0xff;
            }
            else if(num9 < 0)
            {
                num9 = 0;
            }

            unchecked
            {
                return (int)0xff000000L | (num7 << 0x10) | (num8 << 8) | num9;
            }
        }

        public void LockBits()
        {
            var rect = new Rectangle(0, 0, Width, Height);
            _bitmapData = _source.LockBits(rect, ImageLockMode.ReadWrite, _source.PixelFormat);
            _ptr = _bitmapData.Scan0;
            unsafe
            {
                _data = (uint*)_ptr;
            }
        }

        public void SetPixel(int x, int y, uint color)
        {
            var index = y * Width + x;
            unsafe
            {
                _data[index] = (uint)Blend((int)_data[index], (int)color);
            }
        }

        public void UnlockBits()
        {
            _source.UnlockBits(_bitmapData);
            _ptr = IntPtr.Zero;
            _bitmapData = null;

            unsafe
            {
                _data = (uint*)IntPtr.Zero;
            }
        }

        public void Fill(uint color)
        {
            unsafe
            {
                for(var point = _data; point < _data + _dataLength; ++point)
                {
                    *point = color;
                }
            }
        }
    }
}