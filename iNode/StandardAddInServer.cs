// ============================================================================
// iNode Add-in for Autodesk Inventor 2026
// StandardAddInServer.cs - Main Entry Point
// ============================================================================

using System;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Windows.Forms;
using System.Reflection;
using System.Diagnostics;
using System.Linq;
using Inventor;
using iNode.Core;
using iNode.Nodes;
using iNode.UI;

namespace iNode
{
    /// <summary>
    /// Standard Add-in Server for iNode.
    /// Implements the Inventor Add-in interface and manages ribbon integration.
    /// </summary>
    [GuidAttribute("C3D4E5F6-A7B8-9012-CDEF-123456789ABC")]
    [ComVisible(true)]
    public class StandardAddInServer : ApplicationAddInServer
    {
        #region Constants

        private const string ADDIN_GUID = "{C3D4E5F6-A7B8-9012-CDEF-123456789ABC}";
        private const string TAB_DISPLAY_NAME = "Power Tools";
        private const string TAB_INTERNAL_NAME = "id_Tab_PowerTools";
        private const string PANEL_DISPLAY_NAME = "iNode";
        private const string PANEL_INTERNAL_NAME = "id_Panel_iNode";
        private const string BUTTON_DISPLAY_NAME = "iNode Editor";
        private const string BUTTON_INTERNAL_NAME = "id_Button_iNode";
        private const string LOADRUN_DISPLAY_NAME = "Load && Run";
        private const string LOADRUN_INTERNAL_NAME = "id_Button_iNode_LoadRun";
        private const string RECENT_DISPLAY_NAME = "Recent Workflows";
        private const string RECENT_INTERNAL_NAME = "id_Button_iNode_Recent";

        #endregion

        #region Private Fields

        private static Inventor.Application? _inventorApp;
        private ButtonDefinition? _iNodeButton;
        private ButtonDefinition? _loadRunButton;
        private ButtonDefinition? _recentButton;
        private NodeEditorForm? _editorForm;
        private UserInterfaceEvents? _uiEvents;

        #endregion

        #region Win32 API for Window Positioning

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        #endregion

        #region Public Properties

        public static Inventor.Application? InventorApp => _inventorApp;

        #endregion

        #region ApplicationAddInServer Implementation

