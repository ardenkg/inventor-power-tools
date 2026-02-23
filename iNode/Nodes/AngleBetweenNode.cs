// ============================================================================
// iNode Add-in for Autodesk Inventor 2026
// AngleBetweenNode.cs - Measure angle between two vectors
// ============================================================================

using System;
using iNode.Core;

namespace iNode.Nodes
{
    /// <summary>
    /// Measures the angle between two direction vectors.
    /// Output is in degrees.
    /// </summary>
    public class AngleBetweenNode : Node
    {
        public override string TypeName => "AngleBetween";
        public override string Title => "Angle Between";
        public override string Category => "Measure";

        public AngleBetweenNode()
        {
            AddInput("A", "Vector A", PortDataType.Point3D, (1.0, 0.0, 0.0));
            AddInput("B", "Vector B", PortDataType.Point3D, (0.0, 1.0, 0.0));
            AddOutput("Angle", "Angle (\u00B0)", PortDataType.Number);
        }

        protected override void Compute()
        {
            var a = GetInput("A")!.GetPoint3D();
            var b = GetInput("B")!.GetPoint3D();

            double lenA = Math.Sqrt(a.X * a.X + a.Y * a.Y + a.Z * a.Z);
            double lenB = Math.Sqrt(b.X * b.X + b.Y * b.Y + b.Z * b.Z);

            if (lenA < 1e-10 || lenB < 1e-10)
            {
                HasError = true;
                ErrorMessage = "Vectors must be non-zero";
                return;
            }

            double dot = (a.X * b.X + a.Y * b.Y + a.Z * b.Z) / (lenA * lenB);
            dot = Math.Max(-1, Math.Min(1, dot)); // clamp for numerical safety

            GetOutput("Angle")!.Value = Math.Acos(dot) * 180.0 / Math.PI;
        }
    }
}
