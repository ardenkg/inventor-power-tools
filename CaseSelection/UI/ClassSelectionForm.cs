// ============================================================================
// CaseSelection Add-in for Autodesk Inventor 2026
// ClassSelectionForm.cs - Floating Selection Dialog (Code Behind)
// ============================================================================

using System;
using System.Drawing;
using System.Windows.Forms;
using Inventor;
using CaseSelection.Core;

namespace CaseSelection.UI
{
    /// <summary>
    /// Floating dialog that provides the Class Selection interface.
    /// Replicates NX-style selection intent functionality.
    /// </summary>
    public partial class ClassSelectionForm : Form
    {
        #region Private Fields

        private Inventor.Application? _inventorApp;
        private SelectionManager? _selectionManager;
        private bool _isDragging = false;
        private System.Drawing.Point _dragOffset;
        private bool _escapePressed = false;  // Track if escape was pressed once
        private System.Drawing.Color _selectedSearchColor = System.Drawing.Color.FromArgb(192, 192, 192); // Default gray

        #endregion

        #region Constructor

        public ClassSelectionForm()
        {
            InitializeComponent();
            SetupEventHandlers();
            EnableDragging();
            this.KeyPreview = true;  // Enable form to receive key events first
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

            // Subscribe to selection manager events
            if (_selectionManager != null)
            {
                _selectionManager.SelectionChanged += OnSelectionChanged;
                _selectionManager.TemplateFacesChanged += OnTemplateFacesChanged;
                _selectionManager.ColorFacesChanged += OnColorFacesChanged;
                
                // Set the default filter type based on combobox
                _selectionManager.SetFilterType(GetCurrentFilterType());
            }

            UpdateCounts();
            
            // Auto-start interaction when dialog opens
            _selectionManager?.StartInteraction();
        }

        #endregion

        #region Form Dragging

        private void EnableDragging()
        {
            // Enable dragging via header panel
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
            // Wire up hover effects for rows
            SetupRowHoverEffect(pnlSelectAll, lblSelectAll);
            SetupRowHoverEffect(pnlInvert, lblInvert);
            SetupRowHoverEffect(pnlClear, lblClear);
            SetupRowHoverEffect(pnlColorPicker, lblClickToChangeColor);
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
            CloseForm(false);
        }

        private void btnDone_Click(object? sender, EventArgs e)
        {
            // Commit all selected faces to Inventor's native selection
            _selectionManager?.CommitSelection();
            CloseForm(true);
        }

        private void btnCancel_Click(object? sender, EventArgs e)
        {
            _selectionManager?.ClearSelection();
            CloseForm(false);
        }

        private void pnlSelectObjects_Click(object? sender, EventArgs e)
        {
            // Switch to main selection mode (exit template mode if active)
            if (_selectionManager != null)
            {
                // Exit template selection mode if we're in it
                _selectionManager.ExitTemplateSelectionMode();
                
                // Set filter type before starting
                _selectionManager.SetFilterType(GetCurrentFilterType());
                
                if (!_selectionManager.IsInteractionActive)
                {
                    _selectionManager.StartInteraction();
                }
                
                // Update UI to reflect mode change
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

        private void btnFindSimilar_Click(object? sender, EventArgs e)
        {
            if (_selectionManager != null)
            {
                // Set the filter type before finding similar
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
                        _selectedSearchColor = color.Value;
                        
                        // Update UI on the UI thread
                        if (InvokeRequired)
                        {
                            BeginInvoke(new Action(() =>
                            {
                                pnlSelectedColor.BackColor = _selectedSearchColor;
                                lblClickToChangeColor.Text = $"RGB: {_selectedSearchColor.R}, {_selectedSearchColor.G}, {_selectedSearchColor.B}";
                            }));
                        }
                        else
                        {
                            pnlSelectedColor.BackColor = _selectedSearchColor;
                            lblClickToChangeColor.Text = $"RGB: {_selectedSearchColor.R}, {_selectedSearchColor.G}, {_selectedSearchColor.B}";
                        }
                    }
                });
            }
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

        private void OnMainSelectionRowClick(object? sender, EventArgs e)
        {
            if (_selectionManager != null)
            {
                _selectionManager.ExitTemplateSelectionMode();
                UpdateModeIndicators();
            }
        }

        private void UpdateModeIndicators()
        {
            if (_selectionManager != null)
            {
                bool isTemplateMode = _selectionManager.IsTemplateSelectionMode;
                
                // Update Template Faces row - only highlight when in template mode
                pnlTemplate.BackColor = isTemplateMode 
                    ? System.Drawing.Color.FromArgb(255, 200, 100)  // Light orange when active
                    : System.Drawing.Color.Transparent;
                lblTemplate.ForeColor = System.Drawing.Color.FromArgb(40, 40, 40); // Always dark text for light theme
                    
                // Update Select Objects row - only highlight when NOT in template mode
                pnlSelectObjects.BackColor = !isTemplateMode
                    ? System.Drawing.Color.FromArgb(180, 255, 180)  // Green when active (main selection mode)
                    : System.Drawing.Color.Transparent;
                lblSelectObjects.ForeColor = System.Drawing.Color.FromArgb(40, 40, 40); // Always dark text for light theme
            }
        }

        private void cboTypeFilter_SelectedIndexChanged(object? sender, EventArgs e)
        {
            var selectedType = GetCurrentFilterType();

            // Update the selection manager filter
            _selectionManager?.SetFilterType(selectedType);
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

        #region Color Picker Dialog

        private void ShowColorPickerDialog()
        {
            // Use Windows built-in ColorDialog
            using (var colorDialog = new ColorDialog())
            {
                colorDialog.Color = _selectedSearchColor;
                colorDialog.FullOpen = true;
                colorDialog.AnyColor = true;
                colorDialog.AllowFullOpen = true;

                if (colorDialog.ShowDialog(this) == DialogResult.OK)
                {
                    _selectedSearchColor = colorDialog.Color;
                    pnlSelectedColor.BackColor = _selectedSearchColor;
                    lblClickToChangeColor.Text = $"Click to change color";
                }
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

        private void CloseForm(bool commitSelection)
        {
            // Unsubscribe from events
            if (_selectionManager != null)
            {
                _selectionManager.SelectionChanged -= OnSelectionChanged;
                _selectionManager.TemplateFacesChanged -= OnTemplateFacesChanged;
                _selectionManager.ColorFacesChanged -= OnColorFacesChanged;

                if (!commitSelection)
                {
                    _selectionManager.ClearSelection();
                }

                _selectionManager.StopInteraction();
            }

            this.Hide();
            this.DialogResult = commitSelection ? DialogResult.OK : DialogResult.Cancel;
        }

        #endregion

        #region Form Overrides

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);

            // Cleanup
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
                // Add shadow effect
                CreateParams cp = base.CreateParams;
                cp.ClassStyle |= 0x00020000; // CS_DROPSHADOW
                return cp;
            }
        }

        #endregion
    }
}
