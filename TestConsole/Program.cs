// See https://aka.ms/new-console-template for more information
using Sn.Media.OpenCvSharp4;
using Sn.Media;
using Sn.Media.Tests;
using Sn.Media.NAudio;
using Sn.Media.Bass;
using Sn.Media.SdcbFFmpeg;

Console.WriteLine("Hello, World!");

var frameStream = new MediaFileFrameStream(@"C:\Users\Xavier\Videos\2025-02-07 12-58-36.mkv");
var buffer = new byte[frameStream.FrameDataSize];
for (int i = 0; i < 100; i++)
{
    frameStream.Read(buffer);
}

Console.WriteLine($"After 100 read, Position: {frameStream.Position}");
frameStream.Seek(200);
Console.WriteLine($"After seek 200, CurrentPosition: {frameStream.Position}");
frameStream.Read(buffer);
Console.WriteLine($"After 1 read, CurrentPosition: {frameStream.Position}");

Console.ReadLine();