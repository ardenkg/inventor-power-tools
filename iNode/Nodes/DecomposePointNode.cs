// ============================================================================
// iNode Add-in for Autodesk Inventor 2026
// DecomposePointNode.cs - Decompose a Point3D into X, Y, Z components
// ============================================================================

using iNode.Core;

namespace iNode.Nodes
{
    /// <summary>
    /// Decomposes a Point3D into its X, Y, Z components.
    /// Useful for extracting individual coordinates from a point output.
    /// </summary>
    public class DecomposePointNode : Node
    {
        public override string TypeName => "DecomposePoint";
        public override string Title => "Decompose Point";
        public override string Category => "Vector";

        public DecomposePointNode()
        {
            AddInput("Point", "Point", PortDataType.Point3D, (0.0, 0.0, 0.0));
            AddOutput("X", "X", PortDataType.Number);
            AddOutput("Y", "Y", PortDataType.Number);
            AddOutput("Z", "Z", PortDataType.Number);
        }

        protected override void Compute()
        {
            var pt = GetInput("Point")!.GetPoint3D();
            GetOutput("X")!.Value = pt.X;
            GetOutput("Y")!.Value = pt.Y;
            GetOutput("Z")!.Value = pt.Z;
        }
    }
}
