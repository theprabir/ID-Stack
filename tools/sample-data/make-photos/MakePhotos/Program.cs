using System;
using System.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Drawing.Processing;

class Program
{
    static void Main()
    {
        var dir = @"D:\ID-Stack\tools\sample-data\photos";
        Directory.CreateDirectory(dir);

        MakePhoto(Path.Combine(dir, "alice.png"), "ALICE", 120, 150);
        MakePhoto(Path.Combine(dir, "bob.png"), "BOB", 120, 150);
        MakePhoto(Path.Combine(dir, "charmaine.png"), "CHARMAINE", 120, 150);

        Console.WriteLine("done");
    }

    static void MakePhoto(string path, string label, int width, int height)
    {
        using (var image = new Image<Rgba32>(width, height))
        {
            // Deterministic pastel background per person.
            var hue = (byte)(label[0] * 37 % 255);
            image.Mutate(ctx => ctx.Fill(new Rgba32(hue, (byte)(hue / 2), (byte)(255 - hue))));

            // Simple centered "portrait" block so the image is visibly not blank.
            var rect = new SixLabors.ImageSharp.Rectangle(width / 6, height / 5, width * 2 / 3, height / 2);
            image.Mutate(ctx => ctx.Fill(new Rgba32(255, 255, 255, 200), rect));

            image.SaveAsPng(path);
        }
    }
}
