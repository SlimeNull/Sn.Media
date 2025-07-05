// See https://aka.ms/new-console-template for more information
using Sn.Media.OpenCvSharp4;
using Sn.Media;
using Sn.Media.Tests;
using Sn.Media.NAudio;

Console.WriteLine("Hello, World!");

var sampleStream = new SineWaveSampleStream(SampleFormat.Float32, 44100, 1, 1000, 1, 5);
var samplePlayer = new SamplePlayer()
{
    Source = sampleStream
};

samplePlayer.IsPlaying = true;

var frameStream = new VideoFileFrameStream(@"D:\CloudMusic\MV\shanqiu.mp4");
var buffer = new byte[frameStream.FrameDataSize];

while (frameStream.ReadFrame(buffer, 0, buffer.Length))
{
    Console.WriteLine("Next frame");
}