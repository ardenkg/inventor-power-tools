// ============================================================================
// iNode Add-in for Autodesk Inventor 2026
// DistanceNode.cs - Measure distance between two points
// ============================================================================

using System;
using iNode.Core;

namespace iNode.Nodes
{
    /// <summary>
    /// Measures the distance between two 3D points.
    /// Output is in mm.
    /// </summary>
    public class DistanceNode : Node
    {
        public override string TypeName => "Distance";
        public override string Title => "Distance";
        public override string Category => "Measure";

        public DistanceNode()
        {
            AddInput("A", "Point A", PortDataType.Point3D, (0.0, 0.0, 0.0));
            AddInput("B", "Point B", PortDataType.Point3D, (0.0, 0.0, 0.0));
            AddOutput("Distance", "Distance (mm)", PortDataType.Number);
        }

        protected override void Compute()
        {
            var a = GetInput("A")!.GetPoint3D();
            var b = GetInput("B")!.GetPoint3D();

            double dx = b.X - a.X;
            double dy = b.Y - a.Y;
            double dz = b.Z - a.Z;

            GetOutput("Distance")!.Value = Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }
    }
}
