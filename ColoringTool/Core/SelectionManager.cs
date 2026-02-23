// ============================================================================
// ColoringTool Add-in for Autodesk Inventor 2026
// SelectionManager.cs - Core Selection Logic with Color Application
// ============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Inventor;

namespace ColoringTool.Core
{
    /// <summary>
    /// Manages the selection process using InteractionEvents.
    /// Uses Inventor's native SelectSet for visualization.
    /// Supports applying colors to selected faces.
    /// </summary>
    public class SelectionManager : IDisposable
    {
        #region Events

        /// <summary>
        /// Raised when the selected face count changes.
        /// </summary>
        public event EventHandler<int>? SelectionChanged;

        /// <summary>
        /// Raised when the template face count changes.
        /// </summary>
        public event EventHandler<int>? TemplateFacesChanged;

        /// <summary>
        /// Raised when the color face count changes.
        /// </summary>
        public event EventHandler<int>? ColorFacesChanged;

        #endregion

        #region Private Fields

        private readonly Inventor.Application _inventorApp;
        private readonly TopologyAnalyzer _topologyAnalyzer;

        private InteractionEvents? _interactionEvents;
        private MouseEvents? _mouseEvents;
        private SelectEvents? _selectEvents;
        private HighlightSet? _highlightSet;         // Blue highlight for main selection
        private HighlightSet? _templateHighlightSet; // Orange highlight for template faces
        private HighlightSet? _colorHighlightSet;    // Purple highlight for color-matched faces

        private readonly HashSet<Face> _selectedFaces;
        private readonly HashSet<Face> _templateFaces;
        private readonly HashSet<Face> _colorFaces;
        private SelectionFilterType _currentFilter;
        private bool _isInteractionActive;
        private bool _isTemplateSelectionMode;       // True when selecting template faces
        private bool _disposed;
        private bool _isPickColorMode;                 // True when in pick color mode
        private Action<System.Drawing.Color?>? _pickColorCallback;  // Callback for pick color mode

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets the count of currently selected faces.
        /// </summary>
        public int SelectedFaceCount => _selectedFaces.Count;

        /// <summary>
        /// Gets the count of template faces.
        /// </summary>
        public int TemplateFaceCount => _templateFaces.Count;

        /// <summary>
        /// Gets the count of color-matched faces.
        /// </summary>
        public int ColorFaceCount => _colorFaces.Count;

        /// <summary>
        /// Gets the current selection filter type.
        /// </summary>
        public SelectionFilterType CurrentFilter => _currentFilter;

        /// <summary>
        /// Gets whether interaction is currently active.
        /// </summary>
        public bool IsInteractionActive => _isInteractionActive;

        /// <summary>
        /// Gets or sets whether we're in template selection mode.
        /// </summary>
        public bool IsTemplateSelectionMode
        {
            get => _isTemplateSelectionMode;
            set => _isTemplateSelectionMode = value;
        }

        /// <summary>
        /// Gets the currently selected faces.
        /// </summary>
        public IReadOnlyCollection<Face> SelectedFaces => _selectedFaces;

        #endregion

        #region Constructor

        /// <summary>
        /// Creates a new SelectionManager instance.
        /// </summary>
        public SelectionManager(Inventor.Application inventorApp)
        {
            _inventorApp = inventorApp ?? throw new ArgumentNullException(nameof(inventorApp));
            _topologyAnalyzer = new TopologyAnalyzer(inventorApp);

            _selectedFaces = new HashSet<Face>();
            _templateFaces = new HashSet<Face>();
            _colorFaces = new HashSet<Face>();
            _currentFilter = SelectionFilterType.SingleFace;
            _isInteractionActive = false;
            _isTemplateSelectionMode = false;
            _disposed = false;
        }

        #endregion

        #region Interaction Management

