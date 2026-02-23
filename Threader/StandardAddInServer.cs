// ============================================================================
// Threader Add-in for Autodesk Inventor 2026
// StandardAddInServer.cs - Main Entry Point
// ============================================================================

using System;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Windows.Forms;
using System.Reflection;
using System.Diagnostics;
using Inventor;
using Threader.UI;
using Threader.Core;

namespace Threader
{
    /// <summary>
    /// Low-level keyboard hook to capture Ctrl+T and ESC even when Inventor has focus.
    /// </summary>
    internal class GlobalKeyboardHook : IDisposable
    {
        #region Win32 API

        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_SYSKEYDOWN = 0x0104;

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        private const int VK_CONTROL = 0x11;
        private const int VK_ESCAPE = 0x1B;
        private const int VK_L = 0x4C;

        #endregion

        private LowLevelKeyboardProc? _proc;
        private IntPtr _hookID = IntPtr.Zero;
        private readonly Action _onCtrlShiftT;
        private readonly Action _onEscape;
        private readonly Func<bool> _isDialogVisible;

        public GlobalKeyboardHook(Action onCtrlShiftT, Action onEscape, Func<bool> isDialogVisible)
        {
            _onCtrlShiftT = onCtrlShiftT;
            _onEscape = onEscape;
            _isDialogVisible = isDialogVisible;
            _proc = HookCallback;
            _hookID = SetHook(_proc);
        }

