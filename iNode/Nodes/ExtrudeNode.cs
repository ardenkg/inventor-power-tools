// ============================================================================
// iNode Add-in for Autodesk Inventor 2026
// ExtrudeNode.cs - Creates an extrusion from a sketch profile
// ============================================================================

using System;
using System.Collections.Generic;
using Inventor;
using iNode.Core;

namespace iNode.Nodes
{
    /// <summary>
    /// Extrudes a SketchProfileData (from SketchCircle/SketchRectangle/etc.)
    /// to create geometry. Connect a sketch node's Profile output directly.
    /// Optional Body input for Join/Cut/Intersect boolean operations.
    /// </summary>
    public class ExtrudeNode : Node
    {
        public override string TypeName => "Extrude";
        public override string Title => "Extrude";
        public override string Category => "Geometry";

        /// <summary>
        /// Direction of extrusion: "Positive", "Negative", or "Symmetric".
        /// </summary>
        public string Direction { get; set; } = "Positive";

        /// <summary>
        /// Operation type when combined with existing body: "NewBody", "Join", "Cut", "Intersect".
        /// </summary>
        public string Operation { get; set; } = "NewBody";

        public ExtrudeNode()
        {
            AddInput("Profile", "Profile", PortDataType.Profile, null);
            AddInput("Distance", "Distance (mm)", PortDataType.Number, 10.0);
            AddOptionalInput("Body", "Body", PortDataType.Geometry, null);
            AddOutput("Body", "Body", PortDataType.Geometry);
        }

        protected override void Compute()
        {
            double distance = GetInput("Distance")!.GetDouble(10.0);

            if (distance <= 0)
            {
                HasError = true;
                ErrorMessage = "Distance must be positive";
                return;
            }

            if (Context?.IsAvailable != true)
            {
                HasError = true;
                ErrorMessage = "Inventor context not available";
                return;
            }

            var profileData = GetInput("Profile")?.Value as SketchProfileData;

            if (profileData == null)
            {
                HasError = true;
                ErrorMessage = "Connect a Profile input (from a Sketch node)";
                return;
            }

            try
            {
                ComputeFromProfile(profileData, distance);
            }
            catch (Exception ex)
            {
                HasError = true;
                ErrorMessage = $"Profile extrude failed: {ex.Message}";
            }
        }

        // ====================================================================
        // Profile-based extrusion (from SketchCircle/SketchRectangle nodes)
        // Creates transient bodies for both preview and commit modes.
        // ====================================================================

