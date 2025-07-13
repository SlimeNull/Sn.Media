// See https://aka.ms/new-console-template for more information
using Sn.Media.OpenCvSharp4;
using Sn.Media;
using Sn.Media.Windows;

Console.WriteLine("Hello, World!");
WaveOutSamplePlayer player = new WaveOutSamplePlayer();
var fileStream = File.OpenRead(@"D:\CloudMusic\MV\shanqiu.wav");
player.Source = WaveFileSampleStream.Create(fileStream);
player.IsPlaying = true;

while (true)
{
    var key = Console.ReadKey(true).Key;
    if (key == ConsoleKey.Spacebar)
    {
        player.IsPlaying ^= true;
    }
    else if (key == ConsoleKey.LeftArrow)
    {
        player.Position -= TimeSpan.FromSeconds(5);
    }
    else if (key == ConsoleKey.RightArrow)
    {
        player.Position += TimeSpan.FromSeconds(5);
    }
}
Console.ReadLine();