        private IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using var curProcess = Process.GetCurrentProcess();
            using var curModule = curProcess.MainModule;
            return SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(curModule?.ModuleName ?? ""), 0);
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && (wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_SYSKEYDOWN))
            {
                int vkCode = Marshal.ReadInt32(lParam);

                // Check for Ctrl+L
                bool ctrlPressed = (GetAsyncKeyState(VK_CONTROL) & 0x8000) != 0;
                if (ctrlPressed && vkCode == VK_L)
                {
                    try
                    {
                        _onCtrlShiftT?.Invoke();
                    }
                    catch { }
                    return (IntPtr)1; // Block the key
                }

                // Check for ESC (only when dialog is visible)
                if (vkCode == VK_ESCAPE && _isDialogVisible())
                {
                    try
                    {
                        _onEscape?.Invoke();
                    }
                    catch { }
                    return (IntPtr)1; // Block the key
                }
            }

            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        public void Dispose()
        {
            if (_hookID != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_hookID);
                _hookID = IntPtr.Zero;
            }
            _proc = null;
        }
    }

    /// <summary>
    /// Standard Add-in Server for Threader.
    /// Implements the Inventor Add-in interface and manages ribbon integration.
    /// </summary>
    [GuidAttribute("C3D4E5F6-A7B8-9012-CDEF-345678901234")]
    [ComVisible(true)]
    public class StandardAddInServer : ApplicationAddInServer
    {
        #region Constants

        private const string ADDIN_GUID = "{C3D4E5F6-A7B8-9012-CDEF-345678901234}";
        private const string TAB_DISPLAY_NAME = "Power Tools";
        private const string TAB_INTERNAL_NAME = "id_Tab_PowerTools";
        private const string PANEL_DISPLAY_NAME = "Thread Tools";
        private const string PANEL_INTERNAL_NAME = "id_Panel_ThreadTools";
        private const string BUTTON_DISPLAY_NAME = "Threader";
        private const string BUTTON_INTERNAL_NAME = "id_Button_Threader";

        #endregion

        #region Private Fields

        private static Inventor.Application? _inventorApp;
        private ButtonDefinition? _ThreaderButton;
        private ThreaderForm? _modelerForm;
        private ThreadDataManager? _threadDataManager;
        private CylinderAnalyzer? _cylinderAnalyzer;
        private ThreadGenerator? _threadGenerator;
        private ThreadPreviewManager? _previewManager;
        private UserInterfaceEvents? _uiEvents;
        private GlobalKeyboardHook? _keyboardHook;
        private InteractionEvents? _interactionEvents;
        private SelectEvents? _selectEvents;
        private bool _isSelectingCylinder = false;

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

        /// <summary>
        /// Gets the Inventor Application instance.
        /// </summary>
        public static Inventor.Application? InventorApp => _inventorApp;

        #endregion

        #region ApplicationAddInServer Implementation

        /// <summary>
        /// Called when the Add-in is loaded by Inventor.
        /// </summary>
        public void Activate(ApplicationAddInSite addInSiteObject, bool firstTime)
        {
            try
            {
                // Get the Inventor Application object
                _inventorApp = addInSiteObject.Application;

                // Initialize managers
                _threadDataManager = new ThreadDataManager(_inventorApp);
                _cylinderAnalyzer = new CylinderAnalyzer(_inventorApp);
                _threadGenerator = new ThreadGenerator(_inventorApp);
                _previewManager = new ThreadPreviewManager(_inventorApp);

                // Subscribe to UI events
                _uiEvents = _inventorApp.UserInterfaceManager.UserInterfaceEvents;

                // Create the ribbon interface
                CreateRibbonInterface(firstTime);

                // Setup global keyboard hook for Ctrl+Shift+T and ESC
                _keyboardHook = new GlobalKeyboardHook(
                    ShowModelerDialog,
                    HandleEscapeKey,
                    () => _modelerForm != null && !_modelerForm.IsDisposed && _modelerForm.Visible
                );

                System.Diagnostics.Debug.WriteLine("Threader Add-in activated successfully.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error activating Threader Add-in:\n{ex.Message}",
                    "Threader Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Called when the Add-in is unloaded by Inventor.
        /// </summary>
        public void Deactivate()
        {
            try
            {
                // Remove keyboard hook
                if (_keyboardHook != null)
                {
                    _keyboardHook.Dispose();
                    _keyboardHook = null;
                }

                // Stop any active selection
                StopCylinderSelection();

                // Clear preview
                _previewManager?.ClearPreview();

                // Clean up floating form
                if (_modelerForm != null)
                {
                    try
                    {
                        _modelerForm.Close();
                        _modelerForm.Dispose();
                    }
                    catch { }
                    _modelerForm = null;
                }

                // Clean up the button event handler
                if (_ThreaderButton != null)
                {
                    _ThreaderButton.OnExecute -= OnThreaderButtonClick;
                }

                // Release COM objects
                _uiEvents = null;
                _ThreaderButton = null;
                _threadDataManager = null;
                _cylinderAnalyzer = null;
                _threadGenerator = null;
                _previewManager = null;
                _inventorApp = null;

                // Force garbage collection
                GC.Collect();
                GC.WaitForPendingFinalizers();

                System.Diagnostics.Debug.WriteLine("Threader Add-in deactivated successfully.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error during deactivation: {ex.Message}");
            }
        }

        /// <summary>
        /// Automation property - not used.
        /// </summary>
        public object Automation => null!;

        /// <summary>
        /// Called when Inventor needs to execute a command - not used.
        /// </summary>
        public void ExecuteCommand(int commandID)
        {
            // Not implemented
        }

        #endregion

        #region Ribbon Interface Creation

        /// <summary>
        /// Creates the ribbon interface with the "Power Tools" tab and "Thread Tools" panel/button.
        /// </summary>
        private void CreateRibbonInterface(bool firstTime)
        {
            if (_inventorApp == null) return;

            try
            {
                var uiManager = _inventorApp.UserInterfaceManager;
                var controlDefs = _inventorApp.CommandManager.ControlDefinitions;

                // Create the button definition
                CreateButtonDefinition(controlDefs);

                // If first time loading, create the ribbon controls
                if (firstTime)
                {
                    // Add button to Part ribbon only (threads are for parts)
                    AddButtonToRibbons(uiManager);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating ribbon interface: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Creates the button definition for the Threader command.
        /// </summary>
        private void CreateButtonDefinition(ControlDefinitions controlDefs)
        {
            if (_inventorApp == null) return;

            try
            {
                // Try to get existing button definition
                try
                {
                    _ThreaderButton = (ButtonDefinition)controlDefs[BUTTON_INTERNAL_NAME];
                }
                catch
                {
                    // Button doesn't exist, create it
                    _ThreaderButton = null;
                }

                if (_ThreaderButton == null)
                {
                    // Get the icon from resources
                    var largeIcon = GetIconIPictureDisp(32);
                    var smallIcon = GetIconIPictureDisp(16);

                    // Create the button definition
                    _ThreaderButton = controlDefs.AddButtonDefinition(
                        DisplayName: BUTTON_DISPLAY_NAME,
                        InternalName: BUTTON_INTERNAL_NAME,
                        Classification: CommandTypesEnum.kEditMaskCmdType,
                        ClientId: ADDIN_GUID,
                        DescriptionText: "Generate physical geometric threads from cylindrical faces using ISO Metric Profile standards.",
                        ToolTipText: "Threader - Physical Thread Generator",
                        StandardIcon: smallIcon,
                        LargeIcon: largeIcon,
                        ButtonDisplay: ButtonDisplayEnum.kDisplayTextInLearningMode
                    );
                }

                // Subscribe to the button click event
                _ThreaderButton.OnExecute += OnThreaderButtonClick;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating button definition: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Adds the button to Part ribbon.
        /// </summary>
        private void AddButtonToRibbons(UserInterfaceManager uiManager)
        {
            // Add to Part Document ribbon only (threads are created on parts)
            AddButtonToRibbon(uiManager, "Part");
        }

        /// <summary>
        /// Adds the button to a specific ribbon.
        /// </summary>
        private void AddButtonToRibbon(UserInterfaceManager uiManager, string ribbonName)
        {
            try
            {
                // Get the ribbon
                var ribbon = uiManager.Ribbons[ribbonName];
                if (ribbon == null) return;

                // Check if "Power Tools" tab exists, if not create it
                RibbonTab? powerToolsTab = null;
                try
                {
                    powerToolsTab = ribbon.RibbonTabs[TAB_INTERNAL_NAME];
                }
                catch
                {
                    // Tab doesn't exist
                }

                if (powerToolsTab == null)
                {
                    // Create the "Power Tools" tab
                    powerToolsTab = ribbon.RibbonTabs.Add(
                        DisplayName: TAB_DISPLAY_NAME,
                        InternalName: TAB_INTERNAL_NAME,
                        ClientId: ADDIN_GUID,
                        TargetTabInternalName: "", // Add at the end
                        InsertBeforeTargetTab: false,
                        Contextual: false
                    );
                }

                // Check if "Thread Tools" panel exists, if not create it
                RibbonPanel? threadPanel = null;
                try
                {
                    threadPanel = powerToolsTab.RibbonPanels[PANEL_INTERNAL_NAME];
                }
                catch
                {
                    // Panel doesn't exist
                }

                if (threadPanel == null)
                {
                    // Create the "Thread Tools" panel
                    threadPanel = powerToolsTab.RibbonPanels.Add(
                        DisplayName: PANEL_DISPLAY_NAME,
                        InternalName: PANEL_INTERNAL_NAME,
                        ClientId: ADDIN_GUID,
                        TargetPanelInternalName: "",
                        InsertBeforeTargetPanel: false
                    );
                }

                // Add the button to the panel (as a large button)
                if (_ThreaderButton != null)
                {
                    try
                    {
                        threadPanel.CommandControls.AddButton(
                            ButtonDefinition: _ThreaderButton,
                            UseLargeIcon: true,
                            ShowText: true
                        );
                    }
                    catch
                    {
                        // Button might already exist in this panel
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error adding button to {ribbonName} ribbon: {ex.Message}");
            }
        }

        #endregion

        #region Button Event Handler

        /// <summary>
        /// Handles the Threader button click event.
        /// Opens the floating modeler dialog.
        /// </summary>
        private void OnThreaderButtonClick(NameValueMap context)
        {
            try
            {
                ShowModelerDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening Threader window:\n{ex.Message}",
                    "Threader Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #endregion

        #region Modeler Dialog Management

        /// <summary>
        /// Shows the floating modeler dialog.
        /// </summary>
        private void ShowModelerDialog()
        {
            if (_inventorApp == null) return;

            // Check if a part document is open (or a part is being edited in-place in an assembly)
            if (_inventorApp.ActiveDocument == null)
            {
                MessageBox.Show("Please open a document to use Threader.",
                    "Threader", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var activeDocType = _inventorApp.ActiveDocument.DocumentType;
            bool isPartDoc = activeDocType == DocumentTypeEnum.kPartDocumentObject;
            bool isAssemblyDoc = activeDocType == DocumentTypeEnum.kAssemblyDocumentObject;

            if (!isPartDoc && !isAssemblyDoc)
            {
                MessageBox.Show("Please open a Part or Assembly document to use Threader.",
                    "Threader", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // In an assembly, a part must be actively edited in-place
            if (isAssemblyDoc)
            {
                var editDoc = _inventorApp.ActiveEditDocument as PartDocument;
                if (editDoc == null)
                {
                    MessageBox.Show("Please edit a Part in-place (double-click a part) before using Threader.",
                        "Threader", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
            }

            try
            {
                // If form exists and is visible, bring to front
                if (_modelerForm != null && !_modelerForm.IsDisposed && _modelerForm.Visible)
                {
                    _modelerForm.BringToFront();
                    return;
                }

                // Dispose old form if it exists
                if (_modelerForm != null)
                {
                    try { _modelerForm.Dispose(); } catch { }
                    _modelerForm = null;
                }
                
                // Stop any existing selection events to ensure clean state
                StopCylinderSelection();

                // Clear any existing preview
                _previewManager?.ClearPreview();

                // Create new form
                _modelerForm = new ThreaderForm();
                _modelerForm.Initialize(
                    _inventorApp, 
                    _threadDataManager!, 
                    _cylinderAnalyzer!, 
                    _threadGenerator!,
                    _previewManager!
                );
                
                // Position the form on the Inventor window
                PositionFormOnInventorWindow(_modelerForm);
                
                // Subscribe to form's selection request event
                _modelerForm.SelectionRequested += OnFormSelectionRequested;
                
                // Show as non-modal dialog
                _modelerForm.Show();

                // Start cylinder selection
                StartCylinderSelection();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error showing modeler dialog: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Handles ESC key press: closes dialog and clears preview.
        /// </summary>
        private void HandleEscapeKey()
        {
            if (_modelerForm == null || _modelerForm.IsDisposed || !_modelerForm.Visible)
                return;

            // Clear preview
            _previewManager?.ClearPreview();

            // Stop selection
            StopCylinderSelection();

            // Close the form
            _modelerForm.DialogResult = DialogResult.Cancel;
            _modelerForm.Hide();
        }

        /// <summary>
        /// Handles the form's request to restart selection mode.
        /// </summary>
        private void OnFormSelectionRequested()
        {
            // Restart cylinder selection if not already active
            if (!_isSelectingCylinder)
            {
                StartCylinderSelection();
            }
        }

        /// <summary>
        /// Starts interactive cylinder face selection.
        /// </summary>
        public void StartCylinderSelection()
        {
            if (_inventorApp == null || _isSelectingCylinder) return;

            try
            {
                _interactionEvents = _inventorApp.CommandManager.CreateInteractionEvents();
                _interactionEvents.InteractionDisabled = false;
                
                _selectEvents = _interactionEvents.SelectEvents;
                _selectEvents.AddSelectionFilter(SelectionFilterEnum.kPartFaceFilter);
                _selectEvents.SingleSelectEnabled = true;
                
                _selectEvents.OnSelect += OnCylinderSelected;
                _selectEvents.OnPreSelect += OnCylinderPreSelect;
                
                _interactionEvents.Start();
                _isSelectingCylinder = true;
                
                _inventorApp.StatusBarText = "Select a cylindrical face to create threads...";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error starting cylinder selection: {ex.Message}");
            }
        }

        /// <summary>
        /// Stops interactive cylinder face selection.
        /// </summary>
        public void StopCylinderSelection()
        {
            if (!_isSelectingCylinder) return;

            try
            {
                if (_selectEvents != null)
                {
                    _selectEvents.OnSelect -= OnCylinderSelected;
                    _selectEvents.OnPreSelect -= OnCylinderPreSelect;
                    _selectEvents = null;
                }

                if (_interactionEvents != null)
                {
                    _interactionEvents.Stop();
                    _interactionEvents = null;
                }

                _isSelectingCylinder = false;

                if (_inventorApp != null)
                {
                    _inventorApp.StatusBarText = "";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error stopping cylinder selection: {ex.Message}");
            }
        }

        /// <summary>
        /// Handles pre-selection of faces to highlight cylindrical faces.
        /// </summary>
        private void OnCylinderPreSelect(ref object preSelectEntity, out bool doHighlight, ref ObjectCollection morePreSelectEntities, SelectionDeviceEnum selectionDevice, Inventor.Point modelPosition, Point2d viewPosition, Inventor.View view)
        {
            doHighlight = false;

            if (preSelectEntity is Face face && _cylinderAnalyzer != null)
            {
                // Only highlight if it's a cylindrical face
                doHighlight = _cylinderAnalyzer.IsCylindricalFace(face);
            }
        }

        /// <summary>
        /// Handles selection of a cylindrical face.
        /// </summary>
        private void OnCylinderSelected(ObjectsEnumerator justSelectedEntities, SelectionDeviceEnum selectionDevice, Inventor.Point modelPosition, Point2d viewPosition, Inventor.View view)
        {
            if (justSelectedEntities.Count == 0) return;

            var entity = justSelectedEntities[1];
            if (entity is Face face && _cylinderAnalyzer != null && _modelerForm != null)
            {
                if (_cylinderAnalyzer.IsCylindricalFace(face))
                {
                    // Get cylinder info
                    var cylinderInfo = _cylinderAnalyzer.AnalyzeFace(face);
                    
                    if (cylinderInfo != null)
                    {
                        // Update the form with the selected cylinder
                        _modelerForm.SetSelectedCylinder(face, cylinderInfo);
                    }
                }
                else
                {
                    MessageBox.Show("Please select a cylindrical face.", "Threader", 
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        /// <summary>
        /// Positions the form on the same monitor as the Inventor window.
        /// </summary>
        private void PositionFormOnInventorWindow(Form form)
        {
            try
            {
                if (_inventorApp == null) return;

                IntPtr inventorHwnd = GetForegroundWindow();

                if (GetWindowRect(inventorHwnd, out RECT inventorRect))
                {
                    int leftOffset = 200;
                    int topOffset = 250;
                    
                    int formX = inventorRect.Left + leftOffset;
                    int formY = inventorRect.Top + topOffset;
                    
                    if (formX + form.Width > inventorRect.Right - 20)
                        formX = inventorRect.Right - form.Width - 20;
                    if (formY + form.Height > inventorRect.Bottom - 20)
                        formY = inventorRect.Bottom - form.Height - 20;
                    
                    form.StartPosition = FormStartPosition.Manual;
                    form.Location = new System.Drawing.Point(formX, formY);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error positioning form: {ex.Message}");
                form.StartPosition = FormStartPosition.CenterScreen;
            }
        }

        #endregion

        #region Icon Helpers

        /// <summary>
        /// Gets an icon as IPictureDisp for the button.
        /// </summary>
        private object? GetIconIPictureDisp(int size)
        {
            try
            {
                using var bitmap = CreateDefaultIcon(size);
                return IconConverter.BitmapToIPictureDisp(bitmap);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Creates a default icon for the button (thread/coil shape).
        /// </summary>
        private Bitmap CreateDefaultIcon(int size)
        {
            var bitmap = new Bitmap(size, size);
            using var g = Graphics.FromImage(bitmap);
            
            // Background
            g.Clear(System.Drawing.Color.FromArgb(0, 120, 215));
            
            // Draw a simple thread/coil icon
            using var pen = new Pen(System.Drawing.Color.White, size / 10f);
            
            // Draw helix-like pattern
            float cx = size * 0.5f;
            float radius = size * 0.3f;
            
            // Draw three coil segments
            for (int i = 0; i < 3; i++)
            {
                float y = size * (0.2f + i * 0.25f);
                g.DrawArc(pen, cx - radius, y - radius * 0.3f, radius * 2, radius * 0.6f, 180, 180);
            }
            
            // Draw vertical line for cylinder
            g.DrawLine(pen, cx - radius, size * 0.2f, cx - radius, size * 0.8f);
            g.DrawLine(pen, cx + radius, size * 0.2f, cx + radius, size * 0.8f);
            
            return bitmap;
        }

        #endregion
    }

    #region Icon Converter Helper

    /// <summary>
    /// Helper class to convert bitmaps to IPictureDisp for Inventor.
    /// </summary>
    internal class IconConverter : System.Windows.Forms.AxHost
    {
        private IconConverter() : base(Guid.Empty.ToString()) { }

        /// <summary>
        /// Converts a Bitmap to IPictureDisp using AxHost.
        /// </summary>
        public static object? BitmapToIPictureDisp(Image image)
        {
            try
            {
                return GetIPictureDispFromPicture(image);
            }
            catch
            {
                return null;
            }
        }
    }

    #endregion
}
