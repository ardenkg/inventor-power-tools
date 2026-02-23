// ============================================================================
// iNode Add-in for Autodesk Inventor 2026
// MirrorNode.cs - Mirror a body across a plane
// ============================================================================

using System;
using System.Collections.Generic;
using Inventor;
using iNode.Core;

namespace iNode.Nodes
{
    /// <summary>
    /// Mirrors (reflects) a body across a plane defined by a point and normal.
    /// The normal defines which direction the plane faces.
    ///
    /// Common planes:
    ///   XY-plane: Normal = (0, 0, 1)
    ///   XZ-plane: Normal = (0, 1, 0)
    ///   YZ-plane: Normal = (1, 0, 0)
    /// </summary>
    public class MirrorNode : Node
    {
        public override string TypeName => "Mirror";
        public override string Title => "Mirror";
        public override string Category => "Transform";

        public MirrorNode()
        {
            AddInput("Body", "Body", PortDataType.Geometry, null);
            AddInput("PlanePoint", "Plane Point", PortDataType.Point3D, (0.0, 0.0, 0.0));
            AddInput("PlaneNormal", "Plane Normal", PortDataType.Point3D, (1.0, 0.0, 0.0));
            AddOutput("Body", "Body", PortDataType.Geometry);
        }

        protected override void Compute()
        {
            var geomVal = GetInput("Body")!.GetEffectiveValue();
            var planePoint = GetInput("PlanePoint")!.GetPoint3D();
            var planeNormal = GetInput("PlaneNormal")!.GetPoint3D();

            if (geomVal is not BodyData srcBody)
            {
                HasError = true;
                ErrorMessage = "No body connected";
                return;
            }

            if (Context?.IsAvailable != true || srcBody.Body == null)
            {
                GetOutput("Body")!.Value = new BodyData
                {
                    Description = "Mirror",
                    SourceNodeId = Id,
                    IsFromActivePart = srcBody.IsFromActivePart,
                    PendingOperations = new List<PendingOperation>(srcBody.PendingOperations)
                };
                return;
            }

            try
            {
                var copy = Context.TB.Copy(srcBody.Body as SurfaceBody);
                var matrix = Context.CreateMirrorMatrix(
                    planePoint.X, planePoint.Y, planePoint.Z,
                    planeNormal.X, planeNormal.Y, planeNormal.Z);
                Context.TB.Transform(copy, matrix);

                GetOutput("Body")!.Value = new BodyData
                {
                    Body = copy,
                    Description = "Mirror",
                    SourceNodeId = Id,
                    IsFromActivePart = srcBody.IsFromActivePart,
                    PendingOperations = new List<PendingOperation>(srcBody.PendingOperations)
                };
            }
            catch (Exception ex)
            {
                HasError = true;
                ErrorMessage = $"Mirror failed: {ex.Message}";
            }
        }
    }
}
