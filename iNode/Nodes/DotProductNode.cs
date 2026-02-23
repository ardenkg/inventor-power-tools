// ============================================================================
// iNode Add-in for Autodesk Inventor 2026
// DotProductNode.cs - Dot product of two vectors
// ============================================================================

using System;
using iNode.Core;

namespace iNode.Nodes
{
    /// <summary>
    /// Computes the dot product of two 3D vectors.
    /// Result is a scalar. Also outputs the angle between vectors.
    /// </summary>
    public class DotProductNode : Node
    {
        public override string TypeName => "DotProduct";
        public override string Title => "Dot Product";
        public override string Category => "Vector";

        public DotProductNode()
        {
            AddInput("A", "Vector A", PortDataType.Point3D, (1.0, 0.0, 0.0));
            AddInput("B", "Vector B", PortDataType.Point3D, (0.0, 1.0, 0.0));
            AddOutput("Result", "Result", PortDataType.Number);
            AddOutput("Angle", "Angle (\u00B0)", PortDataType.Number);
        }

        protected override void Compute()
        {
            var a = GetInput("A")!.GetPoint3D();
            var b = GetInput("B")!.GetPoint3D();

            double dot = a.X * b.X + a.Y * b.Y + a.Z * b.Z;
            GetOutput("Result")!.Value = dot;

            double lenA = Math.Sqrt(a.X * a.X + a.Y * a.Y + a.Z * a.Z);
            double lenB = Math.Sqrt(b.X * b.X + b.Y * b.Y + b.Z * b.Z);
            if (lenA > 1e-10 && lenB > 1e-10)
            {
                double cosAngle = Math.Max(-1, Math.Min(1, dot / (lenA * lenB)));
                GetOutput("Angle")!.Value = Math.Acos(cosAngle) * 180.0 / Math.PI;
            }
            else
            {
                GetOutput("Angle")!.Value = 0.0;
            }
        }
    }
}
