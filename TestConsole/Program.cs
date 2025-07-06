// See https://aka.ms/new-console-template for more information
using Sn.Media.OpenCvSharp4;
using Sn.Media;
using Sn.Media.Tests;
using Sn.Media.NAudio;
using Sn.Media.Bass;

Console.WriteLine("Hello, World!");



ManagedBass.Bass.Init();
var waveFile = File.OpenRead(@"D:\CloudMusic\MV\shanqiu.wav");


var sampleStream = new Sn.Media.NAudio.AudioFileSampleStream(@"D:\CloudMusic\MV\shanqiu.mp3");
var player = new Sn.Media.Bass.SamplePlayer()
{
    Source = sampleStream.AsNonSeekable().AsFormat(SampleFormat.UInt8),
};

player.IsPlaying = true;

while (true)
{
    Console.CursorLeft = 0;
    Console.Write($"Position: {sampleStream.Position / sampleStream.SampleRate}");
}

Console.ReadKey();