// ============================================================================
// iNode Add-in for Autodesk Inventor 2026
// OriginAxisNode.cs - Outputs a standard axis direction vector (X, Y, or Z)
// ============================================================================

using System;
using System.Collections.Generic;
using iNode.Core;

namespace iNode.Nodes
{
    /// <summary>
    /// Outputs a unit direction vector along a standard axis (X, Y, or Z).
    /// Useful for Move, Rotate, and other transform nodes that need a direction.
    /// </summary>
    public class OriginAxisNode : Node
    {
        public override string TypeName => "OriginAxis";
        public override string Title => "Origin Axis";
        public override string Category => "Input";

        /// <summary>Which axis: "X", "Y", or "Z".</summary>
        public string Axis { get; set; } = "X";

        public OriginAxisNode()
        {
            AddOutput("Direction", "Direction", PortDataType.Point3D);
        }

        protected override void Compute()
        {
            (double, double, double) direction;
            switch (Axis)
            {
                case "Y":
                    direction = (0.0, 1.0, 0.0);
                    break;
                case "Z":
                    direction = (0.0, 0.0, 1.0);
                    break;
                default: // "X"
                    direction = (1.0, 0.0, 0.0);
                    break;
            }

            GetOutput("Direction")!.Value = direction;
        }

        public override string? GetDisplaySummary() => $"{Axis} Axis";

        public override List<ParameterDescriptor> GetEditableParameters()
        {
            return new List<ParameterDescriptor>
            {
                new ParameterDescriptor
                {
                    Label = "Axis",
                    Key = "Axis",
                    Value = Axis,
                    Choices = new[] { "X", "Y", "Z" },
                    DisplayOnNode = $"{Axis} Axis"
                }
            };
        }

        public override Dictionary<string, object?> GetParameters()
        {
            return new Dictionary<string, object?>
            {
                ["Axis"] = Axis
            };
        }

        public override void SetParameters(Dictionary<string, object?> parameters)
        {
            if (parameters.TryGetValue("Axis", out var axis) && axis is string a)
            {
                if (a == "X" || a == "Y" || a == "Z")
                    Axis = a;
            }
        }
    }
}
