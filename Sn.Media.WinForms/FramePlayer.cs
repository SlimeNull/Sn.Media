using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Drawing.Imaging;
using System.Numerics;

namespace Sn.Media.WinForms
{
    public class FramePlayer : Control
    {
        private bool _isChangingTimeByPlayLogic;
        private byte[]? _frameDataBuffer;
        private bool _hasBufferedFrame;
        private TimeSpan _bufferedFrameTime;
        private Bitmap? _frameImageBuffer;

        private TimeSpan _startPlayTime;
        private Stopwatch? _stopwatchFromPlayStarted;

        // control property fields
        private System.Windows.Forms.Timer _timer;
        private IFrameStream? _source;
        private FramePlayerStretch _stretch = FramePlayerStretch.Uniform;
        private TimeSpan _position;
        private bool _isPlaying;

        public IFrameStream? Source
        {
            get => _source;
            set
            {
                _source = value;

                if (value is IFrameStream { CanSeek: true } seekableFrameStream)
                {
                    seekableFrameStream.Seek(Position);
                }

                Invalidate();
            }
        }

        public bool IsPlaying
        {
            get => _isPlaying;
            set
            {
                _isPlaying = value;

                if (value)
                {
                    _startPlayTime = Position;
                    _stopwatchFromPlayStarted = Stopwatch.StartNew();
                    _timer.Start();
                }
                else
                {
                    _timer.Stop();
                }
            }
        }

        public TimeSpan Position
        {
            get => _position;
            set
            {
                _position = value;

                if (!_isChangingTimeByPlayLogic)
                {
                    _startPlayTime = value;
                    _stopwatchFromPlayStarted?.Restart();
                    _hasBufferedFrame = false;

                    if (Source is { CanSeek: true } seekableSource)
                    {
                        seekableSource.Seek(value);
                    }
                }
            }
        }

        public TimeSpan Duration
        {
            get
            {
                if (Source is null)
                {
                    throw new InvalidOperationException("No source specified");
                }
                if (!Source.HasDuration)
                {
                    throw new InvalidOperationException("Source has no duration specified");
                }

                return Source.Duration;
            }
        }

        public FramePlayerStretch Stretch
        {
            get => _stretch;
            set
            {
                _stretch = value;
                Invalidate();
            }
        }

        public FramePlayer()
        {
            DoubleBuffered = true;

            _timer = new System.Windows.Forms.Timer();
            _timer.Interval = 5;
            _timer.Tick += TimerTick;
        }

        private void TimerTick(object? sender, EventArgs e)
        {
            var length = Duration;
            var position = _startPlayTime + _stopwatchFromPlayStarted!.Elapsed;
            if (position > length)
            {
                position = length;
            }

            _isChangingTimeByPlayLogic = true;
            Position = position;
            _isChangingTimeByPlayLogic = false;

            var source = Source;
            var isPlaying = IsPlaying;

            if (!isPlaying ||
                source is null)
            {
                return;
            }

            EnsureFrameBuffer(source);

            bool filled = false;
            bool frameUpdated = false;
            while (true)
            {
                if (!_hasBufferedFrame)
                {
                    _hasBufferedFrame = source.Read(_frameDataBuffer, out _bufferedFrameTime);
                }

                // 有帧
                if (_hasBufferedFrame)
                {
                    if (_bufferedFrameTime <= position)
                    {
#if DEBUG
                        if (filled)
                        {
                            Debug.WriteLine("Skip Frame");
                        }
#endif

                        FillFrameImage(source, _frameImageBuffer, _frameDataBuffer);
                        _hasBufferedFrame = false;
                        filled = true;
                        frameUpdated = true;
                    }
                    else
                    {
                        break;
                    }
                }
                else
                {
                    if (!filled)
                    {
                        ClearFrameImage(_frameImageBuffer);
                        frameUpdated = true;
                    }

                    break;
                }
            }

            if (frameUpdated)
            {
                Invalidate();
            }
        }

