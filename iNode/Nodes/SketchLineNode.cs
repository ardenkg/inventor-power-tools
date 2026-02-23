// ============================================================================
// iNode Add-in for Autodesk Inventor 2026
// SketchLineNode.cs - Sketch line profile between two points
// ============================================================================

using System;
using System.Collections.Generic;
using iNode.Core;

namespace iNode.Nodes
{
    /// <summary>
    /// Creates a line profile between two 2D points on a plane.
    /// Use with Extrude to create thin walls or ribs.
    /// </summary>
    public class SketchLineNode : Node
    {
        public override string TypeName => "SketchLine";
        public override string Title => "Sketch Line";
        public override string Category => "Sketch";

        public SketchLineNode()
        {
            AddOptionalInput("Plane", "Plane", PortDataType.WorkPlane, null);
            AddInput("StartX", "Start X (mm)", PortDataType.Number, 0.0);
            AddInput("StartY", "Start Y (mm)", PortDataType.Number, 0.0);
            AddInput("EndX", "End X (mm)", PortDataType.Number, 10.0);
            AddInput("EndY", "End Y (mm)", PortDataType.Number, 0.0);
            AddOutput("Profile", "Profile", PortDataType.Profile);
        }

        protected override void Compute()
        {
            var planeObj = GetInput("Plane")?.Value;
            var sx = GetInput("StartX")!.GetDouble(0);
            var sy = GetInput("StartY")!.GetDouble(0);
            var ex = GetInput("EndX")!.GetDouble(10);
            var ey = GetInput("EndY")!.GetDouble(0);

            PlaneData? plane = planeObj as PlaneData;
            if (plane == null)
                plane = PlaneData.XY();

            var profile = new SketchProfileData
            {
                Plane = plane,
                Curves = new List<ProfileCurve>
                {
                    new LineProfileCurve
                    {
                        StartX = sx, StartY = sy,
                        EndX = ex, EndY = ey
                    }
                }
            };

            GetOutput("Profile")!.Value = profile;
        }
    }
}
