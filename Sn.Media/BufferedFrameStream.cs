using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Sn.Media
{
    public class BufferedFrameStream : IFrameStream
    {
        private readonly object _bufferLock = new();
        private readonly IFrameStream _frameStream;
        private readonly BackgroundWorker _worker;
        private readonly byte[][] _buffers;
        private volatile bool _noMoreFrames;
        private volatile int _dataBufferIndex;
        private volatile int _dataBufferCount;
        private long _position;

        public FrameFormat Format { get; }

        public Fraction FrameRate { get; }

        public int FrameWidth { get; }

        public int FrameHeight { get; }

        public int FrameStride { get; }

        public int FrameDataSize { get; }

        public bool HasPosition => true;
        public bool HasLength { get; }

        public bool CanSeek { get; }

        public long Position => _position;
        public long Length => _frameStream.Length;

        public BufferedFrameStream(IFrameStream frameStream, int bufferCount)
        {
            if (frameStream is null)
            {
                throw new ArgumentNullException(nameof(frameStream));
            }

            if (bufferCount <= 1)
            {
                throw new ArgumentOutOfRangeException(nameof(bufferCount), "Buffer count must be greater than 1.");
            }

            Format = frameStream.Format;
            FrameRate = frameStream.FrameRate;
            FrameWidth = frameStream.FrameWidth;
            FrameHeight = frameStream.FrameHeight;
            FrameStride = frameStream.FrameStride;
            FrameDataSize = frameStream.FrameDataSize;
            CanSeek = frameStream.CanSeek;
            HasLength = frameStream.HasLength;

            _frameStream = frameStream;
            _worker = new BackgroundWorker();
            _worker.DoWork += WorkerDoWork;
            _buffers = new byte[bufferCount][];
            for (int i = 0; i < _buffers.Length; i++)
            {
                _buffers[i] = new byte[FrameDataSize];
            }

            _worker.RunWorkerAsync();
        }

        public BufferedFrameStream(IFrameStream frameStream) : this(frameStream, 16)
        {

        }

        private void WorkerDoWork(object? sender, DoWorkEventArgs e)
        {
            while (_dataBufferCount < _buffers.Length)
            {
                if (e.Cancel)
                {
                    return; // Exit if the worker is cancelled
                }

                var currentBuffer = _buffers[(_dataBufferIndex + _dataBufferCount) % _buffers.Length];
                if (!_frameStream.Read(currentBuffer))
                {
                    _noMoreFrames = true;
                    break;
                }

                lock (_bufferLock)
                {
                    _dataBufferCount++;
                }
            }
        }

        public void Seek(long position)
        {
            if (!CanSeek)
            {
                throw new InvalidOperationException();
            }

            _worker.CancelAsync();
            while (_worker.CancellationPending)
            {
                // wait
            }

            lock (_bufferLock)
            {
                _dataBufferIndex = 0;
                _dataBufferCount = 0;
            }

            _frameStream.Seek(position);

            if (!_worker.IsBusy)
            {
                _worker.RunWorkerAsync();
            }
        }

        public bool Read(Span<byte> buffer)
        {
            while (_dataBufferCount == 0)
            {
                if (_noMoreFrames)
                {
                    return false; // No more frames to read
                }

                // wait
            }

            var currentBuffer = _buffers[_dataBufferIndex];
            currentBuffer.CopyTo(buffer);

            lock (_bufferLock)
            {
                _dataBufferIndex = (_dataBufferIndex + 1) % _buffers.Length;
                _dataBufferCount--;
            }

            if (!_worker.IsBusy)
            {
                _worker.RunWorkerAsync();
            }

            _position++;
            return true;
        }
    }
}
