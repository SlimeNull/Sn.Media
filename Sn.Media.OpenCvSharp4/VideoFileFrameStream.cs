﻿
using System.Runtime.InteropServices;
using OpenCvSharp;
using PropertyChanged;
using PropertyChanging;

namespace Sn.Media.OpenCvSharp4
{
    [ImplementPropertyChanging]
    [AddINotifyPropertyChangedInterface]
    public class VideoFileFrameStream : IFrameStream, IDisposable
    {
        private readonly VideoCapture _videoCapture;
        private readonly Mat _buffer;
        private bool _firstFrame = true;
        private bool _disposed = false;

        public FrameFormat Format { get; }
        public Fraction FrameRate { get; }
        public int FrameWidth { get; }
        public int FrameHeight { get; }
        public int FrameStride { get; }
        public int FrameDataSize { get; }

        public bool HasDuration { get; }
        public bool CanSeek => false;

        public TimeSpan Duration => TimeSpan.FromSeconds(_videoCapture.FrameCount / _videoCapture.Fps);

        private int _currentFrame = 0;


        public void Seek(TimeSpan time)
        {
            throw new InvalidOperationException();
        }

        public VideoFileFrameStream(string filePath, VideoCaptureAPIs apiPreference)
        {
            _videoCapture = new VideoCapture(filePath, apiPreference);
            _buffer = new Mat();
            if (!_videoCapture.Read(_buffer))
            {
                throw new ArgumentException("Invalid video file");
            }

            var matType = _buffer.Type();
            var pixelBytes = matType.Channels;

            if (matType == MatType.CV_8UC3)
            {
                pixelBytes = 3;
            }
            else if (matType == MatType.CV_8UC4)
            {
                pixelBytes = 4;
            }
            else
            {
                throw new NotSupportedException();
            }

            HasDuration = _videoCapture.FrameCount > 0;
            Format = pixelBytes switch
            {
                3 => FrameFormat.Bgr888,
                4 => FrameFormat.Bgra8888,
                _ => throw new NotSupportedException()
            };

            FrameRate = Fraction.FromValue(_videoCapture.Fps);
            FrameWidth = _videoCapture.FrameWidth;
            FrameHeight = _videoCapture.FrameHeight;
            FrameStride = FrameWidth * pixelBytes;
            FrameDataSize = FrameStride * _buffer.Rows;
        }

        public VideoFileFrameStream(string filePath) : this(filePath, VideoCaptureAPIs.ANY)
        { }

        public unsafe bool Read(Span<byte> buffer, out TimeSpan time)
        {
            EnsureNotDisposed();
            if (!_firstFrame)
            {
                if (!_videoCapture.Read(_buffer))
                {
                    time = default;
                    return false;
                }
            }

            _firstFrame = false;
            int height = FrameHeight;
            int stride = FrameStride;

            fixed (byte* bufferPtr = buffer)
            {
                for (int y = 0; y < height; y++)
                {
#if NET8_0_OR_GREATER
                    NativeMemory.Copy(
                        _buffer.DataPointer + y * stride,
                        bufferPtr + y * stride,
                        (nuint)stride);
#else
                    Buffer.MemoryCopy(
                        _buffer.DataPointer + y * stride,
                        bufferPtr + y * stride,
                        stride,
                        stride);
#endif
                }
            }

            _currentFrame++;
            time = TimeSpan.FromSeconds(_currentFrame * _videoCapture.Fps);
            return true;
        }

        private void EnsureNotDisposed()
        {
            if (_disposed)
            {
                throw new InvalidOperationException("Object disposed");
            }
        }

        public void Dispose()
        {
            EnsureNotDisposed();
            ((IDisposable)_videoCapture).Dispose();
            ((IDisposable)_buffer).Dispose();
            _disposed = true;
        }
    }
}
