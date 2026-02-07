// ============================================================================
// CaseSelection Add-in for Autodesk Inventor 2026
// StandardAddInServer.cs - Main Entry Point
// ============================================================================

using System;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Windows.Forms;
using System.Reflection;
using System.Diagnostics;
using Inventor;
using CaseSelection.UI;
using CaseSelection.Core;

namespace CaseSelection
{
    /// <summary>
    /// Low-level keyboard hook to capture Ctrl+J and ESC even when Inventor has focus.
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
        private const int VK_J = 0x4A;

        #endregion

        private LowLevelKeyboardProc? _proc;
        private IntPtr _hookID = IntPtr.Zero;
        private readonly Action _onCtrlJ;
        private readonly Action _onEscape;
        private readonly Func<bool> _isDialogVisible;

        public GlobalKeyboardHook(Action onCtrlJ, Action onEscape, Func<bool> isDialogVisible)
        {
            _onCtrlJ = onCtrlJ;
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

                // Check for Ctrl+J
                bool ctrlPressed = (GetAsyncKeyState(VK_CONTROL) & 0x8000) != 0;
                if (ctrlPressed && vkCode == VK_J)
                {
                    // Invoke on UI thread
                    try
                    {
                        _onCtrlJ?.Invoke();
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
    /// Standard Add-in Server for CaseSelection.
    /// Implements the Inventor Add-in interface and manages ribbon integration.
    /// </summary>
    [GuidAttribute("A1B2C3D4-E5F6-7890-ABCD-EF1234567890")]
    [ComVisible(true)]
    public class StandardAddInServer : ApplicationAddInServer
    {
        #region Constants

        private const string ADDIN_GUID = "{A1B2C3D4-E5F6-7890-ABCD-EF1234567890}";
        private const string TAB_DISPLAY_NAME = "Power Tools";
        private const string TAB_INTERNAL_NAME = "id_Tab_PowerTools";
        private const string PANEL_DISPLAY_NAME = "Class Selection";
        private const string PANEL_INTERNAL_NAME = "id_Panel_ClassSelection";
        private const string BUTTON_DISPLAY_NAME = "Class Selection";
        private const string BUTTON_INTERNAL_NAME = "id_Button_ClassSelection";

        #endregion

        #region Private Fields

        private static Inventor.Application? _inventorApp;
        private ButtonDefinition? _classSelectionButton;
        private ClassSelectionForm? _selectionForm;
        private SelectionManager? _selectionManager;
        private UserInterfaceEvents? _uiEvents;
        private GlobalKeyboardHook? _keyboardHook;
        private bool _escapePressed = false;  // Track if escape was pressed once

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

                // Initialize the Selection Manager
                _selectionManager = new SelectionManager(_inventorApp);

                // Subscribe to UI events
                _uiEvents = _inventorApp.UserInterfaceManager.UserInterfaceEvents;

                // Create the ribbon interface
                CreateRibbonInterface(firstTime);

                // Setup global keyboard hook for Ctrl+J and ESC
                _keyboardHook = new GlobalKeyboardHook(
                    ShowSelectionDialog,
                    HandleEscapeKey,
                    () => _selectionForm != null && !_selectionForm.IsDisposed && _selectionForm.Visible
                );

                System.Diagnostics.Debug.WriteLine("CaseSelection Add-in activated successfully.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error activating CaseSelection Add-in:\n{ex.Message}",
                    "CaseSelection Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

                // Clean up selection manager
                _selectionManager?.Dispose();
                _selectionManager = null;

                // Clean up floating form
                if (_selectionForm != null)
                {
                    try
                    {
                        _selectionForm.Close();
                        _selectionForm.Dispose();
                    }
                    catch { }
                    _selectionForm = null;
                }

                // Clean up the button event handler
                if (_classSelectionButton != null)
                {
                    _classSelectionButton.OnExecute -= OnClassSelectionButtonClick;
                }

                // Release COM objects
                _uiEvents = null;
                _classSelectionButton = null;
                _inventorApp = null;

                // Force garbage collection
                GC.Collect();
                GC.WaitForPendingFinalizers();

                System.Diagnostics.Debug.WriteLine("CaseSelection Add-in deactivated successfully.");
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
        /// Creates the ribbon interface with the "Power Tools" tab and "Class Selection" panel/button.
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
                    // Add button to Part, Assembly, and Drawing ribbons
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
        /// Creates the button definition for the Class Selection command.
        /// </summary>
        private void CreateButtonDefinition(ControlDefinitions controlDefs)
        {
            if (_inventorApp == null) return;

            try
            {
                // Try to get existing button definition
                try
                {
                    _classSelectionButton = (ButtonDefinition)controlDefs[BUTTON_INTERNAL_NAME];
                }
                catch
                {
                    // Button doesn't exist, create it
                    _classSelectionButton = null;
                }

                if (_classSelectionButton == null)
                {
                    // Get the icon from resources
                    var largeIcon = GetIconIPictureDisp(32);
                    var smallIcon = GetIconIPictureDisp(16);

                    // Create the button definition
                    _classSelectionButton = controlDefs.AddButtonDefinition(
                        DisplayName: BUTTON_DISPLAY_NAME,
                        InternalName: BUTTON_INTERNAL_NAME,
                        Classification: CommandTypesEnum.kQueryOnlyCmdType,
                        ClientId: ADDIN_GUID,
                        DescriptionText: "Open the Smart Selection tool for face-based selection with topology filters.",
                        ToolTipText: "Class Selection - Smart Face Selection Tool",
                        StandardIcon: smallIcon,
                        LargeIcon: largeIcon,
                        ButtonDisplay: ButtonDisplayEnum.kDisplayTextInLearningMode
                    );
                }

                // Subscribe to the button click event
                _classSelectionButton.OnExecute += OnClassSelectionButtonClick;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating button definition: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Adds the button to Part, Assembly, and Drawing ribbons.
        /// </summary>
        private void AddButtonToRibbons(UserInterfaceManager uiManager)
        {
            // Add to Part Document ribbon
            AddButtonToRibbon(uiManager, "Part");
            
            // Add to Assembly Document ribbon
            AddButtonToRibbon(uiManager, "Assembly");
            
            // Add to Drawing Document ribbon (optional, faces less relevant)
            // AddButtonToRibbon(uiManager, "Drawing");
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

                // Check if "Class Selection" panel exists, if not create it
                RibbonPanel? selectionPanel = null;
                try
                {
                    selectionPanel = powerToolsTab.RibbonPanels[PANEL_INTERNAL_NAME];
                }
                catch
                {
                    // Panel doesn't exist
                }

                if (selectionPanel == null)
                {
                    // Create the "Class Selection" panel
                    selectionPanel = powerToolsTab.RibbonPanels.Add(
                        DisplayName: PANEL_DISPLAY_NAME,
                        InternalName: PANEL_INTERNAL_NAME,
                        ClientId: ADDIN_GUID,
                        TargetPanelInternalName: "",
                        InsertBeforeTargetPanel: false
                    );
                }

                // Add the button to the panel (as a large button)
                if (_classSelectionButton != null)
                {
                    try
                    {
                        selectionPanel.CommandControls.AddButton(
                            ButtonDefinition: _classSelectionButton,
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
        /// Handles the Class Selection button click event.
        /// Opens the floating selection dialog.
        /// </summary>
        private void OnClassSelectionButtonClick(NameValueMap context)
        {
            try
            {
                ShowSelectionDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening Class Selection window:\n{ex.Message}",
                    "CaseSelection Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #endregion

        #region Selection Dialog Management

        /// <summary>
        /// Shows the floating selection dialog.
        /// </summary>
        private void ShowSelectionDialog()
        {
            if (_inventorApp == null) return;

            try
            {
                // If form exists and is visible, bring to front
                if (_selectionForm != null && !_selectionForm.IsDisposed && _selectionForm.Visible)
                {
                    _selectionForm.BringToFront();
                    return;
                }

                // Dispose old form if it exists
                if (_selectionForm != null)
                {
                    try { _selectionForm.Dispose(); } catch { }
                    _selectionForm = null;
                }

                // Reset selection manager for fresh start
                _selectionManager?.Reset();

                // Create new form
                _selectionForm = new ClassSelectionForm();
                _selectionForm.Initialize(_inventorApp, _selectionManager!);
                
                // Position the form on the Inventor window (for multi-monitor support)
                PositionFormOnInventorWindow(_selectionForm);
                
                // Show as non-modal dialog
                _selectionForm.Show();
                
                // Reset escape tracking when showing dialog
                _escapePressed = false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error showing selection dialog: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Handles ESC key press: closes the dialog immediately.
        /// </summary>
        private void HandleEscapeKey()
        {
            if (_selectionForm == null || _selectionForm.IsDisposed || !_selectionForm.Visible)
                return;

            // Clear any highlights and close
            if (_selectionManager != null)
            {
                _selectionManager.ClearAllFaces();
            }
            
            _selectionForm.DialogResult = DialogResult.Cancel;
            _selectionForm.Hide();
        }

        /// <summary>
        /// Positions the form on the same monitor as the Inventor window.
        /// Places it on the left side, offset from the edge to avoid covering the explorer.
        /// </summary>
        private void PositionFormOnInventorWindow(Form form)
        {
            try
            {
                if (_inventorApp == null) return;

                // Get the current foreground window (should be Inventor since user just pressed Ctrl+J or clicked button)
                IntPtr inventorHwnd = GetForegroundWindow();

                // Get the Inventor window bounds
                if (GetWindowRect(inventorHwnd, out RECT inventorRect))
                {
                    // Calculate position: left side of Inventor window
                    // About 2 inches from left edge (192 pixels at 96 DPI) to avoid covering explorer
                    int leftOffset = 200;  // ~2 inches at 96 DPI
                    int topOffset = 250;   // Lower on the screen, below ribbon and explorer header
                    
                    int formX = inventorRect.Left + leftOffset;
                    int formY = inventorRect.Top + topOffset;
                    
                    // Ensure form stays within the Inventor window bounds
                    if (formX + form.Width > inventorRect.Right - 20)
                        formX = inventorRect.Right - form.Width - 20;
                    if (formY + form.Height > inventorRect.Bottom - 20)
                        formY = inventorRect.Bottom - form.Height - 20;
                    
                    // Set form location
                    form.StartPosition = FormStartPosition.Manual;
                    form.Location = new System.Drawing.Point(formX, formY);
                    
                    System.Diagnostics.Debug.WriteLine($"Form positioned at ({formX}, {formY}) on Inventor window");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error positioning form: {ex.Message}");
                // Fall back to default positioning
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
                // Create a simple icon programmatically
                using var bitmap = CreateDefaultIcon(size);
                return IconConverter.BitmapToIPictureDisp(bitmap);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Creates a default icon for the button.
        /// </summary>
        private Bitmap CreateDefaultIcon(int size)
        {
            var bitmap = new Bitmap(size, size);
            using var g = Graphics.FromImage(bitmap);
            
            // Background
            g.Clear(System.Drawing.Color.FromArgb(0, 120, 215));
            
            // Draw a simple selection icon (pointer with plus)
            using var pen = new Pen(System.Drawing.Color.White, size / 16f);
            using var brush = new SolidBrush(System.Drawing.Color.White);
            
            // Arrow/pointer shape
            var points = new PointF[]
            {
                new PointF(size * 0.2f, size * 0.15f),
                new PointF(size * 0.2f, size * 0.75f),
                new PointF(size * 0.35f, size * 0.6f),
                new PointF(size * 0.5f, size * 0.85f),
                new PointF(size * 0.6f, size * 0.75f),
                new PointF(size * 0.45f, size * 0.5f),
                new PointF(size * 0.65f, size * 0.5f)
            };
            g.FillPolygon(brush, points);
            
            // Plus sign
            float cx = size * 0.75f;
            float cy = size * 0.25f;
            float psize = size * 0.15f;
            g.DrawLine(pen, cx - psize, cy, cx + psize, cy);
            g.DrawLine(pen, cx, cy - psize, cx, cy + psize);
            
            return bitmap;
        }

        #endregion
    }

    #region Icon Converter Helper

    /// <summary>
    /// Helper class to convert bitmaps to IPictureDisp for Inventor.
    /// Uses AxHost subclass approach for .NET compatibility.
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
