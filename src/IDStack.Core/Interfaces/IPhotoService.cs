using System.Collections.Generic;
using System.Threading.Tasks;
using IDStack.Core.Models.Import;

namespace IDStack.Core.Interfaces
{
    /// <summary>
    /// Loads photos from folders, matches them to data rows, and prepares them for placement.
    /// </summary>
    public interface IPhotoService
    {
        /// <summary>Scans a folder for supported image files.</summary>
        /// <param name="folderPath">Folder to scan (non-recursive).</param>
        /// <returns>All photos found, ordered by file name.</returns>
        Task<List<PhotoRecord>> LoadPhotosFromFolderAsync(string folderPath);

        /// <summary>Finds the photo whose file name (without extension) equals the given name, case-insensitively.</summary>
        /// <param name="name">Name to match.</param>
        /// <param name="photos">Candidate photos.</param>
        /// <returns>The match, or null.</returns>
        Task<PhotoRecord> MatchPhotoByNameAsync(string name, List<PhotoRecord> photos);

        /// <summary>Finds the photo whose file name matches a column's cell value.</summary>
        /// <param name="columnValue">Cell text (a file name or key).</param>
        /// <param name="photos">Candidate photos.</param>
        /// <param name="columnName">Column the value came from (reserved for column-specific rules).</param>
        /// <returns>The match, or null.</returns>
        Task<PhotoRecord> MatchPhotoByColumnAsync(string columnValue, List<PhotoRecord> photos, string columnName);

        /// <summary>Resizes and crops a photo to the target size in pixels.</summary>
        /// <param name="photo">Photo to process.</param>
        /// <param name="targetWidthMm">Target width in millimeters.</param>
        /// <param name="targetHeightMm">Target height in millimeters.</param>
        /// <param name="cropMode">Crop strategy.</param>
        /// <returns>Encoded PNG bytes of the processed image.</returns>
        Task<byte[]> ProcessPhotoAsync(PhotoRecord photo, double targetWidthMm, double targetHeightMm, CropMode cropMode);

        /// <summary>Drops all cached processed images.</summary>
        void ClearPhotoCache();
    }
}
