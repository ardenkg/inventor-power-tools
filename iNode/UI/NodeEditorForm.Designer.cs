// ============================================================================
// iNode Add-in for Autodesk Inventor 2026
// NodeEditorForm.Designer.cs - Designer layout for the node editor window
// ============================================================================

using System.Drawing;
using System.Windows.Forms;

namespace iNode.UI
{
    partial class NodeEditorForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            // ── Instantiate all controls ──
            _titleBar = new Panel();
            _titleLabel = new Label();
            _btnMinimize = new Button();
            _btnMaximize = new Button();
            _btnClose = new Button();
            _toolbar = new Panel();
            _btnSave = new Button();
            _btnLoad = new Button();
            _btnClearAll = new Button();
            _btnZoomReset = new Button();
            _btnFrameAll = new Button();
            _chkPreview = new CheckBox();
            _btnResetAll = new Button();
            _btnCommit = new Button();
            _partSelectorPanel = new Panel();
            _lblPartSelector = new Label();
            _cboDocumentSelect = new ComboBox();
            _canvas = new NodeEditorCanvas();
            _statusBar = new Panel();
            _lblStatus = new Label();
            _lblNodeCount = new Label();
            _searchPopup = new NodeSearchPopup();

            SuspendLayout();
            _titleBar.SuspendLayout();
            _toolbar.SuspendLayout();
            _partSelectorPanel.SuspendLayout();
            _statusBar.SuspendLayout();

            // ═══════════════════════════════════════════════
            // Title Bar  (height 38)
            // ═══════════════════════════════════════════════
            _titleBar.BackColor = Color.FromArgb(0, 122, 204);
            _titleBar.Dock = DockStyle.Top;
            _titleBar.Height = 38;
            _titleBar.Padding = new Padding(0);

            // Title label
            _titleLabel.Text = "  iNode — Visual Parametric Editor";
            _titleLabel.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            _titleLabel.ForeColor = Color.White;
            _titleLabel.AutoSize = false;
            _titleLabel.Dock = DockStyle.Fill;
            _titleLabel.TextAlign = ContentAlignment.MiddleLeft;
            _titleLabel.Padding = new Padding(6, 0, 0, 0);

            // ── Title buttons panel: [─] [□] [×] (close furthest right) ──
            _titleButtonsPanel = new FlowLayoutPanel();
            _titleButtonsPanel.Dock = DockStyle.Right;
            _titleButtonsPanel.FlowDirection = FlowDirection.LeftToRight;
            _titleButtonsPanel.WrapContents = false;
            _titleButtonsPanel.AutoSize = true;
            _titleButtonsPanel.BackColor = Color.Transparent;
            _titleButtonsPanel.Margin = new Padding(0);
            _titleButtonsPanel.Padding = new Padding(0);

            // Minimize  ─
            _btnMinimize.Text = "─";
            _btnMinimize.Font = new Font("Segoe UI", 12F);
            _btnMinimize.ForeColor = Color.White;
            _btnMinimize.BackColor = Color.FromArgb(0, 122, 204);
            _btnMinimize.FlatStyle = FlatStyle.Flat;
            _btnMinimize.FlatAppearance.BorderSize = 0;
            _btnMinimize.FlatAppearance.MouseOverBackColor = Color.FromArgb(50, 255, 255, 255);
            _btnMinimize.Size = new Size(50, 38);
            _btnMinimize.Margin = new Padding(0);
            _btnMinimize.Cursor = Cursors.Hand;
            _btnMinimize.TabStop = false;

            // Maximize □
            _btnMaximize.Text = "□";
            _btnMaximize.Font = new Font("Segoe UI", 12F);
            _btnMaximize.ForeColor = Color.White;
            _btnMaximize.BackColor = Color.FromArgb(0, 122, 204);
            _btnMaximize.FlatStyle = FlatStyle.Flat;
            _btnMaximize.FlatAppearance.BorderSize = 0;
            _btnMaximize.FlatAppearance.MouseOverBackColor = Color.FromArgb(50, 255, 255, 255);
            _btnMaximize.Size = new Size(50, 38);
            _btnMaximize.Margin = new Padding(0);
            _btnMaximize.Cursor = Cursors.Hand;
            _btnMaximize.TabStop = false;

            // Close ×  (RED hover, furthest right)
            _btnClose.Text = "×";
            _btnClose.Font = new Font("Segoe UI", 14F);
            _btnClose.ForeColor = Color.White;
            _btnClose.BackColor = Color.FromArgb(0, 122, 204);
            _btnClose.FlatStyle = FlatStyle.Flat;
            _btnClose.FlatAppearance.BorderSize = 0;
            _btnClose.FlatAppearance.MouseOverBackColor = Color.FromArgb(232, 17, 35);
            _btnClose.Size = new Size(50, 38);
            _btnClose.Margin = new Padding(0);
            _btnClose.Cursor = Cursors.Hand;
            _btnClose.TabStop = false;

