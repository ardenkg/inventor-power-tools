// ============================================================================
// CaseSelection Add-in for Autodesk Inventor 2026
// ClassSelectionControl.cs - Main UI UserControl
// ============================================================================

using System;
using System.Drawing;
using System.Windows.Forms;
using Inventor;
using CaseSelection.Core;

namespace CaseSelection.UI
{
    /// <summary>
    /// UserControl that provides the Class Selection interface.
    /// Replicates NX-style selection intent functionality.
    /// </summary>
    public partial class ClassSelectionControl : UserControl
    {
        #region Events

        /// <summary>
        /// Event raised when the control requests to close.
        /// Boolean parameter indicates whether to commit the selection (true) or cancel (false).
        /// </summary>
        public event EventHandler<bool>? CloseRequested;

        #endregion

        #region Private Fields

        private Inventor.Application? _inventorApp;
        private SelectionManager? _selectionManager;

        // UI Controls - Section 1: Objects
        private Label? _lblSelectObjects;
        private Panel? _pnlGreenSwatch;
        private Button? _btnSelectAll;
        private Button? _btnInvertSelection;

        // UI Controls - Section 2: Find Similar
        private Label? _lblTemplateFaces;
        private Panel? _pnlOrangeSwatch;
        private Button? _btnFindSimilar;
        private Button? _btnAddToMain;

        // UI Controls - Section 3: Filters
        private Label? _lblTypeFilter;
        private ComboBox? _cboTypeFilter;
        private Panel? _pnlBlueSwatch;
        private Label? _lblClearSelection;
        private Button? _btnClear;

        // Footer Controls
        private Button? _btnDone;
        private Button? _btnCancel;

        #endregion

        #region Constructor

        public ClassSelectionControl()
        {
            InitializeComponent();
            BuildUI();
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Initializes the control with the Inventor application and selection manager.
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
            }

            UpdateCounts();
        }

        /// <summary>
        /// Initialize component (designer support).
        /// </summary>
        private void InitializeComponent()
        {
            this.SuspendLayout();
            
            this.AutoScaleDimensions = new SizeF(7F, 15F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(45, 45, 48);
            this.ForeColor = System.Drawing.Color.White;
            this.Font = new System.Drawing.Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
            this.MinimumSize = new Size(260, 420);
            this.Name = "ClassSelectionControl";
            this.Size = new Size(280, 450);
            this.Padding = new Padding(10);
            
            this.ResumeLayout(false);
        }

        #endregion

        #region UI Construction

        /// <summary>
        /// Builds the complete UI layout.
        /// </summary>
        private void BuildUI()
        {
            // Main container with vertical layout
            var mainPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 5,
                BackColor = System.Drawing.Color.FromArgb(45, 45, 48),
                Padding = new Padding(5)
            };

            // Row styles
            mainPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Objects section
            mainPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Find Similar section
            mainPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Filters section
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // Spacer
            mainPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Footer

            // Build sections
            mainPanel.Controls.Add(BuildObjectsSection(), 0, 0);
            mainPanel.Controls.Add(BuildFindSimilarSection(), 0, 1);
            mainPanel.Controls.Add(BuildFiltersSection(), 0, 2);
            mainPanel.Controls.Add(new Panel { Height = 10 }, 0, 3); // Spacer
            mainPanel.Controls.Add(BuildFooterSection(), 0, 4);

            this.Controls.Add(mainPanel);
        }

