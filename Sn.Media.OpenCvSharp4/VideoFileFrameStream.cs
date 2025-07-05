
using System.Runtime.InteropServices;
using OpenCvSharp;

namespace Sn.Media.OpenCvSharp4
{
    public class VideoFileFrameStream : IFrameStream, IDisposable
    {
        private readonly VideoCapture _videoCapture;
        private readonly Mat _buffer;
        private bool _firstFrame = true;
        private long _position;
        private bool _disposed = false;

        public FrameFormat Format { get; }
        public Fraction FrameRate { get; }
        public int FrameWidth { get; }
        public int FrameHeight { get; }
        public int FrameStride { get; }
        public int FrameDataSize { get; }

        public bool HasPosition => true;
        public bool CanSeek => false;

        public long Position => _position;


        public void Seek(long position)
        {
            throw new InvalidOperationException();
        }

        public VideoFileFrameStream(string filePath)
        {
            _videoCapture = new VideoCapture(filePath);
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

        public unsafe bool ReadFrame(byte[] buffer, int offset, int count)
        {
            if (!_firstFrame)
            {
                if (!_videoCapture.Read(_buffer))
                {
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
                    NativeMemory.Copy(
                        _buffer.DataPointer + y * stride,
                        bufferPtr + offset + y * stride,
                        (nuint)stride);
                }
            }

            _position++;
            return true;
        }

        public void Dispose()
        {
            ((IDisposable)_videoCapture).Dispose();
            ((IDisposable)_buffer).Dispose();
        }
    }
}