            _titleButtonsPanel.Controls.Add(_btnMinimize);
            _titleButtonsPanel.Controls.Add(_btnMaximize);
            _titleButtonsPanel.Controls.Add(_btnClose);

            _titleBar.Controls.Add(_titleLabel);
            _titleBar.Controls.Add(_titleButtonsPanel);

            // ═══════════════════════════════════════════════
            // Toolbar  (height 48)
            // ═══════════════════════════════════════════════
            _toolbar.BackColor = Color.FromArgb(240, 240, 240);
            _toolbar.Dock = DockStyle.Top;
            _toolbar.Height = 48;
            _toolbar.Padding = new Padding(8, 5, 8, 5);

            _toolbarFlow = new FlowLayoutPanel();
            _toolbarFlow.Dock = DockStyle.Fill;
            _toolbarFlow.FlowDirection = FlowDirection.LeftToRight;
            _toolbarFlow.WrapContents = false;
            _toolbarFlow.BackColor = Color.Transparent;
            _toolbarFlow.Margin = new Padding(0);
            _toolbarFlow.Padding = new Padding(0);

            SetupToolbarButton(_btnSave, "Save", false);
            SetupToolbarButton(_btnLoad, "Load", false);
            SetupToolbarButton(_btnClearAll, "Clear All", false);

            // Spacer
            var spacer1 = new Panel { Width = 12, Height = 38, BackColor = Color.Transparent, Margin = new Padding(0) };

            SetupToolbarButton(_btnZoomReset, "Zoom 100%", false);
            SetupToolbarButton(_btnFrameAll, "Frame All", false);

            // Preview toggle checkbox
            _chkPreview.Text = "Preview";
            _chkPreview.Font = new Font("Segoe UI", 9F);
            _chkPreview.ForeColor = Color.FromArgb(60, 60, 60);
            _chkPreview.Checked = true;
            _chkPreview.AutoSize = true;
            _chkPreview.Margin = new Padding(10, 10, 0, 0);
            _chkPreview.Cursor = Cursors.Hand;

            _toolbarFlow.Controls.Add(_btnSave);
            _toolbarFlow.Controls.Add(_btnLoad);
            _toolbarFlow.Controls.Add(_btnClearAll);
            _toolbarFlow.Controls.Add(spacer1);
            _toolbarFlow.Controls.Add(_btnZoomReset);
            _toolbarFlow.Controls.Add(_btnFrameAll);
            _toolbarFlow.Controls.Add(_chkPreview);

            // ── Reset All button (RIGHT-docked) ──
            _btnResetAll.Text = "Reset All";
            _btnResetAll.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            _btnResetAll.ForeColor = Color.White;
            _btnResetAll.BackColor = Color.FromArgb(180, 60, 60);
            _btnResetAll.FlatStyle = FlatStyle.Flat;
            _btnResetAll.FlatAppearance.BorderSize = 0;
            _btnResetAll.FlatAppearance.MouseOverBackColor = Color.FromArgb(210, 80, 80);
            _btnResetAll.Size = new Size(100, 38);
            _btnResetAll.Dock = DockStyle.Right;
            _btnResetAll.Cursor = Cursors.Hand;
            _btnResetAll.TextAlign = ContentAlignment.MiddleCenter;
            _btnResetAll.TabStop = false;

            // ── Commit button (RIGHT-docked, must be added before the Fill panel) ──
            _btnCommit.Text = "Commit";
            _btnCommit.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            _btnCommit.ForeColor = Color.White;
            _btnCommit.BackColor = Color.FromArgb(0, 150, 80);
            _btnCommit.FlatStyle = FlatStyle.Flat;
            _btnCommit.FlatAppearance.BorderSize = 0;
            _btnCommit.FlatAppearance.MouseOverBackColor = Color.FromArgb(0, 180, 100);
            _btnCommit.Size = new Size(160, 38);
            _btnCommit.Dock = DockStyle.Right;
            _btnCommit.Cursor = Cursors.Hand;
            _btnCommit.TextAlign = ContentAlignment.MiddleCenter;
            _btnCommit.TabStop = false;

            // Add right-docked buttons first, then FlowPanel (Fill dock) — order matters
            _toolbar.Controls.Add(_toolbarFlow);
            _toolbar.Controls.Add(_btnResetAll);
            _toolbar.Controls.Add(_btnCommit);

            // ═══════════════════════════════════════════════
            // Part Selector Bar  (height 40)
            // ═══════════════════════════════════════════════
            _partSelectorPanel.BackColor = Color.FromArgb(235, 245, 255);
            _partSelectorPanel.Dock = DockStyle.Top;
            _partSelectorPanel.Height = 44;
            _partSelectorPanel.Padding = new Padding(10, 6, 10, 6);

            _lblPartSelector.Text = "Document:";
            _lblPartSelector.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            _lblPartSelector.ForeColor = Color.FromArgb(40, 40, 40);
            _lblPartSelector.AutoSize = true;
            _lblPartSelector.Dock = DockStyle.Left;
            _lblPartSelector.TextAlign = ContentAlignment.MiddleLeft;
            _lblPartSelector.Padding = new Padding(0, 0, 6, 0);

