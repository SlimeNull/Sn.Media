namespace Sn.Media
{
    public class WaveFileSampleStream : ISampleStream
    {
        private readonly WaveFileHeader _header;
        private readonly Stream _stream;
        private readonly long _streamSampleStartPosition;
        private readonly int _bytesPerSampleGroup;

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
            _streamSampleStartPosition = stream.Position;
            _bytesPerSampleGroup = header.BitsPerSample / 8 * header.ChannelCount;
        }

        public SampleFormat Format { get; set; }

        public int SampleRate { get; set; }

        public int Channels { get; set; }

        public bool HasPosition => _stream.CanSeek;

        public bool HasLength => _stream.CanSeek;

        public bool CanSeek => _stream.CanSeek;

        public long Position => (_stream.Position - _streamSampleStartPosition) / _bytesPerSampleGroup;

        public long Length => _header.DataChunkSize / _bytesPerSampleGroup;

        public int Read(Span<byte> buffer)
        {
            return _stream.Read(buffer);
        }

        public void Seek(long position)
        {
            _stream.Seek(_streamSampleStartPosition + position * _bytesPerSampleGroup, SeekOrigin.Begin);
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
