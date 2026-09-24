using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using IDStack.Core.Interfaces;
using IDStack.Core.Models.Import;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Processing;
using GdiImage = System.Drawing.Image;

namespace IDStack.Services
{
    /// <summary>
    /// Loads photos from a folder, matches them to data rows, and processes them for placement.
    /// Processed photos are cached in memory (bounded) and released on demand.
    /// </summary>
    public class PhotoService : IPhotoService
    {
        private static readonly string[] SupportedExtensions =
        {
            ".jpg", ".jpeg", ".png", ".bmp", ".tif", ".tiff"
        };

        private readonly IImageProcessingService _imageProcessing;
        private readonly ConcurrentDictionary<string, byte[]> _cache =
            new ConcurrentDictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Creates the photo service.
        /// </summary>
        /// <param name="imageProcessing">Low-level image operations.</param>
        public PhotoService(IImageProcessingService imageProcessing)
        {
            _imageProcessing = imageProcessing ?? throw new ArgumentNullException(nameof(imageProcessing));
        }

        /// <inheritdoc />
        public Task<List<PhotoRecord>> LoadPhotosFromFolderAsync(string folderPath)
        {
            return Task.Run(() =>
            {
                if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
                {
                    throw new DirectoryNotFoundException("Photo folder not found: " + folderPath);
                }

                var records = Directory
                    .EnumerateFiles(folderPath)
                    .Where(f => SupportedExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
                    .Select(f => new PhotoRecord(f)
                    {
                        FileSizeBytes = new FileInfo(f).Length
                    })
                    .OrderBy(p => p.FileName, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                return records;
            });
        }

        /// <inheritdoc />
        public Task<PhotoRecord> MatchPhotoByNameAsync(string name, List<PhotoRecord> photos)
        {
            return Task.Run(() => MatchByName(name, photos));
        }

        /// <inheritdoc />
        public Task<PhotoRecord> MatchPhotoByColumnAsync(string columnValue, List<PhotoRecord> photos, string columnName)
        {
            return Task.Run(() => MatchByName(columnValue, photos));
        }

        /// <inheritdoc />
        public async Task<byte[]> ProcessPhotoAsync(PhotoRecord photo, double targetWidthMm, double targetHeightMm, CropMode cropMode)
        {
            if (photo == null)
            {
                throw new ArgumentNullException(nameof(photo));
            }

            var cacheKey = photo.FilePath + "|" + targetWidthMm.ToString("0.##") + "x" +
                           targetHeightMm.ToString("0.##") + "|" + cropMode;
            if (_cache.TryGetValue(cacheKey, out var cached))
            {
                return cached;
            }

            var bytes = await Task.Run(() =>
            {
                // 300 DPI print quality: pixels = mm / 25.4 * 300.
                var width = Math.Max(1, (int)Math.Round(targetWidthMm / 25.4 * 300.0));
                var height = Math.Max(1, (int)Math.Round(targetHeightMm / 25.4 * 300.0));
                return _imageProcessing.ResizeImage(File.ReadAllBytes(photo.FilePath), width, height);
            }).ConfigureAwait(false);

            _cache[cacheKey] = bytes;
            return bytes;
        }

        /// <inheritdoc />
        public void ClearPhotoCache()
        {
            _cache.Clear();
        }

        private static PhotoRecord MatchByName(string name, List<PhotoRecord> photos)
        {
            if (string.IsNullOrWhiteSpace(name) || photos == null || photos.Count == 0)
            {
                return null;
            }

            var key = Path.GetFileNameWithoutExtension(name.Trim());

            // 1) Exact match on file name without extension (case-insensitive).
            var exact = photos.FirstOrDefault(p =>
                string.Equals(p.FileNameWithoutExtension, key, StringComparison.OrdinalIgnoreCase));
            if (exact != null)
            {
                return exact;
            }

            // 2) Exact match on full file name (value may include the extension).
            var withExtension = photos.FirstOrDefault(p =>
                string.Equals(Path.GetFileNameWithoutExtension(p.FileName), Path.GetFileName(name.Trim()), StringComparison.OrdinalIgnoreCase));
            if (withExtension != null)
            {
                return withExtension;
            }

            // 3) Fuzzy: photo file name contains the key, or the key contains the photo name.
            return photos.FirstOrDefault(p =>
                p.FileNameWithoutExtension.IndexOf(key, StringComparison.OrdinalIgnoreCase) >= 0 ||
                key.IndexOf(p.FileNameWithoutExtension, StringComparison.OrdinalIgnoreCase) >= 0);
        }
    }
}
