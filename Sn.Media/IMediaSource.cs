using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sn.Media
{
    public interface IMediaSource
    {
        int SampleStreamCount { get; }
        int FrameStreamCount { get; }


        ISampleStream GetSampleStream(int streamIndex);
        IFrameStream GetFrameStream(int streamIndex);
    }
}
