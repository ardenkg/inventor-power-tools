// ============================================================================
// iNode Add-in for Autodesk Inventor 2026
// VectorScaleNode.cs - Scale/multiply a vector by a number
// ============================================================================

using System;
using iNode.Core;

namespace iNode.Nodes
{
    /// <summary>
    /// Multiplies a 3D vector by a scalar factor.
    /// Use to set a direction's magnitude: e.g. Origin Axis → VectorScale(50) → Move.
    /// Also outputs the resulting length.
    /// </summary>
    public class VectorScaleNode : Node
    {
        public override string TypeName => "VectorScale";
        public override string Title => "Vector Scale";
        public override string Category => "Vector";

        public VectorScaleNode()
        {
            AddInput("Vector", "Vector", PortDataType.Point3D, (1.0, 0.0, 0.0));
            AddInput("Factor", "Factor", PortDataType.Number, 1.0);
            AddOutput("Result", "Result", PortDataType.Point3D);
            AddOutput("Length", "Length", PortDataType.Number);
        }

        protected override void Compute()
        {
            var v = GetInput("Vector")!.GetPoint3D();
            double f = GetInput("Factor")!.GetDouble(1.0);

            double rx = v.X * f;
            double ry = v.Y * f;
            double rz = v.Z * f;

            GetOutput("Result")!.Value = (rx, ry, rz);
            GetOutput("Length")!.Value = Math.Sqrt(rx * rx + ry * ry + rz * rz);
        }
    }
}
