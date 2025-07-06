using ManagedBass;

namespace Sn.Media.Bass
{
    public class AudioFileSampleStream : ISampleStream, IDisposable
    {
        private readonly int _streamHandle;
        private readonly int _bytesPerSampleGroup;
        private long _position;
        private bool _disposedValue;

        public SampleFormat Format => SampleFormat.Float32; // Bass only supports Float32 for decoding audio files

        public int SampleRate { get; }

        public int Channels { get; }

        public bool HasPosition => true;
        public bool HasLength => true;
        public bool CanSeek => true;

        public long Position => ManagedBass.Bass.ChannelGetPosition(_streamHandle, PositionFlags.Bytes) / _bytesPerSampleGroup / Channels;
        public long Length => ManagedBass.Bass.ChannelGetLength(_streamHandle, PositionFlags.Bytes) / _bytesPerSampleGroup / Channels;

        private AudioFileSampleStream(int bassStreamHandle)
        {
            _streamHandle = bassStreamHandle;
            if (_streamHandle == 0)
            {
                throw new ArgumentException(ManagedBass.Bass.LastError.ToString());
            }

            ManagedBass.Bass.ChannelGetInfo(_streamHandle, out var channelInfo);
            SampleRate = channelInfo.Frequency;
            Channels = channelInfo.Channels;

            var rawFormat = channelInfo.Resolution switch
            {
                Resolution.Byte => SampleFormat.UInt8,
                Resolution.Short => SampleFormat.Int16,
                Resolution.Float => SampleFormat.Float32,
                _ => throw new InvalidOperationException("Sample format not support")
            };

            _bytesPerSampleGroup = rawFormat.GetByteSize();
        }

        public AudioFileSampleStream(string filePath) :
            this(ManagedBass.Bass.CreateStream(filePath, 0, 0, BassFlags.Decode))
        {

        }

        public AudioFileSampleStream(byte[] file)
            : this(ManagedBass.Bass.CreateStream(file, 0, 0, BassFlags.Decode))
        { }

        public void Seek(long position)
        {
            long bytePosition = position * _bytesPerSampleGroup * Channels;
            ManagedBass.Bass.ChannelSetPosition(_streamHandle, bytePosition, PositionFlags.Bytes);
        }

        public unsafe int Read(Span<byte> buffer)
        {
            fixed (byte* bufferPtr = buffer)
            {
                int ret = ManagedBass.Bass.ChannelGetData(_streamHandle, (nint)bufferPtr, buffer.Length | (int)ManagedBass.DataFlags.Float);
                if (ret > 0)
                {
                    return ret;
                }

                if (ManagedBass.Bass.LastError == Errors.Ended)
                {
                    return 0;
                }

                throw new InvalidOperationException();
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                ManagedBass.Bass.StreamFree(_streamHandle);
                _disposedValue = true;
            }
        }

        ~AudioFileSampleStream()
        {
            // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
            Dispose(disposing: false);
        }

        void IDisposable.Dispose()
        {
            // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
