using System.Buffers;
using System.Runtime.InteropServices;

namespace Sn.Media
{
    public class WaveFileSampleStream : ISampleStream
    {
        private readonly WaveFileHeader _header;
        private readonly Stream _stream;
        private readonly int _bytesPerSampleGroup;
        private readonly long _streamSampleStartPosition;
        private long _position;

#if NET8_0_OR_GREATER

#else
        private readonly ArrayPool<byte> _arrayPool = ArrayPool<byte>.Create();
#endif
        private WaveFileSampleStream(WaveFileHeader header, Stream stream)
        {
            Format = header.AudioFormat switch
            {
                WaveAudioFormat.PCM => header.BitsPerSample switch
                {
                    8 => SampleFormat.UInt8,
                    16 => SampleFormat.Int16,
                    32 => SampleFormat.Int32,
                    _ => throw new NotSupportedException("Invalid PCM format")
                },
                WaveAudioFormat.IeeeFloat => header.BitsPerSample switch
                {
                    32 => SampleFormat.Float32,
                    _ => throw new NotSupportedException("Invalid IEEE float format")
                },
                _ => throw new NotSupportedException("Unsupported audio format")
            };

            SampleRate = (int)header.SampleRate;
            Channels = header.ChannelCount;

            _header = header;
            _stream = stream;
            _bytesPerSampleGroup = header.BitsPerSample / 8 * header.ChannelCount;

            if (stream.CanSeek)
            {
                _streamSampleStartPosition = stream.Position;
            }
        }

        public SampleFormat Format { get; set; }

        public int SampleRate { get; set; }

        public int Channels { get; set; }

        public bool HasLength => _stream.CanSeek;

        public bool CanSeek => _stream.CanSeek;

        public long Position => _position;

        public long Length => _header.DataChunkSize / _bytesPerSampleGroup;

        public int Read(Span<byte> buffer)
        {
#if NET8_0_OR_GREATER
            return _stream.Read(buffer);
#else
            var actualSizeToRead = buffer.Length / _bytesPerSampleGroup * _bytesPerSampleGroup;
            var array = _arrayPool.Rent(actualSizeToRead);
            var read = _stream.Read(array, 0, actualSizeToRead);
            array.AsSpan(0, read).CopyTo(buffer);
            _arrayPool.Return(array);

            _position += _bytesPerSampleGroup / read;
            return read;
#endif
        }

        public void Seek(long position)
        {
            if (!_stream.CanSeek)
            {
                throw new NotSupportedException();
            }

            _stream.Seek(_streamSampleStartPosition + position * _bytesPerSampleGroup, SeekOrigin.Begin);
            _position = position;
        }

        public static unsafe WaveFileSampleStream Create(Stream stream)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }
            if (!stream.CanSeek)
            {
                throw new ArgumentException("Stream must support seeking.", nameof(stream));
            }

            var header = WaveFileHeader.Read(stream);

            return new WaveFileSampleStream(header, stream);
        }
    }
}