            _cboDocumentSelect.Font = new Font("Segoe UI", 10F);
            _cboDocumentSelect.ForeColor = Color.FromArgb(40, 40, 40);
            _cboDocumentSelect.BackColor = Color.White;
            _cboDocumentSelect.DropDownStyle = ComboBoxStyle.DropDownList;
            _cboDocumentSelect.Dock = DockStyle.Fill;
            _cboDocumentSelect.FlatStyle = FlatStyle.Flat;
            _cboDocumentSelect.Margin = new Padding(0);
            _cboDocumentSelect.Cursor = Cursors.Hand;
            _cboDocumentSelect.TabStop = false;

            // Order matters: right-docked first, then fill
            _partSelectorPanel.Controls.Add(_cboDocumentSelect);
            _partSelectorPanel.Controls.Add(_lblPartSelector);

            // ═══════════════════════════════════════════════
            // Status Bar  (height 26)
            // ═══════════════════════════════════════════════
            _statusBar.BackColor = Color.FromArgb(0, 122, 204);
            _statusBar.Dock = DockStyle.Bottom;
            _statusBar.Height = 26;

            _lblStatus.Text = "Ready";
            _lblStatus.Font = new Font("Segoe UI", 9F);
            _lblStatus.ForeColor = Color.White;
            _lblStatus.AutoSize = false;
            _lblStatus.Dock = DockStyle.Fill;
            _lblStatus.TextAlign = ContentAlignment.MiddleLeft;
            _lblStatus.Padding = new Padding(10, 0, 0, 0);

            _lblNodeCount.Text = "Nodes: 0  |  Connections: 0";
            _lblNodeCount.Font = new Font("Segoe UI", 9F);
            _lblNodeCount.ForeColor = Color.White;
            _lblNodeCount.AutoSize = false;
            _lblNodeCount.Dock = DockStyle.Right;
            _lblNodeCount.Width = 280;
            _lblNodeCount.TextAlign = ContentAlignment.MiddleRight;
            _lblNodeCount.Padding = new Padding(0, 0, 10, 0);

            _statusBar.Controls.Add(_lblStatus);
            _statusBar.Controls.Add(_lblNodeCount);

            // ═══════════════════════════════════════════════
            // Canvas  (fill remaining space)
            // ═══════════════════════════════════════════════
            _canvas.Dock = DockStyle.Fill;
            _canvas.TabStop = true;
            _canvas.TabIndex = 0;

            // ═══════════════════════════════════════════════
            // Form
            // ═══════════════════════════════════════════════
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            BackColor = Color.FromArgb(250, 250, 250);
            ClientSize = new Size(1200, 800);
            FormBorderStyle = FormBorderStyle.None;
            KeyPreview = true;
            MinimumSize = new Size(800, 600);
            StartPosition = FormStartPosition.CenterScreen;
            ShowInTaskbar = true;
            Text = "iNode - Visual Parametric Editor";
            DoubleBuffered = true;
            Padding = new Padding(1);

            // Add controls – ORDER MATTERS for docking:
            // Fill must be added first, then Bottom, then Top (top items added last dock topmost)
            Controls.Add(_canvas);          // Fill
            Controls.Add(_statusBar);       // Bottom
            Controls.Add(_partSelectorPanel); // Top (below toolbar)
            Controls.Add(_toolbar);         // Top (below title bar)
            Controls.Add(_titleBar);        // Top (very top)

            _titleBar.ResumeLayout(false);
            _toolbar.ResumeLayout(false);
            _partSelectorPanel.ResumeLayout(false);
            _statusBar.ResumeLayout(false);
            ResumeLayout(false);
        }

        private void SetupToolbarButton(Button btn, string text, bool primary)
        {
            btn.Text = text;
            btn.Font = new Font("Segoe UI", 9.5F, primary ? FontStyle.Bold : FontStyle.Regular);
            btn.ForeColor = primary ? Color.White : Color.FromArgb(40, 40, 40);
            btn.BackColor = primary ? Color.FromArgb(0, 122, 204) : Color.FromArgb(228, 228, 228);
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = primary ? 0 : 1;
            btn.FlatAppearance.BorderColor = Color.FromArgb(200, 200, 200);
            btn.FlatAppearance.MouseOverBackColor = primary
                ? Color.FromArgb(28, 151, 234)
                : Color.FromArgb(215, 225, 240);
            btn.Cursor = Cursors.Hand;
            btn.Margin = new Padding(2, 0, 2, 0);
            btn.AutoSize = false;
            btn.Size = new Size(Math.Max(80, TextRenderer.MeasureText(text, btn.Font).Width + 24), 36);
            btn.TextAlign = ContentAlignment.MiddleCenter;
            btn.TabStop = false;
        }

        // ── Field declarations ──
        private CheckBox _chkPreview;
        private Button _btnResetAll;
    }
}
