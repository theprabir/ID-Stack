using System;
using System.Collections.Generic;

namespace IDCardSoftware.Services
{
    /// <summary>
    /// Navigates between named application views hosted in the main window.
    /// </summary>
    public interface INavigationService
    {
        /// <summary>Raised after navigation so the shell can update visuals.</summary>
        event EventHandler NavigationChanged;

        /// <summary>Gets the current page key (e.g. "Home").</summary>
        string CurrentKey { get; }

        /// <summary>Gets the ViewModel instance registered for the current page.</summary>
        object CurrentViewModel { get; }

        /// <summary>Navigates to the page registered under the given key.</summary>
        /// <param name="key">Page key.</param>
        void NavigateTo(string key);

        /// <summary>Registers a page with its ViewModel factory.</summary>
        /// <param name="key">Page key.</param>
        /// <param name="viewModelFactory">Factory creating the page ViewModel.</param>
        void Register(string key, Func<object> viewModelFactory);
    }

    /// <summary>
    /// Simple registry-based navigation between ViewModels, surfaced through a
    /// DataTemplate mapping in the main window.
    /// </summary>
    public class NavigationService : INavigationService
    {
        private readonly Dictionary<string, Func<object>> _pages =
            new Dictionary<string, Func<object>>(StringComparer.OrdinalIgnoreCase);

        private readonly Dictionary<string, object> _cache = new Dictionary<string, object>();

        /// <inheritdoc />
        public event EventHandler NavigationChanged;

        /// <inheritdoc />
        public string CurrentKey { get; private set; }

        /// <inheritdoc />
        public object CurrentViewModel { get; private set; }

        /// <inheritdoc />
        public void Register(string key, Func<object> viewModelFactory)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("Key is required", nameof(key));
            }

            _pages[key] = viewModelFactory ?? throw new ArgumentNullException(nameof(viewModelFactory));
        }

        /// <inheritdoc />
        public void NavigateTo(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("Key is required", nameof(key));
            }

            if (!_pages.ContainsKey(key))
            {
                throw new InvalidOperationException("No page registered with key: " + key);
            }

            if (key == CurrentKey)
            {
                return;
            }

            if (!_cache.TryGetValue(key, out var viewModel))
            {
                viewModel = _pages[key]();
                _cache[key] = viewModel;
            }

            CurrentKey = key;
            CurrentViewModel = viewModel;
            NavigationChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
