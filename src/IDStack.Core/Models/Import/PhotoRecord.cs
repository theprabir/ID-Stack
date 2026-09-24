using System;

namespace IDStack.Core.Models.Import
{
    /// <summary>
    /// A photo discovered in an import folder, with its matched data row.
    /// </summary>
    public class PhotoRecord
    {
        /// <summary>Creates a photo record.</summary>
        /// <param name="filePath">Full path to the image file.</param>
        public PhotoRecord(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentException("File path is required.", nameof(filePath));
            }

            FilePath = filePath;
            FileName = System.IO.Path.GetFileName(filePath);
            FileNameWithoutExtension = System.IO.Path.GetFileNameWithoutExtension(filePath);
        }

        /// <summary>Full path to the image file.</summary>
        public string FilePath { get; }

        /// <summary>File name with extension.</summary>
        public string FileName { get; }

        /// <summary>File name without extension — the default matching key.</summary>
        public string FileNameWithoutExtension { get; }

        /// <summary>File size in bytes.</summary>
        public long FileSizeBytes { get; set; }

        /// <summary>Data-row key this photo is matched to (typically a cell value).</summary>
        public string MatchKey { get; set; }

        /// <summary>Whether the photo has been matched to a data row.</summary>
        public bool IsMatched => !string.IsNullOrEmpty(MatchKey);

        /// <summary>Returns the file name for display.</summary>
        /// <returns>The file name.</returns>
        public override string ToString()
        {
            return FileName;
        }
    }
}
