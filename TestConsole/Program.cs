// See https://aka.ms/new-console-template for more information
using Sn.Media.OpenCvSharp4;
using Sn.Media;
using Sn.Media.Tests;
using Sn.Media.NAudio;
using Sn.Media.Bass;

Console.WriteLine("Hello, World!");

ManagedBass.Bass.Init();

var sampleStream = new Sn.Media.SdcbFFmpeg.MediaFileSampleStream(@"D:\CloudMusic\MV\water.mp3");
Console.WriteLine($"Length: {sampleStream.Length / sampleStream.SampleRate}");
var player = new Sn.Media.Bass.SamplePlayer()
{
    Source = sampleStream.AsNonSeekable(),
};

player.IsPlaying = true;

while (true)
{
    Console.CursorLeft = 0;
    Console.Write($"Position: {sampleStream.Position / sampleStream.SampleRate}");
}

Console.ReadKey();