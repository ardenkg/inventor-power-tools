// ============================================================================
// iNode Add-in for Autodesk Inventor 2026
// Node.cs - Base class for all nodes in the graph
// ============================================================================

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace iNode.Core
{
    /// <summary>
    /// Base class for all nodes in the node graph.
    /// Each node has typed input/output ports and can execute an operation.
    /// </summary>
    public abstract class Node
    {
        #region Properties

        /// <summary>Unique identifier for this node instance.</summary>
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>The type name used for serialization and factory creation.</summary>
        public abstract string TypeName { get; }

        /// <summary>Display title shown in the node header.</summary>
        public abstract string Title { get; }

        /// <summary>Category for grouping in the node library menu.</summary>
        public abstract string Category { get; }

        /// <summary>Color for the node header, by category.</summary>
        public virtual Color HeaderColor => GetCategoryColor();

        /// <summary>Position of the node on the canvas (top-left corner).</summary>
        public PointF Position { get; set; } = new PointF(100, 100);

        /// <summary>Whether this node is currently selected.</summary>
        public bool IsSelected { get; set; }

        /// <summary>Whether this node has an error after last execution.</summary>
        public bool HasError { get; set; }

        /// <summary>Error message from last execution, if any.</summary>
        public string? ErrorMessage { get; set; }

        /// <summary>Whether this node was executed in the current run.</summary>
        public bool WasExecuted { get; set; }

        /// <summary>
        /// The Inventor execution context, set before Compute() is called.
        /// Nodes that create geometry use this to access TransientBRep.
        /// Null when Inventor is not connected (math-only execution).
        /// </summary>
        public InventorContext? Context { get; set; }

        /// <summary>Ordered list of input ports.</summary>
        public List<Port> Inputs { get; } = new List<Port>();

        /// <summary>Ordered list of output ports.</summary>
        public List<Port> Outputs { get; } = new List<Port>();

        /// <summary>Width of the node on the canvas. Computed dynamically from port labels.</summary>
        public virtual int NodeWidth
        {
            get
            {
                if (_cachedWidth > 0) return _cachedWidth;
                _cachedWidth = ComputeNodeWidth();
                return _cachedWidth;
            }
        }

        private int _cachedWidth = 0;

        /// <summary>
        /// Forces a recalculation of the cached node width on next access.
        /// Call when port labels change.
        /// </summary>
        public void InvalidateWidth() => _cachedWidth = 0;

        /// <summary>
        /// Computes the minimum width needed to display all port labels without truncation.
        /// </summary>
        private int ComputeNodeWidth()
        {
            const int minWidth = 500;
            const int portPadding = 180; // port circle + gap on each side (generous)
            const int headerPadding = 100;

            int maxLabelWidth = 0;

            using var font = new System.Drawing.Font("Segoe UI", 11F);

            // Measure each row: input label on left, output label on right
            int rows = Math.Max(Inputs.Count, Outputs.Count);
            for (int i = 0; i < rows; i++)
            {
                int rowWidth = 0;
                if (i < Inputs.Count)
                    rowWidth += TextWidth(Inputs[i].DisplayName, font);
                if (i < Outputs.Count)
                {
                    var outPort = Outputs[i];
                    // Output labels show concise value descriptions
                    string label = outPort.DisplayName;
                    if (outPort.Value is double dv)
                        label = $"{outPort.DisplayName}: {dv:F2}";
                    else if (outPort.Value is iNode.Nodes.BodyData bd)
                        label = bd.Description;
                    else if (outPort.Value is iNode.Nodes.FaceListData fl)
                        label = fl.Description;
                    else if (outPort.Value is iNode.Nodes.EdgeListData el)
                        label = el.Description;
                    else if (outPort.Value is iNode.Nodes.GeometryData gd)
                        label = gd.Type;
                    rowWidth += TextWidth(label, font);
                }
                if (i < Inputs.Count && i < Outputs.Count)
                    rowWidth += 50; // gap between left and right labels
                maxLabelWidth = Math.Max(maxLabelWidth, rowWidth);
            }

            // Also consider header title width
            using var hdrFont = new System.Drawing.Font("Segoe UI", 13F, System.Drawing.FontStyle.Bold);
            int headerWidth = TextWidth(Title, hdrFont) + headerPadding;

            int computed = Math.Max(maxLabelWidth + portPadding, headerWidth);
            return Math.Max(minWidth, computed);
        }

        private static int TextWidth(string text, System.Drawing.Font font)
        {
            return System.Windows.Forms.TextRenderer.MeasureText(text, font).Width;
        }

        #endregion

        #region Port Registration

        /// <summary>
        /// Adds an input port to this node.
        /// </summary>
        protected Port AddInput(string name, string displayName, PortDataType dataType, object? defaultValue = null)
        {
            var port = new Port(name, displayName, dataType, true, defaultValue) { Owner = this };
            Inputs.Add(port);
            return port;
        }

        /// <summary>
        /// Adds an optional input port — validation will not flag it when unconnected.
        /// </summary>
        protected Port AddOptionalInput(string name, string displayName, PortDataType dataType, object? defaultValue = null)
        {
            var port = new Port(name, displayName, dataType, true, defaultValue) { Owner = this, IsOptional = true };
            Inputs.Add(port);
            return port;
        }

        /// <summary>
        /// Adds an output port to this node.
        /// </summary>
        protected Port AddOutput(string name, string displayName, PortDataType dataType)
        {
            var port = new Port(name, displayName, dataType, false) { Owner = this };
            Outputs.Add(port);
            return port;
        }

        /// <summary>
        /// Gets an input port by name.
        /// </summary>
        public Port? GetInput(string name) => Inputs.FirstOrDefault(p => p.Name == name);

        /// <summary>
        /// Gets an output port by name.
        /// </summary>
        public Port? GetOutput(string name) => Outputs.FirstOrDefault(p => p.Name == name);

        #endregion

        #region Execution

        /// <summary>
        /// Called to execute this node's operation.
        /// Reads from input ports and writes results to output ports.
        /// </summary>
        public void Execute(NodeGraph graph)
        {
            HasError = false;
            ErrorMessage = null;
            WasExecuted = true;

            try
            {
                // Propagate values from connected sources;
                // reset disconnected inputs to their default/user-edited value
                // so stale connection data doesn't persist.
                foreach (var input in Inputs)
                {
                    var conn = graph.GetConnectionToInput(this, input.Name);
                    if (conn != null)
                    {
                        var sourcePort = conn.SourceNode.GetOutput(conn.SourcePortName);
                        if (sourcePort != null)
                        {
                            input.Value = sourcePort.Value;
                        }
                    }
                    else
                    {
                        input.Value = input.DefaultValue;
                    }
                }

                // Run the node-specific computation
                Compute();
            }
            catch (Exception ex)
            {
                HasError = true;
                ErrorMessage = ex.Message;
            }
        }

        /// <summary>
        /// Override in derived classes to perform the node-specific computation.
        /// Read from Inputs[].Value and write to Outputs[].Value.
        /// </summary>
        protected abstract void Compute();

        /// <summary>
        /// Resets execution state for a fresh run.
        /// </summary>
        public void ResetExecution()
        {
            WasExecuted = false;
            HasError = false;
            ErrorMessage = null;
        }

        #endregion

        #region Serialization Helpers

        /// <summary>
        /// Describes an editable parameter for the UI dialog.
        /// Nodes override GetEditableParameters() to provide this metadata.
        /// </summary>
        public class ParameterDescriptor
        {
            /// <summary>Label shown in the edit dialog.</summary>
            public string Label { get; set; } = "";

            /// <summary>Parameter key for reading/writing the value.</summary>
            public string Key { get; set; } = "";

            /// <summary>Current value as a string.</summary>
            public string Value { get; set; } = "";

            /// <summary>
            /// If non-null, the parameter should be shown as a dropdown
            /// with these choices. Null means show a text field.
            /// </summary>
            public string[]? Choices { get; set; }

            /// <summary>
            /// Short display text to show on the node face (e.g., "Union", "All").
            /// Null means don't show on the node body.
            /// </summary>
            public string? DisplayOnNode { get; set; }
        }

        /// <summary>
        /// Returns descriptors for all editable parameters.
        /// Override in derived classes to supply dropdown choices and display hints.
        /// The default implementation creates text fields for all numeric inputs.
        /// </summary>
        public virtual List<ParameterDescriptor> GetEditableParameters()
        {
            var result = new List<ParameterDescriptor>();
            var parms = GetParameters();
            foreach (var kvp in parms)
            {
                if (kvp.Value is double d)
                    result.Add(new ParameterDescriptor { Label = kvp.Key + ":", Key = kvp.Key, Value = d.ToString("G") });
                else if (kvp.Value is int iv)
                    result.Add(new ParameterDescriptor { Label = kvp.Key + ":", Key = kvp.Key, Value = iv.ToString() });
                else if (kvp.Value != null)
                    result.Add(new ParameterDescriptor { Label = kvp.Key + ":", Key = kvp.Key, Value = kvp.Value.ToString() ?? "" });
            }
            return result;
        }

        /// <summary>
        /// Returns a short summary string to display on the node face
        /// below the header (e.g. "Subtract", "All edges").
        /// Returns null if nothing special should be shown.
        /// </summary>
        public virtual string? GetDisplaySummary() => null;

        /// <summary>
        /// Gets the parameter values for serialization.
        /// Override to save custom parameters (slider min/max, etc.).
        /// </summary>
        public virtual Dictionary<string, object?> GetParameters()
        {
            var dict = new Dictionary<string, object?>();
            foreach (var input in Inputs)
            {
                dict[input.Name] = input.Value;
            }
            return dict;
        }

        /// <summary>
        /// Restores parameter values from deserialization.
        /// </summary>
        public virtual void SetParameters(Dictionary<string, object?> parameters)
        {
            foreach (var kvp in parameters)
            {
                var input = GetInput(kvp.Key);
                if (input != null)
                {
                    input.Value = ConvertParameter(kvp.Value, input.DataType);
                    input.DefaultValue = input.Value;
                }
            }
        }

        /// <summary>
        /// Converts a deserialized value to the expected port type.
        /// </summary>
        protected object? ConvertParameter(object? value, PortDataType dataType)
        {
            if (value == null) return null;

            switch (dataType)
            {
                case PortDataType.Number:
                    if (value is double d) return d;
                    if (value is long l) return (double)l;
                    if (value is int i) return (double)i;
                    if (double.TryParse(value.ToString(), out double parsed)) return parsed;
                    return 0.0;

                case PortDataType.Point3D:
                    // Handle Newtonsoft JObject or similar
                    return value;

                default:
                    return value;
            }
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Calculates the total height of this node on the canvas.
        /// Must match the constants in NodeEditorCanvas.
        /// </summary>
        public int GetNodeHeight()
        {
            int headerHeight = 52;
            int portHeight = 48;
            int padding = 34;
            int portCount = Math.Max(Inputs.Count, Outputs.Count);
            return headerHeight + (portCount * portHeight) + padding;
        }

        private Color GetCategoryColor()
        {
            return Category switch
            {
                "Input" => Color.FromArgb(0, 150, 80),         // Green
                "Math" => Color.FromArgb(60, 120, 200),         // Blue
                "Logic" => Color.FromArgb(80, 100, 180),        // Indigo
                "Geometry" => Color.FromArgb(200, 60, 60),      // Red
                "Sketch" => Color.FromArgb(200, 120, 60),       // Warm brown
                "Transform" => Color.FromArgb(160, 100, 200),   // Purple
                "Topology" => Color.FromArgb(220, 140, 40),     // Orange
                "Operations" => Color.FromArgb(40, 160, 170),   // Teal
                "Measure" => Color.FromArgb(100, 170, 120),     // Sage green
                "Vector" => Color.FromArgb(180, 140, 60),       // Gold
                "Utility" => Color.FromArgb(140, 140, 140),     // Light gray
                "Output" => Color.FromArgb(220, 170, 30),       // Gold
                _ => Color.FromArgb(100, 100, 100)              // Gray
            };
        }

        #endregion
    }
}
