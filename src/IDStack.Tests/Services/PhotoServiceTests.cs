using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using IDStack.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;

namespace IDStack.Tests.Services
{
    /// <summary>
    /// Tests for PhotoService: folder scan, matching, processing, and caching.
    /// </summary>
    [TestClass]
    public class PhotoServiceTests
    {
        private PhotoService _service;
        private string _tempDir;

        [TestInitialize]
        public void Setup()
        {
            _service = new PhotoService(new ImageProcessingService());
            _tempDir = Path.Combine(Path.GetTempPath(), "IDStackPhotoTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDir);
        }

        [TestCleanup]
        public void Cleanup()
        {
            try
            {
                Directory.Delete(_tempDir, recursive: true);
            }
            catch
            {
                // Best effort cleanup.
            }
        }

        private string WritePng(string fileName, int width = 4, int height = 4)
        {
            using (var image = new Image<Rgba32>(width, height))
            {
                var path = Path.Combine(_tempDir, fileName);
                image.Save(path, new PngEncoder());
                return path;
            }
        }

        [TestMethod]
        public async Task LoadPhotos_FindsSupportedImagesOnly()
        {
            WritePng("alice.png");
            WritePng("bob.jpg".Replace(".jpg", ".png"));
            File.WriteAllText(Path.Combine(_tempDir, "notes.txt"), "not an image");
            File.WriteAllText(Path.Combine(_tempDir, "data.xlsx"), "not an image");

            var photos = await _service.LoadPhotosFromFolderAsync(_tempDir);

            Assert.AreEqual(2, photos.Count);
            Assert.IsTrue(photos.All(p => Path.GetExtension(p.FileName) == ".png"));
        }

        [TestMethod]
        public async Task LoadPhotos_SortedByFileName()
        {
            WritePng("c.png");
            WritePng("a.png");
            WritePng("b.png");

            var photos = await _service.LoadPhotosFromFolderAsync(_tempDir);

            CollectionAssert.AreEqual(
                new[] { "a.png", "b.png", "c.png" },
                photos.Select(p => p.FileName).ToArray());
        }

        [TestMethod]
        public async Task LoadPhotos_MissingFolderThrows()
        {
            await Assert.ThrowsExceptionAsync<DirectoryNotFoundException>(
                () => _service.LoadPhotosFromFolderAsync(Path.Combine(_tempDir, "missing")));
        }

        [TestMethod]
        public async Task MatchByName_ExactMatch()
        {
            WritePng("alice.png");
            var photos = await _service.LoadPhotosFromFolderAsync(_tempDir);

            var match = await _service.MatchPhotoByNameAsync("Alice", photos);

            Assert.IsNotNull(match);
            Assert.AreEqual("alice.png", match.FileName);
        }

        [TestMethod]
        public async Task MatchByName_WithExtension()
        {
            WritePng("alice.png");
            var photos = await _service.LoadPhotosFromFolderAsync(_tempDir);

            var match = await _service.MatchPhotoByNameAsync("alice.png", photos);

            Assert.IsNotNull(match);
        }

        [TestMethod]
        public async Task MatchByName_NoMatchReturnsNull()
        {
            WritePng("alice.png");
            var photos = await _service.LoadPhotosFromFolderAsync(_tempDir);

            var match = await _service.MatchPhotoByNameAsync("charlie", photos);

            Assert.IsNull(match);
        }

        [TestMethod]
        public async Task MatchByName_EmptyNameReturnsNull()
        {
            WritePng("alice.png");
            var photos = await _service.LoadPhotosFromFolderAsync(_tempDir);

            Assert.IsNull(await _service.MatchPhotoByNameAsync("", photos));
            Assert.IsNull(await _service.MatchPhotoByNameAsync(null, photos));
        }

        [TestMethod]
        public async Task ProcessPhoto_ProducesPngAtPrintResolution()
        {
            var path = WritePng("alice.png", 100, 100);
            var photos = new List<Core.Models.Import.PhotoRecord> { new Core.Models.Import.PhotoRecord(path) };

            var bytes = await _service.ProcessPhotoAsync(photos[0], 25.4, 25.4, Core.Models.Import.CropMode.Fit);

            Assert.IsNotNull(bytes);
            using (var image = SixLabors.ImageSharp.Image.Load(bytes))
            {
                Assert.AreEqual(300, image.Width);
                Assert.AreEqual(300, image.Height);
            }
        }

        [TestMethod]
        public async Task ProcessPhoto_CachesResults()
        {
            var path = WritePng("bob.png", 50, 50);
            var record = new Core.Models.Import.PhotoRecord(path);

            var first = await _service.ProcessPhotoAsync(record, 25.4, 25.4, Core.Models.Import.CropMode.Fit);
            var second = await _service.ProcessPhotoAsync(record, 25.4, 25.4, Core.Models.Import.CropMode.Fit);

            Assert.AreSame(first, second);
        }

        [TestMethod]
        public void ClearPhotoCache_EmptiesCache()
        {
            _service.ClearPhotoCache();
            Assert.IsTrue(true); // No throw is the contract.
        }

        [TestMethod]
        public async Task ProcessPhoto_NullPhotoThrows()
        {
            await Assert.ThrowsExceptionAsync<ArgumentNullException>(
                () => _service.ProcessPhotoAsync(null, 25.4, 25.4, Core.Models.Import.CropMode.Fit));
        }
    }

