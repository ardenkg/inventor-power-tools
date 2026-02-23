// ============================================================================
// iNode Add-in for Autodesk Inventor 2026
// NodeEditDialog.cs — Modal dialog for editing node parameters
//
// Supports both text fields and dropdown ComboBox controls.
// Uses CreateGraphics().DpiX at runtime to scale all sizes correctly.
// No Designer file — everything is explicit and DPI-aware.
// ============================================================================

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using iNode.Core;

namespace iNode.UI
{
    public class NodeEditDialog : Form
    {
        private readonly List<Control> _editControls = new();
        private readonly List<string> _keys = new();

        public Dictionary<string, string> Results { get; } = new();

        /// <summary>
        /// Creates an edit dialog from ParameterDescriptor metadata.
        /// Renders ComboBox for parameters with Choices, TextBox otherwise.
        /// </summary>
        public NodeEditDialog(string nodeTitle,
            List<Node.ParameterDescriptor> descriptors,
            Point anchorScreen)
        {
            // ── Get DPI scale factor ──
            float scale;
            using (var g = CreateGraphics())
            {
                scale = g.DpiX / 96f;
            }

            // ── Scaled dimensions ──
            int S(int px) => (int)(px * scale);

            int labelW    = S(100);
            int gap       = S(10);
            int fieldLeft = labelW + gap;
            int rowH      = S(36);
            int tbH       = S(24);
            int padLeft   = S(20);
            int padRight  = S(20);
            int padTop    = S(16);
            int padBot    = S(12);
            int btnW      = S(90);
            int btnH      = S(32);
            int btnGap    = S(10);
            int formW     = S(380);

            int fieldCount = descriptors.Count;
            float fontSize = 10f;

            // ── Form setup ──
            Text = $"Edit: {nodeTitle}";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.Manual;
            ShowInTaskbar = false;
            MaximizeBox = false;
            MinimizeBox = false;
            Font = new Font("Segoe UI", fontSize);
            BackColor = Color.FromArgb(245, 245, 245);
            AutoScaleMode = AutoScaleMode.None;

            // ── Create labels and edit controls ──
            int y = padTop;
            int contentW = formW - padLeft - padRight;
            int tbW = contentW - fieldLeft;

            for (int i = 0; i < fieldCount; i++)
            {
                var desc = descriptors[i];

                var lbl = new Label
                {
                    Text = desc.Label,
                    Font = new Font("Segoe UI", fontSize),
                    ForeColor = Color.FromArgb(30, 30, 30),
                    TextAlign = ContentAlignment.MiddleRight,
                    Location = new Point(padLeft, y),
                    Size = new Size(labelW, rowH),
                };
                Controls.Add(lbl);

                if (desc.Choices != null && desc.Choices.Length > 0)
                {
                    // Dropdown ComboBox
                    var cbo = new ComboBox
                    {
                        Font = new Font("Segoe UI", fontSize),
                        DropDownStyle = ComboBoxStyle.DropDownList,
                        Location = new Point(padLeft + fieldLeft, y + (rowH - tbH) / 2),
                        Size = new Size(tbW, tbH),
                        Tag = desc.Key,
                        TabIndex = i,
                    };
                    cbo.Items.AddRange(desc.Choices);

                    // Select the current value
                    int selIdx = Array.IndexOf(desc.Choices, desc.Value);
                    cbo.SelectedIndex = selIdx >= 0 ? selIdx : 0;

                    Controls.Add(cbo);
                    _editControls.Add(cbo);
                }
                else
                {
                    // Text field
                    var txt = new TextBox
                    {
                        Text = desc.Value,
                        Font = new Font("Segoe UI", fontSize),
                        Location = new Point(padLeft + fieldLeft, y + (rowH - tbH) / 2),
                        Size = new Size(tbW, tbH),
                        Tag = desc.Key,
                        TabIndex = i,
                    };
                    Controls.Add(txt);
                    _editControls.Add(txt);
                }

                _keys.Add(desc.Key);
                y += rowH;
            }

            // ── Buttons ──
            y += S(8);

            var btnOk = new Button
            {
                Text = "OK",
                Font = new Font("Segoe UI", fontSize, FontStyle.Bold),
                Size = new Size(btnW, btnH),
                BackColor = Color.FromArgb(0, 122, 204),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                DialogResult = DialogResult.OK,
                TabIndex = fieldCount,
            };
            btnOk.FlatAppearance.BorderColor = Color.FromArgb(0, 100, 180);

            var btnCancel = new Button
            {
                Text = "Cancel",
                Font = new Font("Segoe UI", fontSize),
                Size = new Size(btnW, btnH),
                DialogResult = DialogResult.Cancel,
                TabIndex = fieldCount + 1,
            };

            int buttonsRight = formW - padRight;
            btnCancel.Location = new Point(buttonsRight - btnW, y);
            btnOk.Location = new Point(buttonsRight - btnW - btnGap - btnW, y);

            Controls.Add(btnOk);
            Controls.Add(btnCancel);
            AcceptButton = btnOk;
            CancelButton = btnCancel;

            // ── Form size ──
            int formH = y + btnH + padBot;
            ClientSize = new Size(formW, formH);

            // ── Position near node, clamped to screen ──
            var screen = Screen.FromPoint(anchorScreen).WorkingArea;
            int fx = Math.Min(anchorScreen.X, screen.Right - Width - 10);
            int fy = Math.Min(anchorScreen.Y, screen.Bottom - Height - 10);
            fx = Math.Max(screen.Left + 10, fx);
            fy = Math.Max(screen.Top + 10, fy);
            Location = new Point(fx, fy);

            // ── Events ──
            Shown += (s, e) =>
            {
                if (_editControls.Count > 0)
                {
                    _editControls[0].Focus();
                    if (_editControls[0] is TextBox tb)
                        tb.SelectAll();
                }
            };

            FormClosing += (s, e) =>
            {
                if (DialogResult == DialogResult.OK)
                {
                    for (int i = 0; i < _editControls.Count; i++)
                    {
                        if (_editControls[i] is ComboBox cbo)
                            Results[_keys[i]] = cbo.SelectedItem?.ToString() ?? "";
                        else if (_editControls[i] is TextBox tb)
                            Results[_keys[i]] = tb.Text;
                    }
                }
            };
        }

        /// <summary>
        /// Legacy constructor for backward compatibility with old-style parameter lists.
        /// </summary>
        public NodeEditDialog(string nodeTitle,
            List<(string Label, string Key, string Value)> parameters,
            Point anchorScreen)
            : this(nodeTitle, ConvertLegacyParams(parameters), anchorScreen)
        {
        }

        private static List<Node.ParameterDescriptor> ConvertLegacyParams(
            List<(string Label, string Key, string Value)> parameters)
        {
            var result = new List<Node.ParameterDescriptor>();
            foreach (var (label, key, value) in parameters)
            {
                result.Add(new Node.ParameterDescriptor
                {
                    Label = label,
                    Key = key,
                    Value = value
                });
            }
            return result;
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Escape)
            {
                DialogResult = DialogResult.Cancel;
                Close();
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }
    }
}
