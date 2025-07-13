using System.Buffers;
using System.ComponentModel;
using NAudio.Wave;
using PropertyChanged;
using PropertyChanging;

namespace Sn.Media.NAudio
{
    public class WaveSampleStream : ISampleStream, INotifyPropertyChanging, INotifyPropertyChanged
    {
        private static readonly PropertyChangingEventArgs _positionChangingEventArgs = new(nameof(Position));
        private static readonly PropertyChangedEventArgs _positionChangedEventArgs = new(nameof(Position));

        private readonly int _channels;
        private readonly int _bytesPerSample;
        private readonly int _bytesPerSampleGroup;
        private readonly WaveStream? _sourceAsWaveStream;
        private readonly ArrayPool<byte> _bufferPool = ArrayPool<byte>.Create();
        private long _position;

        public IWaveProvider Source { get; }

        public SampleFormat Format { get; }

        public int SampleRate { get; }

        public int Channels { get; }

        public bool HasLength => _sourceAsWaveStream is { };

        public bool CanSeek => _sourceAsWaveStream is { CanSeek: true };

        public long Position
        {
            get
            {
                if (_sourceAsWaveStream is not null)
                {
                    return _sourceAsWaveStream.Position / _bytesPerSample / _channels;
                }

                return _position;
            }
        }

        public long Length
        {
            get
            {
                if (_sourceAsWaveStream is not null)
                {
                    return _sourceAsWaveStream.Length / _bytesPerSample / _channels;
                }

                throw new InvalidOperationException();
            }
        }

        public WaveSampleStream(IWaveProvider source)
        {
            _channels = source.WaveFormat.Channels;
            _bytesPerSample = source.WaveFormat.BitsPerSample / 8;
            _bytesPerSampleGroup = _bytesPerSample * _channels;
            _sourceAsWaveStream = source as WaveStream;

            Source = source;

            if (source.WaveFormat.Encoding == WaveFormatEncoding.Pcm)
            {
                Format = source.WaveFormat.BitsPerSample switch
                {
                    8 => SampleFormat.UInt8,
                    16 => SampleFormat.Int16,
                    32 => SampleFormat.Int32,
                    _ => throw new NotSupportedException($"Unsupported bits per sample for pcm encoding: {source.WaveFormat.BitsPerSample}"),
                };
            }
            else if (source.WaveFormat.Encoding == WaveFormatEncoding.IeeeFloat)
            {
                Format = source.WaveFormat.BitsPerSample switch
                {
                    32 => SampleFormat.Float32,
                    _ => throw new NotSupportedException($"Unsupported bits per sample for float encoding: {source.WaveFormat.BitsPerSample}"),
                };
            }
            else
            {
                throw new ArgumentException("Format of specified source is not supported");
            }

            SampleRate = source.WaveFormat.SampleRate;
            Channels = source.WaveFormat.Channels;

            if (_sourceAsWaveStream is not null)
            {
                _position = _sourceAsWaveStream.Position / _bytesPerSampleGroup;
            }
        }

        public void Seek(long position)
        {
            if (_sourceAsWaveStream is not null && _sourceAsWaveStream.CanSeek)
            {
                long bytePosition = position * _channels * _bytesPerSample;
                _position = position;
                _sourceAsWaveStream.Position = bytePosition;
                return;
            }

            throw new NotSupportedException();
        }

        public int Read(Span<byte> buffer)
        {
            var array = _bufferPool.Rent(buffer.Length);

            PropertyChanging?.Invoke(this, _positionChangingEventArgs);
            var bytesRead = Source.Read(array, 0, buffer.Length);
            PropertyChanged?.Invoke(this, _positionChangedEventArgs);

            if (bytesRead > 0)
            {
                array.AsSpan().Slice(0, bytesRead).CopyTo(buffer);
            }

            _bufferPool.Return(array, clearArray: false);
            _position += bytesRead / _bytesPerSampleGroup;
            return bytesRead;
        }

        public event PropertyChangingEventHandler? PropertyChanging;
        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