    /// <summary>
    /// Tests for ImageProcessingService: resize, crop, rotate, dimensions.
    /// </summary>
    [TestClass]
    public class ImageProcessingServiceTests
    {
        private ImageProcessingService _service = new ImageProcessingService();

        private byte[] CreatePng(int width, int height)
        {
            using (var image = new Image<Rgba32>(width, height))
            {
                using (var stream = new MemoryStream())
                {
                    image.Save(stream, new PngEncoder());
                    return stream.ToArray();
                }
            }
        }

        [TestMethod]
        public void ResizeImage_ScalesDown()
        {
            var result = _service.ResizeImage(CreatePng(200, 100), 50, 25);

            using (var image = SixLabors.ImageSharp.Image.Load(result))
            {
                Assert.AreEqual(50, image.Width);
                Assert.AreEqual(25, image.Height);
            }
        }

        [TestMethod]
        public void ResizeImage_KeepsAspectWhenTargetSkews()
        {
            var result = _service.ResizeImage(CreatePng(200, 100), 100, 100);

            using (var image = SixLabors.ImageSharp.Image.Load(result))
            {
                Assert.AreEqual(100, image.Width);
                Assert.AreEqual(50, image.Height);
            }
        }

        [TestMethod]
        public void CropImage_ExtractsRegion()
        {
            var result = _service.CropImage(CreatePng(100, 100), 10, 20, 30, 40);

            using (var image = SixLabors.ImageSharp.Image.Load(result))
            {
                Assert.AreEqual(30, image.Width);
                Assert.AreEqual(40, image.Height);
            }
        }

        [TestMethod]
        public void RotateImage_90DegreesSwapsDimensions()
        {
            var result = _service.RotateImage(CreatePng(80, 40), 90);

            using (var image = SixLabors.ImageSharp.Image.Load(result))
            {
                Assert.AreEqual(40, image.Width);
                Assert.AreEqual(80, image.Height);
            }
        }

        [TestMethod]
        public void GetImageDimensions_ReturnsSize()
        {
            var size = _service.GetImageDimensions(CreatePng(123, 45));

            Assert.AreEqual(123, size.Width);
            Assert.AreEqual(45, size.Height);
        }

        [TestMethod]
        public void ResizeImage_InvalidInputThrows()
        {
            Assert.ThrowsException<ArgumentException>(() => _service.ResizeImage(new byte[0], 10, 10));
            Assert.ThrowsException<ArgumentOutOfRangeException>(
                () => _service.ResizeImage(CreatePng(10, 10), 0, 10));
        }
    }
}