        /// <summary>
        /// Builds the "Objects" section.
        /// </summary>
        private Control BuildObjectsSection()
        {
            var groupBox = CreateSectionGroupBox("Objects");
            var panel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 2,
                AutoSize = true,
                Padding = new Padding(5)
            };

            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 70));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 25));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30));

            // Row 1: Select Objects label + Green swatch
            _lblSelectObjects = new Label
            {
                Text = "Select Objects (0)",
                ForeColor = System.Drawing.Color.White,
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                Padding = new Padding(0, 5, 0, 5)
            };

            _pnlGreenSwatch = CreateColorSwatch(System.Drawing.Color.FromArgb(0, 128, 0));

            panel.Controls.Add(_lblSelectObjects, 0, 0);
            panel.Controls.Add(_pnlGreenSwatch, 1, 0);

            // Row 2: Select All and Invert Selection buttons
            var buttonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true,
                WrapContents = false,
                Padding = new Padding(0, 5, 0, 5)
            };

            _btnSelectAll = CreateStyledButton("Select All", 90);
            _btnSelectAll.Click += OnSelectAllClick;

            _btnInvertSelection = CreateStyledButton("Invert Selection", 100);
            _btnInvertSelection.Click += OnInvertSelectionClick;

            buttonPanel.Controls.Add(_btnSelectAll);
            buttonPanel.Controls.Add(_btnInvertSelection);

            panel.SetColumnSpan(buttonPanel, 3);
            panel.Controls.Add(buttonPanel, 0, 1);

            groupBox.Controls.Add(panel);
            return groupBox;
        }

        /// <summary>
        /// Builds the "Find Similar" section.
        /// </summary>
        private Control BuildFindSimilarSection()
        {
            var groupBox = CreateSectionGroupBox("Find Similar");
            var panel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 2,
                AutoSize = true,
                Padding = new Padding(5)
            };

            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 70));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 25));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30));

            // Row 1: Template Faces label + Orange swatch
            _lblTemplateFaces = new Label
            {
                Text = "Template Faces (0)",
                ForeColor = System.Drawing.Color.White,
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                Padding = new Padding(0, 5, 0, 5)
            };

            _pnlOrangeSwatch = CreateColorSwatch(System.Drawing.Color.FromArgb(255, 165, 0));

            panel.Controls.Add(_lblTemplateFaces, 0, 0);
            panel.Controls.Add(_pnlOrangeSwatch, 1, 0);

            // Row 2: Find Similar and Add to Main buttons
            var buttonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true,
                WrapContents = false,
                Padding = new Padding(0, 5, 0, 5)
            };

            _btnFindSimilar = CreateStyledButton("Find Similar", 90);
            _btnFindSimilar.Click += OnFindSimilarClick;

            _btnAddToMain = CreateStyledButton("Add to Main", 90);
            _btnAddToMain.Click += OnAddToMainClick;

            buttonPanel.Controls.Add(_btnFindSimilar);
            buttonPanel.Controls.Add(_btnAddToMain);

            panel.SetColumnSpan(buttonPanel, 3);
            panel.Controls.Add(buttonPanel, 0, 1);

            groupBox.Controls.Add(panel);
            return groupBox;
        }

        /// <summary>
        /// Builds the "Filters" section.
        /// </summary>
        private Control BuildFiltersSection()
        {
            var groupBox = CreateSectionGroupBox("Filters");
            var panel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 3,
                AutoSize = true,
                Padding = new Padding(5)
            };

            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 30));

            // Row 1: Type Filter label
            _lblTypeFilter = new Label
            {
                Text = "Type Filter",
                ForeColor = System.Drawing.Color.White,
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                Padding = new Padding(0, 5, 0, 5)
            };
            panel.Controls.Add(_lblTypeFilter, 0, 0);

            // Row 2: ComboBox + Blue swatch
            _cboTypeFilter = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = System.Drawing.Color.FromArgb(60, 60, 65),
                ForeColor = System.Drawing.Color.White,
                FlatStyle = FlatStyle.Flat
            };
            _cboTypeFilter.Items.AddRange(new object[]
            {
                "Single Face",
                "Tangent Faces",
                "Boss/Pocket Faces",
                "Adjacent Faces",
                "Feature Faces",
                "Connected Blend Faces"
            });
            _cboTypeFilter.SelectedIndex = 0;
            _cboTypeFilter.SelectedIndexChanged += OnTypeFilterChanged;

            _pnlBlueSwatch = CreateColorSwatch(System.Drawing.Color.FromArgb(0, 0, 255));

            panel.SetColumnSpan(_cboTypeFilter, 2);
            panel.Controls.Add(_cboTypeFilter, 0, 1);
            panel.Controls.Add(_pnlBlueSwatch, 2, 1);

            // Row 3: Clear Selection label + Red button
            var clearPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true,
                WrapContents = false,
                Padding = new Padding(0, 10, 0, 5)
            };

            _lblClearSelection = new Label
            {
                Text = "Clear Selection",
                ForeColor = System.Drawing.Color.White,
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                Margin = new Padding(0, 5, 10, 0)
            };

            _btnClear = new Button
            {
                Size = new Size(28, 28),
                BackColor = System.Drawing.Color.FromArgb(200, 0, 0),
                FlatStyle = FlatStyle.Flat,
                Text = "X",
                ForeColor = System.Drawing.Color.White,
                Font = new System.Drawing.Font("Segoe UI", 10F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            _btnClear.FlatAppearance.BorderSize = 0;
            _btnClear.Click += OnClearClick;

            clearPanel.Controls.Add(_lblClearSelection);
            clearPanel.Controls.Add(_btnClear);

            panel.SetColumnSpan(clearPanel, 3);
            panel.Controls.Add(clearPanel, 0, 2);

            groupBox.Controls.Add(panel);
            return groupBox;
        }

        /// <summary>
        /// Builds the footer section with Done and Cancel buttons.
        /// </summary>
        private Control BuildFooterSection()
        {
            var panel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                AutoSize = true,
                Padding = new Padding(0, 10, 0, 5)
            };

            _btnCancel = CreateStyledButton("Cancel", 80);
            _btnCancel.BackColor = System.Drawing.Color.FromArgb(100, 100, 100);
            _btnCancel.Click += OnCancelClick;

            _btnDone = CreateStyledButton("Done", 80);
            _btnDone.BackColor = System.Drawing.Color.FromArgb(0, 122, 204);
            _btnDone.Click += OnDoneClick;

            panel.Controls.Add(_btnCancel);
            panel.Controls.Add(_btnDone);

            return panel;
        }

        #endregion

        #region UI Helper Methods

        /// <summary>
        /// Creates a styled GroupBox for sections.
        /// </summary>
        private GroupBox CreateSectionGroupBox(string title)
        {
            return new GroupBox
            {
                Text = title,
                ForeColor = System.Drawing.Color.FromArgb(200, 200, 200),
                Dock = DockStyle.Top,
                AutoSize = true,
                Padding = new Padding(5),
                Margin = new Padding(0, 0, 0, 10),
                Font = new System.Drawing.Font("Segoe UI", 9F, FontStyle.Bold)
            };
        }

        /// <summary>
        /// Creates a color swatch panel.
        /// </summary>
        private Panel CreateColorSwatch(System.Drawing.Color color)
        {
            var panel = new Panel
            {
                Size = new Size(20, 20),
                BackColor = color,
                BorderStyle = BorderStyle.FixedSingle,
                Margin = new Padding(5, 3, 5, 3)
            };
            return panel;
        }

        /// <summary>
        /// Creates a styled button.
        /// </summary>
        private Button CreateStyledButton(string text, int width)
        {
            var button = new Button
            {
                Text = text,
                Size = new Size(width, 28),
                FlatStyle = FlatStyle.Flat,
                BackColor = System.Drawing.Color.FromArgb(70, 70, 75),
                ForeColor = System.Drawing.Color.White,
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 5, 0)
            };
            button.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(100, 100, 100);
            button.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(90, 90, 95);
            button.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(50, 50, 55);
            return button;
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Handles the "Select All" button click.
        /// </summary>
        private void OnSelectAllClick(object? sender, EventArgs e)
        {
            try
            {
                _selectionManager?.SelectAllVisibleFaces();
            }
            catch (Exception ex)
            {
                ShowError("Error selecting all faces", ex);
            }
        }

        /// <summary>
        /// Handles the "Invert Selection" button click.
        /// </summary>
        private void OnInvertSelectionClick(object? sender, EventArgs e)
        {
            try
            {
                _selectionManager?.InvertSelection();
            }
            catch (Exception ex)
            {
                ShowError("Error inverting selection", ex);
            }
        }

        /// <summary>
        /// Handles the "Find Similar" button click.
        /// </summary>
        private void OnFindSimilarClick(object? sender, EventArgs e)
        {
            try
            {
                _selectionManager?.FindSimilarFaces();
            }
            catch (Exception ex)
            {
                ShowError("Error finding similar faces", ex);
            }
        }

        /// <summary>
        /// Handles the "Add to Main" button click.
        /// </summary>
        private void OnAddToMainClick(object? sender, EventArgs e)
        {
            try
            {
                _selectionManager?.AddTemplateFacesToMain();
            }
            catch (Exception ex)
            {
                ShowError("Error adding to main selection", ex);
            }
        }

        /// <summary>
        /// Handles the Type Filter combo box change.
        /// </summary>
        private void OnTypeFilterChanged(object? sender, EventArgs e)
        {
            try
            {
                if (_cboTypeFilter != null && _selectionManager != null)
                {
                    var filterType = (SelectionFilterType)_cboTypeFilter.SelectedIndex;
                    _selectionManager.SetFilterType(filterType);
                }
            }
            catch (Exception ex)
            {
                ShowError("Error changing filter type", ex);
            }
        }

        /// <summary>
        /// Handles the Clear button click.
        /// </summary>
        private void OnClearClick(object? sender, EventArgs e)
        {
            try
            {
                _selectionManager?.ClearSelection();
            }
            catch (Exception ex)
            {
                ShowError("Error clearing selection", ex);
            }
        }

        /// <summary>
        /// Handles the Done button click.
        /// </summary>
        private void OnDoneClick(object? sender, EventArgs e)
        {
            CloseRequested?.Invoke(this, true);
        }

        /// <summary>
        /// Handles the Cancel button click.
        /// </summary>
        private void OnCancelClick(object? sender, EventArgs e)
        {
            CloseRequested?.Invoke(this, false);
        }

        /// <summary>
        /// Handles selection changed events from the SelectionManager.
        /// </summary>
        private void OnSelectionChanged(object? sender, int count)
        {
            UpdateCountsSafe(count, null);
        }

        /// <summary>
        /// Handles template faces changed events from the SelectionManager.
        /// </summary>
        private void OnTemplateFacesChanged(object? sender, int count)
        {
            UpdateCountsSafe(null, count);
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Updates the counts displayed in the UI.
        /// </summary>
        private void UpdateCounts()
        {
            int objectCount = _selectionManager?.SelectedFaceCount ?? 0;
            int templateCount = _selectionManager?.TemplateFaceCount ?? 0;
            UpdateCountsSafe(objectCount, templateCount);
        }

        /// <summary>
        /// Thread-safe update of counts.
        /// </summary>
        private void UpdateCountsSafe(int? objectCount, int? templateCount)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action(() => UpdateCountsSafe(objectCount, templateCount)));
                return;
            }

            if (objectCount.HasValue && _lblSelectObjects != null)
            {
                _lblSelectObjects.Text = $"Select Objects ({objectCount.Value})";
            }

            if (templateCount.HasValue && _lblTemplateFaces != null)
            {
                _lblTemplateFaces.Text = $"Template Faces ({templateCount.Value})";
            }
        }

        /// <summary>
        /// Shows an error message.
        /// </summary>
        private void ShowError(string message, Exception ex)
        {
            MessageBox.Show($"{message}:\n{ex.Message}", "CaseSelection Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        #endregion

        #region Cleanup

        /// <summary>
        /// Clean up resources.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_selectionManager != null)
                {
                    _selectionManager.SelectionChanged -= OnSelectionChanged;
                    _selectionManager.TemplateFacesChanged -= OnTemplateFacesChanged;
                }
            }
            base.Dispose(disposing);
        }

        #endregion
    }
}
