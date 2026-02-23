// ============================================================================
// iNode Add-in for Autodesk Inventor 2026
// Port.cs - Input and Output port definitions
// ============================================================================

using System;

namespace iNode.Core
{
    /// <summary>
    /// Represents a port on a node. Ports are connection points for wires.
    /// </summary>
    public class Port
    {
        /// <summary>Unique identifier for this port within its node.</summary>
        public string Name { get; }

        /// <summary>Display label shown in the UI.</summary>
        public string DisplayName { get; }

        /// <summary>The data type this port carries.</summary>
        public PortDataType DataType { get; }

        /// <summary>Whether this is an input (true) or output (false) port.</summary>
        public bool IsInput { get; }

        /// <summary>The node this port belongs to.</summary>
        public Node? Owner { get; set; }

        /// <summary>The current value on this port (after execution or from default).</summary>
        public object? Value { get; set; }

        /// <summary>Default value for unconnected input ports.</summary>
        public object? DefaultValue { get; set; }

        /// <summary>If true, validation will not flag this port when unconnected with no value.</summary>
        public bool IsOptional { get; set; }

        public Port(string name, string displayName, PortDataType dataType, bool isInput, object? defaultValue = null)
        {
            Name = name;
            DisplayName = displayName;
            DataType = dataType;
            IsInput = isInput;
            DefaultValue = defaultValue;
            Value = defaultValue;
        }

        /// <summary>
        /// Gets the effective value: connected source value, user-set value, or default.
        /// </summary>
        public object? GetEffectiveValue()
        {
            return Value ?? DefaultValue;
        }

        /// <summary>
        /// Gets the value as a double, or returns a fallback.
        /// </summary>
        public double GetDouble(double fallback = 0.0)
        {
            var val = GetEffectiveValue();
            if (val is double d) return d;
            if (val is int i) return i;
            if (val is float f) return f;
            if (val != null && double.TryParse(val.ToString(), out double parsed)) return parsed;
            return fallback;
        }

        /// <summary>
        /// Gets the value as a Point3D tuple, or returns origin.
        /// </summary>
        public (double X, double Y, double Z) GetPoint3D()
        {
            var val = GetEffectiveValue();
            if (val is ValueTuple<double, double, double> pt) return pt;
            if (val is Tuple<double, double, double> tpl) return (tpl.Item1, tpl.Item2, tpl.Item3);
            return (0, 0, 0);
        }
    }
}
