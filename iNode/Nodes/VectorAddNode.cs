// ============================================================================
// iNode Add-in for Autodesk Inventor 2026
// VectorAddNode.cs - Add or subtract two vectors/points
// ============================================================================

using System;
using System.Collections.Generic;
using iNode.Core;

namespace iNode.Nodes
{
    /// <summary>
    /// Adds or subtracts two 3D vectors/points.
    /// Mode "Add": A + B
    /// Mode "Subtract": A − B (useful for computing direction between two points)
    /// </summary>
    public class VectorAddNode : Node
    {
        public override string TypeName => "VectorAdd";
        public override string Title => "Vector Add";
        public override string Category => "Vector";

        public string Mode { get; set; } = "Add";

        public VectorAddNode()
        {
            AddInput("A", "A", PortDataType.Point3D, (0.0, 0.0, 0.0));
            AddInput("B", "B", PortDataType.Point3D, (0.0, 0.0, 0.0));
            AddOutput("Result", "Result", PortDataType.Point3D);
            AddOutput("Length", "Length", PortDataType.Number);
        }

        protected override void Compute()
        {
            var a = GetInput("A")!.GetPoint3D();
            var b = GetInput("B")!.GetPoint3D();

            double rx, ry, rz;
            if (Mode == "Subtract")
            {
                rx = a.X - b.X;
                ry = a.Y - b.Y;
                rz = a.Z - b.Z;
            }
            else
            {
                rx = a.X + b.X;
                ry = a.Y + b.Y;
                rz = a.Z + b.Z;
            }

            GetOutput("Result")!.Value = (rx, ry, rz);
            GetOutput("Length")!.Value = Math.Sqrt(rx * rx + ry * ry + rz * rz);
        }

        public override string? GetDisplaySummary() => Mode == "Subtract" ? "A − B" : "A + B";

        public override List<ParameterDescriptor> GetEditableParameters()
        {
            return new List<ParameterDescriptor>
            {
                new ParameterDescriptor
                {
                    Label = "Mode",
                    Key = "Mode",
                    Value = Mode,
                    Choices = new[] { "Add", "Subtract" },
                    DisplayOnNode = Mode == "Subtract" ? "A − B" : "A + B"
                }
            };
        }

        public override Dictionary<string, object?> GetParameters()
        {
            return new Dictionary<string, object?> { ["Mode"] = Mode };
        }

        public override void SetParameters(Dictionary<string, object?> parameters)
        {
            if (parameters.TryGetValue("Mode", out var m) && m is string s)
            {
                if (s == "Add" || s == "Subtract")
                    Mode = s;
            }
        }
    }
}
