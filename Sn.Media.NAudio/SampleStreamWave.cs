using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NAudio.Wave;

namespace Sn.Media.NAudio
{
    public class SampleStreamWave : IWaveProvider
    {
        public ISampleStream Source { get; }

        public SampleStreamWave(ISampleStream source)
        {
            Source = source;


            WaveFormat = source.Format switch
            {
                SampleFormat.UInt8 => new WaveFormat(source.SampleRate, 8, source.Channels),
                SampleFormat.Int16 => new WaveFormat(source.SampleRate, 16, source.Channels),
                SampleFormat.Int32 => new WaveFormat(source.SampleRate, 32, source.Channels),
                SampleFormat.Float32 => WaveFormat.CreateIeeeFloatWaveFormat(source.SampleRate, source.Channels),
                _ => throw new NotSupportedException($"Unsupported sample format: {source.Format}"),
            };
        }

        public WaveFormat WaveFormat { get; }

        public int Read(byte[] buffer, int offset, int count)
        {
            return Source.Read(new Span<byte>(buffer, offset, count));
        }
    }
}
