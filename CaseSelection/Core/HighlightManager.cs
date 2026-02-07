// ============================================================================
// CaseSelection Add-in for Autodesk Inventor 2026
// HighlightManager.cs - Visual Feedback Management
// ============================================================================

using System;
using System.Collections.Generic;
using System.Drawing;
using Inventor;

namespace CaseSelection.Core
{
    /// <summary>
    /// Manages highlight sets for visual feedback during selection.
    /// Provides solid blue face highlighting (not wireframe edges).
    /// </summary>
    public class HighlightManager : IDisposable
    {
        #region Constants

        /// <summary>
        /// Main selection highlight color (Blue RGB 0, 0, 255).
        /// </summary>
        private readonly System.Drawing.Color MAIN_SELECTION_COLOR = System.Drawing.Color.FromArgb(0, 0, 255);

        /// <summary>
        /// Template faces highlight color (Orange RGB 255, 165, 0).
        /// </summary>
        private readonly System.Drawing.Color TEMPLATE_COLOR = System.Drawing.Color.FromArgb(255, 165, 0);

        /// <summary>
        /// Preview highlight color (Green RGB 0, 128, 0).
        /// </summary>
        private readonly System.Drawing.Color PREVIEW_COLOR = System.Drawing.Color.FromArgb(0, 128, 0);

        /// <summary>
        /// Highlight opacity (0.7 = 70%).
        /// </summary>
        private const double HIGHLIGHT_OPACITY = 0.7;

        #endregion

        #region Private Fields

        private readonly Inventor.Application _inventorApp;
        private HighlightSet? _mainHighlightSet;
        private HighlightSet? _templateHighlightSet;
        private HighlightSet? _previewHighlightSet;
        private bool _disposed = false;

        #endregion

        #region Constructor

        /// <summary>
        /// Creates a new HighlightManager instance.
        /// </summary>
        public HighlightManager(Inventor.Application inventorApp)
        {
            _inventorApp = inventorApp ?? throw new ArgumentNullException(nameof(inventorApp));
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Initializes highlight sets for the active document.
        /// </summary>
        public void Initialize()
        {
            var doc = _inventorApp.ActiveDocument;
            if (doc == null) return;

            try
            {
                // Create main selection highlight set (Blue)
                _mainHighlightSet = doc.HighlightSets.Add();
                ConfigureHighlightSet(_mainHighlightSet, MAIN_SELECTION_COLOR);

                // Create template faces highlight set (Orange)
                _templateHighlightSet = doc.HighlightSets.Add();
                ConfigureHighlightSet(_templateHighlightSet, TEMPLATE_COLOR);

                // Create preview highlight set (Green)
                _previewHighlightSet = doc.HighlightSets.Add();
                ConfigureHighlightSet(_previewHighlightSet, PREVIEW_COLOR);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error initializing highlight sets: {ex.Message}");
            }
        }

        /// <summary>
        /// Updates the main selection highlight with the given faces.
        /// </summary>
        public void UpdateMainSelection(IEnumerable<Face> faces)
        {
            if (_mainHighlightSet == null) return;

            try
            {
                _mainHighlightSet.Clear();

                foreach (var face in faces)
                {
                    // Add the Face object itself for solid highlighting (not edges)
                    _mainHighlightSet.AddItem(face);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating main selection highlight: {ex.Message}");
            }
        }

        /// <summary>
        /// Updates the template faces highlight with the given faces.
        /// </summary>
        public void UpdateTemplateSelection(IEnumerable<Face> faces)
        {
            if (_templateHighlightSet == null) return;

            try
            {
                _templateHighlightSet.Clear();

                foreach (var face in faces)
                {
                    _templateHighlightSet.AddItem(face);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating template selection highlight: {ex.Message}");
            }
        }

        /// <summary>
        /// Updates the preview highlight with the given faces.
        /// </summary>
        public void UpdatePreviewSelection(IEnumerable<Face> faces)
        {
            if (_previewHighlightSet == null) return;

            try
            {
                _previewHighlightSet.Clear();

                foreach (var face in faces)
                {
                    _previewHighlightSet.AddItem(face);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating preview highlight: {ex.Message}");
            }
        }

        /// <summary>
        /// Clears the preview highlight.
        /// </summary>
        public void ClearPreview()
        {
            try
            {
                _previewHighlightSet?.Clear();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error clearing preview: {ex.Message}");
            }
        }

        /// <summary>
        /// Clears all highlights.
        /// </summary>
        public void ClearAll()
        {
            try
            {
                _mainHighlightSet?.Clear();
                _templateHighlightSet?.Clear();
                _previewHighlightSet?.Clear();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error clearing highlights: {ex.Message}");
            }
        }

        /// <summary>
        /// Refreshes the display (forces redraw).
        /// </summary>
        public void UpdateDisplay()
        {
            try
            {
                var doc = _inventorApp.ActiveDocument;
                if (doc != null)
                {
                    // Update the active view to reflect highlight changes
                    var view = _inventorApp.ActiveView;
                    view?.Update();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating display: {ex.Message}");
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Configures a highlight set with the specified color and opacity.
        /// </summary>
        private void ConfigureHighlightSet(HighlightSet highlightSet, System.Drawing.Color color)
        {
            try
            {
                // Create the Inventor color
                var inventorColor = _inventorApp.TransientObjects.CreateColor(
                    (byte)color.R,
                    (byte)color.G,
                    (byte)color.B,
                    HIGHLIGHT_OPACITY
                );

                highlightSet.Color = inventorColor;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error configuring highlight set: {ex.Message}");
            }
        }

        #endregion

        #region IDisposable Implementation

        /// <summary>
        /// Disposes of the highlight manager and cleans up resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes of managed and unmanaged resources.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                ClearAll();

                try
                {
                    // Delete highlight sets
                    _mainHighlightSet?.Delete();
                    _templateHighlightSet?.Delete();
                    _previewHighlightSet?.Delete();
                }
                catch
                {
                    // Highlight sets may already be deleted
                }

                _mainHighlightSet = null;
                _templateHighlightSet = null;
                _previewHighlightSet = null;
            }

            _disposed = true;
        }

        /// <summary>
        /// Finalizer.
        /// </summary>
        ~HighlightManager()
        {
            Dispose(false);
        }

        #endregion
    }
}