        [MemberNotNull(nameof(_frameDataBuffer))]
        [MemberNotNull(nameof(_frameImageBuffer))]
        private void EnsureFrameBuffer(IFrameStream frameStream)
        {
            var pixelFormat = frameStream.Format.ToPixelFormat();

            if (_frameDataBuffer is null ||
                _frameDataBuffer.Length != frameStream.FrameDataSize)
            {
                _frameDataBuffer = new byte[frameStream.FrameDataSize];
            }

            if (_frameImageBuffer is null ||
                _frameImageBuffer.Width != frameStream.FrameWidth ||
                _frameImageBuffer.Height != frameStream.FrameHeight ||
                _frameImageBuffer.PixelFormat != pixelFormat)
            {
                _frameImageBuffer = new Bitmap(
                    frameStream.FrameWidth,
                    frameStream.FrameHeight,
                    pixelFormat);
            }
        }

        private unsafe void FillFrameImage(IFrameStream frameStream, Bitmap writeableBitmap, byte[] frameBuffer)
        {
            var bitmapData = writeableBitmap.LockBits(new Rectangle(0, 0, writeableBitmap.Width, writeableBitmap.Height), ImageLockMode.WriteOnly, writeableBitmap.PixelFormat);
            var buffer = (byte*)bitmapData.Scan0;
            var stride = Math.Min(frameStream.FrameStride, bitmapData.Stride);
            var height = bitmapData.Height;

            fixed (byte* bufferPtr = frameBuffer)
            {
                for (int y = 0; y < writeableBitmap.Height; y++)
                {
#if NET8_0_OR_GREATER
                    NativeMemory.Copy(bufferPtr + y * frameStream.FrameStride,
                                      buffer + y * stride,
                                      (nuint)stride);
#else
                    Buffer.MemoryCopy(bufferPtr + y * frameStream.FrameStride,
                                      buffer + y * stride,
                                      stride,
                                      stride);
#endif
                }
            }

            writeableBitmap.UnlockBits(bitmapData);
        }

        private unsafe void ClearFrameImage(Bitmap writeableBitmap)
        {
            var bitmapData = writeableBitmap.LockBits(new Rectangle(0, 0, writeableBitmap.Width, writeableBitmap.Height), ImageLockMode.WriteOnly, writeableBitmap.PixelFormat);
#if NET8_0_OR_GREATER
            NativeMemory.Clear((void*)bitmapData.Scan0, (nuint)(bitmapData.Stride * bitmapData.Height));
#else
            new Span<byte>((void*)bitmapData.Scan0, bitmapData.Stride * bitmapData.Height).Clear();
#endif

            writeableBitmap.UnlockBits(bitmapData);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var source = Source;
            var stretch = Stretch;

            base.OnPaint(e);

            if (source is null ||
                _frameImageBuffer is null)
            {
                return;
            }

            if (stretch == FramePlayerStretch.None)
            {
                e.Graphics.DrawImage(_frameImageBuffer, new RectangleF(0, 0, _frameImageBuffer.Width, _frameImageBuffer.Height));
            }
            else if (stretch == FramePlayerStretch.Fill)
            {
                e.Graphics.DrawImage(_frameImageBuffer, new RectangleF(0, 0, Width, Height));
            }
            else if (stretch == FramePlayerStretch.Uniform)
            {
                var aspectRatio = (float)_frameImageBuffer.Width / _frameImageBuffer.Height;
                var renderAspectRatio = Width / Height;
                float width, height;
                if (aspectRatio > renderAspectRatio)
                {
                    width = Width;
                    height = Width / aspectRatio;
                }
                else
                {
                    width = Height * aspectRatio;
                    height = Height;
                }

                e.Graphics.DrawImage(_frameImageBuffer, new RectangleF((Width - width) / 2, (Height - height) / 2, width, height));
            }
            else if (stretch == FramePlayerStretch.UniformToFill)
            {
                var aspectRatio = (float)_frameImageBuffer.Width / _frameImageBuffer.Height;
                var renderAspectRatio = Width / Height;
                float width, height;
                if (aspectRatio > renderAspectRatio)
                {
                    width = Height * aspectRatio;
                    height = Height;
                }
                else
                {
                    width = Width;
                    height = Width / aspectRatio;
                }

                e.Graphics.DrawImage(_frameImageBuffer, new RectangleF((Width - width) / 2, (Height - height) / 2, width, height));
            }
        }

    }
}
