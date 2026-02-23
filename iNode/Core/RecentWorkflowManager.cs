// ============================================================================
// iNode Add-in for Autodesk Inventor 2026
// RecentWorkflowManager.cs - Tracks recently used .inode workflow files
// ============================================================================

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace iNode.Core
{
    /// <summary>
    /// Manages a list of recently opened/saved .inode workflow files.
    /// Persists to %APPDATA%\iNode\recent.json.
    /// </summary>
    public static class RecentWorkflowManager
    {
        private const int MAX_RECENT = 10;

        private static readonly string _settingsDir =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "iNode");

        private static readonly string _settingsFile =
            Path.Combine(_settingsDir, "recent.json");

        private static List<string> _recentFiles = new List<string>();
        private static bool _loaded;

        /// <summary>
        /// Gets the list of recent workflow file paths (most recent first).
        /// </summary>
        public static IReadOnlyList<string> RecentFiles
        {
            get
            {
                EnsureLoaded();
                return _recentFiles;
            }
        }

        /// <summary>
        /// Adds a file path to the top of the recent list.
        /// Removes duplicates and trims to MAX_RECENT.
        /// </summary>
        public static void AddRecent(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath)) return;

            EnsureLoaded();

            // Normalize path
            string normalized = Path.GetFullPath(filePath);

            // Remove existing duplicate (case-insensitive on Windows)
            _recentFiles.RemoveAll(f =>
                string.Equals(f, normalized, StringComparison.OrdinalIgnoreCase));

            // Insert at top
            _recentFiles.Insert(0, normalized);

            // Trim
            if (_recentFiles.Count > MAX_RECENT)
                _recentFiles = _recentFiles.Take(MAX_RECENT).ToList();

            Save();
        }

        /// <summary>
        /// Removes entries whose files no longer exist on disk.
        /// </summary>
        public static void PruneInvalid()
        {
            EnsureLoaded();
            int removed = _recentFiles.RemoveAll(f => !File.Exists(f));
            if (removed > 0) Save();
        }

        private static void EnsureLoaded()
        {
            if (_loaded) return;
            _loaded = true;

            try
            {
                if (File.Exists(_settingsFile))
                {
                    string json = File.ReadAllText(_settingsFile);
                    var list = JsonConvert.DeserializeObject<List<string>>(json);
                    if (list != null)
                        _recentFiles = list;
                }
            }
            catch
            {
                _recentFiles = new List<string>();
            }
        }

        private static void Save()
        {
            try
            {
                if (!Directory.Exists(_settingsDir))
                    Directory.CreateDirectory(_settingsDir);

                string json = JsonConvert.SerializeObject(_recentFiles, Formatting.Indented);
                File.WriteAllText(_settingsFile, json);
            }
            catch
            {
                // Silently ignore write failures
            }
        }
    }
}
