using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ManagedBass;

namespace Sn.Media.Bass
{
    public class SamplePlayer : ISamplePlayer
    {
        private ISampleStream? _source;
        private int _streamHandle;
        private bool _isPlaying;

        public ISampleStream? Source
        {
            get => _source;
            set
            {
                if (_streamHandle != 0)
                {
                    ManagedBass.Bass.StreamFree(_streamHandle);
                }

                _source = value;

                if (value is not null)
                {
                    if (value.CanSeek)
                    {
                        _streamHandle = ManagedBass.Bass.CreateStream(StreamSystem.NoBuffer, BassFlags.Default, value.CreateFileProcedures(), 0);
                    }
                    else
                    {
                        var flags = BassFlags.Default;
                        flags |= value.Format switch
                        {
                            SampleFormat.UInt8 => BassFlags.Byte,
                            SampleFormat.Int16 => BassFlags.Default,
                            SampleFormat.Float32 => BassFlags.Float,
                            _ => throw new ArgumentException("Format not support")
                        };

                        _streamHandle = ManagedBass.Bass.CreateStream(value.SampleRate, value.Channels, flags, value.CreateStreamProcedure());
                    }

                    if (_streamHandle == 0)
                    {
                        throw new ArgumentException($"{ManagedBass.Bass.LastError}");
                    }

                    if (_isPlaying)
                    {
                        ManagedBass.Bass.ChannelPlay(_streamHandle);
                    }
                }
            }
        }

        public bool IsPlaying
        {
            get => _isPlaying;
            set
            {
                if (_isPlaying == value)
                {
                    return;
                }

                _isPlaying = value;

                if (_source is not null)
                {
                    if (!value)
                    {
                        ManagedBass.Bass.ChannelPause(_streamHandle);
                    }
                    else
                    {
                        ManagedBass.Bass.ChannelPlay(_streamHandle);
                    }
                }
            }
        }

        public TimeSpan Position
        {
            get
            {
                if (_source is null)
                {
                    throw new InvalidOperationException("No source specified");
                }

                if (!_source.HasPosition)
                {
                    throw new InvalidOperationException("Source has no position");
                }

                var totalSeconds = _source.Position / (double)_source.SampleRate;
                return TimeSpan.FromSeconds(totalSeconds);
            }
            set
            {
                if (_source is null)
                {
                    throw new InvalidOperationException("No source specified");
                }

                if (!_source.CanSeek)
                {
                    throw new InvalidOperationException("Source does not support seeking");
                }

                var totalSamples = (long)(value.TotalSeconds * _source.SampleRate);
                _source.Seek(totalSamples);
            }
        }

        public TimeSpan Length
        {
            get
            {
                if (_source is null)
                {
                    throw new InvalidOperationException("No source");
                }

                if (!_source.HasLength)
                {
                    throw new InvalidOperationException("Source no length");
                }

                return TimeSpan.FromSeconds(_source.Length / (double)_source.SampleRate);
            }
        }
    }
}
