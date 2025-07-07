using NAudio.Wave;

namespace Sn.Media.NAudio
{
    public class SamplePlayer : ISamplePlayer
    {
        private readonly WaveOutEvent _waveOut;
        private ISampleStream? _source;
        private bool _isPlaying;

        public SamplePlayer()
        {
            _waveOut = new WaveOutEvent();
        }

        public ISampleStream? Source
        {
            get => _source;
            set
            {
                _waveOut.Stop();
                _source = value;

                if (value is not null)
                {
                    _waveOut.Init(new SampleStreamWave(value));

                    if (IsPlaying)
                    {
                        _waveOut.Play();
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
                        _waveOut.Pause();
                    }
                    else
                    {
                        _waveOut.Play();
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
                
                if (_isPlaying &&
                    _waveOut.PlaybackState == PlaybackState.Stopped)
                {
                    _waveOut.Play();
                }
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
