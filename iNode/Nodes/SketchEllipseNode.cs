// ============================================================================
// iNode Add-in for Autodesk Inventor 2026
// SketchEllipseNode.cs - Elliptical sketch profile
// ============================================================================

using System;
using System.Collections.Generic;
using iNode.Core;

namespace iNode.Nodes
{
    /// <summary>
    /// Creates an elliptical profile defined by major and minor radii.
    /// </summary>
    public class SketchEllipseNode : Node
    {
        public override string TypeName => "SketchEllipse";
        public override string Title => "Sketch Ellipse";
        public override string Category => "Sketch";

        public SketchEllipseNode()
        {
            AddOptionalInput("Plane", "Plane", PortDataType.WorkPlane, null);
            AddInput("CenterX", "Center X (mm)", PortDataType.Number, 0.0);
            AddInput("CenterY", "Center Y (mm)", PortDataType.Number, 0.0);
            AddInput("MajorRadius", "Major Radius (mm)", PortDataType.Number, 10.0);
            AddInput("MinorRadius", "Minor Radius (mm)", PortDataType.Number, 5.0);
            AddOutput("Profile", "Profile", PortDataType.Profile);
        }

        protected override void Compute()
        {
            var cx = GetInput("CenterX")!.GetDouble(0);
            var cy = GetInput("CenterY")!.GetDouble(0);
            var majorR = GetInput("MajorRadius")!.GetDouble(10);
            var minorR = GetInput("MinorRadius")!.GetDouble(5);

            if (majorR <= 0 || minorR <= 0)
            {
                HasError = true;
                ErrorMessage = "Both radii must be positive";
                return;
            }

            var planeObj = GetInput("Plane")?.Value;
            PlaneData? plane = planeObj as PlaneData;
            if (plane == null) plane = PlaneData.XY();

            var profile = new SketchProfileData
            {
                Plane = plane,
                Curves = new List<ProfileCurve>
                {
                    new EllipseProfileCurve
                    {
                        CenterX = cx, CenterY = cy,
                        MajorRadius = majorR, MinorRadius = minorR
                    }
                }
            };

            GetOutput("Profile")!.Value = profile;
        }
    }
}
