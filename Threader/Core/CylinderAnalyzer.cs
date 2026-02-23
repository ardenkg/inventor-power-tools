// ============================================================================
// Threader Add-in for Autodesk Inventor 2026
// CylinderAnalyzer.cs - Analyzes Cylindrical Faces for Thread Creation
// ============================================================================

using System;
using Inventor;

namespace Threader.Core
{
    /// <summary>
    /// Information about a detected cylindrical surface.
    /// </summary>
    public class CylinderInfo
    {
        /// <summary>
        /// The analyzed face.
        /// </summary>
        public Face? Face { get; set; }

        /// <summary>
        /// Diameter of the cylinder in centimeters (Inventor internal units).
        /// </summary>
        public double DiameterCm { get; set; }

        /// <summary>
        /// Diameter of the cylinder in millimeters.
        /// </summary>
        public double DiameterMm => DiameterCm * 10.0;

        /// <summary>
        /// Radius of the cylinder in centimeters.
        /// </summary>
        public double RadiusCm => DiameterCm / 2.0;

        /// <summary>
        /// Length of the cylindrical face in centimeters.
        /// </summary>
        public double LengthCm { get; set; }

        /// <summary>
        /// Length of the cylindrical face in millimeters.
        /// </summary>
        public double LengthMm => LengthCm * 10.0;

        /// <summary>
        /// The axis direction vector [X, Y, Z] of the cylinder.
        /// </summary>
        public double[] AxisVector { get; set; } = new double[3];

        /// <summary>
        /// The base point [X, Y, Z] of the cylinder in centimeters.
        /// This is the mathematical base point of the infinite cylinder.
        /// </summary>
        public double[] BasePoint { get; set; } = new double[3];

        /// <summary>
        /// The actual start point [X, Y, Z] of the cylindrical face in centimeters.
        /// This is where the cylindrical face actually begins (at one circular edge).
        /// </summary>
        public double[] StartPoint { get; set; } = new double[3];

        /// <summary>
        /// The actual end point [X, Y, Z] of the cylindrical face in centimeters.
        /// This is where the cylindrical face actually ends (at the other circular edge).
        /// </summary>
        public double[] EndPoint { get; set; } = new double[3];

        /// <summary>
        /// True if the start end of the cylinder is open (no wall blocking).
        /// False if there's a wall/obstruction at the start.
        /// </summary>
        public bool StartOpen { get; set; } = true;

        /// <summary>
        /// True if the end of the cylinder is open (no wall blocking).
        /// False if there's a wall/obstruction at the end.
        /// </summary>
        public bool EndOpen { get; set; } = true;

        /// <summary>
        /// True if this is an internal cylinder (hole), false for external (shaft).
        /// </summary>
        public bool IsInternal { get; set; }

        /// <summary>
        /// Whether the analysis was successful and data is valid.
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// Error message if analysis failed.
        /// </summary>
        public string ErrorMessage { get; set; } = "";

        /// <summary>
        /// Description string for display.
        /// </summary>
        public string Description
        {
            get
            {
                if (!IsValid) return "Invalid cylinder";
                string type = IsInternal ? "Internal (Hole)" : "External (Shaft)";
                return $"{type} - Ø{DiameterMm:F2}mm x {LengthMm:F2}mm";
            }
        }
    }

    /// <summary>
    /// Analyzes faces to detect cylindrical surfaces and extract their parameters.
    /// </summary>
    public class CylinderAnalyzer
    {
        #region Private Fields

        private readonly Inventor.Application _inventorApp;

        #endregion

        #region Constructor

