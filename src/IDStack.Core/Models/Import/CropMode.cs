namespace IDStack.Core.Models.Import
{
    /// <summary>
    /// How a photo is cropped to fit its placeholder.
    /// </summary>
    public enum CropMode
    {
        /// <summary>Fit the whole image inside the placeholder (letterbox).</summary>
        Fit,

        /// <summary>Fill the placeholder, cropping overflow symmetrically.</summary>
        Center,

        /// <summary>Stretch to the placeholder aspect ratio without cropping.</summary>
        Stretch
    }
}
