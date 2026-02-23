// ============================================================================
// iNode Add-in for Autodesk Inventor 2026
// NodeEditorForm.cs - Main floating node editor window
// ============================================================================

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using iNode.Core;
using iNode.Nodes;

namespace iNode.UI
{
    /// <summary>
    /// Main free-floating, resizable window for the iNode visual programming editor.
    /// Matches CaseSelection styling: white theme, blue accents, Segoe UI fonts.
    /// </summary>
    public partial class NodeEditorForm : Form
    {
        #region Controls (declared here, laid out in Designer)

        // Win32 keyboard hook — needed because Inventor's native message loop
        // eats WM_KEYDOWN before WinForms can process it.
        private const int WH_KEYBOARD = 2;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_SYSKEYDOWN = 0x0104;
        private const int HC_ACTION = 0;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll")]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll")]
        private static extern uint GetCurrentThreadId();

        private delegate IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam);

        private IntPtr _keyboardHook = IntPtr.Zero;
        private HookProc? _hookDelegate; // prevent GC collection

        // Custom title bar
        private Panel _titleBar = null!;
        private FlowLayoutPanel _titleButtonsPanel = null!;
        private Label _titleLabel = null!;
        private Button _btnMinimize = null!;
        private Button _btnMaximize = null!;
        private Button _btnClose = null!;

        // Toolbar
        private Panel _toolbar = null!;
        private FlowLayoutPanel _toolbarFlow = null!;
        private Button _btnCommit = null!;
        private Button _btnClearAll = null!;
        private Button _btnSave = null!;
        private Button _btnLoad = null!;
        private Button _btnZoomReset = null!;
        private Button _btnFrameAll = null!;

        // Part selector
        private Panel _partSelectorPanel = null!;
        private Label _lblPartSelector = null!;
        private ComboBox _cboDocumentSelect = null!;

        // Canvas
        private NodeEditorCanvas _canvas = null!;

        // Status bar
        private Panel _statusBar = null!;
        private Label _lblStatus = null!;
        private Label _lblNodeCount = null!;

        // Node search popup
        private NodeSearchPopup _searchPopup = null!;
        private PointF _searchWorldPos;

        // 3D preview — always-on with toggle
        private const string PREVIEW_CLIENT_ID = "iNodePreview";
        private bool _previewEnabled = true;

        // Debounce timer for preview updates
        private System.Windows.Forms.Timer? _previewDebounceTimer;
        private const int DEBOUNCE_MS = 50;
        private bool _previewDirty;

        // Target document (explicitly selected by user, not just ActiveDocument)
        private Inventor.PartDocument? _targetDocument;

        // Feature tracking — maps SourceNodeId to committed feature internal names
        // so we can delete features when their source nodes are removed
        private readonly Dictionary<Guid, List<string>> _committedFeatures = new();

        // Undo/redo
        private readonly System.Collections.Generic.List<GraphSnapshot> _undoStack = new();
        private int _undoIndex = -1;
        private bool _suppressUndo;

        // Window dragging
        private bool _isDragging;
        private Point _dragOffset;

        // Window resizing
        private bool _isResizing;
        private Point _resizeStart;
        private Size _resizeStartSize;
        private const int RESIZE_BORDER = 6;

        // Maximize state
        private bool _isMaximized;
        private Rectangle _restoreBounds;

        // Active document info
        private string _activeDocPath = "";
        private string _activeDocName = "";
        private string _activeDocType = "";

        #endregion

        #region Properties

        /// <summary>The node graph being edited.</summary>
        public NodeGraph Graph => _canvas.Graph;

        /// <summary>The Inventor application reference for geometry execution.</summary>
        public Inventor.Application? InventorApp { get; set; }

        #endregion

        #region Constructor

        public NodeEditorForm()
        {
            InitializeComponent();
            WireEvents();
            InstallKeyboardHook();
        }

        private void InstallKeyboardHook()
        {
            _hookDelegate = KeyboardHookCallback;
            uint threadId = GetCurrentThreadId();
            _keyboardHook = SetWindowsHookEx(WH_KEYBOARD, _hookDelegate, IntPtr.Zero, threadId);
        }

        private void RemoveKeyboardHook()
        {
            if (_keyboardHook != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_keyboardHook);
                _keyboardHook = IntPtr.Zero;
            }
        }

        #endregion

        #region Events

        private void WireEvents()
        {
            // Title bar dragging
            _titleBar.MouseDown += TitleBar_MouseDown;
            _titleBar.MouseMove += TitleBar_MouseMove;
            _titleBar.MouseUp += TitleBar_MouseUp;
            _titleLabel.MouseDown += TitleBar_MouseDown;
            _titleLabel.MouseMove += TitleBar_MouseMove;
            _titleLabel.MouseUp += TitleBar_MouseUp;
            _titleBar.DoubleClick += (s, e) => ToggleMaximize();
            _titleLabel.DoubleClick += (s, e) => ToggleMaximize();

            // Title bar buttons
            _btnClose.Click += (s, e) => Close();
            _btnMaximize.Click += (s, e) => ToggleMaximize();
            _btnMinimize.Click += (s, e) => WindowState = FormWindowState.Minimized;

            // Toolbar buttons
            _btnCommit.Click += OnCommitWorkflow;
            _btnResetAll.Click += OnResetAll;
            _btnClearAll.Click += OnClearAll;
            _btnSave.Click += OnSaveWorkflow;
            _btnLoad.Click += OnLoadWorkflow;
            _btnZoomReset.Click += (s, e) => { _canvas.ResetZoom(); _canvas.Focus(); };
            _btnFrameAll.Click += (s, e) => { _canvas.FrameAll(); _canvas.Focus(); };

            // Part selector
            _cboDocumentSelect.SelectedIndexChanged += OnDocumentSelected;
            _cboDocumentSelect.DropDown += (s, ev) => PopulateOpenDocuments();

            // Canvas events
            _canvas.NodeSearchRequested += OnNodeSearchRequested;
            _canvas.GraphModified += OnGraphModified;
            _canvas.GraphModified += (s, ev) => _isDirty = true;
            _canvas.Graph.GraphChanged += (s, e) => UpdateStatusBar();
            _canvas.Graph.NodesRemoved += OnNodesRemoved;

            // Preview toggle
            _chkPreview.CheckedChanged += OnPreviewToggled;

            // Search popup
            _searchPopup.NodeTypeSelected += OnNodeTypeSelected;

            // Resize handling
            MouseDown += Form_MouseDown_Resize;
            MouseMove += Form_MouseMove_Resize;
            MouseUp += Form_MouseUp_Resize;

            // Border paint
            Paint += (s, e) =>
            {
                using var pen = new Pen(Color.FromArgb(180, 180, 180), 1);
                e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
            };
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            // Auto-detect active part on open
            RefreshActivePart();
            // Save initial state for undo
            SaveUndoState();

            // Set up debounce timer for preview updates
            _previewDebounceTimer = new System.Windows.Forms.Timer();
            _previewDebounceTimer.Interval = DEBOUNCE_MS;
            _previewDebounceTimer.Tick += OnPreviewDebounceElapsed;

            // Trigger initial preview if there are nodes
            if (_canvas.Graph.Nodes.Count > 0)
                SchedulePreviewUpdate();
        }

        #endregion

        #region Title Bar Dragging

        private void TitleBar_MouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                _isDragging = true;
                _dragOffset = e.Location;
                if (sender == _titleLabel)
                    _dragOffset = new Point(e.X + _titleLabel.Left, e.Y);
            }
        }

        private void TitleBar_MouseMove(object? sender, MouseEventArgs e)
        {
            if (_isDragging)
            {
                if (_isMaximized)
                {
                    ToggleMaximize();
                    _dragOffset = new Point(Width / 2, _titleBar.Height / 2);
                }

                var screenPoint = PointToScreen(e.Location);
                Location = new Point(screenPoint.X - _dragOffset.X, screenPoint.Y - _dragOffset.Y);
            }
        }

        private void TitleBar_MouseUp(object? sender, MouseEventArgs e)
        {
            _isDragging = false;
        }

        private void ToggleMaximize()
        {
            if (_isMaximized)
            {
                Bounds = _restoreBounds;
                _isMaximized = false;
                _btnMaximize.Text = "□";
            }
            else
            {
                _restoreBounds = Bounds;
                var screen = Screen.FromControl(this);
                Bounds = screen.WorkingArea;
                _isMaximized = true;
                _btnMaximize.Text = "❐";
            }
        }

        #endregion

        #region Window Resizing (border drag)

        private void Form_MouseDown_Resize(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && GetResizeEdge(e.Location) != 0)
            {
                _isResizing = true;
                _resizeStart = PointToScreen(e.Location);
                _resizeStartSize = Size;
            }
        }

        private void Form_MouseMove_Resize(object? sender, MouseEventArgs e)
        {
            if (_isResizing)
            {
                var current = PointToScreen(e.Location);
                int dx = current.X - _resizeStart.X;
                int dy = current.Y - _resizeStart.Y;

                int newW = Math.Max(MinimumSize.Width, _resizeStartSize.Width + dx);
                int newH = Math.Max(MinimumSize.Height, _resizeStartSize.Height + dy);
                Size = new Size(newW, newH);
                return;
            }

            int edge = GetResizeEdge(e.Location);
            Cursor = edge switch
            {
                1 => Cursors.SizeNWSE,
                2 => Cursors.SizeWE,
                3 => Cursors.SizeNS,
                _ => Cursors.Default
            };
        }

        private void Form_MouseUp_Resize(object? sender, MouseEventArgs e)
        {
            _isResizing = false;
        }

        private int GetResizeEdge(Point p)
        {
            if (_isMaximized) return 0;
            bool right = p.X >= Width - RESIZE_BORDER;
            bool bottom = p.Y >= Height - RESIZE_BORDER;
            if (right && bottom) return 1;
            if (right) return 2;
            if (bottom) return 3;
            return 0;
        }

        #endregion

        #region Part Selector

        private void OnDocumentSelected(object? sender, EventArgs e)
        {
            if (_cboDocumentSelect.SelectedIndex < 0) return;
            var item = _cboDocumentSelect.SelectedItem as DocumentItem;
            if (item == null) return;

            _activeDocPath = item.FullPath;
            _activeDocName = item.DisplayName;
            _activeDocType = item.DocType;
            _lblStatus.Text = $"Active: {_activeDocName}";

            // Update the target document reference for preview/commit
            _targetDocument = null;
            if (InventorApp != null && !string.IsNullOrEmpty(item.FullPath))
            {
                try
                {
                    foreach (Inventor.Document doc in InventorApp.Documents)
                    {
                        if (doc.FullFileName == item.FullPath &&
                            doc.DocumentType == Inventor.DocumentTypeEnum.kPartDocumentObject)
                        {
                            _targetDocument = (Inventor.PartDocument)doc;
                            break;
                        }
                    }
                }
                catch { }
            }

            // Re-render preview on the new document
            SchedulePreviewUpdate();
        }

        private void PopulateOpenDocuments()
        {
            if (InventorApp == null)
            {
                _cboDocumentSelect.Items.Clear();
                _cboDocumentSelect.Items.Add(new DocumentItem("(Inventor not connected)", "", ""));
                _cboDocumentSelect.SelectedIndex = 0;
                return;
            }

            string previousPath = _activeDocPath;
            _cboDocumentSelect.Items.Clear();

            try
            {
                int selectedIdx = -1;
                foreach (Inventor.Document doc in InventorApp.Documents)
                {
                    string docType = doc.DocumentType == Inventor.DocumentTypeEnum.kPartDocumentObject
                        ? "Part" : doc.DocumentType == Inventor.DocumentTypeEnum.kAssemblyDocumentObject
                        ? "Assembly" : "Other";

                    var item = new DocumentItem(doc.DisplayName, doc.FullFileName, docType);
                    int idx = _cboDocumentSelect.Items.Add(item);

                    if (doc.FullFileName == previousPath)
                        selectedIdx = idx;
                }

                if (_cboDocumentSelect.Items.Count == 0)
                {
                    _cboDocumentSelect.Items.Add(new DocumentItem("(no documents open)", "", ""));
                    _cboDocumentSelect.SelectedIndex = 0;
                }
                else
                {
                    _cboDocumentSelect.SelectedIndex = selectedIdx >= 0 ? selectedIdx : 0;
                }
            }
            catch (Exception ex)
            {
                _cboDocumentSelect.Items.Clear();
                _cboDocumentSelect.Items.Add(new DocumentItem("(error reading documents)", "", ""));
                _cboDocumentSelect.SelectedIndex = 0;
                System.Diagnostics.Debug.WriteLine($"PopulateOpenDocuments error: {ex.Message}");
            }
        }

        private void RefreshActivePart()
        {
            PopulateOpenDocuments();

            // Auto-select active document
            if (InventorApp != null)
            {
                try
                {
                    var doc = InventorApp.ActiveDocument;
                    if (doc != null)
                    {
                        for (int i = 0; i < _cboDocumentSelect.Items.Count; i++)
                        {
                            if (_cboDocumentSelect.Items[i] is DocumentItem item && item.FullPath == doc.FullFileName)
                            {
                                _cboDocumentSelect.SelectedIndex = i;
                                break;
                            }
                        }
                    }
                }
                catch { }
            }
        }

        #endregion

        #region Toolbar Actions

        private bool _isDirty;

        private void OnGraphModified(object? sender, EventArgs e)
        {
            SaveUndoState();
            UpdateStatusBar();

            // Always schedule a preview update (debounced)
            SchedulePreviewUpdate();
        }

        /// <summary>
        /// Schedules a debounced preview update. Resets the timer on every call
        /// so rapid changes (e.g. slider dragging) coalesce into one update.
        /// </summary>
        private void SchedulePreviewUpdate()
        {
            _previewDirty = true;
            if (_previewDebounceTimer != null)
            {
                _previewDebounceTimer.Stop();
                _previewDebounceTimer.Start();
            }
        }

        /// <summary>
        /// Handles the Preview checkbox toggle — show or hide preview graphics.
        /// </summary>
        private void OnPreviewToggled(object? sender, EventArgs e)
        {
            _previewEnabled = _chkPreview.Checked;
            if (_previewEnabled)
            {
                SchedulePreviewUpdate();
            }
            else
            {
                ClearPreviewGraphics();
                _lblStatus.Text = "Preview disabled";
            }
        }

        /// <summary>
        /// Fires when the debounce timer elapses — executes the graph and updates preview.
        /// </summary>
        private void OnPreviewDebounceElapsed(object? sender, EventArgs e)
        {
            _previewDebounceTimer?.Stop();
            if (!_previewDirty) return;
            _previewDirty = false;

            ExecuteAndPreview();
        }

        /// <summary>
        /// Validates, executes the graph, and updates the live preview.
        /// Called automatically whenever the graph changes.
        /// Skips validation failures from isolated/unconnected nodes so adding
        /// a new node doesn't clear the existing preview.
        /// </summary>
        private void ExecuteAndPreview()
        {
            if (InventorApp == null) return;
            if (!_previewEnabled) return;
            if (_canvas.Graph.Nodes.Count == 0)
            {
                ClearPreviewGraphics();
                return;
            }

            try
            {
                // For preview, skip strict validation. Instead, just execute
                // the graph and let individual nodes set HasError gracefully.
                // This prevents isolated or partially-wired nodes from killing
                // the entire preview for the rest of the graph.
                var context = new Core.InventorContext(InventorApp, _targetDocument);
                context.Mode = Core.ExecutionMode.Preview;
                _canvas.Graph.Execute(context);
                ClearPreviewGraphics();

                // Check if any bodies were produced
                var bodies = GetTerminalBodyOutputs();
                if (bodies.Count > 0)
                {
                    ShowPreviewGraphics();
                    _lblStatus.Text = "Preview active";
                }
                else
                {
                    // No bodies but no crash — graph may be incomplete
                    int errorCount = _canvas.Graph.Nodes.Count(n => n.HasError);
                    _lblStatus.Text = errorCount > 0
                        ? $"Preview: {errorCount} node(s) with issues"
                        : "Preview: no geometry produced";
                }
            }
            catch (Exception ex)
            {
                _lblStatus.Text = $"Preview error: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"Preview error: {ex}");
            }
        }

        /// <summary>
        /// Commits the workflow to the Inventor document as real features
        /// wrapped in a single undo-able transaction.
        /// </summary>
        private void OnCommitWorkflow(object? sender, EventArgs e)
        {
            _lblStatus.Text = "Validating workflow...";
            Application.DoEvents();

            var errors = _canvas.Graph.Validate();
            if (errors.Count > 0)
            {
                string errorList = string.Join("\n", errors);
                MessageBox.Show(this, $"Validation errors:\n\n{errorList}", "Workflow Validation",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _lblStatus.Text = $"Validation failed ({errors.Count} error(s))";
                return;
            }

            _lblStatus.Text = "Committing workflow...";
            _btnCommit.Enabled = false;
            _btnCommit.Text = "Committing...";
            Application.DoEvents();

            try
            {
                var context = InventorApp != null
                    ? new Core.InventorContext(InventorApp, _targetDocument)
                    : null;

                if (context != null)
                    context.Mode = Core.ExecutionMode.Commit;

                bool success = _canvas.Graph.Execute(context);

                if (success)
                {
                    if (InventorApp != null)
                    {
                        ClearPreviewGraphics();
                        CommitGeometryToInventor();
                        _lblStatus.Text = "Workflow committed successfully!";

                        // Re-execute and show preview again after commit
                        SchedulePreviewUpdate();
                    }
                    else
                    {
                        _lblStatus.Text = "Workflow computed (no Inventor connection)";
                    }
                }
                else
                {
                    int errorCount = _canvas.Graph.Nodes.Count(n => n.HasError);
                    _lblStatus.Text = $"Execution completed with {errorCount} error(s)";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Execution error:\n{ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                _lblStatus.Text = "Commit failed";
            }
            finally
            {
                _btnCommit.Enabled = true;
                _btnCommit.Text = "Commit";
                _canvas.Invalidate();
            }
        }

        private void OnClearAll(object? sender, EventArgs e)
        {
            if (_canvas.Graph.Nodes.Count == 0) return;

            var result = MessageBox.Show(this, "Clear all nodes and connections?", "Clear All",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                _canvas.Graph.Clear();
                _canvas.Invalidate();
                _lblStatus.Text = "Canvas cleared";
            }
        }

        /// <summary>
        /// Resets all committed features in Inventor that were created by this workflow.
        /// Iterates through ALL tracked features and deletes them in reverse order.
        /// </summary>
        private void OnResetAll(object? sender, EventArgs e)
        {
            if (_committedFeatures.Count == 0)
            {
                MessageBox.Show(this, "No committed features to reset.", "Reset All",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var result = MessageBox.Show(this,
                "Delete ALL features created by this workflow in Inventor?\n\n" +
                "This will remove all committed geometry, fillets, chamfers,\n" +
                "and color overrides from the feature tree.",
                "Reset All", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

            if (result != DialogResult.Yes) return;

            if (InventorApp == null) return;

            Inventor.PartDocument? partDoc = _targetDocument;
            if (partDoc == null)
            {
                try
                {
                    var doc = InventorApp.ActiveDocument;
                    if (doc != null && doc.DocumentType == Inventor.DocumentTypeEnum.kPartDocumentObject)
                        partDoc = (Inventor.PartDocument)doc;
                }
                catch { }
            }
            if (partDoc == null)
            {
                MessageBox.Show(this, "No active part document found.", "Reset All",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var compDef = partDoc.ComponentDefinition;
            int deletedCount = 0;
            int failedCount = 0;

            // Collect all node IDs and process each
            var allNodeIds = _committedFeatures.Keys.ToList();
            foreach (var nodeId in allNodeIds)
            {
                if (!_committedFeatures.TryGetValue(nodeId, out var featureNames))
                    continue;

                // Delete in reverse order (child features before parent)
                for (int i = featureNames.Count - 1; i >= 0; i--)
                {
                    try
                    {
                        foreach (dynamic feature in compDef.Features)
                        {
                            try
                            {
                                if ((string)feature.Name == featureNames[i])
                                {
                                    feature.Delete();
                                    deletedCount++;
                                    break;
                                }
                            }
                            catch { continue; }
                        }
                    }
                    catch
                    {
                        failedCount++;
                    }
                }
            }

            _committedFeatures.Clear();

            try { InventorApp.ActiveView?.Update(); } catch { }

            string msg = $"Reset complete: {deletedCount} feature(s) deleted.";
            if (failedCount > 0) msg += $" ({failedCount} failed)";
            _lblStatus.Text = msg;
        }

        private void OnSaveWorkflow(object? sender, EventArgs e)
        {
            using var dlg = new SaveFileDialog
            {
                Title = "Save Workflow",
                Filter = "iNode Workflow (*.inode)|*.inode|JSON (*.json)|*.json",
                DefaultExt = "inode",
                FileName = "workflow"
            };

            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                try
                {
                    WorkflowSerializer.Save(_canvas.Graph, dlg.FileName);
                    _isDirty = false;
                    _lblStatus.Text = $"Saved: {System.IO.Path.GetFileName(dlg.FileName)}";

                    // Track as recent
                    RecentWorkflowManager.AddRecent(dlg.FileName);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, $"Error saving workflow:\n{ex.Message}", "Save Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void OnLoadWorkflow(object? sender, EventArgs e)
        {
            using var dlg = new OpenFileDialog
            {
                Title = "Load Workflow",
                Filter = "iNode Workflow (*.inode)|*.inode|JSON (*.json)|*.json|All Files (*.*)|*.*",
                DefaultExt = "inode"
            };

            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                LoadWorkflowFromFile(dlg.FileName);
            }
        }

        /// <summary>
        /// Loads a workflow from a file path. Can be called externally (e.g., from Recent Workflows).
        /// </summary>
        public void LoadWorkflowFromFile(string filePath)
        {
            try
            {
                var graph = WorkflowSerializer.Load(filePath);
                _canvas.Graph = graph;
                graph.GraphChanged += (s, ev) => UpdateStatusBar();
                _canvas.FrameAll();
                _isDirty = false;
                _lblStatus.Text = $"Loaded: {System.IO.Path.GetFileName(filePath)}";
                UpdateStatusBar();

                // Track as recent
                RecentWorkflowManager.AddRecent(filePath);

                // Trigger preview
                SchedulePreviewUpdate();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Error loading workflow:\n{ex.Message}", "Load Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OnNodeSearchRequested(object? sender, PointF worldPos)
        {
            _searchWorldPos = worldPos;
            // Convert world coords → client coords → screen coords
            var clientPt = _canvas.WorldToClient(worldPos);
            var screenPos = _canvas.PointToScreen(new Point((int)clientPt.X, (int)clientPt.Y));
            _searchPopup.ShowAt(screenPos);
        }

        private void OnNodeTypeSelected(object? sender, string typeName)
        {
            _canvas.AddNodeAt(typeName, _searchWorldPos);
        }

        #endregion

        #region Status Bar

        private void UpdateStatusBar()
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(UpdateStatusBar));
                return;
            }

            _lblNodeCount.Text = $"Nodes: {_canvas.Graph.Nodes.Count}  |  Connections: {_canvas.Graph.Connections.Count}";
        }

        #endregion

        #region Inventor Geometry Commit

        /// <summary>
        /// Commits the workflow result to the Inventor document as real features.
        /// All operations are wrapped in a single Inventor Transaction
        /// so they can be undone with a single Ctrl+Z.
        /// Uses _targetDocument (from the part selector combo) instead of ActiveDocument.
        /// </summary>
        private void CommitGeometryToInventor()
        {
            if (InventorApp == null) return;

            Inventor.Transaction? txn = null;

            try
            {
                var partDoc = _targetDocument;

                // Fallback to active document if no target selected
                if (partDoc == null)
                {
                    var doc = InventorApp.ActiveDocument;
                    if (doc == null || doc.DocumentType != Inventor.DocumentTypeEnum.kPartDocumentObject)
                    {
                        _lblStatus.Text = "Please open a Part document first";
                        MessageBox.Show(this, "Please open a Part document to commit geometry.", "No Part Document",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }
                    partDoc = (Inventor.PartDocument)doc;
                }

                var compDef = partDoc.ComponentDefinition;
                var terminalBodies = GetTerminalBodyOutputs();
                var terminalProfiles = GetTerminalProfileOutputs();

                if (terminalBodies.Count == 0 && terminalProfiles.Count == 0)
                {
                    _lblStatus.Text = "No Bake nodes found";
                    MessageBox.Show(this,
                        "No geometry to commit.\n\nConnect your final body or profile to a Bake node to mark it for commit.",
                        "No Bake Nodes", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                // Start a single transaction so all features can be undone together
                txn = InventorApp.TransactionManager.StartTransaction(
                    (Inventor._Document)partDoc, "iNode Commit");

                var failedOps = new List<string>();

                foreach (var bodyData in terminalBodies)
                {
                    try
                    {
                        CommitBodyToDocument(compDef, bodyData, failedOps);
                    }
                    catch (Exception ex)
                    {
                        failedOps.Add($"Body '{bodyData.Description}': {ex.Message}");
                    }
                }

                foreach (var profileData in terminalProfiles)
                {
                    try
                    {
                        CommitProfileToDocument(compDef, profileData, failedOps);
                    }
                    catch (Exception ex)
                    {
                        failedOps.Add($"Profile '{profileData.Description}': {ex.Message}");
                    }
                }

                txn.End();
                txn = null;

                // Report any failed pending operations
                if (failedOps.Count > 0)
                {
                    string msg = "Some operations could not be applied:\n\n" +
                        string.Join("\n", failedOps);
                    MessageBox.Show(this, msg, "Partial Commit",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                // Abort transaction on error — rolls back all changes
                try { txn?.Abort(); } catch { }
                _lblStatus.Text = $"Commit error: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"Inventor commit error: {ex}");
                throw;
            }
        }

        /// <summary>
        /// Collects BodyData from Bake nodes only. Only bodies explicitly
        /// connected to a Bake node get committed to Inventor.
        /// </summary>
        private List<BodyData> GetTerminalBodyOutputs()
        {
            var results = new List<BodyData>();
            var committed = new HashSet<Guid>(); // Track SourceNodeId to avoid duplicates

            foreach (var node in _canvas.Graph.Nodes)
            {
                if (node is Nodes.BakeNode bakeNode && !bakeNode.HasError)
                {
                    var bodyVal = bakeNode.GetInput("Body")?.GetEffectiveValue();
                    if (bodyVal is BodyData bd && bd.Body != null && committed.Add(bd.SourceNodeId))
                        results.Add(bd);
                }
            }
            return results;
        }

        /// <summary>
        /// Collects SketchProfileData from Bake nodes.
        /// Profiles connected to a Bake node's Profile input get committed as Inventor sketches.
        /// </summary>
        private List<SketchProfileData> GetTerminalProfileOutputs()
        {
            var results = new List<SketchProfileData>();

            foreach (var node in _canvas.Graph.Nodes)
            {
                if (node is Nodes.BakeNode bakeNode && !bakeNode.HasError)
                {
                    var profileVal = bakeNode.GetInput("Profile")?.GetEffectiveValue();
                    if (profileVal is SketchProfileData spd && spd.Curves.Count > 0)
                        results.Add(spd);
                }
            }
            return results;
        }

        /// <summary>
        /// Commits a SketchProfileData by creating a PlanarSketch in the
        /// active Inventor part document with the profile's curves drawn on
        /// the appropriate plane.
        /// </summary>
        private void CommitProfileToDocument(
            Inventor.PartComponentDefinition compDef, SketchProfileData profileData,
            List<string> failedOps)
        {
            try
            {
                var plane = profileData.Plane;

                // Find the matching origin work plane
                Inventor.WorkPlane? wp = null;
                try
                {
                    if (plane.StandardPlaneIndex == 3) // XY
                        wp = compDef.WorkPlanes[3];
                    else if (plane.StandardPlaneIndex == 2) // XZ
                        wp = compDef.WorkPlanes[2];
                    else if (plane.StandardPlaneIndex == 1) // YZ
                        wp = compDef.WorkPlanes[1];
                }
                catch { }

                if (wp == null)
                    wp = compDef.WorkPlanes[3]; // default XY

                Inventor.PlanarSketch sketch;

                // If offset, create an offset work plane first
                if (Math.Abs(plane.OffsetCm) > 0.0001)
                {
                    var offsetWP = compDef.WorkPlanes.AddByPlaneAndOffset(
                        wp, plane.OffsetCm);
                    offsetWP.Visible = false;
                    sketch = compDef.Sketches.Add(offsetWP);
                }
                else
                {
                    sketch = compDef.Sketches.Add(wp);
                }

                var tg = InventorApp!.TransientGeometry;
                double mmToCm = Core.InventorContext.MM_TO_CM;

                foreach (var curve in profileData.Curves)
                {
                    try
                    {
                        if (curve is CircleProfileCurve circle)
                        {
                            var center = tg.CreatePoint2d(
                                circle.CenterX * mmToCm,
                                circle.CenterY * mmToCm);
                            sketch.SketchCircles.AddByCenterRadius(
                                center, circle.Radius * mmToCm);
                        }
                        else if (curve is RectangleProfileCurve rect)
                        {
                            double halfW = rect.Width / 2.0 * mmToCm;
                            double halfH = rect.Height / 2.0 * mmToCm;
                            double cx = rect.CenterX * mmToCm;
                            double cy = rect.CenterY * mmToCm;
                            var p1 = tg.CreatePoint2d(cx - halfW, cy - halfH);
                            var p2 = tg.CreatePoint2d(cx + halfW, cy + halfH);
                            sketch.SketchLines.AddAsTwoPointRectangle(p1, p2);
                        }
                        else if (curve is LineProfileCurve line)
                        {
                            var sp = tg.CreatePoint2d(
                                line.StartX * mmToCm, line.StartY * mmToCm);
                            var ep = tg.CreatePoint2d(
                                line.EndX * mmToCm, line.EndY * mmToCm);
                            sketch.SketchLines.AddByTwoPoints(sp, ep);
                        }
                        else if (curve is EllipseProfileCurve ellipse)
                        {
                            var center = tg.CreatePoint2d(
                                ellipse.CenterX * mmToCm,
                                ellipse.CenterY * mmToCm);
                            var majorAxis = tg.CreateUnitVector2d(1.0, 0.0);
                            sketch.SketchEllipses.Add(
                                center, majorAxis,
                                ellipse.MajorRadius * mmToCm,
                                ellipse.MinorRadius * mmToCm);
                        }
                        else if (curve is PolygonProfileCurve polygon)
                        {
                            // Draw polygon as line segments
                            int sides = polygon.Sides;
                            double r = polygon.Radius * mmToCm;
                            double cxP = polygon.CenterX * mmToCm;
                            double cyP = polygon.CenterY * mmToCm;
                            Inventor.Point2d? firstPt = null;
                            Inventor.Point2d? prevPt = null;
                            for (int i = 0; i < sides; i++)
                            {
                                double angle = 2 * Math.PI * i / sides - Math.PI / 2;
                                var pt = tg.CreatePoint2d(
                                    cxP + r * Math.Cos(angle),
                                    cyP + r * Math.Sin(angle));
                                if (prevPt != null)
                                    sketch.SketchLines.AddByTwoPoints(prevPt, pt);
                                else
                                    firstPt = pt;
                                prevPt = pt;
                            }
                            if (prevPt != null && firstPt != null)
                                sketch.SketchLines.AddByTwoPoints(prevPt, firstPt);
                        }
                        else if (curve is SlotProfileCurve slot)
                        {
                            // Draw slot as two lines + two arcs
                            double halfL = slot.Length / 2.0 * mmToCm;
                            double halfW = slot.Width / 2.0 * mmToCm;
                            double cxS = slot.CenterX * mmToCm;
                            double cyS = slot.CenterY * mmToCm;
                            // Approximate as rectangle (Inventor has no direct slot API in sketch)
                            var p1 = tg.CreatePoint2d(cxS - halfL, cyS - halfW);
                            var p2 = tg.CreatePoint2d(cxS + halfL, cyS + halfW);
                            sketch.SketchLines.AddAsTwoPointRectangle(p1, p2);
                        }
                    }
                    catch (Exception ex)
                    {
                        failedOps.Add($"Profile curve: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                failedOps.Add($"Profile commit: {ex.Message}");
            }
        }

        /// <summary>
        /// Inserts a transient SurfaceBody into the part document using
        /// NonParametricBaseFeature, then applies any pending operations
        /// (fillets, chamfers) that couldn't be done on the transient body.
        /// If the body originated from ActiveBody (IsFromActivePart),
        /// suppresses all existing features to avoid duplicating the original body.
        /// </summary>
        private void CommitBodyToDocument(
            Inventor.PartComponentDefinition compDef, BodyData bodyData,
            List<string> failedOps)
        {
            try
            {
                var body = (Inventor.SurfaceBody)bodyData.Body!;

                // If this body came from the active part and was transformed,
                // suppress ALL existing features so only our new base feature
                // (with the transformed body) appears in the part.
                if (bodyData.IsFromActivePart)
                {
                    try
                    {
                        // Collect all suppressible features first (avoid modifying while iterating)
                        var featuresToSuppress = new List<dynamic>();
                        foreach (dynamic feature in compDef.Features)
                        {
                            try
                            {
                                // Skip work-plane/work-axis/work-point features
                                string typeName = feature.Type.ToString();
                                if (typeName.Contains("WorkPlane") || typeName.Contains("WorkAxis") ||
                                    typeName.Contains("WorkPoint"))
                                    continue;
                                featuresToSuppress.Add(feature);
                            }
                            catch { }
                        }

                        foreach (dynamic feature in featuresToSuppress)
                        {
                            try { feature.Suppressed = true; } catch { }
                        }
                    }
                    catch (Exception ex)
                    {
                        failedOps.Add($"Could not suppress original body: {ex.Message}");
                    }
                }

                // Insert the transient body as a base feature
                var baseDef = compDef.Features.NonParametricBaseFeatures
                    .CreateDefinition();
                var bodyCollection = InventorApp!.TransientObjects.CreateObjectCollection();
                bodyCollection.Add(body);
                baseDef.BRepEntities = bodyCollection;
                baseDef.OutputType = Inventor.BaseFeatureOutputTypeEnum.kSolidOutputType;

                var baseFeature = compDef.Features.NonParametricBaseFeatures.AddByDefinition(baseDef);

                // Track the base feature for this node
                if (!_committedFeatures.ContainsKey(bodyData.SourceNodeId))
                    _committedFeatures[bodyData.SourceNodeId] = new List<string>();
                _committedFeatures[bodyData.SourceNodeId].Add(baseFeature.Name);

                // Apply pending operations (fillet, chamfer) on the real inserted body
                if (bodyData.PendingOperations != null && bodyData.PendingOperations.Count > 0)
                {
                    // Get the actual inserted body from the feature
                    Inventor.SurfaceBody? insertedBody = null;
                    if (baseFeature.SurfaceBodies.Count > 0)
                        insertedBody = baseFeature.SurfaceBodies[1]; // 1-based

                    if (insertedBody != null)
                    {
                        ApplyPendingOperations(compDef, insertedBody, bodyData.PendingOperations, failedOps, bodyData.SourceNodeId);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error applying body '{bodyData.Description}': {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Applies pending fillet, chamfer, and color operations on an inserted body.
        /// Uses EdgeSnapshot/FaceSnapshot geometric matching to find the correct
        /// edges/faces on the real body since transient references don't survive insertion.
        /// Tracks features under each operation's OriginNodeId for per-node deletion.
        /// </summary>
        private void ApplyPendingOperations(
            Inventor.PartComponentDefinition compDef,
            Inventor.SurfaceBody body,
            List<PendingOperation> operations,
            List<string> failedOps,
            Guid bodySourceNodeId = default)
        {
            foreach (var op in operations)
            {
                // Track under the operation's own node, falling back to body source
                Guid trackId = op.OriginNodeId != default ? op.OriginNodeId : bodySourceNodeId;

                try
                {
                    if (op is PendingFillet fillet)
                    {
                        var edgeCollection = InventorApp!.TransientObjects.CreateEdgeCollection();
                        foreach (var snapshot in fillet.EdgeSnapshots)
                        {
                            var edge = snapshot.FindMatchingEdge(body);
                            if (edge != null)
                                edgeCollection.Add(edge);
                        }

                        if (edgeCollection.Count > 0)
                        {
                            var filletFeature = compDef.Features.FilletFeatures.AddSimple(
                                edgeCollection,
                                fillet.Radius * Core.InventorContext.MM_TO_CM);
                            TrackFeature(trackId, filletFeature.Name);
                        }
                    }
                    else if (op is PendingChamfer chamfer)
                    {
                        var edgeCollection = InventorApp!.TransientObjects.CreateEdgeCollection();
                        foreach (var snapshot in chamfer.EdgeSnapshots)
                        {
                            var edge = snapshot.FindMatchingEdge(body);
                            if (edge != null)
                                edgeCollection.Add(edge);
                        }

                        if (edgeCollection.Count > 0)
                        {
                            double dist = chamfer.Distance * Core.InventorContext.MM_TO_CM;
                            try
                            {
                                var chamferFeature = compDef.Features.ChamferFeatures.AddUsingDistance(
                                    edgeCollection, dist);
                                try { TrackFeature(trackId, ((dynamic)chamferFeature).Name); } catch { }
                            }
                            catch (Exception ex1)
                            {
                                try
                                {
                                    dynamic chamferFeatures = compDef.Features.ChamferFeatures;
                                    var chamferFeature = chamferFeatures.AddUsingDistance(edgeCollection, dist);
                                    try { TrackFeature(trackId, (string)chamferFeature.Name); } catch { }
                                }
                                catch (Exception ex2)
                                {
                                    throw new Exception(
                                        $"Chamfer failed (primary: {ex1.Message}, fallback: {ex2.Message})");
                                }
                            }
                        }
                    }
                    else if (op is PendingColorFaces colorOp)
                    {
                        ApplyColorToFaces(compDef, body, colorOp, failedOps);
                    }
                    else if (op is PendingShell shell)
                    {
                        ApplyShell(compDef, body, shell, trackId, failedOps);
                    }
                    else if (op is PendingDraft draft)
                    {
                        ApplyDraft(compDef, body, draft, trackId, failedOps);
                    }
                    else if (op is PendingHole hole)
                    {
                        ApplyHole(compDef, body, hole, trackId, failedOps);
                    }
                    else if (op is PendingThicken thicken)
                    {
                        failedOps.Add($"Thicken: not yet supported for committed bodies");
                    }
                    else
                    {
                        failedOps.Add($"{op.OperationName}: unrecognized operation type");
                    }
                }
                catch (Exception ex)
                {
                    failedOps.Add($"{op.OperationName}: {ex.Message}");
                    System.Diagnostics.Debug.WriteLine($"Error applying pending {op.GetType().Name}: {ex.Message}");
                }
            }
        }

        /// <summary>Records a feature name under a node ID for future deletion.</summary>
        private void TrackFeature(Guid nodeId, string featureName)
        {
            if (nodeId == default || string.IsNullOrEmpty(featureName)) return;
            if (!_committedFeatures.ContainsKey(nodeId))
                _committedFeatures[nodeId] = new List<string>();
            _committedFeatures[nodeId].Add(featureName);
        }

        /// <summary>
        /// Applies color to matched faces on a committed body.
        /// Uses the same Appearance asset approach as the ColoringTool add-in:
        /// finds/creates a colored Appearance asset, then sets face.Appearance.
        /// </summary>
        private void ApplyColorToFaces(
            Inventor.PartComponentDefinition compDef,
            Inventor.SurfaceBody body,
            PendingColorFaces colorOp,
            List<string> failedOps)
        {
            try
            {
                var doc = (Inventor.Document)compDef.Document;
                var appearance = GetOrCreateColorAppearance(doc,
                    colorOp.Red, colorOp.Green, colorOp.Blue);

                if (appearance == null)
                {
                    failedOps.Add("Color Faces: could not create appearance asset");
                    return;
                }

                int applied = 0;
                foreach (var snapshot in colorOp.FaceSnapshots)
                {
                    var matchedFace = snapshot.FindMatchingFace(body);
                    if (matchedFace != null)
                    {
                        try
                        {
                            ((dynamic)matchedFace).Appearance = appearance;
                            applied++;
                        }
                        catch { }
                    }
                }

                if (applied == 0)
                    failedOps.Add("Color Faces: no matching faces found on committed body");
            }
            catch (Exception ex)
            {
                failedOps.Add($"Color Faces: {ex.Message}");
            }
        }

        /// <summary>
        /// Applies a Shell feature to the committed body using Inventor's ShellFeature API.
        /// </summary>
        private void ApplyShell(
            Inventor.PartComponentDefinition compDef,
            Inventor.SurfaceBody body,
            PendingShell shell,
            Guid trackId,
            List<string> failedOps)
        {
            try
            {
                double thickness = shell.Thickness * Core.InventorContext.MM_TO_CM;

                // Collect open faces
                var faceCollection = InventorApp!.TransientObjects.CreateObjectCollection();
                foreach (var snapshot in shell.FaceSnapshots)
                {
                    var matchedFace = snapshot.FindMatchingFace(body);
                    if (matchedFace != null)
                        faceCollection.Add(matchedFace);
                }

                if (faceCollection.Count == 0)
                {
                    failedOps.Add("Shell: connect 'Open Faces' input — at least one face must be removed to hollow the body");
                    return;
                }

                // Use Type.InvokeMember for reliable COM marshaling
                dynamic features = compDef.Features;
                object shellFeaturesObj = (object)features.ShellFeatures;
                var sfType = shellFeaturesObj.GetType();

                // Inventor ShellFeatures.Add(FaceCollection, InsideThickness, [OutsideThickness])
                dynamic shellFeature;
                try
                {
                    shellFeature = sfType.InvokeMember(
                        "Add",
                        System.Reflection.BindingFlags.InvokeMethod,
                        null,
                        shellFeaturesObj,
                        new object[] { faceCollection, thickness });
                }
                catch
                {
                    // Try with outsideThickness = 0
                    shellFeature = sfType.InvokeMember(
                        "Add",
                        System.Reflection.BindingFlags.InvokeMethod,
                        null,
                        shellFeaturesObj,
                        new object[] { faceCollection, thickness, 0.0 });
                }
                try { TrackFeature(trackId, (string)shellFeature.Name); } catch { }
            }
            catch (Exception ex)
            {
                failedOps.Add($"Shell: {ex.Message}");
            }
        }

        /// <summary>
        /// Applies a Draft (taper) feature to faces of a committed body.
        /// </summary>
        private void ApplyDraft(
            Inventor.PartComponentDefinition compDef,
            Inventor.SurfaceBody body,
            PendingDraft draft,
            Guid trackId,
            List<string> failedOps)
        {
            try
            {
                var faceCollection = InventorApp!.TransientObjects.CreateObjectCollection();
                foreach (var snapshot in draft.FaceSnapshots)
                {
                    var matchedFace = snapshot.FindMatchingFace(body);
                    if (matchedFace != null)
                        faceCollection.Add(matchedFace);
                }

                if (faceCollection.Count == 0)
                {
                    failedOps.Add("Draft: no matching faces found on committed body");
                    return;
                }

                double angleRad = draft.AngleDegrees * Math.PI / 180.0;

                // Create pull direction vector
                var pullDir = InventorApp.TransientGeometry.CreateUnitVector(
                    draft.PullDirectionX, draft.PullDirectionY, draft.PullDirectionZ);

                // Find a planar face to use as the fixed plane (neutral plane)
                // Use the first planar face on the body, or a work plane
                Inventor.Face? fixedFace = null;
                foreach (Inventor.Face face in body.Faces)
                {
                    if (face.SurfaceType == Inventor.SurfaceTypeEnum.kPlaneSurface)
                    {
                        fixedFace = face;
                        break;
                    }
                }

                if (fixedFace == null)
                {
                    failedOps.Add("Draft: no planar face found for neutral plane");
                    return;
                }

                dynamic features = compDef.Features;
                dynamic draftFeature = features.FaceFeatures.AddTaperByAngle(
                    faceCollection, fixedFace, pullDir, angleRad);
                try { TrackFeature(trackId, (string)draftFeature.Name); } catch { }
            }
            catch (Exception ex)
            {
                failedOps.Add($"Draft: {ex.Message}");
            }
        }

        private void ApplyHole(
            Inventor.PartComponentDefinition compDef,
            Inventor.SurfaceBody body,
            PendingHole hole,
            Guid trackId,
            List<string> failedOps)
        {
            try
            {
                // Find target planar face for hole placement
                Inventor.Face? targetFace = null;
                foreach (var snapshot in hole.FaceSnapshots)
                {
                    var matched = snapshot.FindMatchingFace(body);
                    if (matched != null)
                    {
                        targetFace = matched as Inventor.Face;
                        break;
                    }
                }

                if (targetFace == null)
                {
                    // Fallback: find first planar face on the body
                    foreach (Inventor.Face face in body.Faces)
                    {
                        if (face.SurfaceType == Inventor.SurfaceTypeEnum.kPlaneSurface)
                        {
                            targetFace = face;
                            break;
                        }
                    }
                }

                if (targetFace == null)
                {
                    failedOps.Add("Hole: no planar face found for placement");
                    return;
                }

                double diamCm = hole.Diameter * Core.InventorContext.MM_TO_CM;
                double depthCm = hole.Depth * Core.InventorContext.MM_TO_CM;
                double cboreDiaCm = hole.CounterboreDiameter * Core.InventorContext.MM_TO_CM;
                double cboreDepthCm = hole.CounterboreDepth * Core.InventorContext.MM_TO_CM;
                double csinkDiaCm = hole.CountersinkDiameter * Core.InventorContext.MM_TO_CM;
                double csinkAngleRad = hole.CountersinkAngle * Math.PI / 180.0;
                bool throughAll = hole.Extent == "ThroughAll";

                // Create sketch point at face center for hole placement
                var sketch = compDef.Sketches.Add(targetFace);
                var centerPt = sketch.SketchPoints.Add(
                    InventorApp!.TransientGeometry.CreatePoint2d(0, 0));

                dynamic holeFeatures = compDef.Features.HoleFeatures;
                dynamic holeFeature;

                switch (hole.HoleType)
                {
                    case "Drilled":
                    default:
                        if (throughAll)
                            holeFeature = holeFeatures.AddDrilledByThroughAllExtent(
                                centerPt, diamCm / 2.0);
                        else
                            holeFeature = holeFeatures.AddDrilledByDistanceExtent(
                                centerPt, diamCm / 2.0, depthCm);
                        break;

                    case "Counterbore":
                        if (throughAll)
                            holeFeature = holeFeatures.AddCounterBoreByThroughAllExtent(
                                centerPt, diamCm / 2.0, cboreDiaCm / 2.0, cboreDepthCm);
                        else
                            holeFeature = holeFeatures.AddCounterBoreByDistanceExtent(
                                centerPt, diamCm / 2.0, depthCm, cboreDiaCm / 2.0, cboreDepthCm);
                        break;

                    case "Countersink":
                        if (throughAll)
                            holeFeature = holeFeatures.AddCounterSinkByThroughAllExtent(
                                centerPt, diamCm / 2.0, csinkDiaCm / 2.0, csinkAngleRad);
                        else
                            holeFeature = holeFeatures.AddCounterSinkByDistanceExtent(
                                centerPt, diamCm / 2.0, depthCm, csinkDiaCm / 2.0, csinkAngleRad);
                        break;

                    case "Tapped":
                    {
                        var threadInfo = CreateHoleThreadInfo(compDef, hole, failedOps);
                        if (threadInfo == null) return;
                        if (throughAll)
                            holeFeature = holeFeatures.AddTappedByThroughAllExtent(
                                centerPt, (object)threadInfo, hole.IsRightHand);
                        else
                            holeFeature = holeFeatures.AddTappedByDistanceExtent(
                                centerPt, (object)threadInfo, depthCm, hole.IsRightHand);
                        break;
                    }

                    case "CBore+Tapped":
                    {
                        var threadInfo = CreateHoleThreadInfo(compDef, hole, failedOps);
                        if (threadInfo == null) return;
                        if (throughAll)
                            holeFeature = holeFeatures.AddCBoreTappedByThroughAllExtent(
                                centerPt, (object)threadInfo, hole.IsRightHand,
                                cboreDiaCm / 2.0, cboreDepthCm);
                        else
                            holeFeature = holeFeatures.AddCBoreTappedByDistanceExtent(
                                centerPt, (object)threadInfo, depthCm, hole.IsRightHand,
                                cboreDiaCm / 2.0, cboreDepthCm);
                        break;
                    }

                    case "CSink+Tapped":
                    {
                        var threadInfo = CreateHoleThreadInfo(compDef, hole, failedOps);
                        if (threadInfo == null) return;
                        if (throughAll)
                            holeFeature = holeFeatures.AddCSinkTappedByThroughAllExtent(
                                centerPt, (object)threadInfo, hole.IsRightHand,
                                csinkDiaCm / 2.0, csinkAngleRad);
                        else
                            holeFeature = holeFeatures.AddCSinkTappedByDistanceExtent(
                                centerPt, (object)threadInfo, depthCm, hole.IsRightHand,
                                csinkDiaCm / 2.0, csinkAngleRad);
                        break;
                    }
                }

                try { TrackFeature(trackId, (string)holeFeature.Name); } catch { }
            }
            catch (Exception ex)
            {
                failedOps.Add($"Hole ({hole.HoleType}): {ex.Message}");
            }
        }

        /// <summary>
        /// Creates ThreadInfo for tapped hole types. Re-uses the same
        /// dynamic standard lookup as ApplyThread.
        /// </summary>
        private dynamic? CreateHoleThreadInfo(
            Inventor.PartComponentDefinition compDef,
            PendingHole hole,
            List<string> failedOps)
        {
            try
            {
                dynamic threadFeatures = compDef.Features.ThreadFeatures;
                string standardName = hole.ThreadStandard;

                // Query available standards and find a match
                try
                {
                    var stdTypes = (System.Collections.IEnumerable)threadFeatures.StandardThreadTypes;
                    string? matched = null;
                    foreach (var s in stdTypes)
                    {
                        string name = s.ToString()!;
                        if (string.Equals(name, standardName, StringComparison.OrdinalIgnoreCase))
                        { matched = name; break; }
                    }
                    if (matched == null)
                    {
                        foreach (var s in stdTypes)
                        {
                            string name = s.ToString()!;
                            if (name.IndexOf("Metric", StringComparison.OrdinalIgnoreCase) >= 0 &&
                                standardName.IndexOf("Metric", StringComparison.OrdinalIgnoreCase) >= 0)
                            { matched = name; break; }
                            if (name.IndexOf("Unified", StringComparison.OrdinalIgnoreCase) >= 0 &&
                                standardName.IndexOf("Unified", StringComparison.OrdinalIgnoreCase) >= 0)
                            { matched = name; break; }
                        }
                    }
                    if (matched != null) standardName = matched;
                }
                catch { /* Use as-is */ }

                // Use Type.InvokeMember for reliable COM marshaling
                object tfObj = (object)threadFeatures;
                var tfType = tfObj.GetType();

                // Build candidate designations
                var candidates = new List<string> { hole.ThreadDesignation };
                string spaced = hole.ThreadDesignation.Replace("x", " x ").Replace("X", " X ");
                if (spaced != hole.ThreadDesignation) candidates.Add(spaced);
                string unspaced = hole.ThreadDesignation.Replace(" x ", "x").Replace(" X ", "X");
                if (unspaced != hole.ThreadDesignation && !candidates.Contains(unspaced))
                    candidates.Add(unspaced);

                foreach (var desig in candidates)
                {
                    try
                    {
                        return tfType.InvokeMember(
                            "CreateStandardThreadInfo",
                            System.Reflection.BindingFlags.InvokeMethod,
                            null,
                            tfObj,
                            new object[] { true, standardName, desig });
                    }
                    catch { }
                }

                failedOps.Add($"Hole (Tapped): could not create thread info for " +
                              $"'{hole.ThreadDesignation}' in '{standardName}'");
                return null;
            }
            catch (Exception ex)
            {
                failedOps.Add($"Hole (Tapped): could not create thread info for " +
                              $"'{hole.ThreadDesignation}' ({ex.Message})");
                return null;
            }
        }

        /// <summary>
        /// Gets or creates an Inventor Appearance asset with the given RGB color.
        /// Same algorithm as ColoringTool add-in.
        /// </summary>
        private Inventor.Asset? GetOrCreateColorAppearance(
            Inventor.Document doc, int r, int g, int b)
        {
            try
            {
                string name = $"iNode_Color_{r}_{g}_{b}";

                // Check if we already created this appearance in the document
                Inventor.Assets? docAssets = null;
                if (doc is Inventor.PartDocument pd)
                    docAssets = pd.Assets;
                else if (doc is Inventor.AssemblyDocument ad)
                    docAssets = ad.Assets;
                if (docAssets == null) return null;

                // Look for existing
                foreach (Inventor.Asset asset in docAssets)
                {
                    try
                    {
                        if (asset.DisplayName == name) return asset;
                    }
                    catch { }
                }

                // Find a base appearance from asset libraries
                Inventor.Asset? baseAppearance = FindBaseAppearance();
                if (baseAppearance == null) return null;

                // Copy to document and set color
                var newAppearance = baseAppearance.CopyTo(doc);
                newAppearance.DisplayName = name;
                SetAppearanceColor(newAppearance, (byte)r, (byte)g, (byte)b);
                return newAppearance;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating appearance: {ex.Message}");
                return null;
            }
        }

        #endregion

        /// <summary>
        /// When nodes are deleted from the graph, suppress or delete their
        /// corresponding features from the Inventor feature tree.
        /// </summary>
        private void OnNodesRemoved(object? sender, List<Guid> removedNodeIds)
        {
            if (InventorApp == null) return;

            Inventor.PartDocument? partDoc = _targetDocument;
            if (partDoc == null)
            {
                try
                {
                    var doc = InventorApp.ActiveDocument;
                    if (doc != null && doc.DocumentType == Inventor.DocumentTypeEnum.kPartDocumentObject)
                        partDoc = (Inventor.PartDocument)doc;
                }
                catch { }
            }
            if (partDoc == null) return;

            var compDef = partDoc.ComponentDefinition;
            bool anyDeleted = false;

            foreach (var nodeId in removedNodeIds)
            {
                if (!_committedFeatures.TryGetValue(nodeId, out var featureNames))
                    continue;

                // Delete features in reverse order (child features like fillet/chamfer
                // must be deleted before their parent base feature)
                for (int i = featureNames.Count - 1; i >= 0; i--)
                {
                    try
                    {
                        // Find the feature by name
                        foreach (dynamic feature in compDef.Features)
                        {
                            try
                            {
                                if ((string)feature.Name == featureNames[i])
                                {
                                    feature.Delete();
                                    anyDeleted = true;
                                    break;
                                }
                            }
                            catch { continue; }
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine(
                            $"Error deleting feature '{featureNames[i]}': {ex.Message}");
                    }
                }

                _committedFeatures.Remove(nodeId);
            }

            if (anyDeleted)
            {
                try { InventorApp.ActiveView?.Update(); } catch { }
                _lblStatus.Text = "Feature(s) removed from feature tree";
            }
        }

        #region 3D Preview (Inventor ClientGraphics — Always-On)

        /// <summary>
        /// Shows transient preview geometry in the Inventor viewport using ClientGraphics.
        /// Runs automatically whenever the graph changes (debounced).
        /// Uses _targetDocument (from part selector) instead of ActiveDocument.
        /// </summary>
        private void ShowPreviewGraphics()
        {
            if (InventorApp == null) return;

            // Use target document from part selector; fallback to active
            Inventor.PartDocument? partDoc = _targetDocument;
            if (partDoc == null)
            {
                var doc = InventorApp.ActiveDocument;
                if (doc == null || doc.DocumentType != Inventor.DocumentTypeEnum.kPartDocumentObject) return;
                partDoc = (Inventor.PartDocument)doc;
            }

            var compDef = partDoc.ComponentDefinition;

            Inventor.ClientGraphics? cg = null;
            try { cg = compDef.ClientGraphicsCollection[PREVIEW_CLIENT_ID]; }
            catch { cg = compDef.ClientGraphicsCollection.Add(PREVIEW_CLIENT_ID); }

            // Only preview geometry from terminal nodes
            foreach (var bodyData in GetTerminalBodyOutputs())
            {
                try
                {
                    AddPreviewBody(cg, bodyData, partDoc);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Preview error '{bodyData.Description}': {ex.Message}");
                }
            }

            // Highlight edges and faces from SelectEdge / SelectFace nodes
            // that are NOT consumed by a fillet/chamfer (those get their own highlights)
            AddSelectionHighlights(cg, partDoc);

            InventorApp.ActiveView?.Update();
        }

        /// <summary>
        /// Scans the graph for nodes that output edge/face selections and highlights
        /// them in the 3D viewport. Handles SelectEdge, SelectFace, ListItem,
        /// FilterEdges, and FilterFaces nodes.
        /// </summary>
        private void AddSelectionHighlights(Inventor.ClientGraphics cg, Inventor.PartDocument partDoc)
        {
            foreach (var node in _canvas.Graph.Nodes)
            {
                if (node.HasError) continue;

                try
                {
                    // Gather edge selections from various node types
                    EdgeListData? edgeData = null;
                    FaceListData? faceData = null;

                    if (node is Nodes.SelectEdgeNode)
                    {
                        var v = node.GetOutput("Selected")?.Value;
                        if (v is EdgeListData el) edgeData = el;
                    }
                    else if (node is Nodes.SelectFaceNode)
                    {
                        var v = node.GetOutput("Selected")?.Value;
                        if (v is FaceListData fl) faceData = fl;
                    }
                    else if (node is Nodes.FilterEdgesNode)
                    {
                        var v = node.GetOutput("Edges")?.Value;
                        if (v is EdgeListData el) edgeData = el;
                    }
                    else if (node is Nodes.FilterFacesNode)
                    {
                        var v = node.GetOutput("Faces")?.Value;
                        if (v is FaceListData fl) faceData = fl;
                    }
                    else if (node is Nodes.ListItemNode)
                    {
                        var v = node.GetOutput("Result")?.Value;
                        if (v is EdgeListData el) edgeData = el;
                        else if (v is FaceListData fl) faceData = fl;
                    }

                    // Highlight edges: orange dashed look
                    if (edgeData != null && edgeData.Edges.Count > 0)
                    {
                        bool feedsIntoOp = IsOutputConnectedToOperationNode(node, "Selected")
                            || IsOutputConnectedToOperationNode(node, "Edges")
                            || IsOutputConnectedToOperationNode(node, "Result");

                        // Bright orange for standalone edges, dimmer if feeding an op
                        byte hr = feedsIntoOp ? (byte)180 : (byte)255;
                        byte hg = feedsIntoOp ? (byte)120 : (byte)165;
                        byte hb = 0;
                        AddEdgeHighlights(cg, edgeData.Edges, partDoc, hr, hg, hb);
                    }

                    // Highlight faces: semi-transparent blue
                    if (faceData != null && faceData.Faces.Count > 0)
                    {
                        AddFaceSelectionHighlights(cg, faceData.Faces, partDoc);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Selection highlight error: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Checks if a specific output port of a node feeds into
        /// a Fillet or Chamfer node's Edges input.
        /// </summary>
        private bool IsOutputConnectedToOperationNode(Node node, string outputPortName)
        {
            foreach (var conn in _canvas.Graph.Connections)
            {
                if (conn.SourceNode == node && conn.SourcePortName == outputPortName)
                {
                    if (conn.TargetNode is Nodes.FilletNode || conn.TargetNode is Nodes.ChamferNode)
                        return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Draws a semi-transparent blue overlay on selected faces.
        /// Uses SurfaceGraphics for the face geometry so the highlight
        /// covers the entire face area, not just its edges.
        /// </summary>
        private void AddFaceSelectionHighlights(
            Inventor.ClientGraphics cg, List<object> faces,
            Inventor.PartDocument partDoc)
        {
            // Draw each face with semi-transparent blue surface + bright edge outline
            foreach (var faceObj in faces)
            {
                try
                {
                    dynamic face = faceObj;

                    // Surface fill: semi-transparent blue
                    try
                    {
                        var fillNode = cg.AddNode(cg.Count + 1);
                        fillNode.AddSurfaceGraphics(faceObj);
                        SetHighlightAppearance(fillNode, partDoc, 80, 140, 255, 0.3);
                    }
                    catch { }

                    // Edge outline: brighter blue, opaque
                    var faceEdges = new List<object>();
                    foreach (var edge in face.Edges)
                        faceEdges.Add(edge);

                    if (faceEdges.Count > 0)
                        AddEdgeHighlights(cg, faceEdges, partDoc, 60, 120, 255);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Face highlight error: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Adds a single BodyData's transient SurfaceBody to the ClientGraphics
        /// for visual preview. The body already exists as a transient object
        /// from graph execution — we just display it. Also draws colored
        /// highlights on edges that have pending fillet/chamfer operations.
        /// </summary>
        private void AddPreviewBody(
            Inventor.ClientGraphics cg, BodyData bodyData,
            Inventor.PartDocument partDoc)
        {
            if (bodyData.Body == null) return;

            var body = (Inventor.SurfaceBody)bodyData.Body;
            var gNode = cg.AddNode(cg.Count + 1);
            gNode.AddSurfaceGraphics(body);

            // Determine preview color from the source node type
            string colorKey = bodyData.Description ?? "Unknown";
            try { SetPreviewColor(gNode, partDoc, colorKey); }
            catch { /* Color not supported — geometry shows default */ }

            // Draw edge highlights for pending operations (fillet, chamfer)
            if (bodyData.PendingOperations != null)
            {
                foreach (var op in bodyData.PendingOperations)
                {
                    try
                    {
                        List<object>? edges = null;
                        byte hr = 255, hg = 200, hb = 0; // Default: yellow

                        if (op is PendingFillet pf && pf.LiveEdges.Count > 0)
                        {
                            edges = pf.LiveEdges;
                            hr = 0; hg = 220; hb = 255; // Cyan for fillet
                        }
                        else if (op is PendingChamfer pc && pc.LiveEdges.Count > 0)
                        {
                            edges = pc.LiveEdges;
                            hr = 255; hg = 160; hb = 0; // Orange for chamfer
                        }

                        if (edges != null && edges.Count > 0)
                        {
                            AddEdgeHighlights(cg, edges, partDoc, hr, hg, hb);
                        }

                        // Face color preview: outline faces in the target color
                        if (op is PendingColorFaces pcf && pcf.LiveFaces.Count > 0)
                        {
                            AddFaceColorHighlights(cg, pcf.LiveFaces, partDoc,
                                (byte)pcf.Red, (byte)pcf.Green, (byte)pcf.Blue);
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Edge highlight error: {ex.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// Draws colored highlight outlines on faces to preview color operations.
        /// Renders each face's edge loops as thick colored curves.
        /// </summary>
        private void AddFaceColorHighlights(
            Inventor.ClientGraphics cg, List<object> faces,
            Inventor.PartDocument partDoc,
            byte r, byte g, byte b)
        {
            var highlightNode = cg.AddNode(cg.Count + 1);

            foreach (var faceObj in faces)
            {
                try
                {
                    dynamic face = faceObj;
                    foreach (Inventor.Edge edge in face.Edges)
                    {
                        try
                        {
                            object curveGeometry = edge.Geometry;
                            var curveGraphics = highlightNode.AddCurveGraphics(curveGeometry);
                            try { curveGraphics.LineWeight = 4.0; } catch { }
                        }
                        catch { }
                    }
                }
                catch { }
            }

            try { SetHighlightAppearance(highlightNode, partDoc, r, g, b); }
            catch { }
        }

        /// <summary>
        /// Draws colored highlight curves over edges using ClientGraphics.
        /// Uses CurveGraphics to render each edge's geometric curve as a thick
        /// colored line in the 3D viewport so the user can see which edges
        /// will be affected by fillet/chamfer operations.
        /// </summary>
        private void AddEdgeHighlights(
            Inventor.ClientGraphics cg, List<object> edges,
            Inventor.PartDocument partDoc,
            byte r, byte g, byte b)
        {
            var highlightNode = cg.AddNode(cg.Count + 1);

            foreach (var edgeObj in edges)
            {
                try
                {
                    dynamic edge = edgeObj;

                    // Try adding the edge's curve geometry directly
                    try
                    {
                        object curveGeometry = edge.Geometry;
                        var curveGraphics = highlightNode.AddCurveGraphics(curveGeometry);
                        try { curveGraphics.LineWeight = 4.0; } catch { }
                    }
                    catch
                    {
                        // Fallback: draw a line from start to end point
                        try
                        {
                            var startPt = edge.StartVertex.Point;
                            var stopPt = edge.StopVertex.Point;
                            var line = InventorApp!.TransientGeometry.CreateLineSegment(startPt, stopPt);
                            var curveGraphics = highlightNode.AddCurveGraphics(line);
                            try { curveGraphics.LineWeight = 4.0; } catch { }
                        }
                        catch { /* Skip this edge entirely */ }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Edge highlight failed: {ex.Message}");
                }
            }

            // Set color via appearance on the highlight node
            try
            {
                SetHighlightAppearance(highlightNode, partDoc, r, g, b);
            }
            catch { /* Color setting failed — will show default */ }
        }

        /// <summary>
        /// Sets a solid color appearance on a graphics node used for edge highlights.
        /// </summary>
        private void SetHighlightAppearance(
            Inventor.GraphicsNode gNode, Inventor.PartDocument partDoc,
            byte r, byte g, byte b, double opacity = 1.0)
        {
            // Include opacity in the asset name so we cache separate assets
            int opPct = (int)(opacity * 100);
            string assetName = opacity < 1.0
                ? $"iNode_HL_{r}_{g}_{b}_O{opPct}"
                : $"iNode_HL_{r}_{g}_{b}";
            Inventor.Asset? asset = null;

            try
            {
                foreach (Inventor.Asset a in partDoc.AppearanceAssets)
                {
                    if (a.DisplayName == assetName) { asset = a; break; }
                }
            }
            catch { }

            if (asset == null)
            {
                var baseAppearance = FindBaseAppearance();
                if (baseAppearance != null)
                {
                    try
                    {
                        asset = baseAppearance.CopyTo(partDoc);
                        asset.DisplayName = assetName;
                    }
                    catch { }
                }
            }

            if (asset != null)
            {
                try
                {
                    var invColor = InventorApp!.TransientObjects.CreateColor(r, g, b);
                    invColor.Opacity = opacity;

                    for (int i = 1; i <= asset.Count; i++)
                    {
                        try
                        {
                            Inventor.AssetValue assetValue = asset[i];
                            string valueName = assetValue.Name.ToLower();

                            // Set transparency on the asset if opacity < 1
                            if (opacity < 1.0 && valueName.Contains("generic_transparency"))
                            {
                                if (assetValue is Inventor.FloatAssetValue fav)
                                {
                                    fav.Value = (float)(1.0 - opacity); // transparency = inverse of opacity
                                }
                            }

                            if ((valueName.Contains("generic_diffuse") ||
                                 valueName.Contains("diffuse_color") ||
                                 valueName.Contains("diffuse")) &&
                                !valueName.Contains("map") &&
                                !valueName.Contains("texture"))
                            {
                                if (assetValue is Inventor.ColorAssetValue colorValue)
                                {
                                    colorValue.Value = invColor;
                                }
                            }
                        }
                        catch { continue; }
                    }
                }
                catch { }

                gNode.Appearance = asset;
            }
        }

        /// <summary>
        /// Colors a ClientGraphics node using the same approach as the ColoringTool add-in:
        /// find a "generic opaque" base appearance, copy it into the document, and set
        /// the diffuse color via iterating AssetValue entries for ColorAssetValue.
        /// </summary>
        private void SetPreviewColor(Inventor.GraphicsNode gNode, Inventor.PartDocument partDoc, string description)
        {
            // Default neutral color for all preview bodies — lets selection
            // highlights stand out clearly (blue face, orange edge, etc.)
            byte cr = 180, cg = 185, cb = 190;

            string assetName = "iNode_" + cr + "_" + cg + "_" + cb;
            Inventor.Asset? asset = null;

            // Check if we already created this asset in the document
            try
            {
                foreach (Inventor.Asset a in partDoc.AppearanceAssets)
                {
                    if (a.DisplayName == assetName) { asset = a; break; }
                }
            }
            catch { }

            // If not found, find a proper base appearance and copy it
            if (asset == null)
            {
                var baseAppearance = FindBaseAppearance();
                if (baseAppearance != null)
                {
                    try
                    {
                        asset = baseAppearance.CopyTo(partDoc);
                        asset.DisplayName = assetName;
                    }
                    catch { }
                }
            }

            // Set the diffuse color on the asset
            if (asset != null)
            {
                try
                {
                    SetAppearanceColor(asset, cr, cg, cb);
                }
                catch { /* Color modification not supported */ }

                gNode.Appearance = asset;
            }
        }

        /// <summary>
        /// Finds a suitable base appearance asset from Inventor's asset libraries.
        /// Prioritizes "generic" + "opaque" appearances, falling back to other common types.
        /// Mirrors the approach used in the ColoringTool add-in.
        /// </summary>
        private Inventor.Asset? FindBaseAppearance()
        {
            if (InventorApp == null) return null;

            try
            {
                // First pass: look for "generic" and "opaque" in display name
                foreach (Inventor.AssetLibrary lib in InventorApp.AssetLibraries)
                {
                    try
                    {
                        string libName = lib.DisplayName.ToLower();
                        foreach (Inventor.Asset appearance in lib.AppearanceAssets)
                        {
                            try
                            {
                                string name = appearance.DisplayName.ToLower();
                                if (name.Contains("generic") && name.Contains("opaque"))
                                    return appearance;
                            }
                            catch { continue; }
                        }
                    }
                    catch { continue; }
                }

                // Second pass: look for "plastic", "generic", or "default"
                foreach (Inventor.AssetLibrary lib in InventorApp.AssetLibraries)
                {
                    try
                    {
                        foreach (Inventor.Asset appearance in lib.AppearanceAssets)
                        {
                            try
                            {
                                string name = appearance.DisplayName.ToLower();
                                if (name.Contains("plastic") || name.Contains("generic") || name.Contains("default"))
                                    return appearance;
                            }
                            catch { continue; }
                        }
                    }
                    catch { continue; }
                }

                // Fallback: grab the first available appearance from any library
                foreach (Inventor.AssetLibrary lib in InventorApp.AssetLibraries)
                {
                    try
                    {
                        if (lib.AppearanceAssets.Count > 0)
                            return lib.AppearanceAssets[1]; // 1-based index
                    }
                    catch { continue; }
                }
            }
            catch { }

            return null;
        }

        /// <summary>
        /// Sets the diffuse color on an appearance asset by iterating its values
        /// and finding the ColorAssetValue with "diffuse" in its name.
        /// This mirrors the proven ColoringTool approach and avoids using dynamic/indexer
        /// which can pick up wrong properties or texture maps.
        /// </summary>
        private void SetAppearanceColor(Inventor.Asset appearance, byte r, byte g, byte b)
        {
            var invColor = InventorApp!.TransientObjects.CreateColor(r, g, b);
            invColor.Opacity = 0.7;

            for (int i = 1; i <= appearance.Count; i++)
            {
                try
                {
                    Inventor.AssetValue assetValue = appearance[i];
                    string valueName = assetValue.Name.ToLower();

                    // Look for diffuse color properties, skip texture/map entries
                    if ((valueName.Contains("generic_diffuse") ||
                         valueName.Contains("diffuse_color") ||
                         valueName.Contains("diffuse")) &&
                        !valueName.Contains("map") &&
                        !valueName.Contains("texture"))
                    {
                        if (assetValue is Inventor.ColorAssetValue colorValue)
                        {
                            colorValue.Value = invColor;
                            return;
                        }
                    }
                }
                catch { continue; }
            }
        }

        private void ClearPreviewGraphics()
        {
            if (InventorApp == null) return;
            try
            {
                // Clear from target document
                Inventor.PartDocument? partDoc = _targetDocument;
                if (partDoc == null)
                {
                    var doc = InventorApp.ActiveDocument;
                    if (doc == null || doc.DocumentType != Inventor.DocumentTypeEnum.kPartDocumentObject) return;
                    partDoc = (Inventor.PartDocument)doc;
                }
                try
                {
                    partDoc.ComponentDefinition.ClientGraphicsCollection[PREVIEW_CLIENT_ID].Delete();
                    InventorApp.ActiveView?.Update();
                }
                catch { /* nothing to clear */ }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error clearing preview: {ex.Message}");
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (_isDirty && _canvas.Graph.Nodes.Count > 0)
            {
                var result = MessageBox.Show(
                    this,
                    "You have unsaved changes. Do you want to save before closing?",
                    "iNode – Unsaved Changes",
                    MessageBoxButtons.YesNoCancel,
                    MessageBoxIcon.Warning);

                if (result == DialogResult.Yes)
                {
                    OnSaveWorkflow(this, EventArgs.Empty);
                    // If user cancelled the save dialog, stay open
                    if (_isDirty)
                    {
                        e.Cancel = true;
                        return;
                    }
                }
                else if (result == DialogResult.Cancel)
                {
                    e.Cancel = true;
                    return;
                }
            }

            // Clean up debounce timer
            if (_previewDebounceTimer != null)
            {
                _previewDebounceTimer.Stop();
                _previewDebounceTimer.Dispose();
                _previewDebounceTimer = null;
            }

            RemoveKeyboardHook();
            ClearPreviewGraphics();
            base.OnFormClosing(e);
        }

        #endregion

        #region Undo / Redo

        /// <summary>Lightweight snapshot of a graph state for undo/redo.</summary>
        private class GraphSnapshot
        {
            public System.Collections.Generic.List<(string TypeName, float X, float Y, System.Collections.Generic.Dictionary<string, object?> Params)> Nodes = new();
            public System.Collections.Generic.List<(int SrcIdx, string SrcPort, int TgtIdx, string TgtPort)> Connections = new();
        }

        private GraphSnapshot CaptureSnapshot()
        {
            var snap = new GraphSnapshot();
            var nodeIndexMap = new System.Collections.Generic.Dictionary<Node, int>();

            for (int i = 0; i < _canvas.Graph.Nodes.Count; i++)
            {
                var node = _canvas.Graph.Nodes[i];
                snap.Nodes.Add((node.TypeName, node.Position.X, node.Position.Y, node.GetParameters()));
                nodeIndexMap[node] = i;
            }
            foreach (var conn in _canvas.Graph.Connections)
            {
                if (nodeIndexMap.TryGetValue(conn.SourceNode, out int srcIdx) &&
                    nodeIndexMap.TryGetValue(conn.TargetNode, out int tgtIdx))
                {
                    snap.Connections.Add((srcIdx, conn.SourcePortName, tgtIdx, conn.TargetPortName));
                }
            }
            return snap;
        }

        private void RestoreSnapshot(GraphSnapshot snap)
        {
            _suppressUndo = true;
            _canvas.Graph.Nodes.Clear();
            _canvas.Graph.Connections.Clear();

            foreach (var (typeName, x, y, parms) in snap.Nodes)
            {
                var node = NodeFactory.Create(typeName);
                if (node == null) continue;
                node.Position = new System.Drawing.PointF(x, y);
                if (parms != null && parms.Count > 0)
                    node.SetParameters(parms);
                _canvas.Graph.Nodes.Add(node);
            }
            foreach (var (srcIdx, srcPort, tgtIdx, tgtPort) in snap.Connections)
            {
                if (srcIdx < _canvas.Graph.Nodes.Count && tgtIdx < _canvas.Graph.Nodes.Count)
                {
                    _canvas.Graph.Connect(_canvas.Graph.Nodes[srcIdx], srcPort, _canvas.Graph.Nodes[tgtIdx], tgtPort);
                }
            }

            _suppressUndo = false;
            _canvas.Invalidate();
            UpdateStatusBar();
        }

        private void SaveUndoState()
        {
            if (_suppressUndo) return;

            // Remove redo history
            if (_undoIndex < _undoStack.Count - 1)
                _undoStack.RemoveRange(_undoIndex + 1, _undoStack.Count - _undoIndex - 1);

            _undoStack.Add(CaptureSnapshot());
            if (_undoStack.Count > 50)
            {
                _undoStack.RemoveAt(0);
            }
            _undoIndex = _undoStack.Count - 1;
        }

        private void Undo()
        {
            if (_undoIndex <= 0) return;
            _undoIndex--;
            RestoreSnapshot(_undoStack[_undoIndex]);
            _lblStatus.Text = "Undo";
        }

        private void Redo()
        {
            if (_undoIndex >= _undoStack.Count - 1) return;
            _undoIndex++;
            RestoreSnapshot(_undoStack[_undoIndex]);
            _lblStatus.Text = "Redo";
        }

        #endregion

        #region Keyboard

        /// <summary>
        /// Win32 thread-level keyboard hook callback. Intercepts WM_KEYDOWN
        /// BEFORE Inventor's accelerator table can consume them.
        /// WH_KEYBOARD fires for the thread's message queue, so it works
        /// inside Inventor's native COM message loop (unlike IMessageFilter).
        /// </summary>
        private IntPtr KeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= HC_ACTION && ContainsFocus)
            {
                // lParam bit 31 = transition state (0 = key down)
                bool isKeyDown = ((long)lParam & 0x80000000) == 0;
                if (isKeyDown)
                {
                    Keys vk = (Keys)wParam;
                    Keys modifiers = Control.ModifierKeys;
                    Keys keyData = vk | modifiers;

                    if (HandleShortcut(keyData))
                    {
                        // Return non-zero to swallow the key
                        return (IntPtr)1;
                    }
                }
            }

            return CallNextHookEx(_keyboardHook, nCode, wParam, lParam);
        }

        /// <summary>
        /// Centralized shortcut handling — called from both the hook and ProcessCmdKey.
        /// Returns true if the key was handled.
        /// </summary>
        private bool HandleShortcut(Keys keyData)
        {
            // If focus is on a text box (e.g. in partition selector combo), don't intercept
            if (ActiveControl is TextBox || ActiveControl is ComboBox)
                return false;

            switch (keyData)
            {
                case Keys.Delete:
                    _canvas.DeleteSelected();
                    _canvas.Focus();
                    return true;

                case Keys.Control | Keys.Z:
                    Undo();
                    return true;

                case Keys.Control | Keys.Y:
                    Redo();
                    return true;

                case Keys.Control | Keys.C:
                    _canvas.CopySelected();
                    return true;

                case Keys.Control | Keys.V:
                    _canvas.PasteNodes();
                    return true;

                case Keys.Control | Keys.A:
                    _canvas.Graph.SelectAll();
                    _canvas.Invalidate();
                    return true;

                case Keys.Escape:
                    Close();
                    return true;

                case Keys.F:
                    _canvas.FrameAll();
                    return true;
            }

            return false;
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (HandleShortcut(keyData))
                return true;

            return base.ProcessCmdKey(ref msg, keyData);
        }

        #endregion
    }

    /// <summary>
    /// Helper class for document dropdown items.
    /// </summary>
    internal class DocumentItem
    {
        public string DisplayName { get; }
        public string FullPath { get; }
        public string DocType { get; }

        public DocumentItem(string displayName, string fullPath, string docType)
        {
            DisplayName = displayName;
            FullPath = fullPath;
            DocType = docType;
        }

        public override string ToString()
        {
            if (string.IsNullOrEmpty(DocType))
                return DisplayName;
            return $"{DisplayName}  [{DocType}]";
        }
    }
}
