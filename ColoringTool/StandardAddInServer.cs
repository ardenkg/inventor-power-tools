// ============================================================================
// ColoringTool Add-in for Autodesk Inventor 2026
// StandardAddInServer.cs - Main Entry Point
// ============================================================================

using System;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Windows.Forms;
using System.Reflection;
using System.Diagnostics;
using Inventor;
using ColoringTool.UI;
using ColoringTool.Core;

namespace ColoringTool
{
    /// <summary>
    /// Low-level keyboard hook to capture Ctrl+K and ESC even when Inventor has focus.
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
        private const int VK_K = 0x4B;

        #endregion

        private LowLevelKeyboardProc? _proc;
        private IntPtr _hookID = IntPtr.Zero;
        private readonly Action _onCtrlK;
        private readonly Action _onEscape;
        private readonly Func<bool> _isDialogVisible;

        public GlobalKeyboardHook(Action onCtrlK, Action onEscape, Func<bool> isDialogVisible)
        {
            _onCtrlK = onCtrlK;
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

                // Check for Ctrl+K
                bool ctrlPressed = (GetAsyncKeyState(VK_CONTROL) & 0x8000) != 0;
                if (ctrlPressed && vkCode == VK_K)
                {
                    try
                    {
                        _onCtrlK?.Invoke();
                    }
                    catch { }
                    return (IntPtr)1;
                }

                // Check for ESC (only when dialog is visible)
                if (vkCode == VK_ESCAPE && _isDialogVisible())
                {
                    try
                    {
                        _onEscape?.Invoke();
                    }
                    catch { }
                    return (IntPtr)1;
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
    /// Standard Add-in Server for ColoringTool.
    /// Implements the Inventor Add-in interface and manages ribbon integration.
    /// </summary>
    [GuidAttribute("B2C3D4E5-F6A7-8901-BCDE-F23456789012")]
    [ComVisible(true)]
    public class StandardAddInServer : ApplicationAddInServer
    {
        #region Constants

        private const string ADDIN_GUID = "{B2C3D4E5-F6A7-8901-BCDE-F23456789012}";
        private const string TAB_DISPLAY_NAME = "Power Tools";
        private const string TAB_INTERNAL_NAME = "id_Tab_PowerTools";
        private const string PANEL_DISPLAY_NAME = "Coloring Tool";
        private const string PANEL_INTERNAL_NAME = "id_Panel_ColoringTool";
        private const string BUTTON_DISPLAY_NAME = "Coloring Tool";
        private const string BUTTON_INTERNAL_NAME = "id_Button_ColoringTool";

        #endregion

        #region Private Fields

        private static Inventor.Application? _inventorApp;
        private ButtonDefinition? _coloringToolButton;
        private ColoringToolForm? _coloringForm;
        private SelectionManager? _selectionManager;
        private UserInterfaceEvents? _uiEvents;
        private GlobalKeyboardHook? _keyboardHook;
        private bool _escapePressed = false;

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
                _inventorApp = addInSiteObject.Application;
                _selectionManager = new SelectionManager(_inventorApp);
                _uiEvents = _inventorApp.UserInterfaceManager.UserInterfaceEvents;

                CreateRibbonInterface(firstTime);

                _keyboardHook = new GlobalKeyboardHook(
                    ShowColoringDialog,
                    HandleEscapeKey,
                    () => _coloringForm != null && !_coloringForm.IsDisposed && _coloringForm.Visible
                );

                System.Diagnostics.Debug.WriteLine("ColoringTool Add-in activated successfully.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error activating ColoringTool Add-in:\n{ex.Message}",
                    "ColoringTool Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Called when the Add-in is unloaded by Inventor.
        /// </summary>
        public void Deactivate()
        {
            try
            {
                if (_keyboardHook != null)
                {
                    _keyboardHook.Dispose();
                    _keyboardHook = null;
                }

                _selectionManager?.Dispose();
                _selectionManager = null;

                if (_coloringForm != null)
                {
                    try
                    {
                        _coloringForm.Close();
                        _coloringForm.Dispose();
                    }
                    catch { }
                    _coloringForm = null;
                }

                if (_coloringToolButton != null)
                {
                    _coloringToolButton.OnExecute -= OnColoringToolButtonClick;
                }

                _uiEvents = null;
                _coloringToolButton = null;
                _inventorApp = null;

                GC.Collect();
                GC.WaitForPendingFinalizers();

                System.Diagnostics.Debug.WriteLine("ColoringTool Add-in deactivated successfully.");
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
        }

        #endregion

        #region Ribbon Interface Creation

        /// <summary>
        /// Creates the ribbon interface with the "Power Tools" tab and "Coloring Tool" panel/button.
        /// </summary>
        private void CreateRibbonInterface(bool firstTime)
        {
            if (_inventorApp == null) return;

            try
            {
                var uiManager = _inventorApp.UserInterfaceManager;
                var controlDefs = _inventorApp.CommandManager.ControlDefinitions;

                CreateButtonDefinition(controlDefs);

                if (firstTime)
                {
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
        /// Creates the button definition for the Coloring Tool command.
        /// </summary>
        private void CreateButtonDefinition(ControlDefinitions controlDefs)
        {
            if (_inventorApp == null) return;

            try
            {
                try
                {
                    _coloringToolButton = (ButtonDefinition)controlDefs[BUTTON_INTERNAL_NAME];
                }
                catch
                {
                    _coloringToolButton = null;
                }

                if (_coloringToolButton == null)
                {
                    var largeIcon = GetIconIPictureDisp(32);
                    var smallIcon = GetIconIPictureDisp(16);

                    _coloringToolButton = controlDefs.AddButtonDefinition(
                        DisplayName: BUTTON_DISPLAY_NAME,
                        InternalName: BUTTON_INTERNAL_NAME,
                        Classification: CommandTypesEnum.kQueryOnlyCmdType,
                        ClientId: ADDIN_GUID,
                        DescriptionText: "Open the Coloring Tool to select faces and apply colors.",
                        ToolTipText: "Coloring Tool - Apply Colors to Faces",
                        StandardIcon: smallIcon,
                        LargeIcon: largeIcon,
                        ButtonDisplay: ButtonDisplayEnum.kDisplayTextInLearningMode
                    );
                }

                _coloringToolButton.OnExecute += OnColoringToolButtonClick;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating button definition: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Adds the button to Part and Assembly ribbons.
        /// </summary>
        private void AddButtonToRibbons(UserInterfaceManager uiManager)
        {
            AddButtonToRibbon(uiManager, "Part");
            AddButtonToRibbon(uiManager, "Assembly");
        }

        /// <summary>
        /// Adds the button to a specific ribbon.
        /// </summary>
        private void AddButtonToRibbon(UserInterfaceManager uiManager, string ribbonName)
        {
            try
            {
                var ribbon = uiManager.Ribbons[ribbonName];
                if (ribbon == null) return;

                RibbonTab? powerToolsTab = null;
                try
                {
                    powerToolsTab = ribbon.RibbonTabs[TAB_INTERNAL_NAME];
                }
                catch { }

                if (powerToolsTab == null)
                {
                    powerToolsTab = ribbon.RibbonTabs.Add(
                        DisplayName: TAB_DISPLAY_NAME,
                        InternalName: TAB_INTERNAL_NAME,
                        ClientId: ADDIN_GUID,
                        TargetTabInternalName: "",
                        InsertBeforeTargetTab: false,
                        Contextual: false
                    );
                }

                RibbonPanel? coloringPanel = null;
                try
                {
                    coloringPanel = powerToolsTab.RibbonPanels[PANEL_INTERNAL_NAME];
                }
                catch { }

                if (coloringPanel == null)
                {
                    coloringPanel = powerToolsTab.RibbonPanels.Add(
                        DisplayName: PANEL_DISPLAY_NAME,
                        InternalName: PANEL_INTERNAL_NAME,
                        ClientId: ADDIN_GUID,
                        TargetPanelInternalName: "",
                        InsertBeforeTargetPanel: false
                    );
                }

                if (_coloringToolButton != null)
                {
                    try
                    {
                        coloringPanel.CommandControls.AddButton(
                            ButtonDefinition: _coloringToolButton,
                            UseLargeIcon: true,
                            ShowText: true
                        );
                    }
                    catch { }
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
        /// Handles the Coloring Tool button click event.
        /// </summary>
        private void OnColoringToolButtonClick(NameValueMap context)
        {
            try
            {
                ShowColoringDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening Coloring Tool window:\n{ex.Message}",
                    "ColoringTool Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #endregion

        #region Dialog Management

        /// <summary>
        /// Shows the floating coloring dialog.
        /// </summary>
        private void ShowColoringDialog()
        {
            if (_inventorApp == null) return;

            try
            {
                if (_coloringForm != null && !_coloringForm.IsDisposed && _coloringForm.Visible)
                {
                    _coloringForm.BringToFront();
                    return;
                }

                if (_coloringForm != null)
                {
                    try { _coloringForm.Dispose(); } catch { }
                    _coloringForm = null;
                }

                _selectionManager?.Reset();

                _coloringForm = new ColoringToolForm();
                _coloringForm.Initialize(_inventorApp, _selectionManager!);
                
                PositionFormOnInventorWindow(_coloringForm);
                
                _coloringForm.Show();
                _escapePressed = false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error showing coloring dialog: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Handles ESC key press: closes the dialog immediately.
        /// </summary>
        private void HandleEscapeKey()
        {
            if (_coloringForm == null || _coloringForm.IsDisposed || !_coloringForm.Visible)
                return;

            // Clear any highlights and close
            if (_selectionManager != null)
            {
                _selectionManager.ClearAllFaces();
            }
            
            _coloringForm.DialogResult = DialogResult.Cancel;
            _coloringForm.Hide();
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
                    
                    System.Diagnostics.Debug.WriteLine($"Form positioned at ({formX}, {formY}) on Inventor window");
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
        /// Creates a default icon for the button (paint palette style).
        /// </summary>
        private Bitmap CreateDefaultIcon(int size)
        {
            var bitmap = new Bitmap(size, size);
            using var g = Graphics.FromImage(bitmap);
            
            // Enable anti-aliasing for smoother graphics
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            
            // Background - gradient blue
            g.Clear(System.Drawing.Color.FromArgb(50, 120, 200));
            
            // Draw paint palette shape (oval)
            using var paletteBrush = new SolidBrush(System.Drawing.Color.FromArgb(240, 230, 210));
            g.FillEllipse(paletteBrush, size * 0.08f, size * 0.15f, size * 0.84f, size * 0.7f);
            
            // Thumb hole
            using var holeBrush = new SolidBrush(System.Drawing.Color.FromArgb(50, 120, 200));
            g.FillEllipse(holeBrush, size * 0.12f, size * 0.35f, size * 0.18f, size * 0.25f);
            
            // Color blobs on palette
            float blobSize = size * 0.16f;
            using var redBrush = new SolidBrush(System.Drawing.Color.Red);
            using var greenBrush = new SolidBrush(System.Drawing.Color.Green);
            using var blueBrush = new SolidBrush(System.Drawing.Color.Blue);
            using var yellowBrush = new SolidBrush(System.Drawing.Color.Yellow);
            using var orangeBrush = new SolidBrush(System.Drawing.Color.Orange);
            using var purpleBrush = new SolidBrush(System.Drawing.Color.Purple);
            
            g.FillEllipse(redBrush, size * 0.35f, size * 0.22f, blobSize, blobSize);
            g.FillEllipse(yellowBrush, size * 0.55f, size * 0.20f, blobSize, blobSize);
            g.FillEllipse(greenBrush, size * 0.70f, size * 0.32f, blobSize, blobSize);
            g.FillEllipse(blueBrush, size * 0.72f, size * 0.52f, blobSize, blobSize);
            g.FillEllipse(purpleBrush, size * 0.55f, size * 0.60f, blobSize, blobSize);
            g.FillEllipse(orangeBrush, size * 0.35f, size * 0.55f, blobSize, blobSize);
            
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
