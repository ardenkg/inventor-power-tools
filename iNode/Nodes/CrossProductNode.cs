// ============================================================================
// iNode Add-in for Autodesk Inventor 2026
// CrossProductNode.cs - Cross product of two vectors
// ============================================================================

using System;
using iNode.Core;

namespace iNode.Nodes
{
    /// <summary>
    /// Computes the cross product of two 3D vectors.
    /// The result is a vector perpendicular to both inputs.
    /// </summary>
    public class CrossProductNode : Node
    {
        public override string TypeName => "CrossProduct";
        public override string Title => "Cross Product";
        public override string Category => "Vector";

        public CrossProductNode()
        {
            AddInput("A", "Vector A", PortDataType.Point3D, (1.0, 0.0, 0.0));
            AddInput("B", "Vector B", PortDataType.Point3D, (0.0, 1.0, 0.0));
            AddOutput("Result", "Result", PortDataType.Point3D);
            AddOutput("Length", "Length", PortDataType.Number);
        }

        protected override void Compute()
        {
            var a = GetInput("A")!.GetPoint3D();
            var b = GetInput("B")!.GetPoint3D();

            double rx = a.Y * b.Z - a.Z * b.Y;
            double ry = a.Z * b.X - a.X * b.Z;
            double rz = a.X * b.Y - a.Y * b.X;

            GetOutput("Result")!.Value = (rx, ry, rz);
            GetOutput("Length")!.Value = Math.Sqrt(rx * rx + ry * ry + rz * rz);
        }
    }
}
