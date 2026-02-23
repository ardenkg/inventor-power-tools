// ============================================================================
// iNode Add-in for Autodesk Inventor 2026
// NodeEditorCanvas.cs - The main canvas for rendering and interacting with nodes
// ============================================================================

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using iNode.Core;
using iNode.Nodes;

namespace iNode.UI
{
    /// <summary>
    /// Custom control that renders the node graph and handles all user interaction:
    /// pan, zoom, node dragging, wire creation, selection, context menus.
    /// </summary>
    public class NodeEditorCanvas : Control
    {
        #region Constants — Colors matching CaseSelection style

        // Background & grid
        private static readonly Color BgColor = Color.FromArgb(245, 245, 245);
        private static readonly Color GridColor = Color.FromArgb(225, 225, 225);
        private static readonly Color GridMajorColor = Color.FromArgb(205, 205, 205);

        // Node colors
        private static readonly Color NodeBodyColor = Color.FromArgb(250, 250, 250);
        private static readonly Color NodeBorderColor = Color.FromArgb(180, 180, 180);
        private static readonly Color NodeSelectedBorderColor = Color.FromArgb(0, 122, 204);
        private static readonly Color NodeErrorBorderColor = Color.FromArgb(220, 50, 50);
        private static readonly Color NodeTextColor = Color.FromArgb(40, 40, 40);
        private static readonly Color NodeHeaderTextColor = Color.White;
        private static readonly Color NodeShadowColor = Color.FromArgb(40, 0, 0, 0);

        // Port colors
        private static readonly Color PortDefaultColor = Color.FromArgb(120, 120, 120);
        private static readonly Color PortHoverColor = Color.FromArgb(0, 122, 204);
        private static readonly Color PortConnectedColor = Color.FromArgb(80, 80, 80);

        // Wire colors by data type
        private static readonly Dictionary<PortDataType, Color> WireColors = new Dictionary<PortDataType, Color>
        {
            { PortDataType.Number, Color.FromArgb(80, 180, 80) },      // Green
            { PortDataType.Point3D, Color.FromArgb(200, 160, 50) },    // Gold
            { PortDataType.Geometry, Color.FromArgb(200, 60, 60) },    // Red
            { PortDataType.Face, Color.FromArgb(230, 140, 50) },       // Orange
            { PortDataType.Edge, Color.FromArgb(200, 180, 40) },       // Amber
            { PortDataType.SketchRef, Color.FromArgb(100, 150, 220) }, // Steel blue
            { PortDataType.WorkPlane, Color.FromArgb(160, 120, 200) }, // Purple
            { PortDataType.Profile, Color.FromArgb(200, 120, 60) },    // Warm brown
            { PortDataType.List, Color.FromArgb(60, 180, 180) },       // Teal/cyan
            { PortDataType.Any, Color.FromArgb(120, 120, 120) }        // Gray
        };

        // Selection box
        private static readonly Color SelectionBoxFillColor = Color.FromArgb(30, 0, 122, 204);
        private static readonly Color SelectionBoxBorderColor = Color.FromArgb(100, 0, 122, 204);

        // Layout
        private const int PORT_RADIUS = 9;
        private const int PORT_HIT_RADIUS = 22;
        private const int NODE_HEADER_HEIGHT = 52;
        private const int NODE_PORT_HEIGHT = 48;
        private const int NODE_PADDING = 34;
        private const int NODE_CORNER_RADIUS = 10;
        private const int GRID_SIZE = 20;
        private const int GRID_MAJOR_EVERY = 5;
        private const float MIN_ZOOM = 0.25f;
        private const float MAX_ZOOM = 2.0f;

        #endregion

        #region State

        private NodeGraph _graph = new NodeGraph();

        // View transform
        private float _zoom = 1.0f;
        private PointF _panOffset = new PointF(0, 0);

        // Interaction state
        private enum InteractionMode
        {
            None,
            PanningCanvas,
            DraggingNodes,
            DraggingSlider,
            CreatingWire,
            BoxSelecting
        }

        private InteractionMode _mode = InteractionMode.None;
        private PointF _mouseDownWorld;       // Mouse-down position in world coords
        private PointF _mouseDownScreen;      // Mouse-down position in screen coords
        private PointF _lastMouseScreen;      // Last mouse position in screen coords

        // Wire creation state
        private Node? _wireSourceNode;
        private Port? _wireSourcePort;
        private bool _wireFromOutput;         // true = dragging from output, false = from input
        private PointF _wireEndWorld;         // Current end position of the wire being created

        // Box selection
        private RectangleF _selectionBox;

        // Hover state
        private Node? _hoveredNode;
        private Port? _hoveredPort;
        private Connection? _hoveredConnection;

        // In-node editing (uses modal dialog now)
        private Node? _editingNode;
        private NumberSliderNode? _draggingSlider;

        // Clipboard
        private List<(string TypeName, PointF RelativePos, Dictionary<string, object?> Params)>? _clipboard;

        // Zoom indicator
        private int _zoomIndicatorAlpha = 0;
        private System.Windows.Forms.Timer? _zoomFadeTimer;

        // Zoom debounce
        private bool _isZooming;
        private System.Windows.Forms.Timer? _zoomDebounceTimer;

        // Cached GDI resources (recreated only when zoom changes)
        private float _cachedFontZoom = -1;
        private Font _headerFont = null!;
        private Font _portLabelFont = null!;
        private Font _valueFont = null!;
        private Font _smallValueFont = null!;
        private static readonly SolidBrush _headerTextBrushCached = new SolidBrush(Color.White);
        private static readonly SolidBrush _nodeTextBrushCached = new SolidBrush(Color.FromArgb(40, 40, 40));
        private static readonly SolidBrush _bodyBrushCached = new SolidBrush(Color.FromArgb(250, 250, 250));
        private static readonly SolidBrush _shadowBrushCached = new SolidBrush(Color.FromArgb(40, 0, 0, 0));
        private static readonly SolidBrush _portDefaultBrush = new SolidBrush(PortDefaultColor);
        private static readonly SolidBrush _portHoverBrush = new SolidBrush(PortHoverColor);
        private static readonly SolidBrush _portConnectedBrush = new SolidBrush(PortConnectedColor);
        private static readonly Pen _portOutlinePen = new Pen(Color.FromArgb(60, 60, 60), 1);
        private static readonly StringFormat _headerSf = new StringFormat
        {
            Alignment = StringAlignment.Near,
            LineAlignment = StringAlignment.Center,
            Trimming = StringTrimming.EllipsisCharacter
        };
        private static readonly StringFormat _inputLabelSf = new StringFormat
        {
            Alignment = StringAlignment.Near,
            LineAlignment = StringAlignment.Center,
            Trimming = StringTrimming.EllipsisCharacter
        };
        private static readonly StringFormat _outputLabelSf = new StringFormat
        {
            Alignment = StringAlignment.Far,
            LineAlignment = StringAlignment.Center,
            Trimming = StringTrimming.EllipsisCharacter
        };

        // Context menu
        private ContextMenuStrip _nodeContextMenu = null!;
        private ContextMenuStrip _canvasContextMenu = null!;
        private PointF _contextMenuWorldPos;

        #endregion

        #region Properties

        public NodeGraph Graph
        {
            get => _graph;
            set
            {
                _graph = value;
                Invalidate();
            }
        }

        public float Zoom => _zoom;

        /// <summary>Event raised when the user wants to see the node search popup.</summary>
        public event EventHandler<PointF>? NodeSearchRequested;

        /// <summary>Event raised when the graph is modified.</summary>
        public event EventHandler? GraphModified;

        /// <summary>Whether an inline editor is currently open (always false now — uses modal dialog).</summary>
        public bool IsEditing => false;

        #endregion

        #region Constructor

        public NodeEditorCanvas()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw, true);

            BackColor = BgColor;
            Cursor = Cursors.Arrow;

            BuildContextMenus();
            EnsureCachedFonts();

            // Zoom indicator fade timer
            _zoomFadeTimer = new System.Windows.Forms.Timer { Interval = 50 };
            _zoomFadeTimer.Tick += (s, e) =>
            {
                _zoomIndicatorAlpha = Math.Max(0, _zoomIndicatorAlpha - 10);
                if (_zoomIndicatorAlpha <= 0) _zoomFadeTimer.Stop();
                Invalidate();
            };

