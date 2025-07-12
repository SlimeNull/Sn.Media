using PropertyChanged;
using PropertyChanging;

namespace Sn.Media
{
    [ImplementPropertyChanging]
    [AddINotifyPropertyChangedInterface]
    public class BufferedSampleStream : ISampleStream, IDisposable
    {
        private readonly object _readLock = new();
        private readonly object _bufferModifyLock = new();
        private volatile bool _disposedValue;

        private readonly ISampleStream _sampleStream;
        private readonly byte[] _buffer;
        private readonly byte[] _readBuffer;
        private volatile int _bufferDataIndex;
        private volatile int _bufferDataSize;
        private bool _noMoreSamples;
        private long _position;
        private Task _workTask;

        public SampleFormat Format { get; }

        public int SampleRate { get; }

        public int Channels { get; }

        public bool HasPosition { get; }

        public bool HasLength { get; }

        public bool CanSeek { get; }

        public long Length => _sampleStream.Length;

        public long Position => _position;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sampleStream"></param>
        /// <param name="bufferSize">当前缓存流的缓存数据大小</param>
        /// <param name="readBufferSize">在对采样流读取时所使用的缓冲区大小</param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public BufferedSampleStream(ISampleStream sampleStream, int bufferSize, int readBufferSize)
        {
            if (sampleStream is null)
            {
                throw new ArgumentNullException(nameof(sampleStream));
            }

            if (bufferSize == 0 ||
                bufferSize % sampleStream.Channels != 0)
            {
                throw new ArgumentOutOfRangeException(nameof(bufferSize));
            }

            if (readBufferSize == 0 ||
                readBufferSize % sampleStream.Channels != 0)
            {
                throw new ArgumentOutOfRangeException(nameof(readBufferSize));
            }

            Format = sampleStream.Format;
            SampleRate = sampleStream.SampleRate;
            Channels = sampleStream.Channels;
            HasPosition = true;
            HasLength = sampleStream.HasLength;
            CanSeek = sampleStream.CanSeek;

            _sampleStream = sampleStream;
            _buffer = new byte[bufferSize];
            _readBuffer = new byte[readBufferSize];

            _workTask = Task.Run(WorkProcedure);
        }

        public BufferedSampleStream(ISampleStream sampleStream)
            : this(sampleStream, sampleStream.SampleRate * sampleStream.Channels, 1024 * sampleStream.Channels)
        {

        }

        private void WorkProcedure()
        {
            while (_bufferDataSize < _buffer.Length)
            {
                if (_disposedValue)
                {
                    break;
                }

                lock (_readLock)
                {
                    var indexToFill = (_bufferDataIndex + _bufferDataSize) % _buffer.Length;
                    var sizeToFill = Math.Min(_readBuffer.Length, _buffer.Length - _bufferDataSize);

                    var actualFilled = _sampleStream.Read(_readBuffer.AsSpan().Slice(0, sizeToFill));
                    if (actualFilled <= 0)
                    {
                        _noMoreSamples = true;
                        break;
                    }

                    lock (_bufferModifyLock)
                    {
                        if (indexToFill + sizeToFill > _buffer.Length)
                        {
                            var firstCopyCount = _buffer.Length - indexToFill;
                            Array.Copy(_readBuffer, 0, _buffer, indexToFill, firstCopyCount);
                            Array.Copy(_readBuffer, firstCopyCount, _buffer, 0, sizeToFill - firstCopyCount);
                        }
                        else
                        {
                            Array.Copy(_readBuffer, 0, _buffer, indexToFill, sizeToFill);
                        }

                        _bufferDataSize += actualFilled;
                    }
                }
            }
        }

        public void Seek(long position)
        {
            if (!CanSeek)
            {
                throw new InvalidOperationException();
            }

            lock (_readLock)
            {
                lock (_bufferModifyLock)
                {
                    _bufferDataIndex = 0;
                    _bufferDataSize = 0;
                }

                _noMoreSamples = false;
                _sampleStream.Seek(position);
            }

            if (_workTask.IsCompleted)
            {
                _workTask = Task.Run(WorkProcedure);
            }
        }

        public int Read(Span<byte> buffer)
        {
            while (_bufferDataSize == 0)
            {
                if (_noMoreSamples)
                {
                    return 0; // No more frames to read
                }

                // wait
            }

            var dataSizeToCopy = Math.Min(buffer.Length, _bufferDataSize);

            if (_bufferDataIndex + dataSizeToCopy > _buffer.Length)
            {
                var endIndex = (_bufferDataIndex + dataSizeToCopy) % _buffer.Length;
                _buffer.AsSpan().Slice(_bufferDataIndex).CopyTo(buffer);
                _buffer.AsSpan().Slice(0, endIndex).CopyTo(buffer.Slice(_buffer.Length - _bufferDataIndex));
            }
            else
            {
                _buffer.AsSpan().Slice(_bufferDataIndex, dataSizeToCopy).CopyTo(buffer);
            }

            lock (_bufferModifyLock)
            {
                _bufferDataIndex = (_bufferDataIndex + dataSizeToCopy) % _buffer.Length;
                _bufferDataSize -= dataSizeToCopy;
            }

            if (_workTask.IsCompleted)
            {
                _workTask = Task.Run(WorkProcedure);
            }

            return dataSizeToCopy;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                _disposedValue = true;
            }
        }

        ~BufferedSampleStream()
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
