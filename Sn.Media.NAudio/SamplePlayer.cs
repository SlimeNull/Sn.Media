using NAudio.Wave;

namespace Sn.Media.NAudio
{
    public class SamplePlayer
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
                long originPosition = 0;

                if (_source is not null &&
                    _source.HasPosition)
                {
                    originPosition = _source.Position;
                }

                _waveOut.Stop();
                _source = value;

                if (value is not null)
                {
                    _waveOut.Init(new WaveProviderWrapper(value));

                    if (IsPlaying)
                    {
                        if (value.CanSeek)
                        {
                            value.Seek(originPosition);
                        }

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

                if (!value)
                {
                    if (_source is not null)
                    {
                        _waveOut.Pause();
                    }
                }
                else
                {
                    if (_source is not null)
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
            }
        }
    }
}