        /// <summary>
        /// Starts the interaction session.
        /// </summary>
        public void StartInteraction()
        {
            if (_isInteractionActive) return;

            try
            {
                var doc = _inventorApp.ActiveDocument;
                if (doc != null)
                {
                    // Create highlight set for main selection (blue, opacity 0.3)
                    _highlightSet = doc.HighlightSets.Add();
                    var blueColor = _inventorApp.TransientObjects.CreateColor(0, 120, 215, 0.3);
                    _highlightSet.Color = blueColor;

                    // Create highlight set for template faces (orange, opacity 0.3)
                    _templateHighlightSet = doc.HighlightSets.Add();
                    var orangeColor = _inventorApp.TransientObjects.CreateColor(255, 140, 0, 0.3);
                    _templateHighlightSet.Color = orangeColor;

                    // Create highlight set for color-matched faces (purple, opacity 0.3)
                    _colorHighlightSet = doc.HighlightSets.Add();
                    var purpleColor = _inventorApp.TransientObjects.CreateColor(128, 0, 128, 0.3);
                    _colorHighlightSet.Color = purpleColor;
                }

                // Create interaction events
                _interactionEvents = _inventorApp.CommandManager.CreateInteractionEvents();

                // Configure interaction
                _interactionEvents.InteractionDisabled = false;
                _interactionEvents.StatusBarText = "Coloring Tool: Click faces to select. Shift+Click to deselect.";

                // Get mouse and select events
                _mouseEvents = _interactionEvents.MouseEvents;
                _selectEvents = _interactionEvents.SelectEvents;

                // Configure select events
                _selectEvents.AddSelectionFilter(SelectionFilterEnum.kPartFaceFilter);
                _selectEvents.SingleSelectEnabled = true;
                _selectEvents.WindowSelectEnabled = true;

                // Subscribe to events
                _mouseEvents.OnMouseClick += OnMouseClick;
                _selectEvents.OnPreSelect += OnPreSelect;
                _selectEvents.OnSelect += OnSelect;

                // Start interaction
                _interactionEvents.Start();
                _isInteractionActive = true;

                // Update display with current selection
                UpdateHighlights();

                System.Diagnostics.Debug.WriteLine("Interaction started.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error starting interaction: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Stops the interaction session.
        /// </summary>
        public void StopInteraction()
        {
            if (!_isInteractionActive) return;

            try
            {
                // Unsubscribe from events
                if (_mouseEvents != null)
                {
                    _mouseEvents.OnMouseClick -= OnMouseClick;
                }

                if (_selectEvents != null)
                {
                    _selectEvents.OnPreSelect -= OnPreSelect;
                    _selectEvents.OnSelect -= OnSelect;
                }

                // Stop and cleanup interaction events
                if (_interactionEvents != null)
                {
                    try
                    {
                        _interactionEvents.Stop();
                    }
                    catch { }
                }

                // Cleanup main highlight set
                if (_highlightSet != null)
                {
                    try
                    {
                        _highlightSet.Clear();
                        _highlightSet.Delete();
                    }
                    catch { }
                    _highlightSet = null;
                }

                // Cleanup template highlight set
                if (_templateHighlightSet != null)
                {
                    try
                    {
                        _templateHighlightSet.Clear();
                        _templateHighlightSet.Delete();
                    }
                    catch { }
                    _templateHighlightSet = null;
                }

                // Cleanup color highlight set
                if (_colorHighlightSet != null)
                {
                    try
                    {
                        _colorHighlightSet.Clear();
                        _colorHighlightSet.Delete();
                    }
                    catch { }
                    _colorHighlightSet = null;
                }

                _interactionEvents = null;
                _mouseEvents = null;
                _selectEvents = null;
                _isInteractionActive = false;
                _isTemplateSelectionMode = false;

                System.Diagnostics.Debug.WriteLine("Interaction stopped.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error stopping interaction: {ex.Message}");
            }
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Handles mouse click events.
        /// </summary>
        private void OnMouseClick(
            MouseButtonEnum Button,
            ShiftStateEnum ShiftKeys,
            Inventor.Point ModelPosition,
            Point2d ViewPosition,
            Inventor.View View)
        {
            // Mouse click handling is done via OnSelect for face selection
        }

        /// <summary>
        /// Handles pre-selection (hover) events.
        /// </summary>
        private void OnPreSelect(
            ref object PreSelectEntity,
            out bool DoHighlight,
            ref ObjectCollection MorePreSelectEntities,
            SelectionDeviceEnum SelectionDevice,
            Inventor.Point ModelPosition,
            Point2d ViewPosition,
            Inventor.View View)
        {
            DoHighlight = false;

            try
            {
                if (PreSelectEntity is Face)
                {
                    DoHighlight = true;
                }
            }
            catch
            {
                DoHighlight = false;
            }
        }

        /// <summary>
        /// Handles selection events.
        /// </summary>
        private void OnSelect(
            ObjectsEnumerator JustSelectedEntities,
            SelectionDeviceEnum SelectionDevice,
            Inventor.Point ModelPosition,
            Point2d ViewPosition,
            Inventor.View View)
        {
            try
            {
                // Check if we're in pick color mode
                if (_isPickColorMode && _pickColorCallback != null)
                {
                    foreach (object entity in JustSelectedEntities)
                    {
                        if (entity is Face face)
                        {
                            var color = GetColorFromFace(face);
                            _isPickColorMode = false;
                            var callback = _pickColorCallback;
                            _pickColorCallback = null;
                            
                            // Reset selections before callback
                            if (_selectEvents != null)
                            {
                                try { _selectEvents.ResetSelections(); } catch { }
                            }
                            
                            callback?.Invoke(color);
                            return;
                        }
                    }
                    return;
                }

                bool isShiftHeld = (Control.ModifierKeys & Keys.Shift) == Keys.Shift;

                foreach (object entity in JustSelectedEntities)
                {
                    if (entity is Face face)
                    {
                        ProcessFaceSelection(face, isShiftHeld);
                    }
                }

                if (_selectEvents != null)
                {
                    try
                    {
                        _selectEvents.ResetSelections();
                    }
                    catch { }
                }

                UpdateHighlights();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in OnSelect: {ex.Message}");
            }
        }

        #endregion

        #region Selection Processing

        /// <summary>
        /// Processes a face selection based on current filter and shift state.
        /// In template mode, adds to template faces instead of main selection.
        /// </summary>
        private void ProcessFaceSelection(Face seedFace, bool isDeselecting)
        {
            var facesToProcess = _topologyAnalyzer.GetFaces(seedFace, _currentFilter);

            // Determine which collection to modify based on mode
            var targetCollection = _isTemplateSelectionMode ? _templateFaces : _selectedFaces;

            bool seedAlreadySelected = targetCollection.Contains(seedFace);

            if (isDeselecting || seedAlreadySelected)
            {
                foreach (var face in facesToProcess)
                {
                    targetCollection.Remove(face);
                }
                System.Diagnostics.Debug.WriteLine($"Deselected {facesToProcess.Count} faces.");
            }
            else
            {
                foreach (var face in facesToProcess)
                {
                    targetCollection.Add(face);
                }
                System.Diagnostics.Debug.WriteLine($"Selected {facesToProcess.Count} faces.");
            }

            UpdateHighlights();

            // Notify listeners based on which collection was modified
            if (_isTemplateSelectionMode)
            {
                TemplateFacesChanged?.Invoke(this, _templateFaces.Count);
            }
            else
            {
                SelectionChanged?.Invoke(this, _selectedFaces.Count);
            }
        }

        /// <summary>
        /// Clears all faces (both main and template selections).
        /// Returns true if any faces were cleared.
        /// Preserves the current filter type and ensures interaction remains active.
        /// </summary>
        public bool ClearAllFaces()
        {
            bool hadFaces = _selectedFaces.Count > 0 || _templateFaces.Count > 0 || _colorFaces.Count > 0;
            
            // Preserve the current filter type
            var savedFilter = _currentFilter;

            _selectedFaces.Clear();
            _templateFaces.Clear();
            _colorFaces.Clear();
            _isTemplateSelectionMode = false;
            
            // Clear highlights
            UpdateHighlights();
            
            // CRITICAL: Force restart the interaction to ensure it's properly active
            // ESC key may have terminated the InteractionEvents behind the scenes
            ForceRestartInteraction();
            
            // Restore the filter type
            _currentFilter = savedFilter;

            SelectionChanged?.Invoke(this, 0);
            TemplateFacesChanged?.Invoke(this, 0);
            ColorFacesChanged?.Invoke(this, 0);

            return hadFaces;
        }

        /// <summary>
        /// Force restarts the interaction session.
        /// This is needed after ESC is pressed because Inventor terminates InteractionEvents.
        /// </summary>
        private void ForceRestartInteraction()
        {
            try
            {
                // Stop current interaction if any
                if (_interactionEvents != null)
                {
                    try
                    {
                        // Unsubscribe from events first
                        if (_mouseEvents != null)
                        {
                            _mouseEvents.OnMouseClick -= OnMouseClick;
                        }
                        if (_selectEvents != null)
                        {
                            _selectEvents.OnPreSelect -= OnPreSelect;
                            _selectEvents.OnSelect -= OnSelect;
                        }
                        _interactionEvents.Stop();
                    }
                    catch { }
                    _interactionEvents = null;
                    _mouseEvents = null;
                    _selectEvents = null;
                }
                
                // Mark as inactive so StartInteraction will work
                _isInteractionActive = false;
                
                // Restart interaction
                StartInteraction();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in ForceRestartInteraction: {ex.Message}");
            }
        }

        /// <summary>
        /// Updates both highlight sets to visualize selected and template faces.
        /// </summary>
        private void UpdateHighlights()
        {
            try
            {
                // Update main selection highlights (blue)
                if (_highlightSet != null)
                {
                    _highlightSet.Clear();
                    foreach (var face in _selectedFaces)
                    {
                        try
                        {
                            _highlightSet.AddItem(face);
                        }
                        catch { }
                    }
                }

                // Update template highlights (orange)
                if (_templateHighlightSet != null)
                {
                    _templateHighlightSet.Clear();
                    foreach (var face in _templateFaces)
                    {
                        try
                        {
                            _templateHighlightSet.AddItem(face);
                        }
                        catch { }
                    }
                }

                // Update color highlights (purple)
                if (_colorHighlightSet != null)
                {
                    _colorHighlightSet.Clear();
                    foreach (var face in _colorFaces)
                    {
                        try
                        {
                            _colorHighlightSet.AddItem(face);
                        }
                        catch { }
                    }
                }

                _inventorApp.ActiveView?.Update();

                System.Diagnostics.Debug.WriteLine($"Highlights updated: {_selectedFaces.Count} main, {_templateFaces.Count} template, {_colorFaces.Count} color");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating highlights: {ex.Message}");
            }
        }

        /// <summary>
        /// Resets the selection manager for a new session.
        /// </summary>
        public void Reset()
        {
            StopInteraction();
            _selectedFaces.Clear();
            _templateFaces.Clear();
            _colorFaces.Clear();
            _currentFilter = SelectionFilterType.SingleFace;
            _isTemplateSelectionMode = false;
            System.Diagnostics.Debug.WriteLine("SelectionManager reset.");
        }

        #endregion

        #region Public Selection Methods

        /// <summary>
        /// Sets the current selection filter type.
        /// </summary>
        public void SetFilterType(SelectionFilterType filterType)
        {
            _currentFilter = filterType;
            System.Diagnostics.Debug.WriteLine($"Filter changed to: {filterType}");
        }

        /// <summary>
        /// Selects all visible faces in the active document.
        /// </summary>
        public void SelectAllVisibleFaces()
        {
            try
            {
                var doc = _inventorApp.ActiveDocument;
                if (doc == null) return;

                if (doc is PartDocument partDoc)
                {
                    var compDef = partDoc.ComponentDefinition;
                    foreach (SurfaceBody body in compDef.SurfaceBodies)
                    {
                        foreach (Face face in body.Faces)
                        {
                            _selectedFaces.Add(face);
                        }
                    }
                }
                else if (doc is AssemblyDocument asmDoc)
                {
                    var compDef = asmDoc.ComponentDefinition;
                    foreach (ComponentOccurrence occ in compDef.Occurrences)
                    {
                        AddFacesFromOccurrence(occ);
                    }
                }

                UpdateHighlights();
                SelectionChanged?.Invoke(this, _selectedFaces.Count);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error selecting all faces: {ex.Message}");
            }
        }

        /// <summary>
        /// Adds faces from an occurrence.
        /// </summary>
        private void AddFacesFromOccurrence(ComponentOccurrence occurrence)
        {
            try
            {
                foreach (SurfaceBody body in occurrence.SurfaceBodies)
                {
                    foreach (Face face in body.Faces)
                    {
                        _selectedFaces.Add(face);
                    }
                }

                foreach (ComponentOccurrence subOcc in occurrence.SubOccurrences)
                {
                    AddFacesFromOccurrence(subOcc);
                }
            }
            catch { }
        }

        /// <summary>
        /// Inverts the current selection.
        /// </summary>
        public void InvertSelection()
        {
            try
            {
                var doc = _inventorApp.ActiveDocument;
                if (doc == null) return;

                var allFaces = new HashSet<Face>();

                if (doc is PartDocument partDoc)
                {
                    var compDef = partDoc.ComponentDefinition;
                    foreach (SurfaceBody body in compDef.SurfaceBodies)
                    {
                        foreach (Face face in body.Faces)
                        {
                            allFaces.Add(face);
                        }
                    }
                }

                var invertedSelection = new HashSet<Face>();
                foreach (var face in allFaces)
                {
                    if (!_selectedFaces.Contains(face))
                    {
                        invertedSelection.Add(face);
                    }
                }

                _selectedFaces.Clear();
                foreach (var face in invertedSelection)
                {
                    _selectedFaces.Add(face);
                }

                UpdateHighlights();
                SelectionChanged?.Invoke(this, _selectedFaces.Count);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error inverting selection: {ex.Message}");
            }
        }

        /// <summary>
        /// Clears the current selection.
        /// </summary>
        public void ClearSelection()
        {
            _selectedFaces.Clear();
            _templateFaces.Clear();
            _colorFaces.Clear();
            _isTemplateSelectionMode = false;

            if (_highlightSet != null)
            {
                try { _highlightSet.Clear(); } catch { }
            }
            if (_templateHighlightSet != null)
            {
                try { _templateHighlightSet.Clear(); } catch { }
            }
            if (_colorHighlightSet != null)
            {
                try { _colorHighlightSet.Clear(); } catch { }
            }
            
            _inventorApp.ActiveView?.Update();

            SelectionChanged?.Invoke(this, 0);
            TemplateFacesChanged?.Invoke(this, 0);
            ColorFacesChanged?.Invoke(this, 0);
        }

        #endregion

        #region Find Similar Faces

        /// <summary>
        /// Finds faces similar to the template faces.
        /// Adds found similar faces to template collection (orange).
        /// </summary>
        public void FindSimilarFaces()
        {
            try
            {
                if (_templateFaces.Count == 0) return;

                FindSimilarToTemplate();

                UpdateHighlights();

                TemplateFacesChanged?.Invoke(this, _templateFaces.Count);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error finding similar faces: {ex.Message}");
            }
        }

        /// <summary>
        /// Finds faces similar to the template faces.
        /// </summary>
        private void FindSimilarToTemplate()
        {
            if (_templateFaces.Count == 0) return;

            var doc = _inventorApp.ActiveDocument;
            if (doc == null) return;

            var templateProperties = new List<FaceProperties>();
            foreach (var templateFace in _templateFaces)
            {
                templateProperties.Add(GetFaceProperties(templateFace));
            }

            if (doc is PartDocument partDoc)
            {
                var compDef = partDoc.ComponentDefinition;
                foreach (SurfaceBody body in compDef.SurfaceBodies)
                {
                    foreach (Face face in body.Faces)
                    {
                        if (_templateFaces.Contains(face)) continue;

                        var faceProps = GetFaceProperties(face);
                        if (IsSimilarToAnyTemplate(faceProps, templateProperties))
                        {
                            _templateFaces.Add(face);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Gets properties of a face for similarity comparison.
        /// </summary>
        private FaceProperties GetFaceProperties(Face face)
        {
            var props = new FaceProperties
            {
                SurfaceType = face.SurfaceType,
                Area = face.Evaluator.Area
            };

            try
            {
                if (face.SurfaceType == SurfaceTypeEnum.kCylinderSurface)
                {
                    var cylinder = (Cylinder)face.Geometry;
                    props.Radius = cylinder.Radius;
                }
                else if (face.SurfaceType == SurfaceTypeEnum.kPlaneSurface)
                {
                    var plane = (Plane)face.Geometry;
                    props.Normal = new double[] { plane.Normal.X, plane.Normal.Y, plane.Normal.Z };
                }
            }
            catch
            {
                // Ignore geometry extraction errors
            }

            return props;
        }

        /// <summary>
        /// Checks if a face is similar to any template face.
        /// </summary>
        private bool IsSimilarToAnyTemplate(FaceProperties faceProps, List<FaceProperties> templateProps)
        {
            const double areaTolerance = 0.01;
            const double radiusTolerance = 0.001;

            foreach (var template in templateProps)
            {
                if (faceProps.SurfaceType != template.SurfaceType)
                    continue;

                double areaRatio = faceProps.Area / template.Area;
                if (Math.Abs(1.0 - areaRatio) > areaTolerance)
                    continue;

                if (faceProps.SurfaceType == SurfaceTypeEnum.kCylinderSurface)
                {
                    if (faceProps.Radius.HasValue && template.Radius.HasValue)
                    {
                        if (Math.Abs(faceProps.Radius.Value - template.Radius.Value) > radiusTolerance)
                            continue;
                    }
                }

                return true;
            }

            return false;
        }

        /// <summary>
        /// Adds template faces to the main selection and switches back to main selection mode.
        /// </summary>
        public void AddTemplateFacesToMain()
        {
            foreach (var face in _templateFaces)
            {
                _selectedFaces.Add(face);
            }

            _templateFaces.Clear();
            _isTemplateSelectionMode = false;

            if (_templateHighlightSet != null)
            {
                try { _templateHighlightSet.Clear(); } catch { }
            }

            UpdateHighlights();

            try
            {
                _inventorApp.ActiveView?.Update();
            }
            catch { }

            SelectionChanged?.Invoke(this, _selectedFaces.Count);
            TemplateFacesChanged?.Invoke(this, 0);

            System.Diagnostics.Debug.WriteLine($"Added template faces to main. Total selected: {_selectedFaces.Count}");
        }

        /// <summary>
        /// Switches to template selection mode.
        /// </summary>
        public void EnterTemplateSelectionMode()
        {
            _isTemplateSelectionMode = true;
        }

        /// <summary>
        /// Exits template selection mode without adding templates to main.
        /// </summary>
        public void ExitTemplateSelectionMode()
        {
            _isTemplateSelectionMode = false;
        }

        /// <summary>
        /// Gets the color from the first selected face in the main selection.
        /// Returns null if no faces are selected or if the face has no specific color.
        /// </summary>
        public System.Drawing.Color? GetColorFromSelectedFace()
        {
            try
            {
                // First try to get from main selection
                if (_selectedFaces.Count > 0)
                {
                    var face = _selectedFaces.First();
                    return GetColorFromFace(face);
                }
                
                // Then try template faces
                if (_templateFaces.Count > 0)
                {
                    var face = _templateFaces.First();
                    return GetColorFromFace(face);
                }
                
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting color from selected face: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Enters pick color mode. The next face click will pick the color and call the callback.
        /// </summary>
        /// <param name="callback">The callback to invoke with the picked color (or null if cancelled).</param>
        public void EnterPickColorMode(Action<System.Drawing.Color?> callback)
        {
            _isPickColorMode = true;
            _pickColorCallback = callback;
            
            // Update status bar to guide user
            if (_interactionEvents != null)
            {
                _interactionEvents.StatusBarText = "Pick Color: Click on a face to pick its color.";
            }
        }

        /// <summary>
        /// Exits pick color mode without picking a color.
        /// </summary>
        public void ExitPickColorMode()
        {
            _isPickColorMode = false;
            _pickColorCallback = null;
            
            // Restore normal status bar
            if (_interactionEvents != null)
            {
                _interactionEvents.StatusBarText = "Coloring Tool: Click faces to select. Shift+Click to deselect.";
            }
        }

        /// <summary>
        /// Gets whether we're currently in pick color mode.
        /// </summary>
        public bool IsPickColorMode => _isPickColorMode;

        /// <summary>
        /// Gets the color from a specific face.
        /// </summary>
        private System.Drawing.Color? GetColorFromFace(Face face)
        {
            try
            {
                // Get the face's render style (appearance/color)
                var appearance = face.Appearance;
                if (appearance != null)
                {
                    var color = GetDiffuseColorFromAppearance(appearance);
                    if (color != null)
                    {
                        return color;
                    }
                }

                // Try to get from the parent body
                var body = face.SurfaceBody;
                if (body?.Appearance != null)
                {
                    var color = GetDiffuseColorFromAppearance(body.Appearance);
                    if (color != null)
                    {
                        return color;
                    }
                }

                // Return default gray if no color found
                return System.Drawing.Color.FromArgb(192, 192, 192);
            }
            catch
            {
                return null;
            }
        }

        #endregion

        #region Find Colors

        /// <summary>
        /// Finds all faces in the document that match the specified color.
        /// Adds found faces to the color collection (purple).
        /// </summary>
        public void FindFacesByColor(System.Drawing.Color targetColor)
        {
            try
            {
                // Clear previous color faces
                _colorFaces.Clear();

                var doc = _inventorApp.ActiveDocument;
                if (doc == null) return;

                // Search all faces for matching color
                if (doc is PartDocument partDoc)
                {
                    var compDef = partDoc.ComponentDefinition;
                    foreach (SurfaceBody body in compDef.SurfaceBodies)
                    {
                        foreach (Face face in body.Faces)
                        {
                            if (FaceMatchesColor(face, targetColor))
                            {
                                _colorFaces.Add(face);
                            }
                        }
                    }
                }
                else if (doc is AssemblyDocument asmDoc)
                {
                    var compDef = asmDoc.ComponentDefinition;
                    foreach (ComponentOccurrence occ in compDef.Occurrences)
                    {
                        FindColorFacesInOccurrence(occ, targetColor);
                    }
                }

                UpdateHighlights();
                
                // Force view update to ensure visual refresh
                try
                {
                    _inventorApp.ActiveView?.Update();
                }
                catch { }

                ColorFacesChanged?.Invoke(this, _colorFaces.Count);

                System.Diagnostics.Debug.WriteLine($"Found {_colorFaces.Count} faces matching color RGB({targetColor.R}, {targetColor.G}, {targetColor.B})");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error finding faces by color: {ex.Message}");
            }
        }

        /// <summary>
        /// Recursively finds faces matching the color in an occurrence.
        /// </summary>
        private void FindColorFacesInOccurrence(ComponentOccurrence occurrence, System.Drawing.Color targetColor)
        {
            try
            {
                foreach (SurfaceBody body in occurrence.SurfaceBodies)
                {
                    foreach (Face face in body.Faces)
                    {
                        if (FaceMatchesColor(face, targetColor))
                        {
                            _colorFaces.Add(face);
                        }
                    }
                }

                foreach (ComponentOccurrence subOcc in occurrence.SubOccurrences)
                {
                    FindColorFacesInOccurrence(subOcc, targetColor);
                }
            }
            catch { }
        }

        /// <summary>
        /// Checks if a face matches the target color.
        /// </summary>
        private bool FaceMatchesColor(Face face, System.Drawing.Color targetColor)
        {
            try
            {
                // Get the face's render style (appearance/color)
                var appearance = face.Appearance;
                if (appearance == null)
                {
                    // Face has no specific appearance, check body or part appearance
                    return false;
                }

                // Try to get the diffuse color from the appearance
                var diffuseProperty = GetDiffuseColorFromAppearance(appearance);
                if (diffuseProperty != null)
                {
                    // Compare colors with some tolerance
                    const int colorTolerance = 10; // Allow small differences
                    return Math.Abs(diffuseProperty.Value.R - targetColor.R) <= colorTolerance &&
                           Math.Abs(diffuseProperty.Value.G - targetColor.G) <= colorTolerance &&
                           Math.Abs(diffuseProperty.Value.B - targetColor.B) <= colorTolerance;
                }
            }
            catch
            {
                // Face doesn't have accessible color info
            }

            return false;
        }

        /// <summary>
        /// Extracts the diffuse color from an appearance asset.
        /// </summary>
        private System.Drawing.Color? GetDiffuseColorFromAppearance(Asset appearance)
        {
            try
            {
                // Try to find and extract the diffuse color parameter
                for (int i = 1; i <= appearance.Count; i++)
                {
                    try
                    {
                        AssetValue assetValue = appearance[i];
                        string valueName = assetValue.Name.ToLower();
                        
                        // Look for diffuse color parameter
                        if (valueName.Contains("generic_diffuse") || 
                            valueName.Contains("diffuse_color") ||
                            (valueName.Contains("diffuse") && !valueName.Contains("map") && !valueName.Contains("texture")))
                        {
                            if (assetValue is ColorAssetValue colorValue)
                            {
                                var invColor = colorValue.Value;
                                return System.Drawing.Color.FromArgb(invColor.Red, invColor.Green, invColor.Blue);
                            }
                        }
                    }
                    catch { }
                }

                // If we couldn't find diffuse, try any color parameter
                for (int i = 1; i <= appearance.Count; i++)
                {
                    try
                    {
                        AssetValue assetValue = appearance[i];
                        if (assetValue is ColorAssetValue colorValue && 
                            !assetValue.Name.ToLower().Contains("reflect") &&
                            !assetValue.Name.ToLower().Contains("emit"))
                        {
                            var invColor = colorValue.Value;
                            return System.Drawing.Color.FromArgb(invColor.Red, invColor.Green, invColor.Blue);
                        }
                    }
                    catch { }
                }
            }
            catch
            {
                // Unable to extract color
            }

            return null;
        }

        /// <summary>
        /// Adds the color-matched faces to the main selection.
        /// Faces change from purple highlight to blue highlight.
        /// </summary>
        public void AddColorFacesToMain()
        {
            // Move all color faces to main selection
            foreach (var face in _colorFaces)
            {
                _selectedFaces.Add(face);
            }

            // Clear the color collection
            _colorFaces.Clear();

            // Force refresh of highlights - clear purple, update blue
            if (_colorHighlightSet != null)
            {
                try { _colorHighlightSet.Clear(); } catch { }
            }

            // Update all highlights
            UpdateHighlights();

            // Force view update to ensure visual refresh
            try
            {
                _inventorApp.ActiveView?.Update();
            }
            catch { }

            // Notify listeners
            SelectionChanged?.Invoke(this, _selectedFaces.Count);
            ColorFacesChanged?.Invoke(this, 0);

            System.Diagnostics.Debug.WriteLine($"Added color faces to main. Total selected: {_selectedFaces.Count}");
        }

        #endregion

        #region Color Application

        /// <summary>
        /// Applies the specified color to all selected faces using appearance override.
        /// </summary>
        /// <param name="color">The System.Drawing.Color to apply to faces.</param>
        /// <returns>True if color was successfully applied, false otherwise.</returns>
        public bool ApplyColorToSelectedFaces(System.Drawing.Color color)
        {
            if (_selectedFaces.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine("No faces selected to apply color.");
                return false;
            }

            try
            {
                var doc = _inventorApp.ActiveDocument;
                if (doc == null) return false;

                // Get or create an appearance for this color
                Asset? appearance = GetOrCreateColorAppearance(doc, color);
                if (appearance == null)
                {
                    System.Diagnostics.Debug.WriteLine("Failed to create appearance asset.");
                    return false;
                }

                int successCount = 0;
                foreach (var face in _selectedFaces)
                {
                    try
                    {
                        face.Appearance = appearance;
                        successCount++;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error applying color to face: {ex.Message}");
                    }
                }

                _inventorApp.ActiveView?.Update();

                System.Diagnostics.Debug.WriteLine($"Applied color to {successCount}/{_selectedFaces.Count} faces.");
                return successCount > 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error applying color: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Gets or creates an appearance asset for the specified color.
        /// </summary>
        private Asset? GetOrCreateColorAppearance(Document doc, System.Drawing.Color color)
        {
            try
            {
                string appearanceName = $"ColoringTool_{color.R}_{color.G}_{color.B}";

                // Get document assets
                Assets? docAssets = null;
                if (doc is PartDocument partDoc)
                {
                    docAssets = partDoc.Assets;
                }
                else if (doc is AssemblyDocument asmDoc)
                {
                    docAssets = asmDoc.Assets;
                }

                if (docAssets == null) return null;

                // Check if appearance already exists
                Asset? existingAppearance = null;
                try
                {
                    foreach (Asset asset in docAssets)
                    {
                        if (asset.DisplayName == appearanceName)
                        {
                            existingAppearance = asset;
                            break;
                        }
                    }
                }
                catch { }

                if (existingAppearance != null)
                {
                    return existingAppearance;
                }

                // Find a suitable base appearance from libraries
                Asset? baseAppearance = FindBaseAppearance();
                
                if (baseAppearance == null)
                {
                    System.Diagnostics.Debug.WriteLine("No base appearance found");
                    return null;
                }

                // Copy the base appearance to the document
                Asset newAppearance = baseAppearance.CopyTo(doc);
                newAppearance.DisplayName = appearanceName;

                // Set the diffuse color on the appearance
                SetAppearanceColor(newAppearance, color);

                System.Diagnostics.Debug.WriteLine($"Created appearance: {appearanceName}");
                return newAppearance;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating appearance: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Finds a suitable base appearance from asset libraries.
        /// </summary>
        private Asset? FindBaseAppearance()
        {
            try
            {
                var assetLibraries = _inventorApp.AssetLibraries;
                
                // First pass: look for generic/default appearances
                for (int i = 1; i <= assetLibraries.Count; i++)
                {
                    AssetLibrary lib = assetLibraries[i];
                    try
                    {
                        var libAssets = lib.AppearanceAssets;
                        for (int j = 1; j <= libAssets.Count; j++)
                        {
                            Asset asset = libAssets[j];
                            string name = asset.DisplayName.ToLower();
                            
                            // Look for simple, generic appearances
                            if (name.Contains("generic") && name.Contains("opaque"))
                            {
                                System.Diagnostics.Debug.WriteLine($"Found base appearance: {asset.DisplayName}");
                                return asset;
                            }
                        }
                    }
                    catch { }
                }

                // Second pass: look for any simple appearance
                for (int i = 1; i <= assetLibraries.Count; i++)
                {
                    AssetLibrary lib = assetLibraries[i];
                    try
                    {
                        var libAssets = lib.AppearanceAssets;
                        for (int j = 1; j <= libAssets.Count; j++)
                        {
                            Asset asset = libAssets[j];
                            string name = asset.DisplayName.ToLower();
                            
                            if (name.Contains("plastic") || name.Contains("generic") || 
                                name.Contains("default") || name == "white" || 
                                name == "red" || name == "blue")
                            {
                                System.Diagnostics.Debug.WriteLine($"Found alternative base: {asset.DisplayName}");
                                return asset;
                            }
                        }
                    }
                    catch { }
                }

                // Final fallback: just use the first appearance we can find
                for (int i = 1; i <= assetLibraries.Count; i++)
                {
                    AssetLibrary lib = assetLibraries[i];
                    try
                    {
                        var libAssets = lib.AppearanceAssets;
                        if (libAssets.Count > 0)
                        {
                            System.Diagnostics.Debug.WriteLine($"Using fallback appearance from {lib.DisplayName}");
                            return libAssets[1];
                        }
                    }
                    catch { }
                }

                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error finding base appearance: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Sets the color on an appearance asset.
        /// </summary>
        private void SetAppearanceColor(Asset appearance, System.Drawing.Color color)
        {
            try
            {
                // Try to find and set the diffuse color parameter
                for (int i = 1; i <= appearance.Count; i++)
                {
                    try
                    {
                        AssetValue assetValue = appearance[i];
                        string valueName = assetValue.Name.ToLower();
                        
                        // Look for diffuse color parameter
                        if (valueName.Contains("generic_diffuse") || 
                            valueName.Contains("diffuse_color") ||
                            (valueName.Contains("diffuse") && !valueName.Contains("map") && !valueName.Contains("texture")))
                        {
                            if (assetValue is ColorAssetValue colorValue)
                            {
                                var inventorColor = _inventorApp.TransientObjects.CreateColor(color.R, color.G, color.B);
                                colorValue.Value = inventorColor;
                                System.Diagnostics.Debug.WriteLine($"Set color on parameter: {assetValue.Name}");
                                return; // Successfully set the color
                            }
                        }
                    }
                    catch { }
                }

                // If we couldn't find diffuse, try any color parameter
                for (int i = 1; i <= appearance.Count; i++)
                {
                    try
                    {
                        AssetValue assetValue = appearance[i];
                        if (assetValue is ColorAssetValue colorValue && 
                            !assetValue.Name.ToLower().Contains("reflect") &&
                            !assetValue.Name.ToLower().Contains("emit"))
                        {
                            var inventorColor = _inventorApp.TransientObjects.CreateColor(color.R, color.G, color.B);
                            colorValue.Value = inventorColor;
                            System.Diagnostics.Debug.WriteLine($"Set color on fallback parameter: {assetValue.Name}");
                            return;
                        }
                    }
                    catch { }
                }

                System.Diagnostics.Debug.WriteLine("Could not find suitable color parameter");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error setting appearance color: {ex.Message}");
            }
        }

        #endregion

        #region Helper Classes

        /// <summary>
        /// Properties of a face for similarity comparison.
        /// </summary>
        private class FaceProperties
        {
            public SurfaceTypeEnum SurfaceType { get; set; }
            public double Area { get; set; }
            public double? Radius { get; set; }
            public double[]? Normal { get; set; }
        }

        #endregion

        #region IDisposable Implementation

        /// <summary>
        /// Disposes of the selection manager.
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
                StopInteraction();
                _selectedFaces.Clear();
                _templateFaces.Clear();
                _colorFaces.Clear();
            }

            _disposed = true;
        }

        /// <summary>
        /// Finalizer.
        /// </summary>
        ~SelectionManager()
        {
            Dispose(false);
        }

        #endregion
    }
}
