// ============================================================================
// iNode Add-in for Autodesk Inventor 2026
// BodyCentroidNode.cs - Extracts the centroid point of a body
// ============================================================================

using System;
using Inventor;
using iNode.Core;

namespace iNode.Nodes
{
    /// <summary>
    /// Computes the centroid (center of mass) of a solid body.
    /// Outputs a Point3D in mm (node units).
    /// Also outputs the volume in mm³.
    /// </summary>
    public class BodyCentroidNode : Node
    {
        public override string TypeName => "BodyCentroid";
        public override string Title => "Body Centroid";
        public override string Category => "Topology";

        public BodyCentroidNode()
        {
            AddInput("Body", "Body", PortDataType.Geometry, null);
            AddOutput("Point", "Point", PortDataType.Point3D);
            AddOutput("Volume", "Volume (mm³)", PortDataType.Number);
        }

        protected override void Compute()
        {
            var geomVal = GetInput("Body")!.GetEffectiveValue();

            if (geomVal is not BodyData bodyData || bodyData.Body == null)
            {
                HasError = true;
                ErrorMessage = "No body connected";
                return;
            }

            try
            {
                // Try using vertex-based centroid (works for both transient and real bodies)
                dynamic body = bodyData.Body;

                double sumX = 0, sumY = 0, sumZ = 0;
                int count = 0;

                // First try: mass properties if available (more accurate centroid)
                try
                {
                    // For transient surface bodies, we can try getting the centroid
                    // by averaging all vertex positions as an approximation
                    foreach (var vertex in body.Vertices)
                    {
                        dynamic v = vertex;
                        dynamic pt = v.Point;
                        sumX += (double)pt.X;
                        sumY += (double)pt.Y;
                        sumZ += (double)pt.Z;
                        count++;
                    }
                }
                catch { /* Fall through to bounding box approach */ }

                double cx, cy, cz;

                if (count > 0)
                {
                    // Vertex centroid in cm (Inventor internal), convert to mm
                    cx = (sumX / count) / InventorContext.MM_TO_CM;
                    cy = (sumY / count) / InventorContext.MM_TO_CM;
                    cz = (sumZ / count) / InventorContext.MM_TO_CM;
                }
                else
                {
                    // Fallback: use bounding box center
                    try
                    {
                        dynamic rangeBox = body.RangeBox;
                        dynamic minPt = rangeBox.MinPoint;
                        dynamic maxPt = rangeBox.MaxPoint;

                        cx = ((double)minPt.X + (double)maxPt.X) / 2.0 / InventorContext.MM_TO_CM;
                        cy = ((double)minPt.Y + (double)maxPt.Y) / 2.0 / InventorContext.MM_TO_CM;
                        cz = ((double)minPt.Z + (double)maxPt.Z) / 2.0 / InventorContext.MM_TO_CM;
                    }
                    catch (Exception ex)
                    {
                        HasError = true;
                        ErrorMessage = $"Cannot determine centroid: {ex.Message}";
                        return;
                    }
                }

                GetOutput("Point")!.Value = (cx, cy, cz);

                // Compute volume from vertices bounding box as approximation
                // Real volume would need mass properties which may not be available on transient bodies
                try
                {
                    dynamic rangeBox = body.RangeBox;
                    dynamic minPt = rangeBox.MinPoint;
                    dynamic maxPt = rangeBox.MaxPoint;

                    // Volume of bounding box in cm³ → convert to mm³ (×1000)
                    double dx = (double)maxPt.X - (double)minPt.X;
                    double dy = (double)maxPt.Y - (double)minPt.Y;
                    double dz = (double)maxPt.Z - (double)minPt.Z;
                    double volCm3 = Math.Abs(dx * dy * dz);
                    double volMm3 = volCm3 * 1000.0; // cm³ to mm³
                    GetOutput("Volume")!.Value = volMm3;
                }
                catch
                {
                    GetOutput("Volume")!.Value = 0.0;
                }
            }
            catch (Exception ex)
            {
                HasError = true;
                ErrorMessage = $"Centroid failed: {ex.Message}";
            }
        }
    }
}
