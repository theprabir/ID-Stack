using System;
using System.IO;
using IDStack.Core.Interfaces;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Processing;

namespace IDStack.Services
{
    /// <summary>
    /// Image resize/crop/rotate built on SixLabors.ImageSharp (fully managed, no native deps).
    /// All operations return PNG bytes suitable for print rendering.
    /// </summary>
    public class ImageProcessingService : IImageProcessingService
    {
        /// <inheritdoc />
        public byte[] ResizeImage(byte[] imageData, int width, int height)
        {
            Validate(imageData, width, height);

            using (var image = SixLabors.ImageSharp.Image.Load(imageData))
            {
                image.Mutate(ctx => ctx.Resize(new ResizeOptions
                {
                    Size = new Size(width, height),
                    Mode = ResizeMode.Max
                }));

                return Encode(image);
            }
        }

        /// <inheritdoc />
        public byte[] CropImage(byte[] imageData, int x, int y, int width, int height)
        {
            if (imageData == null || imageData.Length == 0)
            {
                throw new ArgumentException("Image data is required.", nameof(imageData));
            }

            using (var image = SixLabors.ImageSharp.Image.Load(imageData))
            {
                var rect = new Rectangle(
                    Math.Max(0, x),
                    Math.Max(0, y),
                    Math.Min(Math.Max(1, width), image.Width),
                    Math.Min(Math.Max(1, height), image.Height));
                image.Mutate(ctx => ctx.Crop(rect));
                return Encode(image);
            }
        }

        /// <inheritdoc />
        public byte[] RotateImage(byte[] imageData, double angle)
        {
            if (imageData == null || imageData.Length == 0)
            {
                throw new ArgumentException("Image data is required.", nameof(imageData));
            }

            using (var image = SixLabors.ImageSharp.Image.Load(imageData))
            {
                image.Mutate(ctx => ctx.Rotate((float)angle));
                return Encode(image);
            }
        }

        /// <inheritdoc />
        public (int Width, int Height) GetImageDimensions(byte[] imageData)
        {
            if (imageData == null || imageData.Length == 0)
            {
                throw new ArgumentException("Image data is required.", nameof(imageData));
            }

            using (var image = SixLabors.ImageSharp.Image.Load(imageData))
            {
                return (image.Width, image.Height);
            }
        }

        /// <summary>
        /// Resizes to the exact size using the given mode; used by photo processing pipelines.
        /// </summary>
        /// <param name="imageData">Source bytes.</param>
        /// <param name="width">Target width in pixels.</param>
        /// <param name="height">Target height in pixels.</param>
        /// <param name="mode">Resize strategy: Max (fit), BoxPad (fill+crop), Stretch.</param>
        /// <returns>Processed PNG bytes.</returns>
        public byte[] ResizeExact(byte[] imageData, int width, int height, ResizeMode mode)
        {
            Validate(imageData, width, height);

            using (var image = SixLabors.ImageSharp.Image.Load(imageData))
            {
                image.Mutate(ctx => ctx.Resize(new ResizeOptions
                {
                    Size = new Size(width, height),
                    Mode = mode
                }));

                return Encode(image);
            }
        }

        private static void Validate(byte[] imageData, int width, int height)
        {
            if (imageData == null || imageData.Length == 0)
            {
                throw new ArgumentException("Image data is required.", nameof(imageData));
            }

            if (width <= 0 || height <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(width), "Dimensions must be positive.");
            }
        }

        private static byte[] Encode(Image image)
        {
            using (var stream = new MemoryStream())
            {
                image.Save(stream, new PngEncoder());
                return stream.ToArray();
            }
        }
    }
}