            // Zoom debounce timer — restore grid after zoom stops
            _zoomDebounceTimer = new System.Windows.Forms.Timer { Interval = 200 };
            _zoomDebounceTimer.Tick += (s, e) =>
            {
                _isZooming = false;
                _zoomDebounceTimer!.Stop();
                Invalidate();
            };
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _zoomFadeTimer?.Dispose();
                _zoomDebounceTimer?.Dispose();
                _nodeContextMenu?.Dispose();
                _canvasContextMenu?.Dispose();
                _headerFont?.Dispose();
                _portLabelFont?.Dispose();
                _valueFont?.Dispose();
                _smallValueFont?.Dispose();
            }
            base.Dispose(disposing);
        }

        #endregion

        #region Coordinate Transforms

        /// <summary>
        /// Recreates cached font objects when zoom level changes.
        /// Avoids creating Font objects on every paint frame per node.
        /// </summary>
        private void EnsureCachedFonts()
        {
            if (Math.Abs(_cachedFontZoom - _zoom) > 0.0001f)
            {
                _headerFont?.Dispose();
                _portLabelFont?.Dispose();
                _valueFont?.Dispose();
                _smallValueFont?.Dispose();
                _headerFont = new Font("Segoe UI", 13F * _zoom, FontStyle.Bold);
                _portLabelFont = new Font("Segoe UI", 11F * _zoom);
                _valueFont = new Font("Segoe UI", 10F * _zoom);
                _smallValueFont = new Font("Segoe UI", 8.5F * _zoom);
                _cachedFontZoom = _zoom;
            }
        }

        private PointF ScreenToWorld(PointF screen)
        {
            return new PointF(
                (screen.X - _panOffset.X) / _zoom,
                (screen.Y - _panOffset.Y) / _zoom);
        }

        private PointF WorldToScreen(PointF world)
        {
            return new PointF(
                world.X * _zoom + _panOffset.X,
                world.Y * _zoom + _panOffset.Y);
        }

        /// <summary>
        /// Converts a world-space point to client (control) coordinates.
        /// Public so the form can position popups correctly.
        /// </summary>
        public PointF WorldToClient(PointF world)
        {
            return WorldToScreen(world);
        }

        private RectangleF WorldToScreen(RectangleF world)
        {
            var tl = WorldToScreen(new PointF(world.X, world.Y));
            return new RectangleF(tl.X, tl.Y, world.Width * _zoom, world.Height * _zoom);
        }

        #endregion

        #region Painting

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;

            bool isDragging = _mode == InteractionMode.DraggingNodes || _mode == InteractionMode.PanningCanvas || _isZooming;

            if (isDragging)
            {
                g.SmoothingMode = SmoothingMode.HighSpeed;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.SystemDefault;
            }
            else
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            }
            g.InterpolationMode = InterpolationMode.Low;
            g.CompositingQuality = CompositingQuality.HighSpeed;
            g.PixelOffsetMode = PixelOffsetMode.HighSpeed;

            EnsureCachedFonts();

            if (isDragging)
                g.Clear(BgColor); // Skip grid for performance during drag
            else
                DrawGrid(g);

            DrawConnections(g);
            DrawWirePreview(g);
            DrawNodes(g);
            DrawSelectionBox(g);

            if (!isDragging)
                DrawZoomIndicator(g);
        }

        private void DrawGrid(Graphics g)
        {
            var smallSize = GRID_SIZE * _zoom;
            if (smallSize < 4) return; // Don't draw grid when very zoomed out

            float ox = _panOffset.X % (GRID_SIZE * _zoom);
            float oy = _panOffset.Y % (GRID_SIZE * _zoom);

            using var pen = new Pen(GridColor, 1);
            using var penMajor = new Pen(GridMajorColor, 1);

            int gridStep = GRID_SIZE;
            int majorStep = GRID_SIZE * GRID_MAJOR_EVERY;

            for (float x = ox; x < Width; x += smallSize)
            {
                float worldX = (x - _panOffset.X) / _zoom;
                bool isMajor = Math.Abs(worldX % majorStep) < 1;
                g.DrawLine(isMajor ? penMajor : pen, x, 0, x, Height);
            }

            for (float y = oy; y < Height; y += smallSize)
            {
                float worldY = (y - _panOffset.Y) / _zoom;
                bool isMajor = Math.Abs(worldY % majorStep) < 1;
                g.DrawLine(isMajor ? penMajor : pen, 0, y, Width, y);
            }
        }

        private void DrawNodes(Graphics g)
        {
            foreach (var node in _graph.Nodes)
            {
                DrawNode(g, node);
            }
        }

        private void DrawNode(Graphics g, Node node)
        {
            var rect = GetNodeScreenRect(node);
            if (!rect.IntersectsWith(ClientRectangle)) return; // Culling

            float cr = NODE_CORNER_RADIUS * _zoom;
            var path = CreateRoundedRectPath(rect, cr);

            // Shadow
            g.FillPath(_shadowBrushCached, CreateRoundedRectPath(
                new RectangleF(rect.X + 2 * _zoom, rect.Y + 2 * _zoom, rect.Width, rect.Height), cr));

            // Body fill
            g.FillPath(_bodyBrushCached, path);

            // Header
            float headerH = NODE_HEADER_HEIGHT * _zoom;
            var headerRect = new RectangleF(rect.X, rect.Y, rect.Width, headerH);
            var headerPath = CreateRoundedRectPath(headerRect, cr, true, true, false, false);
            using (var headerBrush = new SolidBrush(node.HeaderColor))
                g.FillPath(headerBrush, headerPath);
            headerPath.Dispose();

            // Header text — vertically centered with padding
            var titleRect = new RectangleF(rect.X + 12 * _zoom, rect.Y + 2 * _zoom, rect.Width - 24 * _zoom, headerH - 4 * _zoom);
            g.DrawString(node.Title, _headerFont, _headerTextBrushCached, titleRect, _headerSf);

            // Display summary (e.g. "Union", "All") on the header right side
            var summary = node.GetDisplaySummary();
            if (!string.IsNullOrEmpty(summary))
            {
                using var summaryFont = new Font("Segoe UI", 9F * _zoom, FontStyle.Italic);
                using var summarySf = new StringFormat { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Center };
                var summaryRect = new RectangleF(rect.X + 12 * _zoom, rect.Y + 2 * _zoom, rect.Width - 24 * _zoom, headerH - 4 * _zoom);
                using var summaryBrush = new SolidBrush(Color.FromArgb(200, 255, 255, 255));
                g.DrawString(summary, summaryFont, summaryBrush, summaryRect, summarySf);
            }

            // Border
            Color borderColor = node.HasError ? NodeErrorBorderColor :
                                node.IsSelected ? NodeSelectedBorderColor : NodeBorderColor;
            float borderWidth = (node.IsSelected || node.HasError) ? 2f * _zoom : 1f * _zoom;
            using var borderPen = new Pen(borderColor, borderWidth);
            g.DrawPath(borderPen, path);
            path.Dispose();

            // Draw ports
            DrawPorts(g, node, rect, true);  // Inputs
            DrawPorts(g, node, rect, false); // Outputs

            // Draw inline values for input nodes
            DrawInlineValues(g, node, rect);
        }

        private void DrawPorts(Graphics g, Node node, RectangleF nodeRect, bool inputs)
        {
            var ports = inputs ? node.Inputs : node.Outputs;
            float startY = nodeRect.Y + NODE_HEADER_HEIGHT * _zoom;

            for (int i = 0; i < ports.Count; i++)
            {
                var port = ports[i];
                float cy = startY + (i + 0.5f) * NODE_PORT_HEIGHT * _zoom;
                float cx = inputs ? nodeRect.Left : nodeRect.Right;

                // Port circle
                bool isHovered = _hoveredPort == port;
                bool isConnected = _graph.IsPortConnected(node, port.Name, inputs);

                float r = PORT_RADIUS * _zoom;
                var portBrush = isHovered ? _portHoverBrush :
                                isConnected ? _portConnectedBrush : _portDefaultBrush;
                g.FillEllipse(portBrush, cx - r, cy - r, r * 2, r * 2);
                g.DrawEllipse(_portOutlinePen, cx - r, cy - r, r * 2, r * 2);

                // Port label
                var labelSf = inputs ? _inputLabelSf : _outputLabelSf;

                float labelX = inputs ? cx + (PORT_RADIUS + 6) * _zoom : cx - (PORT_RADIUS + 6) * _zoom;
                // Limit label width to avoid overlap when both sides have ports
                bool hasBothSides = node.Inputs.Count > 0 && node.Outputs.Count > 0;
                float labelW = hasBothSides ? nodeRect.Width * 0.58f : nodeRect.Width * 0.85f;
                var labelRect = inputs
                    ? new RectangleF(labelX, cy - 24 * _zoom, labelW, 48 * _zoom)
                    : new RectangleF(labelX - labelW, cy - 24 * _zoom, labelW, 48 * _zoom);

                string label = port.DisplayName;

                // Show value on output ports
                if (!inputs && port.Value != null)
                {
                    if (port.Value is double dv)
                        label = $"{port.DisplayName}: {dv:F2}";
                    else if (port.Value is BodyData bd)
                        label = bd.Description;
                    else if (port.Value is FaceListData fl)
                        label = fl.Description;
                    else if (port.Value is EdgeListData el)
                        label = el.Description;
                    else if (port.Value is GeometryData gd)
                        label = gd.Type;
                }

                g.DrawString(label, _portLabelFont, _nodeTextBrushCached, labelRect, labelSf);
            }
        }

        private void DrawInlineValues(Graphics g, Node node, RectangleF nodeRect)
        {
            // For slider nodes, draw the slider bar
            if (node is NumberSliderNode slider)
            {
                float startY = nodeRect.Y + NODE_HEADER_HEIGHT * _zoom;
                float sliderY = startY + node.Outputs.Count * NODE_PORT_HEIGHT * _zoom + 4 * _zoom;
                float sliderX = nodeRect.X + 10 * _zoom;
                float sliderW = nodeRect.Width - 20 * _zoom;
                float sliderH = 32 * _zoom;

                // Track background with rounded corners
                var trackRect = new RectangleF(sliderX, sliderY, sliderW, sliderH);
                float sliderCr = 4 * _zoom;
                var trackPath = CreateRoundedRectPath(trackRect, sliderCr);
                using var trackBrush = new SolidBrush(Color.FromArgb(205, 210, 218));
                g.FillPath(trackBrush, trackPath);

                // Fill bar
                float range = (float)(slider.Max - slider.Min);
                float pct = range > 0 ? (float)((slider.CurrentValue - slider.Min) / range) : 0;
                if (pct > 0.001f)
                {
                    float fillW = Math.Max(sliderCr * 2, sliderW * pct);
                    var fillRect = new RectangleF(sliderX, sliderY, fillW, sliderH);
                    using var fillPath = CreateRoundedRectPath(fillRect, sliderCr, true, pct > 0.95f, pct > 0.95f, true);
                    using var fillBrush = new SolidBrush(Color.FromArgb(0, 122, 204));
                    g.FillPath(fillBrush, fillPath);
                    fillPath.Dispose();
                }

                // Border
                using var borderPen = new Pen(Color.FromArgb(140, 150, 165), 1 * _zoom);
                g.DrawPath(borderPen, trackPath);
                trackPath.Dispose();

                // Value text — small readable text on slider
                var valStr = $"{slider.CurrentValue:F1}   [{slider.Min} - {slider.Max}]";
                var valRect = new RectangleF(sliderX, sliderY, sliderW, sliderH);
                using var valSf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                using var sliderValFont = new Font("Segoe UI", 8.5F * _zoom, FontStyle.Bold);
                // Dark shadow for contrast against the blue fill
                using var valShadowBrush = new SolidBrush(Color.FromArgb(200, 0, 0, 0));
                g.DrawString(valStr, sliderValFont, valShadowBrush,
                    new RectangleF(valRect.X + 1, valRect.Y + 1, valRect.Width, valRect.Height), valSf);
                // White foreground
                using var valFgBrush = new SolidBrush(Color.White);
                g.DrawString(valStr, sliderValFont, valFgBrush, valRect, valSf);
            }

            // For number nodes, show value
            if (node is NumberNode numNode)
            {
                float startY = nodeRect.Y + NODE_HEADER_HEIGHT * _zoom;
                float valY = startY + node.Outputs.Count * NODE_PORT_HEIGHT * _zoom + 2 * _zoom;
                float valX = nodeRect.X + 10 * _zoom;
                float valW = nodeRect.Width - 20 * _zoom;

                g.DrawString($"= {numNode.CurrentValue}", _valueFont, _nodeTextBrushCached, valX, valY);
            }

            // For Point3D nodes, show X,Y,Z from input ports
            if (node is Point3DNode ptNode)
            {
                float startY = nodeRect.Y + NODE_HEADER_HEIGHT * _zoom;
                float valY = startY + node.Outputs.Count * NODE_PORT_HEIGHT * _zoom + 2 * _zoom;
                float valX = nodeRect.X + 10 * _zoom;
                double px = ptNode.GetInput("X")?.GetDouble() ?? 0;
                double py = ptNode.GetInput("Y")?.GetDouble() ?? 0;
                double pz = ptNode.GetInput("Z")?.GetDouble() ?? 0;
                g.DrawString($"X={px:F1}  Y={py:F1}  Z={pz:F1}", _smallValueFont, _nodeTextBrushCached, valX, valY);
            }

            // For Boolean nodes, show operation type
            if (node is BooleanNode boolNode)
            {
                float startY = nodeRect.Y + NODE_HEADER_HEIGHT * _zoom;
                int portCount = Math.Max(node.Inputs.Count, node.Outputs.Count);
                float valY = startY + portCount * NODE_PORT_HEIGHT * _zoom + 4 * _zoom;
                float valX = nodeRect.X + 10 * _zoom;
                float valW = nodeRect.Width - 20 * _zoom;

                using var opBrush = new SolidBrush(Color.FromArgb(50, 50, 50));
                g.DrawString($"Operation: {boolNode.Operation}", _valueFont, opBrush, valX, valY);
            }

            // For SelectEdge/SelectFace nodes, show mode
            if (node is SelectEdgeNode selEdge)
            {
                float startY = nodeRect.Y + NODE_HEADER_HEIGHT * _zoom;
                int portCount = Math.Max(node.Inputs.Count, node.Outputs.Count);
                float valY = startY + portCount * NODE_PORT_HEIGHT * _zoom + 4 * _zoom;
                float valX = nodeRect.X + 10 * _zoom;

                using var opBrush = new SolidBrush(Color.FromArgb(50, 50, 50));
                g.DrawString($"Mode: {selEdge.Mode}", _valueFont, opBrush, valX, valY);
            }

            if (node is SelectFaceNode selFace)
            {
                float startY = nodeRect.Y + NODE_HEADER_HEIGHT * _zoom;
                int portCount = Math.Max(node.Inputs.Count, node.Outputs.Count);
                float valY = startY + portCount * NODE_PORT_HEIGHT * _zoom + 4 * _zoom;
                float valX = nodeRect.X + 10 * _zoom;

                using var opBrush = new SolidBrush(Color.FromArgb(50, 50, 50));
                g.DrawString($"Mode: {selFace.Mode}", _valueFont, opBrush, valX, valY);
            }

            // For Note nodes, draw the note text in the body area
            if (node is NoteNode noteNode)
            {
                float startY = nodeRect.Y + NODE_HEADER_HEIGHT * _zoom + 8 * _zoom;
                float valX = nodeRect.X + 14 * _zoom;
                float valW = nodeRect.Width - 28 * _zoom;
                float valH = nodeRect.Height - NODE_HEADER_HEIGHT * _zoom - 16 * _zoom;

                var noteRect = new RectangleF(valX, startY, valW, valH);
                using var noteSf = new StringFormat
                {
                    Alignment = StringAlignment.Near,
                    LineAlignment = StringAlignment.Near,
                    Trimming = StringTrimming.EllipsisWord,
                    FormatFlags = 0  // allow word wrap
                };
                using var noteBrush = new SolidBrush(Color.FromArgb(70, 70, 70));
                g.DrawString(noteNode.NoteText, _valueFont, noteBrush, noteRect, noteSf);
            }

            // For ListViewer nodes, draw the tree structure in the body area
            if (node is ListViewerNode listViewer && !string.IsNullOrEmpty(listViewer.StructureText))
            {
                float portSectionH = Math.Max(node.Inputs.Count, node.Outputs.Count) * NODE_PORT_HEIGHT * _zoom;
                float startY = nodeRect.Y + NODE_HEADER_HEIGHT * _zoom + portSectionH + 4 * _zoom;
                float valX = nodeRect.X + 14 * _zoom;
                float valW = nodeRect.Width - 28 * _zoom;
                float valH = nodeRect.Height - NODE_HEADER_HEIGHT * _zoom - portSectionH - 12 * _zoom;

                var lvRect = new RectangleF(valX, startY, valW, Math.Max(valH, 20));
                using var lvSf = new StringFormat
                {
                    Alignment = StringAlignment.Near,
                    LineAlignment = StringAlignment.Near,
                    Trimming = StringTrimming.EllipsisCharacter,
                    FormatFlags = StringFormatFlags.NoWrap
                };

                // Draw each line of the tree structure using monospace-like font
                using var lvBrush = new SolidBrush(Color.FromArgb(55, 55, 55));
                using var lvFont = new Font("Consolas", 9f * _zoom, FontStyle.Regular);
                string[] lines = listViewer.StructureText.Split('\n');
                float lineH = 16 * _zoom;
                for (int li = 0; li < lines.Length && startY + li * lineH < nodeRect.Bottom - 4 * _zoom; li++)
                {
                    g.DrawString(lines[li], lvFont, lvBrush, valX, startY + li * lineH);
                }
            }

            // For Display nodes with multi-line text, draw in body
            if (node is DisplayNode dispNode && !string.IsNullOrEmpty(dispNode.DisplayText) && dispNode.DisplayText.Contains("\n"))
            {
                float portSectionH = Math.Max(node.Inputs.Count, node.Outputs.Count) * NODE_PORT_HEIGHT * _zoom;
                float startY = nodeRect.Y + NODE_HEADER_HEIGHT * _zoom + portSectionH + 4 * _zoom;
                float valX = nodeRect.X + 14 * _zoom;
                float valW = nodeRect.Width - 28 * _zoom;
                float valH = nodeRect.Height - NODE_HEADER_HEIGHT * _zoom - portSectionH - 12 * _zoom;

                var dispRect = new RectangleF(valX, startY, valW, Math.Max(valH, 20));
                using var dispBrush = new SolidBrush(Color.FromArgb(55, 55, 55));
                using var dispFont = new Font("Consolas", 9f * _zoom, FontStyle.Regular);
                string[] lines = dispNode.DisplayText.Split('\n');
                float lineH = 16 * _zoom;
                for (int li = 0; li < lines.Length && startY + li * lineH < nodeRect.Bottom - 4 * _zoom; li++)
                {
                    g.DrawString(lines[li], dispFont, dispBrush, valX, startY + li * lineH);
                }
            }

        }

        private void DrawConnections(Graphics g)
        {
            foreach (var conn in _graph.Connections)
            {
                var srcNode = conn.SourceNode;
                var tgtNode = conn.TargetNode;
                var srcPort = srcNode.GetOutput(conn.SourcePortName);
                var tgtPort = tgtNode.GetInput(conn.TargetPortName);
                if (srcPort == null || tgtPort == null) continue;

                var p1 = GetPortScreenPosition(srcNode, srcPort, false);
                var p2 = GetPortScreenPosition(tgtNode, tgtPort, true);

                var dataType = conn.GetDataType();
                Color wireColor = WireColors.ContainsKey(dataType) ? WireColors[dataType] : WireColors[PortDataType.Any];
                bool isHovered = _hoveredConnection == conn;

                float thickness = (isHovered ? 4.5f : 3f) * _zoom;
                using var pen = new Pen(wireColor, thickness);
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;
                DrawBezierWire(g, p1, p2, pen);
            }
        }

        private void DrawWirePreview(Graphics g)
        {
            if (_mode != InteractionMode.CreatingWire || _wireSourcePort == null) return;

            PointF start, end;
            if (_wireFromOutput)
            {
                start = GetPortScreenPosition(_wireSourceNode!, _wireSourcePort, false);
                end = WorldToScreen(_wireEndWorld);
            }
            else
            {
                end = GetPortScreenPosition(_wireSourceNode!, _wireSourcePort, true);
                start = WorldToScreen(_wireEndWorld);
            }

            var dataType = _wireSourcePort.DataType;
            Color wireColor = WireColors.ContainsKey(dataType) ? WireColors[dataType] : WireColors[PortDataType.Any];

            using var pen = new Pen(Color.FromArgb(180, wireColor), 3f * _zoom);
            pen.DashStyle = DashStyle.Dash;
            pen.StartCap = LineCap.Round;
            pen.EndCap = LineCap.Round;
            DrawBezierWire(g, start, end, pen);
        }

        private void DrawBezierWire(Graphics g, PointF start, PointF end, Pen pen)
        {
            float dx = Math.Abs(end.X - start.X);
            float dy = Math.Abs(end.Y - start.Y);
            float tangent = Math.Max(80 * _zoom, Math.Max(dx * 0.6f, dy * 0.3f));

            var cp1 = new PointF(start.X + tangent, start.Y);
            var cp2 = new PointF(end.X - tangent, end.Y);

            g.DrawBezier(pen, start, cp1, cp2, end);
        }

        private void DrawSelectionBox(Graphics g)
        {
            if (_mode != InteractionMode.BoxSelecting) return;

            using var fill = new SolidBrush(SelectionBoxFillColor);
            using var border = new Pen(SelectionBoxBorderColor, 1);

            var screenBox = GetSelectionBoxScreen();
            g.FillRectangle(fill, screenBox);
            g.DrawRectangle(border, screenBox.X, screenBox.Y, screenBox.Width, screenBox.Height);
        }

        private void DrawZoomIndicator(Graphics g)
        {
            if (_zoomIndicatorAlpha <= 0) return;

            string text = $"{(_zoom * 100):F0}%";
            using var font = new Font("Segoe UI", 11F, FontStyle.Bold);
            using var brush = new SolidBrush(Color.FromArgb(_zoomIndicatorAlpha, 40, 40, 40));
            using var bgBrush = new SolidBrush(Color.FromArgb(_zoomIndicatorAlpha / 2, 255, 255, 255));

            var size = g.MeasureString(text, font);
            var rect = new RectangleF(Width - size.Width - 16, Height - size.Height - 12, size.Width + 8, size.Height + 4);
            g.FillRectangle(bgBrush, rect);
            g.DrawString(text, font, brush, rect.X + 4, rect.Y + 2);
        }

        #endregion

        #region Hit Testing

        private Node? HitTestNode(PointF worldPos)
        {
            // Iterate in reverse so topmost nodes are hit first
            for (int i = _graph.Nodes.Count - 1; i >= 0; i--)
            {
                var node = _graph.Nodes[i];
                var rect = GetNodeWorldRect(node);
                if (rect.Contains(worldPos))
                    return node;
            }
            return null;
        }

        private (Node node, Port port, bool isInput)? HitTestPort(PointF worldPos)
        {
            foreach (var node in _graph.Nodes)
            {
                // Check output ports
                foreach (var port in node.Outputs)
                {
                    var portPos = GetPortWorldPosition(node, port, false);
                    if (Distance(worldPos, portPos) < PORT_HIT_RADIUS)
                        return (node, port, false);
                }

                // Check input ports
                foreach (var port in node.Inputs)
                {
                    var portPos = GetPortWorldPosition(node, port, true);
                    if (Distance(worldPos, portPos) < PORT_HIT_RADIUS)
                        return (node, port, true);
                }
            }
            return null;
        }

        private Connection? HitTestConnection(PointF screenPos)
        {
            foreach (var conn in _graph.Connections)
            {
                var srcPort = conn.SourceNode.GetOutput(conn.SourcePortName);
                var tgtPort = conn.TargetNode.GetInput(conn.TargetPortName);
                if (srcPort == null || tgtPort == null) continue;

                var p1 = GetPortScreenPosition(conn.SourceNode, srcPort, false);
                var p2 = GetPortScreenPosition(conn.TargetNode, tgtPort, true);

                if (IsNearBezier(screenPos, p1, p2, 10f * _zoom))
                    return conn;
            }
            return null;
        }

        private bool IsNearBezier(PointF point, PointF start, PointF end, float threshold)
        {
            // Sample the bezier curve and check distance at each sample
            float dx = Math.Abs(end.X - start.X);
            float dy = Math.Abs(end.Y - start.Y);
            float tangent = Math.Max(80 * _zoom, Math.Max(dx * 0.6f, dy * 0.3f));
            var cp1 = new PointF(start.X + tangent, start.Y);
            var cp2 = new PointF(end.X - tangent, end.Y);

            int samples = 20;
            for (int i = 0; i <= samples; i++)
            {
                float t = i / (float)samples;
                float u = 1 - t;
                float x = u * u * u * start.X + 3 * u * u * t * cp1.X + 3 * u * t * t * cp2.X + t * t * t * end.X;
                float y = u * u * u * start.Y + 3 * u * u * t * cp1.Y + 3 * u * t * t * cp2.Y + t * t * t * end.Y;

                if (Distance(point, new PointF(x, y)) < threshold)
                    return true;
            }
            return false;
        }

        private bool HitTestSlider(Node node, PointF worldPos, out float normalizedX)
        {
            normalizedX = 0;
            if (node is not NumberSliderNode) return false;

            var nodeRect = GetNodeWorldRect(node);
            float startY = nodeRect.Y + NODE_HEADER_HEIGHT;
            float sliderY = startY + node.Outputs.Count * NODE_PORT_HEIGHT + 4;
            float sliderX = nodeRect.X + 10;
            float sliderW = nodeRect.Width - 20;
            float sliderH = 26;

            var sliderRect = new RectangleF(sliderX, sliderY, sliderW, sliderH);
            if (sliderRect.Contains(worldPos))
            {
                normalizedX = (worldPos.X - sliderX) / sliderW;
                normalizedX = Math.Max(0, Math.Min(1, normalizedX));
                return true;
            }
            return false;
        }

        #endregion

        #region Node/Port Geometry

        private RectangleF GetNodeWorldRect(Node node)
        {
            int portCount = Math.Max(node.Inputs.Count, node.Outputs.Count);
            int height = NODE_HEADER_HEIGHT + portCount * NODE_PORT_HEIGHT + NODE_PADDING;

            // Extra space for inline controls — generous to prevent clipping
            if (node is NumberSliderNode)
                height += 50;  // slider track bar
            else if (node is NumberNode)
                height += 36;  // "= value" text
            else if (node is Point3DNode)
                height += 36;  // "X= Y= Z=" text
            else if (node is BooleanNode)
                height += 30;  // operation label
            else if (node is SelectEdgeNode || node is SelectFaceNode)
                height += 30;  // mode label
            else if (node is ListItemNode || node is FilterEdgesNode || node is FilterFacesNode || node is ColorFacesNode)
                height += 30;  // mode/summary label
            else if (node is WorkPlaneNode)
                height += 30;  // plane mode label
            else if (node is ExtrudeNode || node is RevolveNode)
                height += 30;  // direction/operation label
            else if (node is ReferencePartNode)
                height += 30;  // part index label
            else if (node is VectorNode)
                height += 30;  // mode label
            // New node height adjustments for nodes with GetDisplaySummary
            else if (node is MinMaxNode || node is TrigNode || node is PowerNode || node is RoundNode || node is PINode)
                height += 30;  // math mode label
            else if (node is CompareNode || node is ConditionalNode)
                height += 30;  // logic mode label
            else if (node is HoleNode || node is SplitBodyNode || node is DraftNode)
                height += 30;  // operation mode label
            else if (node is DisplayNode displayNode)
            {
                // Add height based on display text line count
                int lineCount = (displayNode.DisplayText ?? "").Split('\n').Length;
                height += Math.Max(30, lineCount * 18 + 10);
            }
            else if (node is NoteNode)
                height += 60;  // note body text area
            else if (node is ListViewerNode lv)
            {
                // Variable height based on structure text
                int lineCount = (lv.StructureText ?? "").Split('\n').Length;
                height += Math.Max(40, lineCount * 18 + 10);
            }

            return new RectangleF(node.Position.X, node.Position.Y, node.NodeWidth, height);
        }

        private RectangleF GetNodeScreenRect(Node node)
        {
            var world = GetNodeWorldRect(node);
            return WorldToScreen(world);
        }

        private PointF GetPortWorldPosition(Node node, Port port, bool isInput)
        {
            var rect = GetNodeWorldRect(node);
            var ports = isInput ? node.Inputs : node.Outputs;
            int idx = ports.IndexOf(port);
            if (idx < 0) return PointF.Empty;

            float x = isInput ? rect.Left : rect.Right;
            float y = rect.Y + NODE_HEADER_HEIGHT + (idx + 0.5f) * NODE_PORT_HEIGHT;
            return new PointF(x, y);
        }

        private PointF GetPortScreenPosition(Node node, Port port, bool isInput)
        {
            return WorldToScreen(GetPortWorldPosition(node, port, isInput));
        }

        private RectangleF GetSelectionBoxScreen()
        {
            float x1 = Math.Min(_mouseDownScreen.X, _lastMouseScreen.X);
            float y1 = Math.Min(_mouseDownScreen.Y, _lastMouseScreen.Y);
            float x2 = Math.Max(_mouseDownScreen.X, _lastMouseScreen.X);
            float y2 = Math.Max(_mouseDownScreen.Y, _lastMouseScreen.Y);
            return new RectangleF(x1, y1, x2 - x1, y2 - y1);
        }

        #endregion

        #region Mouse Handling

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            Focus();

            var screenPos = new PointF(e.X, e.Y);
            var worldPos = ScreenToWorld(screenPos);
            _mouseDownWorld = worldPos;
            _mouseDownScreen = screenPos;
            _lastMouseScreen = screenPos;

            if (e.Button == MouseButtons.Middle ||
                (e.Button == MouseButtons.Left && ModifierKeys.HasFlag(Keys.Space)))
            {
                _mode = InteractionMode.PanningCanvas;
                Cursor = Cursors.SizeAll;
                return;
            }

            if (e.Button == MouseButtons.Left)
            {
                // Check port hit first
                var portHit = HitTestPort(worldPos);
                if (portHit.HasValue)
                {
                    var (node, port, isInput) = portHit.Value;

                    if (isInput)
                    {
                        // If input is already connected, detach and start dragging from the other end
                        var existingConn = _graph.GetConnectionToInput(node, port.Name);
                        if (existingConn != null)
                        {
                            _wireSourceNode = existingConn.SourceNode;
                            _wireSourcePort = existingConn.SourceNode.GetOutput(existingConn.SourcePortName);
                            _wireFromOutput = true;
                            _graph.RemoveConnection(existingConn);
                        }
                        else
                        {
                            _wireSourceNode = node;
                            _wireSourcePort = port;
                            _wireFromOutput = false;
                        }
                    }
                    else
                    {
                        _wireSourceNode = node;
                        _wireSourcePort = port;
                        _wireFromOutput = true;
                    }

                    _wireEndWorld = worldPos;
                    _mode = InteractionMode.CreatingWire;
                    Invalidate();
                    return;
                }

                // Check slider hit
                var hitNode = HitTestNode(worldPos);

                // Double-click to edit values — check BEFORE slider hit so
                // double-clicking a slider opens the text editor instead of dragging
                if (hitNode != null && e.Clicks == 2)
                {
                    OpenInlineEditor(hitNode, worldPos);
                    return;
                }

                if (hitNode is NumberSliderNode slider && HitTestSlider(hitNode, worldPos, out float normalizedX))
                {
                    double raw = slider.Min + normalizedX * (slider.Max - slider.Min);
                    slider.CurrentValue = slider.Step > 0 ? Math.Round(raw / slider.Step) * slider.Step : Math.Round(raw, 2);
                    _draggingSlider = slider;
                    _mode = InteractionMode.DraggingSlider;
                    GraphModified?.Invoke(this, EventArgs.Empty);
                    Invalidate();
                    return;
                }

                // Check node hit
                if (hitNode != null)
                {

                    if (!hitNode.IsSelected && !ModifierKeys.HasFlag(Keys.Control))
                        _graph.DeselectAll();

                    if (ModifierKeys.HasFlag(Keys.Control))
                        hitNode.IsSelected = !hitNode.IsSelected;
                    else
                        hitNode.IsSelected = true;

                    // Bring to front
                    _graph.Nodes.Remove(hitNode);
                    _graph.Nodes.Add(hitNode);

                    _mode = InteractionMode.DraggingNodes;
                    Invalidate();
                    return;
                }

                // Check connection hit
                var connHit = HitTestConnection(screenPos);
                if (connHit != null)
                {
                    _graph.RemoveConnection(connHit);
                    GraphModified?.Invoke(this, EventArgs.Empty);
                    Invalidate();
                    return;
                }

                // Double-click on empty canvas — open node search
                if (e.Clicks == 2)
                {
                    NodeSearchRequested?.Invoke(this, worldPos);
                    return;
                }

                // Empty canvas click — start box selection
                if (!ModifierKeys.HasFlag(Keys.Control))
                    _graph.DeselectAll();

                _mode = InteractionMode.BoxSelecting;
                Invalidate();
            }

            if (e.Button == MouseButtons.Right)
            {
                _contextMenuWorldPos = worldPos;
                var hitNodeR = HitTestNode(worldPos);
                if (hitNodeR != null)
                {
                    if (!hitNodeR.IsSelected)
                    {
                        _graph.DeselectAll();
                        hitNodeR.IsSelected = true;
                    }
                    _nodeContextMenu.Show(this, e.Location);
                }
                else
                {
                    _canvasContextMenu.Show(this, e.Location);
                }
                Invalidate();
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            var screenPos = new PointF(e.X, e.Y);
            var worldPos = ScreenToWorld(screenPos);

            switch (_mode)
            {
                case InteractionMode.PanningCanvas:
                    float dx = screenPos.X - _lastMouseScreen.X;
                    float dy = screenPos.Y - _lastMouseScreen.Y;
                    _panOffset.X += dx;
                    _panOffset.Y += dy;
                    _lastMouseScreen = screenPos;
                    Invalidate();
                    return;

                case InteractionMode.DraggingNodes:
                    float wdx = worldPos.X - _mouseDownWorld.X;
                    float wdy = worldPos.Y - _mouseDownWorld.Y;
                    foreach (var node in _graph.GetSelectedNodes())
                    {
                        node.Position = new PointF(node.Position.X + wdx, node.Position.Y + wdy);
                    }
                    _mouseDownWorld = worldPos;
                    _lastMouseScreen = screenPos;
                    Invalidate();
                    return;

                case InteractionMode.DraggingSlider:
                    if (_draggingSlider != null)
                    {
                        // Find the node rect for computing slider position
                        var sliderNode = _graph.Nodes.FirstOrDefault(n => n == _draggingSlider);
                        if (sliderNode != null)
                        {
                            var nodeRect = GetNodeWorldRect(sliderNode);
                            float slX = nodeRect.X + 10;
                            float slW = nodeRect.Width - 20;
                            float norm = Math.Max(0, Math.Min(1, (worldPos.X - slX) / slW));
                            double rawVal = _draggingSlider.Min + norm * (_draggingSlider.Max - _draggingSlider.Min);
                            double newVal = _draggingSlider.Step > 0 ? Math.Round(rawVal / _draggingSlider.Step) * _draggingSlider.Step : Math.Round(rawVal, 2);
                            if (Math.Abs(newVal - _draggingSlider.CurrentValue) > 0.001)
                            {
                                _draggingSlider.CurrentValue = newVal;
                                GraphModified?.Invoke(this, EventArgs.Empty);
                            }
                        }
                    }
                    _lastMouseScreen = screenPos;
                    Invalidate();
                    return;

                case InteractionMode.CreatingWire:
                    _wireEndWorld = worldPos;
                    _lastMouseScreen = screenPos;

                    // Highlight hovered port
                    var portHit = HitTestPort(worldPos);
                    _hoveredPort = portHit.HasValue ? portHit.Value.port : null;

                    Invalidate();
                    return;

                case InteractionMode.BoxSelecting:
                    _lastMouseScreen = screenPos;
                    // Select nodes within box
                    var boxScreen = GetSelectionBoxScreen();
                    foreach (var node in _graph.Nodes)
                    {
                        var nodeScreen = GetNodeScreenRect(node);
                        node.IsSelected = boxScreen.IntersectsWith(nodeScreen);
                    }
                    Invalidate();
                    return;
            }

            // Hover detection
            _lastMouseScreen = screenPos;
            var prevHoveredPort = _hoveredPort;
            var prevHoveredNode = _hoveredNode;
            var prevHoveredConn = _hoveredConnection;

            var ph = HitTestPort(worldPos);
            _hoveredPort = ph.HasValue ? ph.Value.port : null;
            _hoveredNode = HitTestNode(worldPos);
            _hoveredConnection = _hoveredPort == null ? HitTestConnection(screenPos) : null;

            // Update cursor
            if (_hoveredPort != null)
                Cursor = Cursors.Hand;
            else if (_hoveredConnection != null)
                Cursor = Cursors.Hand;
            else if (_hoveredNode != null)
                Cursor = Cursors.SizeAll;
            else
                Cursor = Cursors.Arrow;

            if (_hoveredPort != prevHoveredPort || _hoveredNode != prevHoveredNode || _hoveredConnection != prevHoveredConn)
                Invalidate();
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            var worldPos = ScreenToWorld(new PointF(e.X, e.Y));

            if (_mode == InteractionMode.CreatingWire && _wireSourceNode != null && _wireSourcePort != null)
            {
                // Try to connect
                var portHit = HitTestPort(worldPos);
                if (portHit.HasValue)
                {
                    var (targetNode, targetPort, isInput) = portHit.Value;

                    if (_wireFromOutput && isInput)
                    {
                        _graph.Connect(_wireSourceNode, _wireSourcePort.Name, targetNode, targetPort.Name);
                        GraphModified?.Invoke(this, EventArgs.Empty);
                    }
                    else if (!_wireFromOutput && !isInput)
                    {
                        _graph.Connect(targetNode, targetPort.Name, _wireSourceNode, _wireSourcePort.Name);
                        GraphModified?.Invoke(this, EventArgs.Empty);
                    }
                }
            }

            // Fire GraphModified on drag end so undo captures position changes
            if (_mode == InteractionMode.DraggingNodes)
            {
                GraphModified?.Invoke(this, EventArgs.Empty);
            }

            if (_mode == InteractionMode.DraggingSlider)
            {
                _draggingSlider = null;
            }

            _mode = InteractionMode.None;
            _wireSourceNode = null;
            _wireSourcePort = null;
            Cursor = Cursors.Arrow;
            Invalidate();
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);

            // Zoom towards mouse position
            var mouseScreen = new PointF(e.X, e.Y);
            var worldBefore = ScreenToWorld(mouseScreen);

            float zoomDelta = e.Delta > 0 ? 1.1f : 0.9f;
            _zoom = Math.Max(MIN_ZOOM, Math.Min(MAX_ZOOM, _zoom * zoomDelta));

            // Adjust pan so the point under the mouse stays in place
            var worldAfter = ScreenToWorld(mouseScreen);
            _panOffset.X += (worldAfter.X - worldBefore.X) * _zoom;
            _panOffset.Y += (worldAfter.Y - worldBefore.Y) * _zoom;

            // Show zoom indicator
            _zoomIndicatorAlpha = 255;
            _zoomFadeTimer?.Start();

            // Debounce zoom — hide grid during active scrolling
            _isZooming = true;
            _zoomDebounceTimer?.Stop();
            _zoomDebounceTimer?.Start();

            Invalidate();
        }

        #endregion

        #region Keyboard Handling

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            switch (keyData)
            {
                case Keys.Escape:
                    return false; // Let form handle

                case Keys.Delete:
                    DeleteSelected();
                    return true;

                case Keys.Control | Keys.A:
                    _graph.SelectAll();
                    Invalidate();
                    return true;

                case Keys.F:
                    FrameAll();
                    return true;

                case Keys.Control | Keys.C:
                    CopySelected();
                    return true;

                case Keys.Control | Keys.V:
                    PasteNodes();
                    return true;
            }

            // Let Ctrl+Z, Ctrl+Y, etc. bubble up to the Form
            return base.ProcessCmdKey(ref msg, keyData);
        }

        #endregion

        #region Context Menus

        private void BuildContextMenus()
        {
            // --- Node right-click menu ---
            _nodeContextMenu = new ContextMenuStrip();
            _nodeContextMenu.Renderer = new ToolStripProfessionalRenderer(new MenuColorTable());

            var deleteItem = new ToolStripMenuItem("Delete");
            deleteItem.Click += (s, e) => DeleteSelected();

            var duplicateItem = new ToolStripMenuItem("Duplicate");
            duplicateItem.Click += (s, e) => DuplicateSelected();

            _nodeContextMenu.Items.AddRange(new ToolStripItem[] { deleteItem, duplicateItem });

            // --- Canvas right-click menu (add nodes) ---
            _canvasContextMenu = new ContextMenuStrip();
            _canvasContextMenu.Renderer = new ToolStripProfessionalRenderer(new MenuColorTable());

            var searchItem = new ToolStripMenuItem("Search Nodes...");
            searchItem.Click += (s, e) => NodeSearchRequested?.Invoke(this, _contextMenuWorldPos);

            var runTestsItem = new ToolStripMenuItem("🧪 Run All Tests");
            runTestsItem.Click += (s, e) =>
            {
                try
                {
                    var report = NodeTestRunner.RunAllTests();
                    System.Windows.Forms.MessageBox.Show(
                        report.GetSummary(),
                        $"iNode Tests — {report.Passed}/{report.TotalTests} passed",
                        MessageBoxButtons.OK,
                        report.Failed > 0 ? MessageBoxIcon.Warning : MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    System.Windows.Forms.MessageBox.Show(
                        $"Test runner error: {ex.Message}",
                        "iNode Tests",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            };

            _canvasContextMenu.Items.Add(searchItem);
            _canvasContextMenu.Items.Add(runTestsItem);
            _canvasContextMenu.Items.Add(new ToolStripSeparator());

            // Build categorized submenu
            var categories = NodeFactory.GetByCategory();
            foreach (var cat in categories)
            {
                var catMenu = new ToolStripMenuItem(cat.Key);
                foreach (var reg in cat.Value)
                {
                    var regCapture = reg;
                    var item = new ToolStripMenuItem(reg.DisplayName);
                    item.Click += (s, e) =>
                    {
                        var node = NodeFactory.Create(regCapture.TypeName);
                        if (node != null)
                        {
                            node.Position = _contextMenuWorldPos;
                            _graph.AddNode(node);
                            GraphModified?.Invoke(this, EventArgs.Empty);
                            Invalidate();
                        }
                    };
                    catMenu.DropDownItems.Add(item);
                }
                _canvasContextMenu.Items.Add(catMenu);
            }
        }

        /// <summary>Custom color table for context menus matching CaseSelection style.</summary>
        private class MenuColorTable : ProfessionalColorTable
        {
            public override Color MenuBorder => Color.FromArgb(180, 180, 180);
            public override Color MenuItemBorder => Color.FromArgb(0, 122, 204);
            public override Color MenuItemSelected => Color.FromArgb(220, 230, 245);
            public override Color MenuItemSelectedGradientBegin => Color.FromArgb(220, 230, 245);
            public override Color MenuItemSelectedGradientEnd => Color.FromArgb(220, 230, 245);
            public override Color MenuStripGradientBegin => Color.FromArgb(250, 250, 250);
            public override Color MenuStripGradientEnd => Color.FromArgb(250, 250, 250);
            public override Color ToolStripDropDownBackground => Color.FromArgb(250, 250, 250);
            public override Color ImageMarginGradientBegin => Color.FromArgb(250, 250, 250);
            public override Color ImageMarginGradientMiddle => Color.FromArgb(250, 250, 250);
            public override Color ImageMarginGradientEnd => Color.FromArgb(250, 250, 250);
            public override Color SeparatorDark => Color.FromArgb(210, 210, 210);
            public override Color SeparatorLight => Color.FromArgb(250, 250, 250);
        }

        #endregion

        #region Actions

        public void DeleteSelected()
        {
            var selected = _graph.GetSelectedNodes();
            if (selected.Count == 0) return;

            if (selected.Count > 1)
            {
                var result = MessageBox.Show(
                    $"Delete {selected.Count} selected nodes?",
                    "Confirm Delete",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);
                if (result != DialogResult.Yes) return;
            }

            _graph.RemoveSelectedNodes();
            GraphModified?.Invoke(this, EventArgs.Empty);
            Invalidate();
        }

        public void DuplicateSelected()
        {
            var selected = _graph.GetSelectedNodes().ToList();
            _graph.DeselectAll();

            foreach (var srcNode in selected)
            {
                var newNode = NodeFactory.Create(srcNode.TypeName);
                if (newNode != null)
                {
                    newNode.Position = new PointF(srcNode.Position.X + 30, srcNode.Position.Y + 30);
                    newNode.SetParameters(srcNode.GetParameters());
                    newNode.IsSelected = true;
                    _graph.AddNode(newNode);
                }
            }

            GraphModified?.Invoke(this, EventArgs.Empty);
            Invalidate();
        }

        public void CopySelected()
        {
            var selected = _graph.GetSelectedNodes();
            if (selected.Count == 0) return;

            _clipboard = new List<(string TypeName, PointF RelativePos, Dictionary<string, object?> Params)>();
            var anchor = selected[0].Position;

            foreach (var node in selected)
            {
                _clipboard.Add((
                    node.TypeName,
                    new PointF(node.Position.X - anchor.X, node.Position.Y - anchor.Y),
                    node.GetParameters()));
            }
        }

        public void PasteNodes()
        {
            if (_clipboard == null || _clipboard.Count == 0) return;

            var center = ScreenToWorld(new PointF(Width / 2f, Height / 2f));
            _graph.DeselectAll();

            foreach (var (typeName, relPos, parms) in _clipboard)
            {
                var node = NodeFactory.Create(typeName);
                if (node == null) continue;

                node.Position = new PointF(center.X + relPos.X + 30, center.Y + relPos.Y + 30);
                if (parms != null && parms.Count > 0)
                    node.SetParameters(parms);
                node.IsSelected = true;
                _graph.AddNode(node);
            }

            GraphModified?.Invoke(this, EventArgs.Empty);
            Invalidate();
        }

        public void FrameAll()
        {
            if (_graph.Nodes.Count == 0)
            {
                _zoom = 1.0f;
                _panOffset = new PointF(Width / 2f, Height / 2f);
                Invalidate();
                return;
            }

            // Find bounding box of all nodes
            float minX = float.MaxValue, minY = float.MaxValue;
            float maxX = float.MinValue, maxY = float.MinValue;

            foreach (var node in _graph.Nodes)
            {
                var rect = GetNodeWorldRect(node);
                minX = Math.Min(minX, rect.Left);
                minY = Math.Min(minY, rect.Top);
                maxX = Math.Max(maxX, rect.Right);
                maxY = Math.Max(maxY, rect.Bottom);
            }

            float worldW = maxX - minX + 100;
            float worldH = maxY - minY + 100;

            _zoom = Math.Min((float)Width / worldW, (float)Height / worldH);
            _zoom = Math.Max(MIN_ZOOM, Math.Min(MAX_ZOOM, _zoom));

            float cx = (minX + maxX) / 2f;
            float cy = (minY + maxY) / 2f;

            _panOffset.X = Width / 2f - cx * _zoom;
            _panOffset.Y = Height / 2f - cy * _zoom;

            Invalidate();
        }

        public void ResetZoom()
        {
            _zoom = 1.0f;
            _panOffset = new PointF(50, 50);
            _zoomIndicatorAlpha = 255;
            _zoomFadeTimer?.Start();
            Invalidate();
        }

        public void AddNodeAtCenter(string typeName)
        {
            var node = NodeFactory.Create(typeName);
            if (node == null) return;

            var center = ScreenToWorld(new PointF(Width / 2f, Height / 2f));
            node.Position = center;
            _graph.DeselectAll();
            node.IsSelected = true;
            _graph.AddNode(node);
            GraphModified?.Invoke(this, EventArgs.Empty);
            Invalidate();
        }

        public void AddNodeAt(string typeName, PointF worldPos)
        {
            var node = NodeFactory.Create(typeName);
            if (node == null) return;

            node.Position = worldPos;
            _graph.DeselectAll();
            node.IsSelected = true;
            _graph.AddNode(node);
            GraphModified?.Invoke(this, EventArgs.Empty);
            Invalidate();
        }

        #endregion

        #region Inline Editing

        private void OpenInlineEditor(Node node, PointF worldPos)
        {
            _editingNode = node;

            // Use the node's own parameter metadata
            var descriptors = node.GetEditableParameters();

            // For slider nodes, add the slider-specific parameters
            if (node is NumberSliderNode slider)
            {
                descriptors = new List<Node.ParameterDescriptor>
                {
                    new Node.ParameterDescriptor { Label = "Min:", Key = "Min", Value = slider.Min.ToString("G") },
                    new Node.ParameterDescriptor { Label = "Max:", Key = "Max", Value = slider.Max.ToString("G") },
                    new Node.ParameterDescriptor { Label = "Value:", Key = "CurrentValue", Value = slider.CurrentValue.ToString("G") },
                    new Node.ParameterDescriptor { Label = "Step:", Key = "Step", Value = slider.Step.ToString("G") },
                };
            }
            else if (node is NumberNode numNode)
            {
                descriptors = new List<Node.ParameterDescriptor>
                {
                    new Node.ParameterDescriptor { Label = "Value:", Key = "CurrentValue", Value = numNode.CurrentValue.ToString("G") },
                };
            }
            else if (node is Point3DNode ptNode)
            {
                double px = ptNode.GetInput("X")?.GetDouble() ?? 0;
                double py = ptNode.GetInput("Y")?.GetDouble() ?? 0;
                double pz = ptNode.GetInput("Z")?.GetDouble() ?? 0;
                descriptors = new List<Node.ParameterDescriptor>
                {
                    new Node.ParameterDescriptor { Label = "X:", Key = "X", Value = px.ToString("G") },
                    new Node.ParameterDescriptor { Label = "Y:", Key = "Y", Value = py.ToString("G") },
                    new Node.ParameterDescriptor { Label = "Z:", Key = "Z", Value = pz.ToString("G") },
                };
            }

            if (descriptors.Count == 0) { _editingNode = null; return; }

            // Position dialog near the node on screen
            var nodeRect = GetNodeScreenRect(node);
            var screenPt = PointToScreen(new Point(
                (int)nodeRect.X,
                (int)(nodeRect.Y + nodeRect.Height + 8)));

            using var dlg = new NodeEditDialog(node.Title, descriptors, screenPt);
            var ownerForm = FindForm();
            var result = ownerForm != null ? dlg.ShowDialog(ownerForm) : dlg.ShowDialog();

            if (result == DialogResult.OK)
            {
                ApplyDialogResults(node, dlg.Results);
            }

            _editingNode = null;
        }

        private void ApplyDialogResults(Node node, Dictionary<string, string> results)
        {
            var values = new Dictionary<string, double>();
            foreach (var kvp in results)
            {
                if (double.TryParse(kvp.Value, out double val))
                    values[kvp.Key] = val;
            }

            if (node is NumberSliderNode slider)
            {
                if (values.TryGetValue("Min", out double min)) slider.Min = min;
                if (values.TryGetValue("Max", out double max)) slider.Max = max;
                if (values.TryGetValue("CurrentValue", out double cv))
                    slider.CurrentValue = Math.Max(slider.Min, Math.Min(slider.Max, cv));
                if (values.TryGetValue("Step", out double step)) slider.Step = step;
            }
            else if (node is NumberNode numNode)
            {
                if (values.TryGetValue("CurrentValue", out double cv)) numNode.CurrentValue = cv;
            }
            else if (node is Point3DNode ptNode)
            {
                if (values.TryGetValue("X", out double x)) { var p = ptNode.GetInput("X"); if (p != null) { p.Value = x; p.DefaultValue = x; } }
                if (values.TryGetValue("Y", out double y)) { var p = ptNode.GetInput("Y"); if (p != null) { p.Value = y; p.DefaultValue = y; } }
                if (values.TryGetValue("Z", out double z)) { var p = ptNode.GetInput("Z"); if (p != null) { p.Value = z; p.DefaultValue = z; } }
            }
            else if (node is BooleanNode boolApplyNode)
            {
                if (results.TryGetValue("Operation", out string? opStr) && opStr != null)
                {
                    // Dropdown provides exact values now; keep first-letter fallback
                    if (opStr == "Subtract" || opStr == "Intersect" || opStr == "Union")
                        boolApplyNode.Operation = opStr;
                    else
                    {
                        var lower = opStr.Trim().ToLowerInvariant();
                        if (lower.StartsWith("s")) boolApplyNode.Operation = "Subtract";
                        else if (lower.StartsWith("i")) boolApplyNode.Operation = "Intersect";
                        else boolApplyNode.Operation = "Union";
                    }
                }
            }
            else if (node is SelectEdgeNode selEdgeApply)
            {
                if (results.TryGetValue("Mode", out string? modeStr) && modeStr != null)
                {
                    var valid = new[] { "All", "Index", "Linear", "Circular" };
                    if (Array.Exists(valid, v => v == modeStr))
                        selEdgeApply.Mode = modeStr;
                    else
                    {
                        var lower = modeStr.Trim().ToLowerInvariant();
                        selEdgeApply.Mode = lower switch
                        {
                            var s when s.StartsWith("i") => "Index",
                            var s when s.StartsWith("l") => "Linear",
                            var s when s.StartsWith("c") => "Circular",
                            _ => "All"
                        };
                    }
                }
            }
            else if (node is SelectFaceNode selFaceApply)
            {
                if (results.TryGetValue("Mode", out string? modeStr) && modeStr != null)
                {
                    var valid = new[] { "All", "Index", "Planar", "Cylindrical" };
                    if (Array.Exists(valid, v => v == modeStr))
                        selFaceApply.Mode = modeStr;
                    else
                    {
                        var lower = modeStr.Trim().ToLowerInvariant();
                        selFaceApply.Mode = lower switch
                        {
                            var s when s.StartsWith("i") => "Index",
                            var s when s.StartsWith("p") => "Planar",
                            var s when s.StartsWith("c") => "Cylindrical",
                            _ => "All"
                        };
                    }
                }
            }
            else
            {
                // Generic parameter setting
                var parms = new Dictionary<string, object?>();
                foreach (var kvp in results)
                {
                    if (double.TryParse(kvp.Value, out double dv))
                        parms[kvp.Key] = dv;
                    else
                        parms[kvp.Key] = kvp.Value;
                }
                if (parms.Count > 0)
                    node.SetParameters(parms);
            }

            node.InvalidateWidth();
            GraphModified?.Invoke(this, EventArgs.Empty);
            Invalidate();
        }

        #endregion

        #region Helpers

        private static float Distance(PointF a, PointF b)
        {
            float dx = a.X - b.X;
            float dy = a.Y - b.Y;
            return (float)Math.Sqrt(dx * dx + dy * dy);
        }

        private static GraphicsPath CreateRoundedRectPath(RectangleF rect, float radius,
            bool topLeft = true, bool topRight = true, bool bottomRight = true, bool bottomLeft = true)
        {
            var path = new GraphicsPath();
            float d = radius * 2;

            if (topLeft)
                path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            else
                path.AddLine(rect.X, rect.Y, rect.X, rect.Y);

            if (topRight)
                path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            else
                path.AddLine(rect.Right, rect.Y, rect.Right, rect.Y);

            if (bottomRight)
                path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            else
                path.AddLine(rect.Right, rect.Bottom, rect.Right, rect.Bottom);

            if (bottomLeft)
                path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            else
                path.AddLine(rect.X, rect.Bottom, rect.X, rect.Bottom);

            path.CloseFigure();
            return path;
        }

        #endregion
    }
}
