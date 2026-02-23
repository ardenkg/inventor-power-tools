// ============================================================================
// iNode Add-in for Autodesk Inventor 2026
// NormalizeNode.cs - Normalize a vector to unit length
// ============================================================================

using System;
using iNode.Core;

namespace iNode.Nodes
{
    /// <summary>
    /// Normalizes a 3D vector to unit length (magnitude = 1).
    /// Also outputs the original length.
    /// </summary>
    public class NormalizeNode : Node
    {
        public override string TypeName => "Normalize";
        public override string Title => "Normalize";
        public override string Category => "Vector";

        public NormalizeNode()
        {
            AddInput("Vector", "Vector", PortDataType.Point3D, (1.0, 0.0, 0.0));
            AddOutput("Result", "Unit Vector", PortDataType.Point3D);
            AddOutput("Length", "Original Length", PortDataType.Number);
        }

        protected override void Compute()
        {
            var v = GetInput("Vector")!.GetPoint3D();
            double len = Math.Sqrt(v.X * v.X + v.Y * v.Y + v.Z * v.Z);

            if (len < 1e-10)
            {
                HasError = true;
                ErrorMessage = "Cannot normalize zero-length vector";
                return;
            }

            GetOutput("Result")!.Value = (v.X / len, v.Y / len, v.Z / len);
            GetOutput("Length")!.Value = len;
        }
    }
}