        private void ComputeFromProfile(SketchProfileData profileData, double distanceMm)
        {
            var plane = profileData.Plane;
            double distCm = distanceMm * InventorContext.MM_TO_CM;

            // Compute direction vector in world coordinates
            double dirX = plane.NormalX, dirY = plane.NormalY, dirZ = plane.NormalZ;
            if (Direction == "Negative")
            {
                dirX = -dirX; dirY = -dirY; dirZ = -dirZ;
            }

            object? resultBody = null;

            foreach (var curve in profileData.Curves)
            {
                object? curvebody = null;

                if (curve is CircleProfileCurve circle)
                {
                    curvebody = CreateCylinderFromCircle(plane, circle, distCm, dirX, dirY, dirZ);
                }
                else if (curve is RectangleProfileCurve rect)
                {
                    curvebody = CreateBoxFromRectangle(plane, rect, distCm, dirX, dirY, dirZ);
                }

                if (curvebody != null)
                {
                    if (resultBody == null)
                        resultBody = curvebody;
                    else
                    {
                        // Union multiple curves into one body
                        try
                        {
                            Context!.TB.DoBoolean(
                                resultBody as SurfaceBody,
                                curvebody as SurfaceBody,
                                BooleanTypeEnum.kBooleanTypeUnion);
                        }
                        catch { /* keep first body */ }
                    }
                }
            }

            if (resultBody == null)
            {
                HasError = true;
                ErrorMessage = "Failed to create body from profile";
                return;
            }

            // For Symmetric direction, mirror the body the other way too
            if (Direction == "Symmetric")
            {
                try
                {
                    object? mirrorBody = null;
                    double negDirX = -plane.NormalX, negDirY = -plane.NormalY, negDirZ = -plane.NormalZ;

                    foreach (var curve in profileData.Curves)
                    {
                        object? cb = null;
                        if (curve is CircleProfileCurve circle)
                            cb = CreateCylinderFromCircle(plane, circle, distCm, negDirX, negDirY, negDirZ);
                        else if (curve is RectangleProfileCurve rect)
                            cb = CreateBoxFromRectangle(plane, rect, distCm, negDirX, negDirY, negDirZ);

                        if (cb != null)
                        {
                            if (mirrorBody == null)
                                mirrorBody = cb;
                            else
                                try
                                {
                                    Context!.TB.DoBoolean(
                                        mirrorBody as SurfaceBody,
                                        cb as SurfaceBody,
                                        BooleanTypeEnum.kBooleanTypeUnion);
                                }
                                catch { }
                        }
                    }

                    if (mirrorBody != null)
                    {
                        Context!.TB.DoBoolean(
                            resultBody as SurfaceBody,
                            mirrorBody as SurfaceBody,
                            BooleanTypeEnum.kBooleanTypeUnion);
                    }
                }
                catch { /* continue with single direction */ }
            }

            // Apply boolean operation with Body input if connected
            var bodyInput = GetInput("Body")?.Value as BodyData;
            if (bodyInput?.Body != null && Operation != "NewBody")
            {
                try
                {
                    BooleanTypeEnum boolType;
                    switch (Operation)
                    {
                        case "Cut":
                            boolType = BooleanTypeEnum.kBooleanTypeDifference;
                            break;
                        case "Intersect":
                            boolType = BooleanTypeEnum.kBooleanTypeIntersect;
                            break;
                        default: // Join
                            boolType = BooleanTypeEnum.kBooleanTypeUnion;
                            break;
                    }

                    // Clone the input body to avoid modifying it
                    var clonedBody = Context!.TB.Copy(bodyInput.Body as SurfaceBody);

                    if (Operation == "Cut")
                    {
                        // Body - ExtrudeShape: boolean modifies clonedBody in-place
                        Context.TB.DoBoolean(
                            clonedBody,
                            resultBody as SurfaceBody,
                            BooleanTypeEnum.kBooleanTypeDifference);
                        resultBody = clonedBody;
                    }
                    else
                    {
                        Context.TB.DoBoolean(
                            clonedBody,
                            resultBody as SurfaceBody,
                            boolType);
                        resultBody = clonedBody;
                    }
                }
                catch (Exception ex)
                {
                    HasError = true;
                    ErrorMessage = $"Boolean operation failed: {ex.Message}";
                    return;
                }
            }

            var bodyData = new BodyData
            {
                Body = resultBody,
                SourceNodeId = Id,
                Description = $"Extrude({profileData.Description}, {distanceMm}mm, {Direction})"
            };

            // Copy pending operations from input body if we did a boolean
            if (bodyInput?.Body != null && Operation != "NewBody")
            {
                bodyData.PendingOperations.AddRange(bodyInput.PendingOperations);
            }

            GetOutput("Body")!.Value = bodyData;
        }

        /// <summary>
        /// Create a cylinder from a circle profile curve placed on a plane.
        /// </summary>
        private object? CreateCylinderFromCircle(
            PlaneData plane, CircleProfileCurve circle,
            double distCm, double dirX, double dirY, double dirZ)
        {
            // Compute 3D center of the circle on the plane
            plane.To3D(circle.CenterX, circle.CenterY,
                out double cx, out double cy, out double cz);

            double radiusCm = circle.Radius * InventorContext.MM_TO_CM;

            // Bottom and top points
            var bottomPt = Context!.TG.CreatePoint(cx, cy, cz);
            var topPt = Context.TG.CreatePoint(
                cx + dirX * distCm,
                cy + dirY * distCm,
                cz + dirZ * distCm);

            return Context.TB.CreateSolidCylinderCone(
                bottomPt, topPt, radiusCm, radiusCm, radiusCm);
        }

