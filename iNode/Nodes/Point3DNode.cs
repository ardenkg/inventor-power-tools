// ============================================================================
// iNode Add-in for Autodesk Inventor 2026
// Point3DNode.cs - 3D point input node
// ============================================================================

using System;
using System.Collections.Generic;
using iNode.Core;

namespace iNode.Nodes
{
    /// <summary>
    /// A 3D point input node. User provides X, Y, Z coordinates via
    /// number inputs or editable parameters.
    /// </summary>
    public class Point3DNode : Node
    {
        public override string TypeName => "Point3D";
        public override string Title => "Point (X,Y,Z)";
        public override string Category => "Input";

        public Point3DNode()
        {
            AddInput("X", "X (mm)", PortDataType.Number, 0.0);
            AddInput("Y", "Y (mm)", PortDataType.Number, 0.0);
            AddInput("Z", "Z (mm)", PortDataType.Number, 0.0);
            AddOutput("Point", "Point", PortDataType.Point3D);
        }

        protected override void Compute()
        {
            double x = GetInput("X")!.GetDouble();
            double y = GetInput("Y")!.GetDouble();
            double z = GetInput("Z")!.GetDouble();
            GetOutput("Point")!.Value = (x, y, z);
        }
    }
}
