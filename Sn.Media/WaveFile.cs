using Sn.Media.Internal;

namespace Sn.Media
{
    public static class WaveFile
    {
        public static unsafe void Write(Stream dest, ISampleStream sampleStream)
        {
            if (!dest.CanSeek &&
                sampleStream.HasLength)
            {
                throw new ArgumentException("");
            }

            var bytesPerSample = sampleStream.Format.GetByteSize();
            var bytesPerSampleGroup = bytesPerSample * sampleStream.Channels;

            var dataSize = 0u;
            var startPosition = 0L;

            if (sampleStream.HasLength)
            {
                dataSize = (uint)(bytesPerSampleGroup * sampleStream.Length);

                var header = WaveFileHeader.Create(sampleStream, null);
                dest.WriteStructure(header);
            }
            else
            {
                startPosition = dest.Position;
                dest.Seek(sizeof(WaveFileHeader), SeekOrigin.Current);
            }

            var sampleBytesRead = 0L;
            var buffer = new byte[bytesPerSampleGroup * 1024]; // 1K sample group buffer
            while (true)
            {
                int bytesRead = sampleStream.Read(buffer);
                if (bytesRead <= 0)
                {
                    break; // No more samples to read
                }

                dest.Write(buffer, 0, bytesRead);
                sampleBytesRead += bytesRead;
            }

            if (dataSize == 0)
            {
                var endPosition = dest.Position;
                dest.Seek(startPosition, SeekOrigin.Begin);

                var header = WaveFileHeader.Create(sampleStream, (uint)sampleBytesRead);
                dest.WriteStructure(header);

                dest.Seek(endPosition, SeekOrigin.Begin);
            }
        }
    }
}
