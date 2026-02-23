// ============================================================================
// iNode Add-in for Autodesk Inventor 2026
// NodeSearchPopup.cs - Quick search popup for adding nodes
// ============================================================================

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using iNode.Core;

namespace iNode.UI
{
    /// <summary>
    /// A floating popup with a search box and categorized list of nodes.
    /// Appears when the user right-clicks or presses a shortcut to add nodes.
    /// </summary>
    public class NodeSearchPopup : Form
    {
        private TextBox _searchBox;
        private ListBox _resultsList;
        private List<NodeFactory.NodeRegistration> _currentResults;

        /// <summary>Event fired when the user selects a node type.</summary>
        public event EventHandler<string>? NodeTypeSelected;

        public NodeSearchPopup()
        {
            _currentResults = new List<NodeFactory.NodeRegistration>();

            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            Size = new Size(500, 620);
            BackColor = Color.FromArgb(250, 250, 250);
            TopMost = true;
            AutoScaleMode = AutoScaleMode.None;

            // Border panel
            var borderPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(250, 250, 250),
                Padding = new Padding(1)
            };
            borderPanel.Paint += (s, e) =>
            {
                using var pen = new Pen(Color.FromArgb(180, 180, 180), 1);
                e.Graphics.DrawRectangle(pen, 0, 0, borderPanel.Width - 1, borderPanel.Height - 1);
            };
            Controls.Add(borderPanel);

            // Header
            var header = new Panel
            {
                Dock = DockStyle.Top,
                Height = 44,
                BackColor = Color.FromArgb(0, 122, 204),
                Padding = new Padding(14, 0, 0, 0)
            };
            var headerLabel = new Label
            {
                Text = "Add Node",
                Dock = DockStyle.Fill,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft
            };
            header.Controls.Add(headerLabel);

            // Search box
            _searchBox = new TextBox
            {
                Dock = DockStyle.Top,
                Font = new Font("Segoe UI", 14F),
                BackColor = Color.White,
                ForeColor = Color.FromArgb(40, 40, 40),
                BorderStyle = BorderStyle.FixedSingle,
                Height = 40
            };
            _searchBox.TextChanged += OnSearchTextChanged;
            _searchBox.KeyDown += OnSearchKeyDown;

