namespace Sn.Media.Tests
{
    public class SineWaveSampleStream : ISampleStream
    {
        private readonly SampleFormat _format;
        private readonly int _sampleRate;
        private readonly int _channels;
        private readonly double _frequency; // 正弦波频率
        private readonly double _amplitude; // 振幅
        private readonly int _duration; // 持续时间（秒）
        private readonly int _sampleGroupBytes;
        private readonly long _endPosition;
        private long _position; // 当前样本位置

        public SineWaveSampleStream(SampleFormat format, int sampleRate, int channels, double frequency, double amplitude, int duration)
        {
            _format = format;
            _sampleRate = sampleRate;
            _channels = channels;
            _frequency = frequency;
            _amplitude = amplitude;
            _duration = duration;
            _sampleGroupBytes = format.GetByteSize() * channels; // 每个样本组的字节数
            _endPosition = (long)SampleRate * duration;
            _position = 0; // 初始化位置信息
        }
        public int Channels => _channels;
        public int SampleRate => _sampleRate;
        public SampleFormat Format => _format;

        public bool HasPosition => true;
        public bool CanSeek => true;

        public long Position => _position;

        private double GetSampleValue(long sampleIndex)
        {
            double time = (double)sampleIndex / _sampleRate; // 计算时间
            return _amplitude * Math.Sin(2 * Math.PI * _frequency * time); // 计算正弦波值
        }

        private unsafe int AddSample(byte[] buffer, int offset, long sampleIndex)
        {
            var sampleValue = GetSampleValue(sampleIndex);
            fixed (byte* bufferPtr = buffer)
            {
                switch (Format)
                {
                    case SampleFormat.UInt8:
                        for (int c = 0; c < Channels; c++)
                        {
                            bufferPtr[offset + c] = (byte)((sampleValue + 1.0) * 127.5); // 将值转换为 0-255 范围
                        }
                        return Channels * 1;
                    case SampleFormat.Int16:
                        for (int c = 0; c < Channels; c++)
                        {
                            short int16Value = (short)(sampleValue * short.MaxValue);
                            *((short*)(bufferPtr + offset) + c) = int16Value;
                        }
                        return Channels * 2;
                    case SampleFormat.Int32:
                        for (int c = 0; c < Channels; c++)
                        {
                            int int32Value = (int)(sampleValue * int.MaxValue);
                            *((int*)(bufferPtr + offset) + c) = int32Value;
                        }
                        return Channels * 4;
                    case SampleFormat.Float32:
                        for (int c = 0; c < Channels; c++)
                        {
                            float float32Value = (float)sampleValue;
                            *((float*)(bufferPtr + offset) + c) = float32Value;
                        }
                        return Channels * 4;
                    default:
                        throw new NotSupportedException($"Unsupported sample format: {Format}");
                }
            }
        }

        public void Seek(long position)
        {
            _position = position;
        }

        public int ReadSamples(byte[] buffer, int offset, int count)
        {
            int readBytes = 0;
            while (
                readBytes + _sampleGroupBytes <= count)
            {
                if (_endPosition > 0 &&
                    _position < _endPosition)
                {
                    break;
                }

                readBytes += AddSample(buffer, offset + readBytes, _position);
                _position++;
            }

            return readBytes;
        }
    }
}
