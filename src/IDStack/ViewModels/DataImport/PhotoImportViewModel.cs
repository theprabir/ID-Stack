using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using IDStack.Commands;
using IDStack.Core.Interfaces;
using IDStack.Core.Models.Import;

namespace IDStack.ViewModels.DataImport
{
    /// <summary>
    /// Loads a photo folder, matches photos to data rows, and reports match coverage.
    /// </summary>
    public class PhotoImportViewModel : ViewModelBase
    {
        private readonly IPhotoService _photoService;

        private string _folderPath;
        private string _statusText;
        private bool _isLoading;
        private string _matchColumnName;

        /// <summary>
        /// Creates the photo import view model.
        /// </summary>
        /// <param name="photoService">Photo service.</param>
        public PhotoImportViewModel(IPhotoService photoService)
        {
            _photoService = photoService ?? throw new ArgumentNullException(nameof(photoService));

            BrowseFolderCommand = new RelayCommand(_ => FolderBrowseRequested?.Invoke(this, EventArgs.Empty), _ => !IsLoading);
            RematchCommand = new RelayCommand(_ => RematchRequested?.Invoke(this, EventArgs.Empty), _ => HasPhotos);
        }

        /// <summary>Raised when the view model wants a folder dialog.</summary>
        public event EventHandler FolderBrowseRequested;

        /// <summary>Raised when the user asks to re-run matching.</summary>
        public event EventHandler RematchRequested;

        /// <summary>Browse-for-folder command.</summary>
        public RelayCommand BrowseFolderCommand { get; }

        /// <summary>Re-run photo matching command.</summary>
        public RelayCommand RematchCommand { get; }

        /// <summary>All loaded photos.</summary>
        public ObservableCollection<PhotoRecord> Photos { get; } = new ObservableCollection<PhotoRecord>();

        /// <summary>Selected folder path.</summary>
        public string FolderPath
        {
            get { return _folderPath; }
            set { SetProperty(ref _folderPath, value); }
        }

        /// <summary>Data column holding photo file names, or empty for name matching.</summary>
        public string MatchColumnName
        {
            get { return _matchColumnName; }
            set { SetProperty(ref _matchColumnName, value); }
        }

        /// <summary>Status line (photo/match counts, errors).</summary>
        public string StatusText
        {
            get { return _statusText; }
            private set { SetProperty(ref _statusText, value); }
        }

        /// <summary>Whether a folder scan is in progress.</summary>
        public bool IsLoading
        {
            get { return _isLoading; }
            private set { SetProperty(ref _isLoading, value); }
        }

        /// <summary>Number of photos matched to at least one row.</summary>
        public int MatchedCount
        {
            get { return Photos.Count(p => p.IsMatched); }
        }

        /// <summary>Whether any photos are loaded.</summary>
        public bool HasPhotos => Photos.Count > 0;

        /// <summary>
        /// Scans the given folder for images.
        /// </summary>
        /// <param name="path">Folder to scan.</param>
        /// <returns>Task completing when the scan finishes.</returns>
        public async Task LoadFolderAsync(string path)
        {
            IsLoading = true;
            FolderPath = path;
            StatusText = "Scanning…";
            try
            {
                var photos = await _photoService.LoadPhotosFromFolderAsync(path).ConfigureAwait(true);
                Photos.Clear();
                foreach (var photo in photos)
                {
                    Photos.Add(photo);
                }

                StatusText = Photos.Count + " photos found in " + Path.GetFileName(path.TrimEnd('\\', '/')) + ".";
            }
            catch (Exception ex) when (ex is DirectoryNotFoundException || ex is UnauthorizedAccessException ||
                                       ex is ArgumentException)
            {
                StatusText = "Could not read the folder: " + ex.Message;
                Photos.Clear();
            }
            finally
            {
                IsLoading = false;
                OnPropertyChanged(nameof(HasPhotos));
            }
        }

        /// <summary>
        /// Matches loaded photos to data rows using the selected strategy.
        /// </summary>
        /// <param name="data">Imported data.</param>
        /// <param name="keyColumn">
        /// Column whose values are matched against photo file names; null uses every string column.
        /// </param>
        /// <returns>Task completing when matching finishes.</returns>
        public async Task MatchToDataAsync(ExcelData data, string keyColumn)
        {
            if (data == null || Photos.Count == 0)
            {
                return;
            }

            await Task.Run(() =>
            {
                var photoList = Photos.ToList();
                foreach (var photo in photoList)
                {
                    photo.MatchKey = null;
                }

                var columns = string.IsNullOrWhiteSpace(keyColumn)
                    ? data.ColumnNames
                    : new List<string> { keyColumn };

                foreach (var row in data.Rows)
                {
                    foreach (var column in columns)
                    {
                        var value = row.Get(column);
                        var match = _photoService.MatchPhotoByColumnAsync(value, photoList, column).Result;
                        if (match != null && !match.IsMatched)
                        {
                            match.MatchKey = row.Get(data.ColumnNames[0]);
                            break;
                        }
                    }
                }
            }).ConfigureAwait(true);

            OnPropertyChanged(nameof(MatchedCount));
            StatusText = MatchedCount + " of " + Photos.Count + " photos matched to rows.";
        }
    }
}
