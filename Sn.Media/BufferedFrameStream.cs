using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using PropertyChanged;
using PropertyChanging;

namespace Sn.Media
{
    [ImplementPropertyChanging]
    [AddINotifyPropertyChangedInterface]
    public class BufferedFrameStream : IFrameStream, IDisposable
    {
        private readonly object _readLock = new();
        private readonly object _bufferModifyLock = new();
        private volatile bool _disposedValue;

        private readonly IFrameStream _frameStream;
        private readonly (byte[] Buffer, TimeSpan Time)[] _buffers;
        private volatile bool _noMoreFrames;
        private volatile int _dataBufferIndex;
        private volatile int _dataBufferCount;
        private Task _workTask;


        public FrameFormat Format { get; }

        public Fraction FrameRate { get; }

        public int FrameWidth { get; }

        public int FrameHeight { get; }

        public int FrameStride { get; }

        public int FrameDataSize { get; }

        public bool HasDuration { get; }

        public bool CanSeek { get; }

        public TimeSpan Duration => _frameStream.Duration;

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
            HasDuration = frameStream.HasDuration;

            _frameStream = frameStream;
            _buffers = new (byte[], TimeSpan)[bufferCount];
            for (int i = 0; i < _buffers.Length; i++)
            {
                _buffers[i] = (new byte[FrameDataSize], default);
            }

            _workTask = Task.Run(WorkProcedure);
        }

        public BufferedFrameStream(IFrameStream frameStream) : this(frameStream, 16)
        {

        }

        private void WorkProcedure()
        {
            while (_dataBufferCount < _buffers.Length)
            {
                if (_disposedValue)
                {
                    break;
                }

                lock (_readLock)
                {
                    var currentBufferIndex = (_dataBufferIndex + _dataBufferCount) % _buffers.Length;
                    var currentBuffer = _buffers[currentBufferIndex];
                    if (!_frameStream.Read(currentBuffer.Buffer, out var time))
                    {
                        _noMoreFrames = true;
                        break;
                    }

                    _buffers[currentBufferIndex] = (currentBuffer.Buffer, time);
                    lock (_bufferModifyLock)
                    {
                        _dataBufferCount++;
                    }
                }
            }
        }

        public void Seek(TimeSpan time)
        {
            if (!CanSeek)
            {
                throw new InvalidOperationException();
            }

            lock (_readLock)
            {
                lock (_bufferModifyLock)
                {
                    _dataBufferIndex = 0;
                    _dataBufferCount = 0;
                }

                _noMoreFrames = false;
                _frameStream.Seek(time);
            }

            if (_workTask.IsCompleted)
            {
                _workTask = Task.Run(WorkProcedure);
            }
        }

        public bool Read(Span<byte> buffer, out TimeSpan time)
        {
            while (_dataBufferCount == 0)
            {
                if (_noMoreFrames)
                {
                    time = default;
                    return false; // No more frames to read
                }

                // wait
            }

            var currentBuffer = _buffers[_dataBufferIndex];
            currentBuffer.Buffer.CopyTo(buffer);
            time = currentBuffer.Time;

            lock (_bufferModifyLock)
            {
                _dataBufferIndex = (_dataBufferIndex + 1) % _buffers.Length;
                _dataBufferCount--;
            }

            if (_workTask.IsCompleted)
            {
                _workTask = Task.Run(WorkProcedure);
            }

            return true;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                _disposedValue = true;
            }
        }

        ~BufferedFrameStream()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }

}
