// See https://aka.ms/new-console-template for more information
using Sn.Media.OpenCvSharp4;
using Sn.Media;

Console.WriteLine("Hello, World!");

var frameStream = new VideoFileFrameStream(@"D:\CloudMusic\MV\shanqiu.mp4");
var buffer = new byte[frameStream.FrameDataSize];

while (frameStream.ReadFrame(buffer, 0, buffer.Length))
{
    Console.WriteLine("Next frame");
}