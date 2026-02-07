// ============================================================================
// Threader Add-in for Autodesk Inventor 2026
// ThreaderForm.cs - Floating Threader Dialog (Code Behind)
// ============================================================================

using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Linq;
using Inventor;
using Threader.Core;

namespace Threader.UI
{
    /// <summary>
    /// Floating dialog that provides the Threader interface.
    /// Matches the UI/UX patterns from CaseSelection.
    /// </summary>
    public partial class ThreaderForm : Form
    {
        #region Private Fields

        private Inventor.Application? _inventorApp;
        private ThreadDataManager? _threadDataManager;
        private CylinderAnalyzer? _cylinderAnalyzer;
        private ThreadGenerator? _threadGenerator;
        private ThreadPreviewManager? _previewManager;
        
        private bool _isDragging = false;
        private System.Drawing.Point _dragOffset;
        
        // Support multiple hole selection
        private List<Face> _selectedFaces = new();
        private List<CylinderInfo> _selectedCylinders = new();
        
        // Two-step thread selection
        private string _selectedThreadFamily = "ISO";
        private List<double> _availableDiameters = new();
        private List<ThreadStandard> _pitchOptions = new();
        private ThreadStandard? _selectedThread;
        
        private bool _previewEnabled = true;
        
        // Track if we're in selection mode (button should stay green)
        private bool _isInSelectionMode = true;
        
        // Event to request selection restart from host
        public event Action? SelectionRequested;

        #endregion

        #region Constructor

        public ThreaderForm()
        {
            InitializeComponent();
            SetupEventHandlers();
            EnableDragging();
            this.KeyPreview = true;
        }

        #endregion

        #region Keyboard Handling

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Escape)
            {
                // Clear preview and close
                _previewManager?.ClearPreview();
                this.DialogResult = DialogResult.Cancel;
                this.Hide();
                return true;
            }
            
            return base.ProcessCmdKey(ref msg, keyData);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Initializes the form with the required managers.
        /// </summary>
        public void Initialize(
            Inventor.Application inventorApp,
            ThreadDataManager threadDataManager,
            CylinderAnalyzer cylinderAnalyzer,
            ThreadGenerator threadGenerator,
            ThreadPreviewManager previewManager)
        {
            _inventorApp = inventorApp;
            _threadDataManager = threadDataManager;
            _cylinderAnalyzer = cylinderAnalyzer;
            _threadGenerator = threadGenerator;
            _previewManager = previewManager;

            // Initialize thread data
            _threadDataManager.Initialize();

            // Update UI state
            UpdateUI();
            
            // Start in selection mode - highlight the select button green
            pnlSelectCylinder.BackColor = System.Drawing.Color.FromArgb(180, 255, 180);
        }

        /// <summary>
        /// Sets the selected cylinder from external selection.
        /// Adds to existing selection if called multiple times.
        /// </summary>
        public void SetSelectedCylinder(Face face, CylinderInfo cylinderInfo)
        {
            // Check if this face is already selected
            bool alreadySelected = false;
            for (int i = 0; i < _selectedFaces.Count; i++)
            {
                try
                {
                    if (ReferenceEquals(_selectedFaces[i], face))
                    {
                        alreadySelected = true;
                        break;
                    }
                }
                catch { }
            }
            
            if (!alreadySelected)
            {
                _selectedFaces.Add(face);
                _selectedCylinders.Add(cylinderInfo);
            }
            
            // Update the cylinder display
            UpdateCylinderDisplay();
            
            // Get matching threads for this diameter (use first cylinder as reference)
            LoadMatchingThreads();
            
            // Update preview if enabled
            UpdatePreview();
        }

        /// <summary>
        /// Clears all selected cylinders.
        /// </summary>
        public void ClearSelectedCylinders()
        {
            _selectedFaces.Clear();
            _selectedCylinders.Clear();
            UpdateCylinderDisplay();
            UpdateUI();
        }

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
            // Wire up hover effects for rows
            SetupRowHoverEffect(pnlSelectCylinder, lblSelectCylinder);

