// ============================================================================
// ColoringTool Add-in for Autodesk Inventor 2026
// ColoringToolForm.cs - Floating Coloring Dialog (Code Behind)
// Blue theme with comprehensive color picker
// ============================================================================

using System;
using System.Drawing;
using System.Windows.Forms;
using Inventor;
using ColoringTool.Core;

namespace ColoringTool.UI
{
    /// <summary>
    /// Floating dialog that provides the Coloring Tool interface.
    /// Allows selecting faces and applying colors to them.
    /// </summary>
    public partial class ColoringToolForm : Form
    {
        #region Private Fields

        private Inventor.Application? _inventorApp;
        private SelectionManager? _selectionManager;
        private bool _isDragging = false;
        private System.Drawing.Point _dragOffset;
        private bool _escapePressed = false;
        private System.Drawing.Color _selectedColor = System.Drawing.Color.FromArgb(192, 192, 192); // Default Inventor grey
        private System.Drawing.Color _selectedSearchColor = System.Drawing.Color.FromArgb(192, 192, 192); // Default search color

        #endregion

        #region Constructor

        public ColoringToolForm()
        {
            InitializeComponent();
            SetupEventHandlers();
            EnableDragging();
            this.KeyPreview = true;
        }

        #endregion

        #region Keyboard Handling

        /// <summary>
        /// Handle escape key: close the form immediately.
        /// </summary>
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Escape)
            {
                // Clear any highlights and close
                if (_selectionManager != null)
                {
                    _selectionManager.ClearAllFaces();
                }
                this.DialogResult = DialogResult.Cancel;
                this.Hide();
                return true;
            }
            
            return base.ProcessCmdKey(ref msg, keyData);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Initializes the form with the Inventor application and selection manager.
        /// </summary>
        public void Initialize(Inventor.Application inventorApp, SelectionManager selectionManager)
        {
            _inventorApp = inventorApp;
            _selectionManager = selectionManager;

            if (_selectionManager != null)
            {
                _selectionManager.SelectionChanged += OnSelectionChanged;
                _selectionManager.TemplateFacesChanged += OnTemplateFacesChanged;
                _selectionManager.ColorFacesChanged += OnColorFacesChanged;
                _selectionManager.SetFilterType(GetCurrentFilterType());
            }

            UpdateCounts();
            
            _selectionManager?.StartInteraction();
        }

        /// <summary>
        /// Gets the currently selected color.
        /// </summary>
        public System.Drawing.Color SelectedColor => _selectedColor;

        #endregion

        #region Form Dragging

        private void EnableDragging()
        {
            pnlHeader.MouseDown += Header_MouseDown;
            pnlHeader.MouseMove += Header_MouseMove;
            pnlHeader.MouseUp += Header_MouseUp;

            lblTitle.MouseDown += Header_MouseDown;
            lblTitle.MouseMove += Header_MouseMove;
            lblTitle.MouseUp += Header_MouseUp;
        }

