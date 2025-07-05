// See https://aka.ms/new-console-template for more information
using Sn.Media.OpenCvSharp4;
using Sn.Media;
using Sn.Media.Tests;
using Sn.Media.NAudio;

Console.WriteLine("Hello, World!");

var sampleStream = new AudioFileSampleStream(@"D:\CloudMusic\MV\shanqiu.mp4");
var samplePlayer = new SamplePlayer()
{
    Source = sampleStream
};

samplePlayer.IsPlaying = true;

while (true)
{
    Console.CursorLeft = 0;
    Console.Write($"Position: {sampleStream.Position / sampleStream.SampleRate}");
}

Console.ReadKey();