            // Results list
            _resultsList = new ListBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 13F),
                BackColor = Color.FromArgb(250, 250, 250),
                ForeColor = Color.FromArgb(40, 40, 40),
                BorderStyle = BorderStyle.None,
                IntegralHeight = false,
                DrawMode = DrawMode.OwnerDrawVariable,
                ItemHeight = 52
            };
            _resultsList.MeasureItem += (s, ev) => { ev.ItemHeight = 52; };
            _resultsList.DrawItem += OnDrawItem;
            _resultsList.DoubleClick += OnResultDoubleClick;
            _resultsList.Click += OnResultClick;
            _resultsList.KeyDown += OnResultsKeyDown;

            // Order matters for docking: Fill first, then Top items
            // (last-added Top control docks topmost)
            borderPanel.Controls.Add(_resultsList);  // Fill — gets remaining space
            borderPanel.Controls.Add(_searchBox);     // Top — below header
            borderPanel.Controls.Add(header);         // Top — at very top

            // Populate initial results
            RefreshResults("");

            // Close when deactivated
            Deactivate += (s, e) => Hide();
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Escape)
            {
                Hide();
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void OnSearchTextChanged(object? sender, EventArgs e)
        {
            RefreshResults(_searchBox.Text);
        }

        private void OnSearchKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Down && _resultsList.Items.Count > 0)
            {
                _resultsList.Focus();
                _resultsList.SelectedIndex = 0;
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Enter && _resultsList.Items.Count > 0)
            {
                if (_resultsList.SelectedIndex < 0) _resultsList.SelectedIndex = 0;
                SelectCurrentItem();
                e.Handled = true;
            }
        }

        private void OnResultsKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                SelectCurrentItem();
                e.Handled = true;
            }
        }

        private void OnResultDoubleClick(object? sender, EventArgs e)
        {
            SelectCurrentItem();
        }

        private void OnResultClick(object? sender, EventArgs e)
        {
            SelectCurrentItem();
        }

        private void SelectCurrentItem()
        {
            if (_resultsList.SelectedIndex >= 0 && _resultsList.SelectedIndex < _currentResults.Count)
            {
                var selected = _currentResults[_resultsList.SelectedIndex];
                NodeTypeSelected?.Invoke(this, selected.TypeName);
                Hide();
            }
        }

        private void RefreshResults(string query)
        {
            _currentResults = NodeFactory.Search(query);
            _resultsList.Items.Clear();
            foreach (var reg in _currentResults)
            {
                _resultsList.Items.Add($"[{reg.Category}] {reg.DisplayName}");
            }
            if (_resultsList.Items.Count > 0)
                _resultsList.SelectedIndex = 0;
        }

        private void OnDrawItem(object? sender, DrawItemEventArgs e)
        {
            if (e.Index < 0 || e.Index >= _currentResults.Count) return;

            var reg = _currentResults[e.Index];

            // Clip to item bounds to prevent text bleeding outside the popup
            e.Graphics.SetClip(e.Bounds);

            // Background
            bool isSelected = (e.State & DrawItemState.Selected) != 0;
            var bgColor = isSelected ? Color.FromArgb(220, 230, 245) : Color.FromArgb(250, 250, 250);
            using var bgBrush = new SolidBrush(bgColor);
            e.Graphics.FillRectangle(bgBrush, e.Bounds);

            // Category color dot
            var catColor = reg.Category switch
            {
                "Input" => Color.FromArgb(0, 150, 80),
                "Math" => Color.FromArgb(60, 120, 200),
                "Logic" => Color.FromArgb(80, 100, 180),
                "Geometry" => Color.FromArgb(200, 60, 60),
                "Transform" => Color.FromArgb(160, 100, 200),
                "Topology" => Color.FromArgb(220, 140, 40),
                "Operations" => Color.FromArgb(40, 160, 170),
                "Sketch" => Color.FromArgb(200, 120, 60),
                "Measure" => Color.FromArgb(100, 170, 120),
                "Vector" => Color.FromArgb(180, 140, 60),
                "Utility" => Color.FromArgb(140, 140, 140),
                _ => Color.Gray
            };
            // Center vertically within item bounds
            int dotSize = 12;
            int dotY = e.Bounds.Y + (e.Bounds.Height - dotSize) / 2;
            using var dotBrush = new SolidBrush(catColor);
            e.Graphics.FillEllipse(dotBrush, e.Bounds.X + 8, dotY, dotSize, dotSize);

            // Category label — right-aligned, vertically centered
            using var catFont = new Font("Segoe UI", 8.5F);
            using var catBrush = new SolidBrush(Color.FromArgb(120, 120, 120));
            var catSize = e.Graphics.MeasureString(reg.Category, catFont);
            float catX = e.Bounds.Right - catSize.Width - 10;
            float catY = e.Bounds.Y + (e.Bounds.Height - catSize.Height) / 2;
            e.Graphics.DrawString(reg.Category, catFont, catBrush, catX, catY);

            // Node name — vertically centered, clipped between dot and category
            using var font = new Font("Segoe UI", 10F);
            using var textBrush = new SolidBrush(Color.FromArgb(40, 40, 40));
            float nameX = e.Bounds.X + 28;
            float nameMaxW = catX - nameX - 6;
            var nameSize = e.Graphics.MeasureString(reg.DisplayName, font);
            float nameY = e.Bounds.Y + (e.Bounds.Height - nameSize.Height) / 2;
            var nameRect = new RectangleF(nameX, nameY, nameMaxW, nameSize.Height);
            using var nameSf = new StringFormat { Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap };
            e.Graphics.DrawString(reg.DisplayName, font, textBrush, nameRect, nameSf);

            e.Graphics.ResetClip();
        }

        /// <summary>
        /// Shows the popup at the specified screen location and resets the search.
        /// </summary>
        public void ShowAt(Point screenLocation)
        {
            Location = screenLocation;
            _searchBox.Text = "";
            RefreshResults("");
            Show();
            _searchBox.Focus();
        }
    }
}