        private void Header_MouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                _isDragging = true;
                _dragOffset = e.Location;
            }
        }

        private void Header_MouseMove(object? sender, MouseEventArgs e)
        {
            if (_isDragging)
            {
                var screenPoint = PointToScreen(e.Location);
                this.Location = new System.Drawing.Point(
                    screenPoint.X - _dragOffset.X,
                    screenPoint.Y - _dragOffset.Y);
            }
        }

        private void Header_MouseUp(object? sender, MouseEventArgs e)
        {
            _isDragging = false;
        }

        #endregion

        #region Event Handlers Setup

        private void SetupEventHandlers()
        {
            SetupRowHoverEffect(pnlSelectAll, lblSelectAll);
            SetupRowHoverEffect(pnlInvert, lblInvert);
            SetupRowHoverEffect(pnlClear, lblClear);
            SetupRowHoverEffect(pnlColorPicker, lblClickToChange);
            SetupRowHoverEffect(pnlSearchColorPicker, lblSearchClickToChange);
        }

        private void SetupRowHoverEffect(Panel panel, Label label)
        {
            var normalColor = System.Drawing.Color.Transparent;
            var hoverColor = System.Drawing.Color.FromArgb(220, 230, 245); // Light blue hover for white theme

            panel.MouseEnter += (s, e) => panel.BackColor = hoverColor;
            panel.MouseLeave += (s, e) => panel.BackColor = normalColor;
            label.MouseEnter += (s, e) => panel.BackColor = hoverColor;
            label.MouseLeave += (s, e) => panel.BackColor = normalColor;
        }

        #endregion

        #region Button Click Handlers

        private void btnClose_Click(object? sender, EventArgs e)
        {
            CloseForm();
        }

        private void btnDone_Click(object? sender, EventArgs e)
        {
            // Apply color and close
            ApplyColorToSelection();
            CloseForm();
        }

        private void btnApply_Click(object? sender, EventArgs e)
        {
            // Apply color then clear selections
            ApplyColorToSelection();
            
            // Clear both Select Objects and Template Faces after applying
            if (_selectionManager != null)
            {
                _selectionManager.ClearAllFaces();
                UpdateCounts();
                UpdateModeIndicators();
            }
        }

        private void btnCloseBottom_Click(object? sender, EventArgs e)
        {
            // Just close without applying
            CloseForm();
        }

        private void pnlSelectObjects_Click(object? sender, EventArgs e)
        {
            if (_selectionManager != null)
            {
                // Exit template selection mode if we're in it
                _selectionManager.ExitTemplateSelectionMode();
                
                _selectionManager.SetFilterType(GetCurrentFilterType());
                
                if (!_selectionManager.IsInteractionActive)
                {
                    _selectionManager.StartInteraction();
                }
                
                UpdateModeIndicators();
            }
        }

        private void pnlSelectAll_Click(object? sender, EventArgs e)
        {
            if (_selectionManager != null)
            {
                _selectionManager.SelectAllVisibleFaces();
                UpdateCounts();
            }
        }

        private void pnlInvert_Click(object? sender, EventArgs e)
        {
            if (_selectionManager != null)
            {
                _selectionManager.InvertSelection();
                UpdateCounts();
            }
        }

        private void pnlClear_Click(object? sender, EventArgs e)
        {
            if (_selectionManager != null)
            {
                _selectionManager.ClearSelection();
                UpdateCounts();
            }
        }

        private void OnTemplateRowClick(object? sender, EventArgs e)
        {
            if (_selectionManager != null)
            {
                _selectionManager.EnterTemplateSelectionMode();
                UpdateModeIndicators();
            }
        }

        private void btnFindSimilar_Click(object? sender, EventArgs e)
        {
            if (_selectionManager != null)
            {
                _selectionManager.SetFilterType(GetCurrentFilterType());
                _selectionManager.FindSimilarFaces();
                UpdateCounts();
                UpdateModeIndicators();
            }
        }

        private void btnAddToMain_Click(object? sender, EventArgs e)
        {
            if (_selectionManager != null)
            {
                _selectionManager.AddTemplateFacesToMain();
                UpdateCounts();
                UpdateModeIndicators();
            }
        }

        private void pnlSearchColorPicker_Click(object? sender, EventArgs e)
        {
            ShowSearchColorPickerDialog();
        }

        private void btnFindColors_Click(object? sender, EventArgs e)
        {
            if (_selectionManager != null)
            {
                _selectionManager.FindFacesByColor(_selectedSearchColor);
                UpdateCounts();
            }
        }

        private void btnAddColorToMain_Click(object? sender, EventArgs e)
        {
            if (_selectionManager != null)
            {
                _selectionManager.AddColorFacesToMain();
                UpdateCounts();
            }
        }

        private void cboTypeFilter_SelectedIndexChanged(object? sender, EventArgs e)
        {
            var selectedType = GetCurrentFilterType();
            _selectionManager?.SetFilterType(selectedType);
        }

        private void pnlColorPicker_Click(object? sender, EventArgs e)
        {
            ShowColorPickerDialog();
        }

        private void btnPickColorFromFace_Click(object? sender, EventArgs e)
        {
            if (_selectionManager != null)
            {
                // Show prompt to user
                MessageBox.Show(this, "Click on a face in the model to pick its color.", "Pick Color", MessageBoxButtons.OK, MessageBoxIcon.Information);
                
                // Enter pick color mode - next face click will pick the color
                _selectionManager.EnterPickColorMode((color) =>
                {
                    if (color.HasValue)
                    {
                        _selectedColor = color.Value;
                        
                        // Update UI on the UI thread
                        if (InvokeRequired)
                        {
                            BeginInvoke(new Action(() =>
                            {
                                pnlSelectedColor.BackColor = _selectedColor;
                                lblClickToChange.Text = $"RGB: {_selectedColor.R}, {_selectedColor.G}, {_selectedColor.B}";
                            }));
                        }
                        else
                        {
                            pnlSelectedColor.BackColor = _selectedColor;
                            lblClickToChange.Text = $"RGB: {_selectedColor.R}, {_selectedColor.G}, {_selectedColor.B}";
                        }
                    }
                });
            }
        }

        private void btnPickSearchColorFromFace_Click(object? sender, EventArgs e)
        {
            if (_selectionManager != null)
            {
                // Show prompt to user
                MessageBox.Show(this, "Click on a face in the model to pick its color.", "Pick Color", MessageBoxButtons.OK, MessageBoxIcon.Information);
                
                // Enter pick color mode - next face click will pick the search color
                _selectionManager.EnterPickColorMode((color) =>
                {
                    if (color.HasValue)
                    {
                        _selectedSearchColor = color.Value;
                        
                        // Update UI on the UI thread
                        if (InvokeRequired)
                        {
                            BeginInvoke(new Action(() =>
                            {
                                pnlSearchSelectedColor.BackColor = _selectedSearchColor;
                                lblSearchClickToChange.Text = $"RGB: {_selectedSearchColor.R}, {_selectedSearchColor.G}, {_selectedSearchColor.B}";
                            }));
                        }
                        else
                        {
                            pnlSearchSelectedColor.BackColor = _selectedSearchColor;
                            lblSearchClickToChange.Text = $"RGB: {_selectedSearchColor.R}, {_selectedSearchColor.G}, {_selectedSearchColor.B}";
                        }
                    }
                });
            }
        }

        #endregion

        #region Color Picker Dialog

        private void ShowColorPickerDialog()
        {
            // Use Windows built-in ColorDialog - reliable and always readable
            using (var colorDialog = new ColorDialog())
            {
                colorDialog.Color = _selectedColor;
                colorDialog.FullOpen = true; // Show custom colors section by default
                colorDialog.AnyColor = true; // Allow any color
                colorDialog.AllowFullOpen = true; // Allow user to define custom colors

                if (colorDialog.ShowDialog(this) == DialogResult.OK)
                {
                    _selectedColor = colorDialog.Color;
                    pnlSelectedColor.BackColor = _selectedColor;
                    lblClickToChange.Text = $"RGB: {_selectedColor.R}, {_selectedColor.G}, {_selectedColor.B}";
                }
            }
        }

        private void ShowSearchColorPickerDialog()
        {
            using (var colorDialog = new ColorDialog())
            {
                colorDialog.Color = _selectedSearchColor;
                colorDialog.FullOpen = true;
                colorDialog.AnyColor = true;
                colorDialog.AllowFullOpen = true;

                if (colorDialog.ShowDialog(this) == DialogResult.OK)
                {
                    _selectedSearchColor = colorDialog.Color;
                    pnlSearchSelectedColor.BackColor = _selectedSearchColor;
                    lblSearchClickToChange.Text = $"RGB: {_selectedSearchColor.R}, {_selectedSearchColor.G}, {_selectedSearchColor.B}";
                }
            }
        }

        private void PickColorFromSelectedFace()
        {
            // This method is now unused - pick mode is handled via callback
        }

        private void PickSearchColorFromSelectedFace()
        {
            // This method is now unused - pick mode is handled via callback
        }

        #endregion

        #region Color Application

        private void ApplyColorToSelection()
        {
            if (_selectionManager != null)
            {
                bool success = _selectionManager.ApplyColorToSelectedFaces(_selectedColor);
                if (success)
                {
                    System.Diagnostics.Debug.WriteLine($"Applied color {_selectedColor} to selected faces.");
                }
            }
        }

        #endregion

        #region Mode Indicators

        private void UpdateModeIndicators()
        {
            if (_selectionManager != null)
            {
                bool isTemplateMode = _selectionManager.IsTemplateSelectionMode;
                
                // Update Template Faces row - highlight when in template mode
                pnlTemplate.BackColor = isTemplateMode 
                    ? System.Drawing.Color.FromArgb(255, 200, 100)  // Light orange when active
                    : System.Drawing.Color.Transparent;
                lblTemplate.ForeColor = System.Drawing.Color.FromArgb(40, 40, 40); // Always dark text for light theme
                    
                // Update Select Objects row - highlight when NOT in template mode
                pnlSelectObjects.BackColor = !isTemplateMode
                    ? System.Drawing.Color.FromArgb(180, 255, 180)  // Green when active
                    : System.Drawing.Color.Transparent;
                lblSelectObjects.ForeColor = System.Drawing.Color.FromArgb(40, 40, 40); // Always dark text for light theme
            }
        }

        #endregion

        #region Selection Manager Events

        private void OnSelectionChanged(object? sender, int count)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action(() => UpdateCounts()));
            }
            else
            {
                UpdateCounts();
            }
        }

        private void OnTemplateFacesChanged(object? sender, int count)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action(() => UpdateCounts()));
            }
            else
            {
                UpdateCounts();
            }
        }

        private void OnColorFacesChanged(object? sender, int count)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action(() => UpdateCounts()));
            }
            else
            {
                UpdateCounts();
            }
        }

        #endregion

        #region Helper Methods

        private void UpdateCounts()
        {
            int mainCount = _selectionManager?.SelectedFaceCount ?? 0;
            int templateCount = _selectionManager?.TemplateFaceCount ?? 0;
            int colorCount = _selectionManager?.ColorFaceCount ?? 0;

            lblSelectObjects.Text = $"Select Objects ({mainCount})";
            lblTemplate.Text = $"Template Faces ({templateCount})";
            lblColorFaces.Text = $"Face Colors ({colorCount})";
        }

        public SelectionFilterType GetCurrentFilterType()
        {
            if (cboTypeFilter == null) return SelectionFilterType.SingleFace;

            return cboTypeFilter.SelectedIndex switch
            {
                0 => SelectionFilterType.SingleFace,
                1 => SelectionFilterType.TangentFaces,
                2 => SelectionFilterType.BossPocketFaces,
                3 => SelectionFilterType.AdjacentFaces,
                4 => SelectionFilterType.FeatureFaces,
                5 => SelectionFilterType.ConnectedBlendFaces,
                _ => SelectionFilterType.SingleFace
            };
        }

        private void CloseForm()
        {
            if (_selectionManager != null)
            {
                _selectionManager.SelectionChanged -= OnSelectionChanged;
                _selectionManager.TemplateFacesChanged -= OnTemplateFacesChanged;
                _selectionManager.ColorFacesChanged -= OnColorFacesChanged;
                _selectionManager.StopInteraction();
            }

            this.Hide();
            this.DialogResult = DialogResult.Cancel;
        }

        #endregion

        #region Form Overrides

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);

            if (_selectionManager != null)
            {
                _selectionManager.SelectionChanged -= OnSelectionChanged;
                _selectionManager.TemplateFacesChanged -= OnTemplateFacesChanged;
                _selectionManager.ColorFacesChanged -= OnColorFacesChanged;
            }
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ClassStyle |= 0x00020000; // CS_DROPSHADOW
                return cp;
            }
        }

        #endregion
    }
}