        public CylinderAnalyzer(Inventor.Application inventorApp)
        {
            _inventorApp = inventorApp ?? throw new ArgumentNullException(nameof(inventorApp));
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Analyzes a face to determine if it's cylindrical and extracts its parameters.
        /// </summary>
        /// <param name="face">The face to analyze.</param>
        /// <returns>CylinderInfo with the analysis results.</returns>
        public CylinderInfo AnalyzeFace(Face face)
        {
            var result = new CylinderInfo { Face = face };

            try
            {
                if (face == null)
                {
                    result.ErrorMessage = "No face provided.";
                    return result;
                }

                // Check if the face has a cylindrical surface
                var surfaceType = face.SurfaceType;
                
                if (surfaceType != SurfaceTypeEnum.kCylinderSurface)
                {
                    result.ErrorMessage = "Selected face is not cylindrical.";
                    return result;
                }

                // Get the cylinder geometry from the face
                var cylinder = face.Geometry as Cylinder;
                if (cylinder == null)
                {
                    result.ErrorMessage = "Could not get cylinder geometry.";
                    return result;
                }

                // Extract cylinder parameters
                result.DiameterCm = cylinder.Radius * 2.0;  // Radius is in cm, convert to diameter
                
                // Get axis direction
                var axisVector = cylinder.AxisVector;
                result.AxisVector[0] = axisVector.X;
                result.AxisVector[1] = axisVector.Y;
                result.AxisVector[2] = axisVector.Z;

                // Get base point (mathematical origin of infinite cylinder)
                var basePoint = cylinder.BasePoint;
                result.BasePoint[0] = basePoint.X;
                result.BasePoint[1] = basePoint.Y;
                result.BasePoint[2] = basePoint.Z;

                // Determine if internal or external FIRST (needed for wall detection)
                result.IsInternal = DetermineIfInternal(face);

                // Calculate the actual start and end points of the cylindrical face
                // by finding the centers of the circular edges
                CalculateFaceExtents(face, cylinder, result);

                // Determine cylinder length using the face's edge loop
                result.LengthCm = CalculateCylinderLength(face, cylinder);

                result.IsValid = true;
                return result;
            }
            catch (Exception ex)
            {
                result.ErrorMessage = $"Analysis error: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"CylinderAnalyzer error: {ex}");
                return result;
            }
        }

        /// <summary>
        /// Calculates the actual start and end points of the cylindrical face
        /// by finding the centers of the circular edges.
        /// Also determines if each end is open or blocked by a wall.
        /// </summary>
        private void CalculateFaceExtents(Face face, Cylinder cylinder, CylinderInfo result)
        {
            try
            {
                var axisDir = cylinder.AxisVector;
                var basePoint = cylinder.BasePoint;
                
                double minParam = double.MaxValue;
                double maxParam = double.MinValue;
                Inventor.Point? minPoint = null;
                Inventor.Point? maxPoint = null;
                Edge? minEdge = null;
                Edge? maxEdge = null;

                // Find circular edges and get their centers
                foreach (Edge edge in face.Edges)
                {
                    if (edge.GeometryType == CurveTypeEnum.kCircleCurve)
                    {
                        Circle circle = (Circle)edge.Geometry;
                        Inventor.Point center = circle.Center;
                        
                        // Project center onto axis to get parameter
                        double param = (center.X - basePoint.X) * axisDir.X +
                                       (center.Y - basePoint.Y) * axisDir.Y +
                                       (center.Z - basePoint.Z) * axisDir.Z;
                        
                        if (param < minParam)
                        {
                            minParam = param;
                            minPoint = center;
                            minEdge = edge;
                        }
                        if (param > maxParam)
                        {
                            maxParam = param;
                            maxPoint = center;
                            maxEdge = edge;
                        }
                    }
                }

                // Set start and end points
                if (minPoint != null)
                {
                    result.StartPoint[0] = minPoint.X;
                    result.StartPoint[1] = minPoint.Y;
                    result.StartPoint[2] = minPoint.Z;
                }
                else
                {
                    // Fallback to base point
                    result.StartPoint[0] = basePoint.X;
                    result.StartPoint[1] = basePoint.Y;
                    result.StartPoint[2] = basePoint.Z;
                }

                if (maxPoint != null)
                {
                    result.EndPoint[0] = maxPoint.X;
                    result.EndPoint[1] = maxPoint.Y;
                    result.EndPoint[2] = maxPoint.Z;
                }
                else
                {
                    // Fallback to start point offset by length
                    result.EndPoint[0] = result.StartPoint[0] + axisDir.X * result.LengthCm;
                    result.EndPoint[1] = result.StartPoint[1] + axisDir.Y * result.LengthCm;
                    result.EndPoint[2] = result.StartPoint[2] + axisDir.Z * result.LengthCm;
                }

                // Determine if each end is open or blocked
                // Pass isInternal so we can handle holes vs shafts differently
                result.StartOpen = minEdge != null ? IsEdgeOpen(minEdge, face, result.IsInternal) : true;
                result.EndOpen = maxEdge != null ? IsEdgeOpen(maxEdge, face, result.IsInternal) : true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error calculating face extents: {ex.Message}");
                // Use base point as fallback
                result.StartPoint[0] = result.BasePoint[0];
                result.StartPoint[1] = result.BasePoint[1];
                result.StartPoint[2] = result.BasePoint[2];
                result.EndPoint[0] = result.BasePoint[0] + result.AxisVector[0] * result.LengthCm;
                result.EndPoint[1] = result.BasePoint[1] + result.AxisVector[1] * result.LengthCm;
                result.EndPoint[2] = result.BasePoint[2] + result.AxisVector[2] * result.LengthCm;
            }
        }

        /// <summary>
        /// Checks if the given face is a cylindrical surface.
        /// </summary>
        public bool IsCylindricalFace(Face face)
        {
            if (face == null) return false;

            try
            {
                return face.SurfaceType == SurfaceTypeEnum.kCylinderSurface;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Re-finds a cylindrical face in the active document that matches the given CylinderInfo's
        /// geometric properties (center, radius, axis). This is needed because after modifying the
        /// part (e.g., creating a thread), Face COM references become stale.
        /// </summary>
        /// <param name="originalInfo">The original CylinderInfo with geometric properties to match.</param>
        /// <returns>A new CylinderInfo with a fresh Face reference, or null if not found.</returns>
        public CylinderInfo? RefindCylinder(CylinderInfo originalInfo)
        {
            try
            {
                // Support both direct part editing and in-place editing within an assembly
                var doc = (_inventorApp.ActiveEditDocument as PartDocument)
                          ?? (_inventorApp.ActiveDocument as PartDocument);
                if (doc == null) return null;

                var compDef = doc.ComponentDefinition;
                double tolerance = 0.01; // 0.1mm tolerance for matching

                // Calculate the center point of the original cylinder
                double origCenterX = (originalInfo.StartPoint[0] + originalInfo.EndPoint[0]) / 2.0;
                double origCenterY = (originalInfo.StartPoint[1] + originalInfo.EndPoint[1]) / 2.0;
                double origCenterZ = (originalInfo.StartPoint[2] + originalInfo.EndPoint[2]) / 2.0;
                double origRadius = originalInfo.RadiusCm;

                Face? bestMatch = null;
                double bestDistance = double.MaxValue;

                foreach (SurfaceBody body in compDef.SurfaceBodies)
                {
                    foreach (Face face in body.Faces)
                    {
                        try
                        {
                            if (face.SurfaceType != SurfaceTypeEnum.kCylinderSurface)
                                continue;

                            var cylinder = face.Geometry as Cylinder;
                            if (cylinder == null) continue;

                            // Check radius matches
                            if (Math.Abs(cylinder.Radius - origRadius) > tolerance)
                                continue;

                            // Check axis direction matches (parallel, either direction)
                            double dot = cylinder.AxisVector.X * originalInfo.AxisVector[0] +
                                         cylinder.AxisVector.Y * originalInfo.AxisVector[1] +
                                         cylinder.AxisVector.Z * originalInfo.AxisVector[2];
                            if (Math.Abs(Math.Abs(dot) - 1.0) > 0.01)
                                continue;

                            // Find center of this face
                            double fCenterX = 0, fCenterY = 0, fCenterZ = 0;
                            int edgeCount = 0;
                            foreach (Edge edge in face.Edges)
                            {
                                if (edge.GeometryType == CurveTypeEnum.kCircleCurve)
                                {
                                    Circle circle = (Circle)edge.Geometry;
                                    fCenterX += circle.Center.X;
                                    fCenterY += circle.Center.Y;
                                    fCenterZ += circle.Center.Z;
                                    edgeCount++;
                                }
                            }

                            if (edgeCount == 0) continue;
                            fCenterX /= edgeCount;
                            fCenterY /= edgeCount;
                            fCenterZ /= edgeCount;

                            // Distance between centers
                            double dist = Math.Sqrt(
                                Math.Pow(fCenterX - origCenterX, 2) +
                                Math.Pow(fCenterY - origCenterY, 2) +
                                Math.Pow(fCenterZ - origCenterZ, 2));

                            if (dist < bestDistance)
                            {
                                bestDistance = dist;
                                bestMatch = face;
                            }
                        }
                        catch { continue; }
                    }
                }

                // Only accept if reasonably close (within 1mm)
                if (bestMatch != null && bestDistance < 0.1)
                {
                    return AnalyzeFace(bestMatch);
                }

                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"RefindCylinder error: {ex.Message}");
                return null;
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Determines if a circular edge at the end of a cylinder is "open" (can extend threads)
        /// or blocked by a wall/shoulder that would prevent extension.
        /// </summary>
        /// <param name="edge">The circular edge to check.</param>
        /// <param name="cylinderFace">The cylindrical face the edge belongs to.</param>
        /// <param name="isInternal">True if this is a hole, false if it's a shaft.</param>
        /// <returns>True if open (can extend threads), false if blocked.</returns>
        private bool IsEdgeOpen(Edge edge, Face cylinderFace, bool isInternal)
        {
            try
            {
                // Get the cylinder geometry for radius comparison
                var thisCylinder = cylinderFace.Geometry as Cylinder;
                if (thisCylinder == null)
                    return true;  // Can't determine, assume open
                
                double thisRadius = thisCylinder.Radius;
                
                // Get all faces that share this edge
                var adjacentFaces = edge.Faces;
                
                foreach (Face adjFace in adjacentFaces)
                {
                    // Skip the cylinder face itself
                    if (adjFace.Equals(cylinderFace))
                        continue;
                    
                    SurfaceTypeEnum surfaceType = adjFace.SurfaceType;
                    
                    // Check for shoulder/step (different-radius cylinder)
                    if (surfaceType == SurfaceTypeEnum.kCylinderSurface)
                    {
                        var adjCylinder = adjFace.Geometry as Cylinder;
                        if (adjCylinder != null)
                        {
                            double adjRadius = adjCylinder.Radius;
                            double radiusDiff = adjRadius - thisRadius;  // Positive = larger adjacent
                            
                            if (isInternal)
                            {
                                // For internal (hole): blocked if adjacent cylinder is SMALLER
                                // (like a counterbore going to a smaller hole - the wall blocks)
                                if (radiusDiff < -0.0001)
                                {
                                    System.Diagnostics.Debug.WriteLine($"IsEdgeOpen: Hole meets smaller hole - BLOCKED");
                                    return false;
                                }
                            }
                            else
                            {
                                // For external (shaft): blocked if adjacent cylinder is LARGER
                                // (like a shaft meeting a larger diameter shoulder)
                                if (radiusDiff > 0.0001)
                                {
                                    System.Diagnostics.Debug.WriteLine($"IsEdgeOpen: Shaft meets larger shoulder - BLOCKED");
                                    return false;
                                }
                            }
                        }
                    }
                    
                    // For external shafts, a torus at the edge is typically a fillet - OPEN
                    // For internal holes, a cone at the bottom is a drill point - could be OPEN
                    
                    // Check for planar face perpendicular to the cylinder axis (flat wall / shoulder)
                    if (surfaceType == SurfaceTypeEnum.kPlaneSurface)
                    {
                        try
                        {
                            var plane = adjFace.Geometry as Plane;
                            if (plane != null)
                            {
                                // Check if the plane normal is roughly parallel to the cylinder axis
                                // (i.e. the flat face is perpendicular to the cylinder)
                                var planeNormal = plane.Normal;
                                var axisVec = thisCylinder.AxisVector;
                                double dot = Math.Abs(
                                    planeNormal.X * axisVec.X +
                                    planeNormal.Y * axisVec.Y +
                                    planeNormal.Z * axisVec.Z);
                                
                                if (dot > 0.9)  // Plane is roughly perpendicular to cylinder
                                {
                                    // Determine if the planar face extends BEYOND the cylinder radius.
                                    // - Shoulder/flange: annular ring extending outward past cylinder radius = WALL
                                    // - End cap: disk staying within cylinder radius = not a wall
                                    // - Hole opening: surface extending outward = not a wall
                                    // - Blind hole floor: disk within hole radius = WALL
                                    //
                                    // Check by looking at OTHER edges of this planar face for vertices
                                    // farther from the cylinder axis than the cylinder radius.
                                    bool faceExtendsBeyond = false;
                                    var axisPoint = thisCylinder.BasePoint;
                                    var axisDir = thisCylinder.AxisVector;
                                    
                                    foreach (Edge adjEdge in adjFace.Edges)
                                    {
                                        if (adjEdge.Equals(edge)) continue; // Skip the shared edge
                                        try
                                        {
                                            Inventor.Point? vp = null;
                                            try { vp = adjEdge.StartVertex?.Point; } catch { }
                                            if (vp == null) try { vp = adjEdge.StopVertex?.Point; } catch { }
                                            if (vp == null) continue;
                                            
                                            // Distance from vertex to cylinder axis line
                                            double apx = vp.X - axisPoint.X;
                                            double apy = vp.Y - axisPoint.Y;
                                            double apz = vp.Z - axisPoint.Z;
                                            double cx = apy * axisDir.Z - apz * axisDir.Y;
                                            double cy = apz * axisDir.X - apx * axisDir.Z;
                                            double cz = apx * axisDir.Y - apy * axisDir.X;
                                            double dist = Math.Sqrt(cx*cx + cy*cy + cz*cz);
                                            
                                            if (dist > thisRadius + 0.001)
                                            {
                                                faceExtendsBeyond = true;
                                                break;
                                            }
                                        }
                                        catch { }
                                    }
                                    
                                    if (!isInternal && faceExtendsBeyond)
                                    {
                                        // External shaft: face extends beyond shaft = shoulder/wall = BLOCKED
                                        System.Diagnostics.Debug.WriteLine($"IsEdgeOpen: Shaft meets wall (planar extends beyond radius) - BLOCKED");
                                        return false;
                                    }
                                    else if (isInternal && !faceExtendsBeyond)
                                    {
                                        // Internal hole: face stays within hole = blind hole floor = BLOCKED
                                        System.Diagnostics.Debug.WriteLine($"IsEdgeOpen: Hole meets floor (planar within radius) - BLOCKED");
                                        return false;
                                    }
                                }
                            }
                        }
                        catch { }
                    }
                }
                
                // No blocking geometry found
                System.Diagnostics.Debug.WriteLine($"IsEdgeOpen: No blocking geometry - OPEN");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"IsEdgeOpen error: {ex.Message}");
                return true;  // Default to open if we can't determine
            }
        }

        /// <summary>
        /// Calculates the length of the cylinder based on its edges.
        /// </summary>
        private double CalculateCylinderLength(Face face, Cylinder cylinder)
        {
            try
            {
                double minParam = double.MaxValue;
                double maxParam = double.MinValue;

                // Get the axis vector for projection
                var axisX = cylinder.AxisVector.X;
                var axisY = cylinder.AxisVector.Y;
                var axisZ = cylinder.AxisVector.Z;

                // Get base point for reference
                var baseX = cylinder.BasePoint.X;
                var baseY = cylinder.BasePoint.Y;
                var baseZ = cylinder.BasePoint.Z;

                // Find the extent of the cylinder along its axis
                foreach (Edge edge in face.Edges)
                {
                    try
                    {
                        // Get edge end points
                        var startPoint = edge.StartVertex?.Point;
                        var stopPoint = edge.StopVertex?.Point;

                        if (startPoint != null)
                        {
                            double param = ProjectPointOnAxis(
                                startPoint.X, startPoint.Y, startPoint.Z,
                                baseX, baseY, baseZ,
                                axisX, axisY, axisZ);
                            minParam = Math.Min(minParam, param);
                            maxParam = Math.Max(maxParam, param);
                        }

                        if (stopPoint != null)
                        {
                            double param = ProjectPointOnAxis(
                                stopPoint.X, stopPoint.Y, stopPoint.Z,
                                baseX, baseY, baseZ,
                                axisX, axisY, axisZ);
                            minParam = Math.Min(minParam, param);
                            maxParam = Math.Max(maxParam, param);
                        }
                    }
                    catch { }
                }

                if (minParam != double.MaxValue && maxParam != double.MinValue)
                {
                    return maxParam - minParam;
                }

                // Fallback: use face area and radius to estimate length
                double area = face.Evaluator.Area;
                double circumference = 2.0 * Math.PI * cylinder.Radius;
                return area / circumference;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error calculating cylinder length: {ex.Message}");
                return 1.0;  // Default to 1cm if calculation fails
            }
        }

        /// <summary>
        /// Projects a point onto the cylinder axis and returns the parameter along the axis.
        /// </summary>
        private double ProjectPointOnAxis(
            double px, double py, double pz,
            double bx, double by, double bz,
            double ax, double ay, double az)
        {
            // Vector from base point to the point
            double vx = px - bx;
            double vy = py - by;
            double vz = pz - bz;

            // Dot product with axis (projection)
            return vx * ax + vy * ay + vz * az;
        }

        /// <summary>
        /// Determines if the cylindrical face is internal (hole) or external (shaft).
        /// Uses the IsParamReversed property - but inverted based on testing.
        /// </summary>
        private bool DetermineIfInternal(Face face)
        {
            try
            {
                // Method 1: Use Face.IsParamReversed
                // Testing showed this is INVERTED from expected:
                // IsParamReversed = false means internal (hole)
                // IsParamReversed = true means external (shaft)
                try
                {
                    bool isReversed = face.IsParamReversed;
                    System.Diagnostics.Debug.WriteLine($"DetermineIfInternal: IsParamReversed={isReversed}");
                    // INVERT the result
                    return !isReversed;
                }
                catch (Exception ex1)
                {
                    System.Diagnostics.Debug.WriteLine($"IsParamReversed failed: {ex1.Message}");
                }

                // Method 2: Fallback - check the creating feature type
                try
                {
                    var creatingFeature = face.CreatedByFeature;
                    if (creatingFeature != null)
                    {
                        string featureType = creatingFeature.Type.ToString();
                        System.Diagnostics.Debug.WriteLine($"DetermineIfInternal: FeatureType={featureType}");
                        
                        // HoleFeature is definitely internal
                        if (featureType.Contains("Hole"))
                            return true;
                            
                        // Check if it's a cut operation (extrude cut, revolve cut)
                        if (featureType.Contains("Extrude"))
                        {
                            var extrudeFeature = creatingFeature as ExtrudeFeature;
                            if (extrudeFeature != null && 
                                extrudeFeature.Operation == PartFeatureOperationEnum.kCutOperation)
                                return true;
                        }
                        
                        if (featureType.Contains("Revolve"))
                        {
                            var revolveFeature = creatingFeature as RevolveFeature;
                            if (revolveFeature != null && 
                                revolveFeature.Operation == PartFeatureOperationEnum.kCutOperation)
                                return true;
                        }
                    }
                }
                catch (Exception ex2)
                {
                    System.Diagnostics.Debug.WriteLine($"Feature check failed: {ex2.Message}");
                }

                // Default to external (shaft)
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error determining internal/external: {ex.Message}");
                return false;
            }
        }

        #endregion
    }
}