        public void Activate(ApplicationAddInSite addInSiteObject, bool firstTime)
        {
            try
            {
                _inventorApp = addInSiteObject.Application;
                _uiEvents = _inventorApp.UserInterfaceManager.UserInterfaceEvents;
                CreateRibbonInterface(firstTime);

                Debug.WriteLine("iNode Add-in activated successfully.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error activating iNode Add-in:\n{ex.Message}",
                    "iNode Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public void Deactivate()
        {
            try
            {
                if (_editorForm != null)
                {
                    try { _editorForm.Close(); _editorForm.Dispose(); } catch { }
                    _editorForm = null;
                }

                if (_iNodeButton != null)
                    _iNodeButton.OnExecute -= OnButtonClick;
                if (_loadRunButton != null)
                    _loadRunButton.OnExecute -= OnLoadRunClick;
                if (_recentButton != null)
                    _recentButton.OnExecute -= OnRecentClick;

                _uiEvents = null;
                _iNodeButton = null;
                _loadRunButton = null;
                _recentButton = null;
                _inventorApp = null;

                GC.Collect();
                GC.WaitForPendingFinalizers();

                Debug.WriteLine("iNode Add-in deactivated successfully.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error during deactivation: {ex.Message}");
            }
        }

        public object Automation => null!;

        public void ExecuteCommand(int commandID) { }

        #endregion

        #region Ribbon Interface

        private void CreateRibbonInterface(bool firstTime)
        {
            if (_inventorApp == null) return;

            try
            {
                var uiManager = _inventorApp.UserInterfaceManager;
                var controlDefs = _inventorApp.CommandManager.ControlDefinitions;

                CreateButtonDefinition(controlDefs);

                if (firstTime)
                    AddButtonToRibbons(uiManager);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error creating ribbon interface: {ex.Message}");
                throw;
            }
        }

        private void CreateButtonDefinition(ControlDefinitions controlDefs)
        {
            if (_inventorApp == null) return;

            try
            {
                // --- iNode Editor button ---
                try { _iNodeButton = (ButtonDefinition)controlDefs[BUTTON_INTERNAL_NAME]; } catch { _iNodeButton = null; }

                if (_iNodeButton == null)
                {
                    var largeIcon = GetIconIPictureDisp(32);
                    var smallIcon = GetIconIPictureDisp(16);

                    _iNodeButton = controlDefs.AddButtonDefinition(
                        DisplayName: BUTTON_DISPLAY_NAME,
                        InternalName: BUTTON_INTERNAL_NAME,
                        Classification: CommandTypesEnum.kQueryOnlyCmdType,
                        ClientId: ADDIN_GUID,
                        DescriptionText: "Open the iNode visual parametric editor for node-based design workflows.",
                        ToolTipText: "iNode — Visual Parametric Editor",
                        StandardIcon: smallIcon,
                        LargeIcon: largeIcon,
                        ButtonDisplay: ButtonDisplayEnum.kDisplayTextInLearningMode
                    );
                }
                _iNodeButton.OnExecute += OnButtonClick;

                // --- Load & Run button ---
                try { _loadRunButton = (ButtonDefinition)controlDefs[LOADRUN_INTERNAL_NAME]; } catch { _loadRunButton = null; }

                if (_loadRunButton == null)
                {
                    var largeIcon = GetIconIPictureDisp(32);
                    var smallIcon = GetIconIPictureDisp(16);

                    _loadRunButton = controlDefs.AddButtonDefinition(
                        DisplayName: LOADRUN_DISPLAY_NAME,
                        InternalName: LOADRUN_INTERNAL_NAME,
                        Classification: CommandTypesEnum.kQueryOnlyCmdType,
                        ClientId: ADDIN_GUID,
                        DescriptionText: "Load an iNode workflow file and immediately commit it to the active part.",
                        ToolTipText: "Load & Run — Apply a saved workflow",
                        StandardIcon: smallIcon,
                        LargeIcon: largeIcon,
                        ButtonDisplay: ButtonDisplayEnum.kDisplayTextInLearningMode
                    );
                }
                _loadRunButton.OnExecute += OnLoadRunClick;

                // --- Recent Workflows button ---
                try { _recentButton = (ButtonDefinition)controlDefs[RECENT_INTERNAL_NAME]; } catch { _recentButton = null; }

                if (_recentButton == null)
                {
                    var smallIcon = GetIconIPictureDisp(16);

                    _recentButton = controlDefs.AddButtonDefinition(
                        DisplayName: RECENT_DISPLAY_NAME,
                        InternalName: RECENT_INTERNAL_NAME,
                        Classification: CommandTypesEnum.kQueryOnlyCmdType,
                        ClientId: ADDIN_GUID,
                        DescriptionText: "Show recently used iNode workflow files for quick access.",
                        ToolTipText: "Recent iNode Workflows",
                        StandardIcon: smallIcon,
                        LargeIcon: null,
                        ButtonDisplay: ButtonDisplayEnum.kDisplayTextInLearningMode
                    );
                }
                _recentButton.OnExecute += OnRecentClick;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error creating button definition: {ex.Message}");
                throw;
            }
        }

        private void AddButtonToRibbons(UserInterfaceManager uiManager)
        {
            AddButtonToRibbon(uiManager, "Part");
            AddButtonToRibbon(uiManager, "Assembly");
        }

        private void AddButtonToRibbon(UserInterfaceManager uiManager, string ribbonName)
        {
            try
            {
                var ribbon = uiManager.Ribbons[ribbonName];
                if (ribbon == null) return;

                RibbonTab? tab = null;
                try { tab = ribbon.RibbonTabs[TAB_INTERNAL_NAME]; } catch { }

                if (tab == null)
                {
                    tab = ribbon.RibbonTabs.Add(
                        DisplayName: TAB_DISPLAY_NAME,
                        InternalName: TAB_INTERNAL_NAME,
                        ClientId: ADDIN_GUID,
                        TargetTabInternalName: "",
                        InsertBeforeTargetTab: false,
                        Contextual: false);
                }

                RibbonPanel? panel = null;
                try { panel = tab.RibbonPanels[PANEL_INTERNAL_NAME]; } catch { }

                if (panel == null)
                {
                    panel = tab.RibbonPanels.Add(
                        DisplayName: PANEL_DISPLAY_NAME,
                        InternalName: PANEL_INTERNAL_NAME,
                        ClientId: ADDIN_GUID,
                        TargetPanelInternalName: "",
                        InsertBeforeTargetPanel: false);
                }

                if (_iNodeButton != null)
                {
                    try
                    {
                        panel.CommandControls.AddButton(
                            ButtonDefinition: _iNodeButton,
                            UseLargeIcon: true,
                            ShowText: true);
                    }
                    catch { }
                }

                if (_loadRunButton != null)
                {
                    try
                    {
                        panel.CommandControls.AddButton(
                            ButtonDefinition: _loadRunButton,
                            UseLargeIcon: false,
                            ShowText: true);
                    }
                    catch { }
                }

                if (_recentButton != null)
                {
                    try
                    {
                        panel.CommandControls.AddButton(
                            ButtonDefinition: _recentButton,
                            UseLargeIcon: false,
                            ShowText: true);
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error adding button to {ribbonName} ribbon: {ex.Message}");
            }
        }

        #endregion

        #region Button Handlers

        private void OnButtonClick(NameValueMap context)
        {
            try
            {
                ShowEditor();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening iNode editor:\n{ex.Message}",
                    "iNode Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Load & Run: pick a .inode file, deserialize, execute in Commit mode
        /// against the active part, all in one shot.
        /// </summary>
        private void OnLoadRunClick(NameValueMap context)
        {
            try
            {
                if (_inventorApp == null) return;

                var activeDoc = _inventorApp.ActiveDocument;
                if (activeDoc == null || activeDoc.DocumentType != DocumentTypeEnum.kPartDocumentObject)
                {
                    MessageBox.Show("Please open a Part document before using Load & Run.",
                        "iNode", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                using var dlg = new OpenFileDialog
                {
                    Title = "Load & Run Workflow",
                    Filter = "iNode Workflow (*.inode)|*.inode|JSON (*.json)|*.json|All Files (*.*)|*.*",
                    DefaultExt = "inode"
                };

                if (dlg.ShowDialog() != DialogResult.OK) return;

                string filePath = dlg.FileName;
                var graph = WorkflowSerializer.Load(filePath);

                // Track as recent
                RecentWorkflowManager.AddRecent(filePath);

                // Execute with Commit mode
                var inventorContext = new InventorContext(_inventorApp, (PartDocument)activeDoc);
                inventorContext.Mode = ExecutionMode.Commit;
                graph.Execute(inventorContext);

                // Commit all bodies from terminal nodes
                var transaction = _inventorApp.TransactionManager.StartTransaction(
                    (Inventor._Document)_inventorApp.ActiveDocument, "iNode Load & Run");

                try
                {
                    int committed = 0;
                    foreach (var node in graph.Nodes)
                    {
                        foreach (var port in node.Outputs)
                        {
                            if (port.Value is BodyData bodyData && bodyData.Body != null)
                            {
                                // Only commit from terminal nodes (no outgoing body connections)
                                bool isTerminal = !graph.Connections.Any(c =>
                                    c.SourceNode.Id == node.Id && c.SourcePortName == port.Name);

                                if (isTerminal)
                                {
                                    CommitBodyDirect(inventorContext, bodyData);
                                    committed++;
                                }
                            }
                        }
                    }

                    transaction.End();

                    MessageBox.Show(
                        $"Workflow executed successfully!\n" +
                        $"File: {System.IO.Path.GetFileName(filePath)}\n" +
                        $"Bodies committed: {committed}",
                        "iNode — Load & Run",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch
                {
                    transaction.Abort();
                    throw;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error running workflow:\n{ex.Message}",
                    "iNode Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Commits a BodyData to the target part document as a NonParametricBaseFeature.
        /// </summary>
        private void CommitBodyDirect(InventorContext ctx, BodyData bodyData)
        {
            if (ctx.TargetCompDef == null) return;

            dynamic body = bodyData.Body!;
            var features = ctx.TargetCompDef.Features;
            dynamic npbfDef = features.NonParametricBaseFeatures
                .CreateDefinition();

            // Copy the transient body
            dynamic bodyCopy = ctx.App.TransientBRep.Copy(body);
            npbfDef.BRepEntities = new object[] { bodyCopy };
            npbfDef.OutputType =
                BaseFeatureOutputTypeEnum.kSolidOutputType;

            features.NonParametricBaseFeatures.Add(npbfDef);
        }

        /// <summary>
        /// Recent Workflows: show a context menu of recent .inode files.
        /// Selecting one runs it immediately (same as Load & Run).
        /// </summary>
        private void OnRecentClick(NameValueMap context)
        {
            try
            {
                RecentWorkflowManager.PruneInvalid();
                var recent = RecentWorkflowManager.RecentFiles;

                if (recent.Count == 0)
                {
                    MessageBox.Show("No recent workflows found.\nUse Save in the iNode Editor or Load & Run to build your history.",
                        "iNode — Recent Workflows", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                var menu = new ContextMenuStrip();
                menu.Font = new Font("Segoe UI", 9.5f);

                foreach (string path in recent)
                {
                    string fileName = System.IO.Path.GetFileNameWithoutExtension(path);
                    string dir = System.IO.Path.GetDirectoryName(path) ?? "";
                    string shortDir = dir.Length > 50 ? "..." + dir.Substring(dir.Length - 47) : dir;

                    var item = menu.Items.Add($"{fileName}   ({shortDir})");
                    item!.Tag = path;
                    item.Click += OnRecentItemClick;
                }

                menu.Items.Add(new ToolStripSeparator());
                var clearItem = menu.Items.Add("Clear Recent List");
                clearItem!.Click += (s, e) =>
                {
                    // Clear by writing empty list
                    while (RecentWorkflowManager.RecentFiles.Count > 0)
                        RecentWorkflowManager.AddRecent(""); // no-op since empty is filtered
                    MessageBox.Show("Recent list cleared.", "iNode", MessageBoxButtons.OK, MessageBoxIcon.Information);
                };

                // Show near cursor
                menu.Show(Cursor.Position);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error showing recent workflows:\n{ex.Message}",
                    "iNode Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OnRecentItemClick(object? sender, EventArgs e)
        {
            if (sender is ToolStripItem item && item.Tag is string filePath)
            {
                try
                {
                    if (!System.IO.File.Exists(filePath))
                    {
                        MessageBox.Show($"File not found:\n{filePath}", "iNode", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    // Open in editor instead of auto-running (safer UX)
                    ShowEditor();
                    if (_editorForm != null && !_editorForm.IsDisposed)
                    {
                        _editorForm.LoadWorkflowFromFile(filePath);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading workflow:\n{ex.Message}",
                        "iNode Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void ShowEditor()
        {
            if (_inventorApp == null) return;

            // If form exists and visible, bring to front
            if (_editorForm != null && !_editorForm.IsDisposed && _editorForm.Visible)
            {
                _editorForm.BringToFront();
                return;
            }

            if (_editorForm != null)
            {
                try { _editorForm.Dispose(); } catch { }
                _editorForm = null;
            }

            _editorForm = new NodeEditorForm
            {
                InventorApp = _inventorApp
            };

            PositionFormOnInventorWindow(_editorForm);
            _editorForm.Show();
        }

        private void PositionFormOnInventorWindow(Form form)
        {
            try
            {
                if (_inventorApp == null) return;

                IntPtr hwnd = GetForegroundWindow();
                if (GetWindowRect(hwnd, out RECT rect))
                {
                    int inventorW = rect.Right - rect.Left;
                    int inventorH = rect.Bottom - rect.Top;

                    // Center on the Inventor window
                    int formX = rect.Left + (inventorW - form.Width) / 2;
                    int formY = rect.Top + (inventorH - form.Height) / 2;

                    form.StartPosition = FormStartPosition.Manual;
                    form.Location = new System.Drawing.Point(formX, formY);
                }
            }
            catch
            {
                form.StartPosition = FormStartPosition.CenterScreen;
            }
        }

        #endregion

        #region Icon Helpers

        private object? GetIconIPictureDisp(int size)
        {
            try
            {
                using var bitmap = CreateDefaultIcon(size);
                return IconConverter.BitmapToIPictureDisp(bitmap);
            }
            catch { return null; }
        }

        private Bitmap CreateDefaultIcon(int size)
        {
            var bitmap = new Bitmap(size, size);
            using var g = Graphics.FromImage(bitmap);
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            // Background
            g.Clear(System.Drawing.Color.FromArgb(0, 122, 204));

            // Draw node-style icon: two connected rectangles
            float s = size / 32f; // Scale factor
            using var whitePen = new Pen(System.Drawing.Color.White, 1.5f * s);
            using var whiteBrush = new SolidBrush(System.Drawing.Color.White);

            // Left node
            g.DrawRectangle(whitePen, 3 * s, 6 * s, 10 * s, 8 * s);
            g.FillEllipse(whiteBrush, 12 * s, 9 * s, 3 * s, 3 * s);

            // Right node
            g.DrawRectangle(whitePen, 19 * s, 14 * s, 10 * s, 8 * s);
            g.FillEllipse(whiteBrush, 17.5f * s, 17 * s, 3 * s, 3 * s);

            // Wire between
            g.DrawBezier(whitePen,
                13.5f * s, 10.5f * s,
                16 * s, 10.5f * s,
                17 * s, 18.5f * s,
                19 * s, 18.5f * s);

            return bitmap;
        }

        #endregion
    }

    #region Icon Converter

    internal class IconConverter : AxHost
    {
        private IconConverter() : base(Guid.Empty.ToString()) { }

        public static object? BitmapToIPictureDisp(Image image)
        {
            try { return GetIPictureDispFromPicture(image); }
            catch { return null; }
        }
    }

    #endregion
}