        /// <summary>
        /// Create a box from a rectangle profile curve placed on a plane.
        /// The box is oriented along the plane's coordinate system.
        /// </summary>
        private object? CreateBoxFromRectangle(
            PlaneData plane, RectangleProfileCurve rect,
            double distCm, double dirX, double dirY, double dirZ)
        {
            double halfW = rect.Width / 2.0 * InventorContext.MM_TO_CM;
            double halfH = rect.Height / 2.0 * InventorContext.MM_TO_CM;

            // Compute 3D center of rectangle on the plane
            plane.To3D(rect.CenterX, rect.CenterY,
                out double cx, out double cy, out double cz);

            // Four corners of the rectangle in world space
            double c0x = cx - halfW * plane.XAxisX - halfH * plane.YAxisX;
            double c0y = cy - halfW * plane.XAxisY - halfH * plane.YAxisY;
            double c0z = cz - halfW * plane.XAxisZ - halfH * plane.YAxisZ;

            double c1x = cx + halfW * plane.XAxisX + halfH * plane.YAxisX;
            double c1y = cy + halfW * plane.XAxisY + halfH * plane.YAxisY;
            double c1z = cz + halfW * plane.XAxisZ + halfH * plane.YAxisZ;

            // For axis-aligned planes, create a simple box
            // For arbitrary planes, create a box along Z then transform
            bool isAxisAligned = plane.StandardPlaneIndex > 0 && Math.Abs(plane.OffsetCm) < 0.0001;

            if (plane.StandardPlaneIndex == 3 || // XY plane
                (Math.Abs(plane.NormalZ - 1.0) < 0.001 && Math.Abs(plane.NormalX) < 0.001 && Math.Abs(plane.NormalY) < 0.001))
            {
                // XY plane (or parallel): box is straightforward
                double minZ = Math.Min(cz, cz + dirZ * distCm);
                double maxZ = Math.Max(cz, cz + dirZ * distCm);
                if (Math.Abs(maxZ - minZ) < 0.0001) maxZ = minZ + distCm;

                var box = Context!.TG.CreateBox();
                box.MinPoint = Context.TG.CreatePoint(
                    cx - halfW, cy - halfH, minZ);
                box.MaxPoint = Context.TG.CreatePoint(
                    cx + halfW, cy + halfH, maxZ);
                return Context.TB.CreateSolidBlock(box);
            }
            else if (plane.StandardPlaneIndex == 2 || // XZ plane
                     (Math.Abs(plane.NormalY - 1.0) < 0.001 && Math.Abs(plane.NormalX) < 0.001 && Math.Abs(plane.NormalZ) < 0.001))
            {
                // XZ plane (or parallel)
                double minY = Math.Min(cy, cy + dirY * distCm);
                double maxY = Math.Max(cy, cy + dirY * distCm);
                if (Math.Abs(maxY - minY) < 0.0001) maxY = minY + distCm;

                var box = Context!.TG.CreateBox();
                box.MinPoint = Context.TG.CreatePoint(
                    cx - halfW, minY, cz - halfH);
                box.MaxPoint = Context.TG.CreatePoint(
                    cx + halfW, maxY, cz + halfH);
                return Context.TB.CreateSolidBlock(box);
            }
            else if (plane.StandardPlaneIndex == 1 || // YZ plane
                     (Math.Abs(plane.NormalX - 1.0) < 0.001 && Math.Abs(plane.NormalY) < 0.001 && Math.Abs(plane.NormalZ) < 0.001))
            {
                // YZ plane (or parallel)
                double minX = Math.Min(cx, cx + dirX * distCm);
                double maxX = Math.Max(cx, cx + dirX * distCm);
                if (Math.Abs(maxX - minX) < 0.0001) maxX = minX + distCm;

                var box = Context!.TG.CreateBox();
                box.MinPoint = Context.TG.CreatePoint(
                    minX, cy - halfW, cz - halfH);
                box.MaxPoint = Context.TG.CreatePoint(
                    maxX, cy + halfW, cz + halfH);
                return Context.TB.CreateSolidBlock(box);
            }
            else
            {
                // Arbitrary plane: create a box along the plane normal
                // Use cylinder-like approach: create at origin, transform
                // Fallback: create an axis-aligned approximation
                double allMin = -Math.Max(halfW, halfH);
                double allMax = Math.Max(halfW, halfH);

                var box = Context!.TG.CreateBox();
                box.MinPoint = Context.TG.CreatePoint(
                    cx + allMin, cy + allMin, cz);
                box.MaxPoint = Context.TG.CreatePoint(
                    cx + allMax, cy + allMax, cz + distCm);
                return Context.TB.CreateSolidBlock(box);
            }
        }

        public override string? GetDisplaySummary() => $"{Direction}, {Operation}";

        public override List<ParameterDescriptor> GetEditableParameters()
        {
            return new List<ParameterDescriptor>
            {
                new ParameterDescriptor
                {
                    Label = "Direction",
                    Key = "Direction",
                    Value = Direction,
                    Choices = new[] { "Positive", "Negative", "Symmetric" },
                    DisplayOnNode = Direction
                },
                new ParameterDescriptor
                {
                    Label = "Operation",
                    Key = "Operation",
                    Value = Operation,
                    Choices = new[] { "NewBody", "Join", "Cut", "Intersect" },
                    DisplayOnNode = Operation
                }
            };
        }

        public override Dictionary<string, object?> GetParameters()
        {
            return new Dictionary<string, object?>
            {
                ["Direction"] = Direction,
                ["Operation"] = Operation
            };
        }

        public override void SetParameters(Dictionary<string, object?> parameters)
        {
            if (parameters.TryGetValue("Direction", out var dir) && dir is string d)
            {
                if (d == "Positive" || d == "Negative" || d == "Symmetric")
                    Direction = d;
            }
            if (parameters.TryGetValue("Operation", out var op) && op is string o)
            {
                if (o == "NewBody" || o == "Join" || o == "Cut" || o == "Intersect")
                    Operation = o;
            }
        }
    }
}
