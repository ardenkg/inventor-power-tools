// ============================================================================
// iNode Add-in for Autodesk Inventor 2026
// VectorNode.cs - Creates a direction vector from two points or components
// ============================================================================

using System;
using System.Collections.Generic;
using iNode.Core;

namespace iNode.Nodes
{
    /// <summary>
    /// Creates a 3D direction vector.
    /// Mode "Components": user supplies X, Y, Z directly.
    /// Mode "TwoPoints": computes (End − Start).
    /// Outputs a Point3D tuple representing the vector.
    /// Optionally normalizes and/or scales the vector.
    /// </summary>
    public class VectorNode : Node
    {
        public override string TypeName => "Vector";
        public override string Title => "Vector";
        public override string Category => "Math";

        public string Mode { get; set; } = "Components";

        public VectorNode()
        {
            // Components mode inputs
            AddInput("X", "X", PortDataType.Number, 0.0);
            AddInput("Y", "Y", PortDataType.Number, 0.0);
            AddInput("Z", "Z", PortDataType.Number, 0.0);

            // Two-point mode inputs
            AddOptionalInput("Start", "Start", PortDataType.Point3D, null);
            AddOptionalInput("End", "End", PortDataType.Point3D, null);

            // Optional scale
            AddInput("Scale", "Scale", PortDataType.Number, 1.0);

            // Outputs
            AddOutput("Vector", "Vector", PortDataType.Point3D);
            AddOutput("Length", "Length", PortDataType.Number);
        }

        protected override void Compute()
        {
            double vx, vy, vz;

            if (Mode == "TwoPoints")
            {
                var startPt = GetInput("Start")!.GetPoint3D();
                var endPt = GetInput("End")!.GetPoint3D();

                vx = endPt.X - startPt.X;
                vy = endPt.Y - startPt.Y;
                vz = endPt.Z - startPt.Z;
            }
            else // Components
            {
                vx = GetInput("X")!.GetDouble();
                vy = GetInput("Y")!.GetDouble();
                vz = GetInput("Z")!.GetDouble();
            }

            double scale = GetInput("Scale")!.GetDouble(1.0);
            vx *= scale;
            vy *= scale;
            vz *= scale;

            double length = Math.Sqrt(vx * vx + vy * vy + vz * vz);

            GetOutput("Vector")!.Value = (vx, vy, vz);
            GetOutput("Length")!.Value = length;
        }

        public override string? GetDisplaySummary() => Mode;

        public override List<ParameterDescriptor> GetEditableParameters()
        {
            return new List<ParameterDescriptor>
            {
                new ParameterDescriptor
                {
                    Label = "Mode",
                    Key = "Mode",
                    Value = Mode,
                    Choices = new[] { "Components", "TwoPoints" },
                    DisplayOnNode = Mode
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
                if (s == "Components" || s == "TwoPoints")
                    Mode = s;
            }
        }
    }
}
