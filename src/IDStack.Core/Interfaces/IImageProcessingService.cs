namespace IDStack.Core.Interfaces
{
    /// <summary>
    /// Low-level image operations used by photo import and batch rendering.
    /// </summary>
    public interface IImageProcessingService
    {
        /// <summary>Resizes image bytes to the exact pixel size.</summary>
        /// <param name="imageData">Source image bytes (any supported format).</param>
        /// <param name="width">Target width in pixels.</param>
        /// <param name="height">Target height in pixels.</param>
        /// <returns>Resized PNG bytes.</returns>
        byte[] ResizeImage(byte[] imageData, int width, int height);

        /// <summary>Crops a rectangular region.</summary>
        /// <param name="imageData">Source image bytes.</param>
        /// <param name="x">Left pixel.</param>
        /// <param name="y">Top pixel.</param>
        /// <param name="width">Region width in pixels.</param>
        /// <param name="height">Region height in pixels.</param>
        /// <returns>Cropped PNG bytes.</returns>
        byte[] CropImage(byte[] imageData, int x, int y, int width, int height);

        /// <summary>Rotates an image clockwise.</summary>
        /// <param name="imageData">Source image bytes.</param>
        /// <param name="angle">Angle in degrees (clockwise).</param>
        /// <returns>Rotated PNG bytes.</returns>
        byte[] RotateImage(byte[] imageData, double angle);

        /// <summary>Reads image dimensions without decoding pixels into managed memory.</summary>
        /// <param name="imageData">Image bytes.</param>
        /// <returns>Width and height in pixels.</returns>
        (int Width, int Height) GetImageDimensions(byte[] imageData);
    }
}