            // Owner-draw the footer buttons so text stays white even when disabled
            btnApply.Paint += FooterButton_Paint;
            btnDone.Paint += FooterButton_Paint;
            btnCancel.Paint += FooterButton_Paint;
        }

        /// <summary>
        /// Custom paint handler that always renders button text in white,
        /// even when the button is disabled (WinForms defaults to gray text).
        /// </summary>
        private void FooterButton_Paint(object? sender, PaintEventArgs e)
        {
            if (sender is not Button btn) return;

            // Let the base draw the background
            // Then draw the text ourselves on top
            using var brush = new SolidBrush(System.Drawing.Color.White);
            var sf = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            };
            e.Graphics.DrawString(btn.Text, btn.Font, brush, btn.ClientRectangle, sf);
        }

        private void SetupRowHoverEffect(Panel panel, Label label)
        {
            var normalColor = System.Drawing.Color.Transparent;
            var hoverColor = System.Drawing.Color.FromArgb(220, 230, 245);
            var activeColor = System.Drawing.Color.FromArgb(180, 255, 180);

            panel.MouseEnter += (s, e) => {
                if (!_isInSelectionMode && _selectedCylinders.Count == 0)
                    panel.BackColor = hoverColor;
            };
            panel.MouseLeave += (s, e) => {
                // Keep green if in selection mode or has cylinder selected
                if (_isInSelectionMode || _selectedCylinders.Count > 0)
                    panel.BackColor = activeColor;
                else
                    panel.BackColor = normalColor;
            };
            label.MouseEnter += (s, e) => {
                if (!_isInSelectionMode && _selectedCylinders.Count == 0)
                    panel.BackColor = hoverColor;
            };
            label.MouseLeave += (s, e) => {
                // Keep green if in selection mode or has cylinder selected
                if (_isInSelectionMode || _selectedCylinders.Count > 0)
                    panel.BackColor = activeColor;
                else
                    panel.BackColor = normalColor;
            };
        }

        #endregion

        #region Button Click Handlers

        private void btnClose_Click(object? sender, EventArgs e)
        {
            CloseForm(false);
        }

        private void btnApply_Click(object? sender, EventArgs e)
        {
            // Create the thread but keep the dialog open
            if (CreateThread(keepPreview: false))
            {
                // Clear selection so user can select another face
                ClearSelection();
                _previewManager?.ClearPreview();
                _inventorApp!.StatusBarText = "Thread created! Select another cylindrical face...";
                
                // Reset the panel color to prompt for new selection
                pnlSelectCylinder.BackColor = System.Drawing.Color.FromArgb(180, 255, 180);
            }
        }

        private void btnDone_Click(object? sender, EventArgs e)
        {
            // Create the thread and close (preview clears automatically on close)
            if (CreateThread(keepPreview: false))
            {
                CloseForm(true);
            }
        }

        private void btnCancel_Click(object? sender, EventArgs e)
        {
            CloseForm(false);
        }

        private void pnlSelectCylinder_Click(object? sender, EventArgs e)
        {
            // Enter selection mode
            _isInSelectionMode = true;
            
            // Highlight the panel green to show selection mode is active
            pnlSelectCylinder.BackColor = System.Drawing.Color.FromArgb(180, 255, 180);
            
            // Update status bar to prompt user
            _inventorApp!.StatusBarText = "Select a cylindrical face to create threads...";
            
            // Request the host to restart selection events
            SelectionRequested?.Invoke();
        }

        private void cboThreadStandard_SelectedIndexChanged(object? sender, EventArgs e)
        {
            // Map combo index to thread family string
            _selectedThreadFamily = cboThreadStandard.SelectedIndex == 1 ? "ANSI" : "ISO";

            // Reload sizes and pitches for the selected standard
            LoadMatchingThreads();
            UpdatePreview();
        }

        private void cboThreadDesignation_SelectedIndexChanged(object? sender, EventArgs e)
        {
            // M size selected - load pitch options for this size
            if (cboThreadDesignation.SelectedIndex >= 0 && 
                cboThreadDesignation.SelectedIndex < _availableDiameters.Count)
            {
                double selectedDiameter = _availableDiameters[cboThreadDesignation.SelectedIndex];
                LoadPitchOptions(selectedDiameter);
            }
        }

        private void cboPitch_SelectedIndexChanged(object? sender, EventArgs e)
        {
            // Pitch selected - update thread details
            if (cboPitch.SelectedIndex >= 0 && 
                cboPitch.SelectedIndex < _pitchOptions.Count)
            {
                _selectedThread = _pitchOptions[cboPitch.SelectedIndex];
                UpdateThreadDetails();
                UpdatePreview();
                UpdateUI();
            }
        }

        private void numThreadLength_ValueChanged(object? sender, EventArgs e)
        {
            UpdatePreview();
        }

        private void chkPreview_CheckedChanged(object? sender, EventArgs e)
        {
            _previewEnabled = chkPreview.Checked;
            
            if (_previewEnabled)
            {
                UpdatePreview();
            }
            else
            {
                _previewManager?.ClearPreview();
            }
        }

        private void chkRightHand_CheckedChanged(object? sender, EventArgs e)
        {
            UpdatePreview();
        }

        private void chkReverseDirection_CheckedChanged(object? sender, EventArgs e)
        {
            UpdatePreview();
        }

        #endregion

        #region UI Update Methods

        private void UpdateUI()
        {
            var firstCylinder = _selectedCylinders.Count > 0 ? _selectedCylinders[0] : null;
            bool hasCylinder = firstCylinder != null && firstCylinder.IsValid;
            bool hasThread = _selectedThread != null;
            
            // Enable/disable controls based on state
            cboThreadDesignation.Enabled = hasCylinder;
            cboThreadStandard.Enabled = hasCylinder;
            numThreadLength.Enabled = hasCylinder && hasThread;
            chkReverseDirection.Enabled = hasCylinder && hasThread;
            chkRightHand.Enabled = hasCylinder && hasThread;
            chkPreview.Enabled = hasCylinder && hasThread;
            cboProfileType.Enabled = hasCylinder && hasThread;
            chkResizeCylinder.Enabled = hasCylinder && hasThread;
            btnApply.Enabled = hasCylinder && hasThread;
            btnDone.Enabled = hasCylinder && hasThread;
            
            // Update the Select Cylinder row appearance
            if (hasCylinder)
            {
                pnlSelectCylinder.BackColor = System.Drawing.Color.FromArgb(180, 255, 180);
            }
            else
            {
                pnlSelectCylinder.BackColor = System.Drawing.Color.Transparent;
            }
        }

        private void UpdateCylinderDisplay()
        {
            var firstCylinder = _selectedCylinders.Count > 0 ? _selectedCylinders[0] : null;
            
            if (firstCylinder != null && firstCylinder.IsValid)
            {
                if (_selectedCylinders.Count == 1)
                {
                    lblCylinderInfo.Text = firstCylinder.Description;
                }
                else
                {
                    lblCylinderInfo.Text = $"{_selectedCylinders.Count} holes selected";
                }
                lblDiameter.Text = $"Ø {firstCylinder.DiameterMm:F2} mm";
                
                // Show shortest length when multiple selected
                double minLength = _selectedCylinders.Min(c => c.LengthMm);
                lblLength.Text = $"L: {minLength:F2} mm";
                
                // Set max thread length to shortest cylinder length
                // Always default to full cylinder length
                decimal newMax = (decimal)minLength;
                numThreadLength.Maximum = newMax;
                numThreadLength.Value = newMax;
            }
            else
            {
                lblCylinderInfo.Text = "No cylinder selected";
                lblDiameter.Text = "Ø -- mm";
                lblLength.Text = "L: -- mm";
            }
            
            UpdateUI();
        }

        private void LoadMatchingThreads()
        {
            cboThreadDesignation.Items.Clear();
            cboPitch.Items.Clear();
            _availableDiameters.Clear();
            _pitchOptions.Clear();
            _selectedThread = null;

            var firstCylinder = _selectedCylinders.Count > 0 ? _selectedCylinders[0] : null;
            
            if (firstCylinder == null || !firstCylinder.IsValid || _threadDataManager == null)
            {
                cboPitch.Enabled = false;
                UpdateUI();
                return;
            }

            // Get unique sizes for the appropriate type and standard (internal/external)
            var (sizes, diameters, recommendedIndex) = _threadDataManager.GetAvailableSizes(
                firstCylinder.IsInternal,
                firstCylinder.DiameterCm,
                _selectedThreadFamily);

            _availableDiameters = diameters;

            // Populate the size combo box
            for (int i = 0; i < sizes.Count; i++)
            {
                string displayText;
                if (i == recommendedIndex)
                {
                    displayText = $"★ {sizes[i]}";  // Mark recommended
                }
                else
                {
                    displayText = sizes[i];
                }
                cboThreadDesignation.Items.Add(displayText);
            }

            // Select the recommended match
            if (sizes.Count > 0)
            {
                cboThreadDesignation.SelectedIndex = recommendedIndex;
                // This will trigger LoadPitchOptions via the event handler
            }
            else
            {
                lblThreadInfo.Text = "No thread sizes available";
            }

            UpdateUI();
        }

        private void LoadPitchOptions(double nominalDiameterCm)
        {
            cboPitch.Items.Clear();
            _pitchOptions.Clear();
            _selectedThread = null;

            var firstCylinder = _selectedCylinders.Count > 0 ? _selectedCylinders[0] : null;
            
            if (firstCylinder == null || _threadDataManager == null)
            {
                cboPitch.Enabled = false;
                UpdateUI();
                return;
            }

            // Get pitch options for this size and standard
            _pitchOptions = _threadDataManager.GetPitchOptionsForSize(
                nominalDiameterCm,
                firstCylinder.IsInternal,
                _selectedThreadFamily);

            // Populate pitch combo box with cleaner formatting
            for (int i = 0; i < _pitchOptions.Count; i++)
            {
                var thread = _pitchOptions[i];
                string displayText;

                if (_selectedThreadFamily == "ANSI")
                {
                    // Show TPI for ANSI threads
                    double tpi = 2.54 / thread.Pitch;  // Pitch is in cm
                    string tpiStr = Math.Abs(tpi - Math.Round(tpi)) < 0.01 ? $"{tpi:F0}" : $"{tpi:F1}";
                    displayText = i == 0 ? $"{tpiStr} TPI - Coarse" : $"{tpiStr} TPI - Fine";
                }
                else
                {
                    // Show mm for ISO threads
                    double pitchMm = thread.Pitch * 10;
                    string pitchStr = pitchMm % 1 == 0 ? $"{pitchMm:F0}" : $"{pitchMm:G3}";
                    displayText = i == 0 ? $"{pitchStr} mm - Coarse" : $"{pitchStr} mm - Fine";
                }
                
                cboPitch.Items.Add(displayText);
            }

            cboPitch.Enabled = _pitchOptions.Count > 0;

            // Select the coarse pitch by default
            if (_pitchOptions.Count > 0)
            {
                cboPitch.SelectedIndex = 0;
                _selectedThread = _pitchOptions[0];
                UpdateThreadDetails();
            }

            UpdateUI();
        }

        private void UpdateThreadDetails()
        {
            if (_selectedThread == null)
            {
                lblThreadInfo.Text = "Select a thread designation";
                lblPitch.Text = "Pitch: -- mm";
                lblMajorDia.Text = "Major Ø: -- mm";
                lblMinorDia.Text = "Minor Ø: -- mm";
                return;
            }

            // Show thread info with sizing context
            string sizeNote = "";
            var firstCylinder = _selectedCylinders.Count > 0 ? _selectedCylinders[0] : null;
            
            if (firstCylinder != null && firstCylinder.IsValid)
            {
                double cylDia = firstCylinder.DiameterCm;
                double threadMajorDia = _selectedThread.MajorDiameter;
                
                if (firstCylinder.IsInternal)
                {
                    // Internal thread - check if hole needs enlargement
                    if (cylDia < threadMajorDia)
                    {
                        double enlargeAmount = (threadMajorDia - cylDia) * 10; // to mm
                        sizeNote = $" (Hole will be enlarged by {enlargeAmount:F2}mm)";
                    }
                }
                else
                {
                    // External thread - check if shaft is large enough
                    if (cylDia < threadMajorDia)
                    {
                        sizeNote = " (WARNING: Shaft too small)";
                    }
                    else if (cylDia > threadMajorDia * 1.05)
                    {
                        double cutAmount = (cylDia - threadMajorDia) * 10; // to mm
                        sizeNote = $" (Will cut {cutAmount:F2}mm from Ø)";
                    }
                }
            }

            lblThreadInfo.Text = _selectedThread.FullThreadName + sizeNote;
            lblPitch.Text = $"Pitch: {_selectedThread.Pitch * 10:F2} mm";
            lblMajorDia.Text = $"Major Ø: {_selectedThread.MajorDiameter * 10:F2} mm";
            lblMinorDia.Text = $"Minor Ø: {_selectedThread.MinorDiameter * 10:F2} mm";
        }

        private void UpdatePreview()
        {
            if (!_previewEnabled || _previewManager == null)
            {
                return;
            }

            var firstCylinder = _selectedCylinders.Count > 0 ? _selectedCylinders[0] : null;
            
            if (firstCylinder == null || !firstCylinder.IsValid || _selectedThread == null)
            {
                _previewManager.ClearPreview();
                return;
            }

            // Show preview with current settings
            double threadLengthCm = (double)numThreadLength.Value / 10.0;  // mm to cm
            bool rightHand = chkRightHand.Checked;
            bool startFromEnd = chkReverseDirection.Checked;  // Reversed = start from other end

            _previewManager.ShowPreview(
                firstCylinder,
                _selectedThread,
                threadLengthCm,
                rightHand,
                startFromEnd);
        }

        #endregion

        #region Thread Creation

        private bool CreateThread(bool keepPreview = false)
        {
            if (_selectedCylinders.Count == 0 || _selectedThread == null || _threadGenerator == null)
            {
                MessageBox.Show("Please select a cylinder and thread designation first.",
                    "Threader", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }

            // Only clear preview if not keeping it
            if (!keepPreview)
            {
                _previewManager?.ClearPreview();
            }

            int successCount = 0;
            int failCount = 0;
            string lastError = "";

            try
            {
                this.Cursor = Cursors.WaitCursor;
                _inventorApp!.StatusBarText = $"Creating threads on {_selectedCylinders.Count} hole(s)...";

                // Get the part component definition for unique name generation
                var doc = (_inventorApp.ActiveEditDocument as PartDocument)
                          ?? (_inventorApp.ActiveDocument as PartDocument);
                var compDef = doc?.ComponentDefinition;

                // Create thread on each selected cylinder
                for (int i = 0; i < _selectedCylinders.Count; i++)
                {
                    var cylinder = _selectedCylinders[i];
                    
                    // After the first thread, Face references become stale due to geometry changes
                    // Re-find the cylindrical face by matching geometric properties
                    if (i > 0 && _cylinderAnalyzer != null)
                    {
                        var refound = _cylinderAnalyzer.RefindCylinder(cylinder);
                        if (refound != null && refound.IsValid)
                        {
                            cylinder = refound;
                            System.Diagnostics.Debug.WriteLine($"Re-found cylinder {i+1} successfully");
                        }
                        else
                        {
                            failCount++;
                            lastError = $"Could not re-find hole #{i+1} after geometry changed. Try creating threads one at a time.";
                            System.Diagnostics.Debug.WriteLine($"Failed to re-find cylinder {i+1}");
                            continue;
                        }
                    }
                    
                    // Convert profile type selection to enum
                    ThreadProfileType profileType = cboProfileType.SelectedIndex switch
                    {
                        1 => ThreadProfileType.Triangular,
                        2 => ThreadProfileType.Square,
                        _ => ThreadProfileType.Trapezoidal
                    };
                    
                    // Generate a unique feature name by checking existing features
                    string baseName = $"Thread_{_selectedThread.Designation}";
                    string featureName = GetUniqueFeatureName(compDef, baseName);

                    var options = new ThreadGenerationOptions
                    {
                        CylinderInfo = cylinder,
                        ThreadStandard = _selectedThread,
                        ThreadLengthCm = (double)numThreadLength.Value / 10.0,
                        RightHand = chkRightHand.Checked,
                        FullDepth = true,
                        FeatureName = featureName,
                        ResizeCylinder = chkResizeCylinder.Checked,
                        ProfileType = profileType,
                        StartFromEnd = chkReverseDirection.Checked  // Reversed = start from other end
                    };

                    // Validate
                    var validation = _threadGenerator.ValidateThread(options);
                    if (!validation.IsValid)
                    {
                        failCount++;
                        lastError = validation.Message;
                        continue;
                    }

                    // Generate
                    var result = _threadGenerator.GenerateThread(options);
                    if (result.Success)
                    {
                        successCount++;
                    }
                    else
                    {
                        failCount++;
                        lastError = result.Message;
                    }
                }

                // Show result
                if (failCount == 0)
                {
                    MessageBox.Show($"Successfully created {successCount} thread(s)!",
                        "Threader", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return true;
                }
                else if (successCount > 0)
                {
                    MessageBox.Show($"Created {successCount} thread(s), {failCount} failed.\nLast error: {lastError}",
                        "Threader", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return true;
                }
                else
                {
                    MessageBox.Show($"Failed to create threads:\n{lastError}",
                        "Threader", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error creating thread:\n{ex.Message}",
                    "Threader", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            finally
            {
                this.Cursor = Cursors.Default;
                _inventorApp!.StatusBarText = "";
            }
        }

        private void ClearSelection()
        {
            _selectedFaces.Clear();
            _selectedCylinders.Clear();
            _selectedThread = null;
            _availableDiameters.Clear();
            _pitchOptions.Clear();
            
            cboThreadDesignation.Items.Clear();
            cboThreadDesignation.Text = "";
            cboPitch.Items.Clear();
            cboPitch.Text = "";
            cboPitch.Enabled = false;
            
            UpdateCylinderDisplay();
            UpdateThreadDetails();
            _previewManager?.ClearPreview();
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Generates a unique feature name by checking existing features in the model.
        /// Appends an incrementing number suffix until a unique name is found.
        /// </summary>
        private string GetUniqueFeatureName(PartComponentDefinition? compDef, string baseName)
        {
            if (compDef == null)
                return $"{baseName}_{DateTime.Now:HHmmssfff}";

            // Collect all existing feature names that start with our base name
            var existingNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                foreach (PartFeature feature in compDef.Features)
                {
                    try { existingNames.Add(feature.Name); } catch { }
                }
                foreach (PlanarSketch sketch in compDef.Sketches)
                {
                    try { existingNames.Add(sketch.Name); } catch { }
                }
                foreach (WorkAxis axis in compDef.WorkAxes)
                {
                    try { existingNames.Add(axis.Name); } catch { }
                }
                foreach (WorkPlane plane in compDef.WorkPlanes)
                {
                    try { existingNames.Add(plane.Name); } catch { }
                }
            }
            catch { }

            // Find the next available number
            for (int n = 1; n <= 999; n++)
            {
                string candidate = $"{baseName}_{n}";
                // Check that none of the sub-feature names exist either
                if (!existingNames.Contains(candidate) &&
                    !existingNames.Contains($"{candidate}_Coil") &&
                    !existingNames.Contains($"{candidate}_Sketch") &&
                    !existingNames.Contains($"{candidate}_Axis"))
                {
                    return candidate;
                }
            }

            // Fallback: use timestamp
            return $"{baseName}_{DateTime.Now:HHmmssfff}";
        }

        #endregion

        #region Form Close

        private void CloseForm(bool success)
        {
            // Clear preview
            _previewManager?.ClearPreview();
            
            this.Hide();
            this.DialogResult = success ? DialogResult.OK : DialogResult.Cancel;
        }

        #endregion

        #region Form Overrides

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            _previewManager?.ClearPreview();
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
