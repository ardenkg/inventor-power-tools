// ============================================================================
// Threader Add-in for Autodesk Inventor 2026
// ThreadGenerator.cs - Generates Physical Thread Geometry using Sweep with Helix
// Based on Autodesk SDK Variable Pitch Helix sample pattern
// ============================================================================

using System;
using System.Collections.Generic;
using Inventor;

namespace Threader.Core
{
    public class ThreadGenerationOptions
    {
        public ThreadStandard? ThreadStandard { get; set; }
        public CylinderInfo? CylinderInfo { get; set; }
        public double? ThreadLengthCm { get; set; }
        public double StartOffsetCm { get; set; } = 0;
        public bool RightHand { get; set; } = true;
        public bool FullDepth { get; set; } = true;
        public string FeatureName { get; set; } = "Thread";
        public bool ResizeCylinder { get; set; } = true;
        public ThreadProfileType ProfileType { get; set; } = ThreadProfileType.Trapezoidal;
        /// <summary>
        /// If true, thread starts from the "end" (top/max) of the cylinder instead of the "start" (bottom/min).
        /// </summary>
        public bool StartFromEnd { get; set; } = false;
    }

    public enum ThreadProfileType
    {
        Triangular,
        Trapezoidal,
        Square
    }

    public class ThreadGenerationResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public object? CreatedFeature { get; set; }
        public int CoilCount { get; set; }
    }

    /// <summary>
    /// Generates physical thread geometry using Sweep along a 3D helical spline.
    /// This approach is based on the Autodesk SDK Variable Pitch Helix sample.
    /// </summary>
    public class ThreadGenerator
    {
        private readonly Inventor.Application _inventorApp;
        private const double PI = Math.PI;

        public ThreadGenerator(Inventor.Application inventorApp)
        {
            _inventorApp = inventorApp ?? throw new ArgumentNullException(nameof(inventorApp));
        }

        public ThreadGenerationResult GenerateThread(ThreadGenerationOptions options)
        {
            var result = new ThreadGenerationResult { Success = false };

            try
            {
                if (options.ThreadStandard == null)
                {
                    result.Message = "No thread standard specified.";
                    return result;
                }

                if (options.CylinderInfo == null || !options.CylinderInfo.IsValid)
                {
                    result.Message = "Invalid cylinder information.";
                    return result;
                }

                // Support both direct part editing and in-place editing within an assembly
                var doc = (_inventorApp.ActiveEditDocument as PartDocument)
                          ?? (_inventorApp.ActiveDocument as PartDocument);
                if (doc == null)
                {
                    result.Message = "No active part document. If in an assembly, please edit a part in-place first.";
                    return result;
                }

                var compDef = doc.ComponentDefinition;
                var transGeom = _inventorApp.TransientGeometry;

                double pitch = options.ThreadStandard.Pitch;
                double threadLength = options.ThreadLengthCm ?? options.CylinderInfo.LengthCm;
                int numberOfTurns = (int)Math.Floor(threadLength / pitch);

                if (numberOfTurns < 1)
                {
                    result.Message = "Thread length is too short for the selected pitch.";
                    return result;
                }

                var transaction = _inventorApp.TransactionManager.StartTransaction(
                    (_Document)doc, "Create Thread");

                try
                {
                    // Use the SDK-pattern sweep approach
                    var threadResult = CreateThreadWithHelicalSweep(compDef, options, pitch, threadLength, numberOfTurns);

                    if (threadResult.Success)
                    {
                        transaction.End();
                        return threadResult;
                    }

                    transaction.Abort();
                    result.Message = threadResult.Message;
                    return result;
                }
                catch (Exception ex)
                {
                    transaction.Abort();
                    result.Message = $"Thread creation failed: {ex.Message}";
                    return result;
                }
            }
            catch (Exception ex)
            {
                result.Message = $"Error generating thread: {ex.Message}";
                return result;
            }
        }

        public (bool IsValid, string Message) ValidateThread(ThreadGenerationOptions options)
        {
            if (options.ThreadStandard == null)
                return (false, "No thread standard selected.");

            if (options.CylinderInfo == null || !options.CylinderInfo.IsValid)
                return (false, "Invalid cylinder selection.");

            double threadLength = options.ThreadLengthCm ?? options.CylinderInfo.LengthCm;
            double pitch = options.ThreadStandard.Pitch;
            int numTurns = (int)Math.Floor(threadLength / pitch);

            if (numTurns < 1)
                return (false, $"Thread length ({threadLength * 10:F2}mm) is too short for pitch ({pitch * 10:F2}mm).");

            return (true, $"Thread will have {numTurns} helical turns over {threadLength * 10:F2}mm length.");
        }

        /// <summary>
        /// Creates thread using Coil Feature.
        /// Creates WorkAxis first, then optionally resizes cylinder, then creates thread.
        /// Thread extends slightly beyond cylinder edges for continuous appearance.
        /// </summary>
        private ThreadGenerationResult CreateThreadWithHelicalSweep(
            PartComponentDefinition compDef,
            ThreadGenerationOptions options,
            double pitch,
            double threadLength,
            int numberOfTurns)
        {
            var result = new ThreadGenerationResult { Success = false };
            var transGeom = _inventorApp.TransientGeometry;
            var cylinderInfo = options.CylinderInfo!;
            var threadStd = options.ThreadStandard!;

            PlanarSketch? profileSketch = null;
            WorkAxis? cylinderAxis = null;
            string step = "init";

            try
            {
                Face cylFace = cylinderInfo.Face!;
                double currentRadius = cylinderInfo.RadiusCm;
                double[] axisVec = cylinderInfo.AxisVector;
                double[] startPt = cylinderInfo.StartPoint;
                double[] endPt = cylinderInfo.EndPoint;
                bool isInternal = cylinderInfo.IsInternal;

                // Normalize axis
                double axLen = Math.Sqrt(axisVec[0]*axisVec[0] + axisVec[1]*axisVec[1] + axisVec[2]*axisVec[2]);
                if (axLen > 1e-6) { axisVec[0] /= axLen; axisVec[1] /= axLen; axisVec[2] /= axLen; }

                // CRITICAL: Create WorkAxis BEFORE any geometry modifications
                // After resize, the face reference becomes invalid
                step = "Creating WorkAxis from face";
                cylinderAxis = compDef.WorkAxes.AddByRevolvedFace(cylFace, false);

                // Determine target radius based on thread standard
                double targetRadius;
                if (isInternal)
                {
                    targetRadius = threadStd.TapDrillDiameter / 2.0;
                }
                else
                {
                    targetRadius = threadStd.MajorDiameter / 2.0;
                }

                // Thread geometry dimensions
                double threadDepth = (threadStd.MajorDiameter - threadStd.MinorDiameter) / 2.0;
                double profileWidth = pitch * 0.5;

                // Determine profile radius (may change after resize)
                double profileRadius = currentRadius;
                bool didResize = false;
                
                step = "Resizing cylinder if needed";
                if (options.ResizeCylinder && Math.Abs(currentRadius - targetRadius) > 0.001)
                {
                    // Calculate the 3D region that covers only the thread length, not the full cylinder.
                    // This avoids resizing beyond where threads actually exist.
                    double resizeCylLen = cylinderInfo.LengthCm;
                    double resizeLength = Math.Min(threadLength, resizeCylLen);

                    double[] resizeStartPt, resizeEndPt;
                    if (Math.Abs(resizeLength - resizeCylLen) < 0.001)
                    {
                        // Thread covers entire cylinder — use full extents
                        resizeStartPt = startPt;
                        resizeEndPt = endPt;
                    }
                    else if (options.StartFromEnd)
                    {
                        // Thread starts from end, goes toward start
                        resizeStartPt = new double[] {
                            endPt[0] - axisVec[0] * resizeLength,
                            endPt[1] - axisVec[1] * resizeLength,
                            endPt[2] - axisVec[2] * resizeLength
                        };
                        resizeEndPt = endPt;
                    }
                    else
                    {
                        // Thread starts from start, goes toward end
                        resizeStartPt = startPt;
                        resizeEndPt = new double[] {
                            startPt[0] + axisVec[0] * resizeLength,
                            startPt[1] + axisVec[1] * resizeLength,
                            startPt[2] + axisVec[2] * resizeLength
                        };
                    }

                    bool resized = ResizeCylinder(compDef, cylFace, currentRadius, targetRadius, isInternal, resizeStartPt, resizeEndPt, options.FeatureName);
                    if (resized)
                    {
                        profileRadius = targetRadius;
                        didResize = true;
                    }
                }

                step = "Determining orientation";
                double absX = Math.Abs(axisVec[0]);
                double absY = Math.Abs(axisVec[1]);
                double absZ = Math.Abs(axisVec[2]);

                // Create a work plane that CONTAINS the cylinder axis
                // Use AddByLinePlaneAndAngle to guarantee the plane contains the axis
                // This works for cylinders at any position, not just at origin
                step = "Creating work plane containing cylinder axis";
                WorkPlane axisPlane;
                
                // Choose a reference plane that's not parallel to the axis
                WorkPlane referencePlane;
                if (absZ > absX && absZ > absY)
                {
                    // Z-aligned: use XZ plane as reference (axis is mostly perpendicular to Y)
                    referencePlane = compDef.WorkPlanes[2]; // XZ plane
                }
                else if (absY > absX)
                {
                    // Y-aligned: use XY plane as reference
                    referencePlane = compDef.WorkPlanes[3]; // XY plane
                }
                else
                {
                    // X-aligned: use XY plane as reference
                    referencePlane = compDef.WorkPlanes[3]; // XY plane
                }

                // Create plane containing the axis at 0 degrees to reference plane
                // This creates a plane that passes through the axis line
                axisPlane = compDef.WorkPlanes.AddByLinePlaneAndAngle(cylinderAxis, referencePlane, "0 deg", false);
                
                step = "Creating sketch on axis plane";
                profileSketch = compDef.Sketches.Add(axisPlane);

                // Calculate extension at each end of the cylinder:
                // - All ends: extend 2 pitches beyond so the coil overshoots generously.
                //   We then trim the coil body with a simple extrude-cut at each termination
                //   point BEFORE subtracting from the shaft. This gives a perfectly clean edge.
                double extension = 2.0 * pitch;
                double startExtension = extension;
                double endExtension = extension;

                // Only apply the "far end" extension if the thread actually reaches that end.
                bool threadReachesFarEnd = (threadLength >= cylinderInfo.LengthCm - 0.001);
                
                step = "Calculating profile position";
                
                // When using AddByLinePlaneAndAngle:
                // - The plane contains the cylinder axis
                // - The sketch X-axis aligns with the axis direction
                // - The sketch Y-axis is perpendicular to the axis (radial direction)
                // - BUT the Y-axis could point in either direction!
                //
                // To determine the correct side, we get an actual point ON the cylinder surface
                // and convert it to sketch space. The Y value tells us which side the surface is on.
                
                // Convert 3D StartPoint and EndPoint to 2D sketch coordinates (these are ON THE AXIS)
                Inventor.Point startPoint3D = transGeom.CreatePoint(startPt[0], startPt[1], startPt[2]);
                Inventor.Point endPoint3D = transGeom.CreatePoint(endPt[0], endPt[1], endPt[2]);
                Point2d startPointSketch = profileSketch.ModelToSketchSpace(startPoint3D);
                Point2d endPointSketch = profileSketch.ModelToSketchSpace(endPoint3D);
                
                // Get a point ON THE CYLINDER SURFACE to determine which side of Y=0 the surface is
                Inventor.Point? surfacePoint3D = null;
                foreach (Edge edge in cylFace.Edges)
                {
                    if (edge.StartVertex != null)
                    {
                        surfacePoint3D = edge.StartVertex.Point;
                        break;
                    }
                }
                
                // Fallback: calculate a surface point using axis + radius
                if (surfacePoint3D == null)
                {
                    // Find a perpendicular direction to offset from axis
                    double px = 1, py = 0, pz = 0;
                    if (Math.Abs(axisVec[0]) > 0.9)
                    {
                        px = 0; py = 1; pz = 0;
                    }
                    // Cross product to get perpendicular
                    double crossX = axisVec[1] * pz - axisVec[2] * py;
                    double crossY = axisVec[2] * px - axisVec[0] * pz;
                    double crossZ = axisVec[0] * py - axisVec[1] * px;
                    double crossLen = Math.Sqrt(crossX*crossX + crossY*crossY + crossZ*crossZ);
                    if (crossLen > 1e-9)
                    {
                        crossX /= crossLen; crossY /= crossLen; crossZ /= crossLen;
                    }
                    surfacePoint3D = transGeom.CreatePoint(
                        startPt[0] + crossX * profileRadius,
                        startPt[1] + crossY * profileRadius,
                        startPt[2] + crossZ * profileRadius);
                }
                
                Point2d surfacePointSketch = profileSketch.ModelToSketchSpace(surfacePoint3D);
                
                double profileX, profileY;
                
                // The surface Y coordinate tells us which side of the axis the cylinder surface is
                // It will be either positive or negative depending on sketch orientation
                profileY = surfacePointSketch.Y;
                
                // Normalize to actual radius (in case the edge point was slightly off)
                double surfaceYSign = Math.Sign(profileY);
                if (surfaceYSign == 0) surfaceYSign = 1;  // Default to positive if exactly on axis
                profileY = surfaceYSign * profileRadius;
                
                // cutDirection determines which way the profile extends from the base:
                // - Internal thread: cut outward (away from axis, into surrounding material)
                // - External thread: cut inward (toward axis, into shaft material)
                // The sign of profileY affects which direction is "outward" vs "inward"
                double baseCutDirection = isInternal ? 1.0 : -1.0;
                double cutDirection = baseCutDirection * surfaceYSign;
                
                // Profile is positioned relative to the cylinder in sketch coordinates
                // The sketch X axis may point either direction along the cylinder axis
                // Determine cylinder extent in sketch X coordinates and track which is Start/End
                double sketchMinX, sketchMaxX;
                bool minIsStart;  // True if sketchMinX corresponds to CylinderInfo.StartPoint
                if (startPointSketch.X < endPointSketch.X)
                {
                    sketchMinX = startPointSketch.X;
                    sketchMaxX = endPointSketch.X;
                    minIsStart = true;
                }
                else
                {
                    sketchMinX = endPointSketch.X;
                    sketchMaxX = startPointSketch.X;
                    minIsStart = false;
                }
                
                // Determine extensions at min and max X based on which end they correspond to
                double minExtension = minIsStart ? startExtension : endExtension;
                double maxExtension = minIsStart ? endExtension : startExtension;

                // Always use the FULL cylinder length for the coil, not threadLength.
                // The coil spans the entire cylinder + extensions on both sides.
                // Trimming handles cutting the coil to the specified thread length.
                double cylLength = cylinderInfo.LengthCm;
                double extendedLength = cylLength + minExtension + maxExtension;
                
                // Ensure we still have at least one pitch of thread
                if (extendedLength < pitch)
                    extendedLength = pitch;
                
                // Determine where to start based on StartFromEnd option
                // StartFromEnd = false: thread starts from the "min X" end (bottom)
                // StartFromEnd = true: thread starts from the "max X" end (top)
                double sketchStartX, sketchEndX;
                if (options.StartFromEnd)
                {
                    // Start from end (max X), thread extends toward min X
                    sketchStartX = sketchMaxX;
                    sketchEndX = sketchMinX;
                    profileX = sketchStartX + maxExtension;  // Extend beyond max end only if open
                }
                else
                {
                    // Start from beginning (min X), thread extends toward max X
                    sketchStartX = sketchMinX;
                    sketchEndX = sketchMaxX;
                    profileX = sketchStartX - minExtension;  // Extend before min end only if open
                }
                bool depthAlongX = false;  // Depth (thread cut) is in the Y direction (toward/away from axis)

                step = $"Drawing {options.ProfileType} profile";
                
                // Draw the appropriate thread profile shape
                DrawThreadProfile(
                    profileSketch, transGeom,
                    profileX, profileY,
                    threadDepth, profileWidth, pitch,
                    cutDirection, depthAlongX,
                    options.ProfileType);

                step = "Getting profile";
                Profile profile = profileSketch.Profiles.AddForSolid();

                // Get reference to the original body BEFORE creating coil
                step = "Getting original body reference";
                SurfaceBody originalBody = cylFace.SurfaceBody;

                step = "Creating coil feature as new body";
                double revolutions = extendedLength / pitch;  // Use extended length
                bool clockwise = options.RightHand;
                if (isInternal) clockwise = !clockwise;

                // Create coil as a NEW BODY (not cut) so we can fillet it first
                CoilFeature coil = compDef.Features.CoilFeatures.AddByPitchAndRevolution(
                    profile,
                    cylinderAxis,
                    pitch,
                    revolutions,
                    PartFeatureOperationEnum.kNewBodyOperation,
                    clockwise);

                coil.Name = options.FeatureName + "_CoilBody";

                // Check if coil direction is correct.
                // The coil starts at the profile position and spirals AWAY from it.
                // So the coil's center should be on the opposite side of profileX from the profile.
                // Expected: coil center moves from profileX toward the cylinder center.
                try
                {
                    double cylCenterX = (sketchMinX + sketchMaxX) / 2.0;
                    // Which direction should the coil grow? From profileX toward cylCenter
                    double expectedDirection = cylCenterX - profileX; // positive = coil should go +X

                    Box coilBox = coil.SurfaceBodies[1].RangeBox;
                    Point2d coilMinSketch = profileSketch.ModelToSketchSpace(coilBox.MinPoint);
                    Point2d coilMaxSketch = profileSketch.ModelToSketchSpace(coilBox.MaxPoint);
                    double coilCenterX = (coilMinSketch.X + coilMaxSketch.X) / 2.0;
                    double actualDirection = coilCenterX - profileX; // positive = coil went +X

                    // If signs differ, the coil went the wrong way
                    if (expectedDirection * actualDirection < 0)
                    {
                        coil.AxisDirectionReversed = !coil.AxisDirectionReversed;
                    }
                }
                catch (Exception dirEx)
                {
                    System.Diagnostics.Debug.WriteLine($"Direction check failed: {dirEx.Message}");
                }

                // Fillet thread root edges on the coil body
                // For internal threads: roots are at outer radius
                // For external threads: roots are at inner radius
                step = "Filleting coil root edges";
                double filletRadius = pitch * 0.125;  // Standard thread root radius (H/8 for ISO metric)
                string filletStatus = "";
                try
                {
                    filletStatus = FilletCoilEdges(compDef, coil, cylinderAxis, filletRadius, isInternal);
                }
                catch (Exception filletEx)
                {
                    filletStatus = $"Fillet skipped: {filletEx.Message}";
                }

                // Now subtract the filleted coil from the original body
                step = "Subtracting filleted coil from cylinder";
                ObjectCollection toolBodies = _inventorApp.TransientObjects.CreateObjectCollection();
                
                // Get the coil's body directly from the coil feature
                // This ensures we get the correct body even if other bodies exist
                SurfaceBody? coilBody = null;
                try
                {
                    // CoilFeature.SurfaceBodies gives us the bodies created by this specific feature
                    if (coil.SurfaceBodies.Count > 0)
                    {
                        coilBody = coil.SurfaceBodies[1];  // 1-indexed in Inventor
                    }
                }
                catch
                {
                    // Fallback: find the newest body that's not the original
                    foreach (SurfaceBody body in compDef.SurfaceBodies)
                    {
                        if (body != originalBody)
                        {
                            coilBody = body;
                            break;
                        }
                    }
                }

                if (coilBody != null)
                {
                    // Trim the coil body at each termination point BEFORE combining
                    // with the shaft. This is a simple extrude-cut through the coil body
                    // that cleanly removes the overshoot at each end.
                    step = "Trimming coil at thread boundaries";
                    try
                    {
                        // Calculate the 3D trim points
                        // Near end: where thread starts (startPt or endPt based on StartFromEnd)
                        // Far end: threadLength from near end, or cylinder end if full-length
                        // For partial threads, we trim at the thread length boundary.
                        // For full-length threads, we trim at the cylinder end if walled.
                        // We may also need to trim the near end if walled.
                        double[] nearEndPt, farEndPt;
                        bool trimNearEnd, trimFarEnd;
                        
                        if (options.StartFromEnd)
                        {
                            nearEndPt = endPt;
                            trimNearEnd = true;  // Always trim to prevent overshoot affecting other features
                            
                            if (threadReachesFarEnd)
                            {
                                farEndPt = startPt;
                                trimFarEnd = true;  // Always trim to prevent overshoot affecting other features
                            }
                            else
                            {
                                // Partial thread: always trim at thread length boundary
                                farEndPt = new double[] {
                                    endPt[0] - axisVec[0] * threadLength,
                                    endPt[1] - axisVec[1] * threadLength,
                                    endPt[2] - axisVec[2] * threadLength
                                };
                                trimFarEnd = true;
                            }
                        }
                        else
                        {
                            nearEndPt = startPt;
                            trimNearEnd = true;  // Always trim to prevent overshoot affecting other features
                            
                            if (threadReachesFarEnd)
                            {
                                farEndPt = endPt;
                                trimFarEnd = true;  // Always trim to prevent overshoot affecting other features
                            }
                            else
                            {
                                farEndPt = new double[] {
                                    startPt[0] + axisVec[0] * threadLength,
                                    startPt[1] + axisVec[1] * threadLength,
                                    startPt[2] + axisVec[2] * threadLength
                                };
                                trimFarEnd = true;
                            }
                        }
                        
                        // Use axisVec to determine outward directions consistently
                        // Near end: thread starts here, outward is opposite to thread direction
                        // Far end: thread ends here, outward is along thread direction
                        // Thread direction = from nearEndPt toward farEndPt
                        double[] threadDir = new double[] {
                            farEndPt[0] - nearEndPt[0],
                            farEndPt[1] - nearEndPt[1],
                            farEndPt[2] - nearEndPt[2]
                        };
                        double tdLen = Math.Sqrt(threadDir[0]*threadDir[0] + threadDir[1]*threadDir[1] + threadDir[2]*threadDir[2]);
                        if (tdLen > 1e-9) { threadDir[0] /= tdLen; threadDir[1] /= tdLen; threadDir[2] /= tdLen; }
                        
                        // Trim near end
                        if (trimNearEnd)
                        {
                            double[] nearOutward = new double[] { -threadDir[0], -threadDir[1], -threadDir[2] };
                            TrimCoilAtPoint(compDef, coilBody, transGeom, axisVec, nearOutward,
                                nearEndPt, options.FeatureName + "_TrimNear");
                        }
                        
                        // Trim far end (thread length boundary or walled cylinder end)
                        if (trimFarEnd)
                        {
                            double[] farOutward = new double[] { threadDir[0], threadDir[1], threadDir[2] };
                            TrimCoilAtPoint(compDef, coilBody, transGeom, axisVec, farOutward,
                                farEndPt, options.FeatureName + "_TrimFar");
                        }
                        
                        // Both sides are always trimmed above, so no additional
                        // partial-thread far-wall trim is needed.
                    }
                    catch (Exception trimEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"Coil trim skipped: {trimEx.Message}");
                    }
                    
                    // Now subtract the trimmed coil from the original body
                    step = "Subtracting trimmed coil from cylinder";
                    toolBodies.Add(coilBody);
                    CombineFeature combine = compDef.Features.CombineFeatures.Add(
                        originalBody,
                        toolBodies,
                        PartFeatureOperationEnum.kCutOperation,
                        true);
                    combine.Name = options.FeatureName;

                    // After cutting, fillet the thread CREST edges on the cylinder
                    step = "Filleting thread crest edges";
                    try
                    {
                        string crestFilletResult = FilletThreadCrests(compDef, cylinderAxis, filletRadius, profileRadius, threadDepth, isInternal, options.FeatureName);
                        if (!string.IsNullOrEmpty(crestFilletResult))
                        {
                            filletStatus += " " + crestFilletResult;
                        }
                    }
                    catch (Exception crestEx)
                    {
                        filletStatus += $" [Crest fillet skipped: {crestEx.Message}]";
                    }
                }
                else
                {
                    filletStatus += " [Warning: Could not find coil body for combine]";
                }

                // Hide construction geometry
                try { profileSketch.Visible = false; } catch { }
                try { cylinderAxis.Visible = false; } catch { }
                try { axisPlane.Visible = false; } catch { }

                // Rename features for organization in feature tree
                string prefix = options.FeatureName;
                
                try
                {
                    if (profileSketch != null) profileSketch.Name = $"{prefix}_Sketch";
                    if (cylinderAxis != null) cylinderAxis.Name = $"{prefix}_Axis";
                    if (axisPlane != null) axisPlane.Name = $"{prefix}_Plane";
                    coil.Name = $"{prefix}_Coil";
                    // Fillet and combine features are already named with the
                    // correct prefix at creation time — no rename loop needed.
                }
                catch { }

                // Features are named with prefix for organization in feature tree

                result.Success = true;
                result.CreatedFeature = coil;
                result.CoilCount = (int)(threadLength / pitch);  // Report actual turns, not extended
                
                string resizeNote = didResize
                    ? $" (resized from Ø{currentRadius*20:F2}mm to Ø{targetRadius*20:F2}mm)"
                    : "";
                string filletNote = !string.IsNullOrEmpty(filletStatus) ? $" [{filletStatus}]" : "";
                result.Message = $"Created {options.ProfileType} {(isInternal ? "internal" : "external")} thread: {threadStd.Designation}, {result.CoilCount} turns{resizeNote}.{filletNote}";
                return result;
            }
            catch (Exception ex)
            {
                string details = $"Step: {step}\n" +
                    $"Cylinder: {cylinderInfo.RadiusCm * 20:F3}mm dia\n" +
                    $"Thread: {threadStd.Designation}\n" +
                    $"Profile: {options.ProfileType}\n" +
                    $"Major Ø: {threadStd.MajorDiameter * 10:F3}mm\n" +
                    $"Minor Ø: {threadStd.MinorDiameter * 10:F3}mm\n" +
                    $"Error: {ex.Message}";
                
                if (ex is System.Runtime.InteropServices.COMException comEx)
                {
                    details += $"\nCOM: 0x{comEx.ErrorCode:X8}";
                }

                result.Message = details;
                return result;
            }
        }

        /// <summary>
        /// Resizes a cylindrical feature to match the target radius.
        /// </summary>
        private bool ResizeCylinder(
            PartComponentDefinition compDef,
            Face cylFace,
            double currentRadius,
            double targetRadius,
            bool isInternal,
            double[] startPt,
            double[] endPt,
            string featureName)
        {
            try
            {
                var transGeom = _inventorApp.TransientGeometry;
                double radiusDiff = targetRadius - currentRadius;

                if (isInternal)
                {
                    // For holes: use a revolve to add/remove material
                    // If target > current, we need to enlarge the hole (cut more)
                    // If target < current, we need to reduce the hole (add material) - this is rare
                    
                    if (Math.Abs(radiusDiff) < 0.0001) return true; // Already correct size

                    // Find a circular edge to use
                    Edge? circularEdge = null;
                    foreach (Edge edge in cylFace.Edges)
                    {
                        if (edge.GeometryType == CurveTypeEnum.kCircleCurve)
                        {
                            circularEdge = edge;
                            break;
                        }
                    }

                    if (circularEdge == null) return false;

                    // Create work axis from the face
                    WorkAxis axis = compDef.WorkAxes.AddByRevolvedFace(cylFace, false);

                    // Get cylinder info
                    Cylinder cyl = (Cylinder)cylFace.Geometry;
                    var basePoint = cyl.BasePoint;

                    // Determine reference plane orientation (one that's not parallel to the axis)
                    var axisVec = cyl.AxisVector;
                    double absZ = Math.Abs(axisVec.Z);
                    double absY = Math.Abs(axisVec.Y);
                    double absX = Math.Abs(axisVec.X);

                    WorkPlane refPlane = absZ > absX && absZ > absY 
                        ? compDef.WorkPlanes[2]  // XZ
                        : compDef.WorkPlanes[3]; // XY

                    // Create a plane containing the axis (not an offset plane)
                    WorkPlane axisPlane = compDef.WorkPlanes.AddByLinePlaneAndAngle(axis, refPlane, "0 deg", false);

                    // Create a sketch on the axis-containing plane
                    PlanarSketch resizeSketch = compDef.Sketches.Add(axisPlane);

                    // Use actual face start/end points to position the resize rectangle
                    // (basePoint is the math origin of the infinite cylinder, not the face extent)
                    Inventor.Point startPt3D = transGeom.CreatePoint(startPt[0], startPt[1], startPt[2]);
                    Inventor.Point endPt3D = transGeom.CreatePoint(endPt[0], endPt[1], endPt[2]);
                    Point2d startPtSketch = resizeSketch.ModelToSketchSpace(startPt3D);
                    Point2d endPtSketch = resizeSketch.ModelToSketchSpace(endPt3D);
                    
                    // Draw a thin rectangle from current radius to target radius along the cylinder
                    double startX = Math.Min(startPtSketch.X, endPtSketch.X);
                    double endX = Math.Max(startPtSketch.X, endPtSketch.X);
                    
                    Point2d c1, c2;
                    if (radiusDiff > 0)
                    {
                        // Enlarging hole: cut from current to target radius
                        c1 = transGeom.CreatePoint2d(startX, currentRadius);
                        c2 = transGeom.CreatePoint2d(endX, targetRadius);
                    }
                    else
                    {
                        // Reducing hole: add from target to current radius
                        c1 = transGeom.CreatePoint2d(startX, targetRadius);
                        c2 = transGeom.CreatePoint2d(endX, currentRadius);
                    }

                    resizeSketch.SketchLines.AddAsTwoPointRectangle(c1, c2);
                    Profile resizeProfile = resizeSketch.Profiles.AddForSolid();

                    // Revolve to resize
                    RevolveFeature revolve = compDef.Features.RevolveFeatures.AddFull(
                        resizeProfile,
                        axis,
                        radiusDiff > 0 ? PartFeatureOperationEnum.kCutOperation : PartFeatureOperationEnum.kJoinOperation);

                    revolve.Name = $"{featureName}_Resize";
                    try { resizeSketch.Visible = false; resizeSketch.Name = $"{featureName}_ResizeSketch"; } catch { }
                    try { axis.Visible = false; axis.Name = $"{featureName}_ResizeAxis"; } catch { }
                    try { axisPlane.Visible = false; axisPlane.Name = $"{featureName}_ResizePlane"; } catch { }

                    return true;
                }
                else
                {
                    // For shafts: same revolve approach but reversed join/cut logic
                    // If target > current, we need to enlarge the shaft (add material)
                    // If target < current, we need to reduce the shaft (cut material)

                    if (Math.Abs(radiusDiff) < 0.0001) return true; // Already correct size

                    // Create work axis from the cylindrical face
                    WorkAxis axis = compDef.WorkAxes.AddByRevolvedFace(cylFace, false);

                    // Get cylinder geometry
                    Cylinder cyl = (Cylinder)cylFace.Geometry;

                    // Determine reference plane orientation (one that's not parallel to the axis)
                    var axisVec = cyl.AxisVector;
                    double absZ = Math.Abs(axisVec.Z);
                    double absY = Math.Abs(axisVec.Y);
                    double absX = Math.Abs(axisVec.X);

                    WorkPlane refPlane = absZ > absX && absZ > absY
                        ? compDef.WorkPlanes[2]  // XZ
                        : compDef.WorkPlanes[3]; // XY

                    // Create a plane containing the axis
                    WorkPlane axisPlane = compDef.WorkPlanes.AddByLinePlaneAndAngle(axis, refPlane, "0 deg", false);

                    // Create a sketch on the axis-containing plane
                    PlanarSketch resizeSketch = compDef.Sketches.Add(axisPlane);

                    // Use actual face start/end points to position the resize rectangle
                    Inventor.Point startPt3D = transGeom.CreatePoint(startPt[0], startPt[1], startPt[2]);
                    Inventor.Point endPt3D = transGeom.CreatePoint(endPt[0], endPt[1], endPt[2]);
                    Point2d startPtSketch = resizeSketch.ModelToSketchSpace(startPt3D);
                    Point2d endPtSketch = resizeSketch.ModelToSketchSpace(endPt3D);

                    // Draw a thin rectangle from current radius to target radius along the cylinder
                    double startX = Math.Min(startPtSketch.X, endPtSketch.X);
                    double endX = Math.Max(startPtSketch.X, endPtSketch.X);

                    Point2d c1, c2;
                    if (radiusDiff > 0)
                    {
                        // Enlarging shaft: add material from current to target radius
                        c1 = transGeom.CreatePoint2d(startX, currentRadius);
                        c2 = transGeom.CreatePoint2d(endX, targetRadius);
                    }
                    else
                    {
                        // Reducing shaft: cut from target to current radius
                        c1 = transGeom.CreatePoint2d(startX, targetRadius);
                        c2 = transGeom.CreatePoint2d(endX, currentRadius);
                    }

                    resizeSketch.SketchLines.AddAsTwoPointRectangle(c1, c2);
                    Profile resizeProfile = resizeSketch.Profiles.AddForSolid();

                    // Revolve to resize — opposite operations from holes:
                    // Enlarging shaft = join, reducing shaft = cut
                    RevolveFeature revolve = compDef.Features.RevolveFeatures.AddFull(
                        resizeProfile,
                        axis,
                        radiusDiff > 0 ? PartFeatureOperationEnum.kJoinOperation : PartFeatureOperationEnum.kCutOperation);

                    revolve.Name = $"{featureName}_Resize";
                    try { resizeSketch.Visible = false; resizeSketch.Name = $"{featureName}_ResizeSketch"; } catch { }
                    try { axis.Visible = false; axis.Name = $"{featureName}_ResizeAxis"; } catch { }
                    try { axisPlane.Visible = false; axisPlane.Name = $"{featureName}_ResizePlane"; } catch { }

                    return true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Cylinder resize failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Draws the thread profile shape based on the selected type.
        /// All profiles follow ISO/DIN standards for thread geometry.
        /// </summary>
        private void DrawThreadProfile(
            PlanarSketch sketch,
            TransientGeometry transGeom,
            double centerX, double centerY,
            double depth, double width, double pitch,
            double cutDirection, bool depthAlongX,
            ThreadProfileType profileType)
        {
            // cutDirection: +1 = outward (internal), -1 = inward (external)
            // depthAlongX: true = depth in X, width in Y; false = depth in Y, width in X
            
            switch (profileType)
            {
                case ThreadProfileType.Triangular:
                    DrawTriangularProfile(sketch, transGeom, centerX, centerY, depth, pitch, cutDirection, depthAlongX);
                    break;
                    
                case ThreadProfileType.Square:
                    DrawSquareProfile(sketch, transGeom, centerX, centerY, depth, width, cutDirection, depthAlongX);
                    break;
                    
                case ThreadProfileType.Trapezoidal:
                default:
                    DrawTrapezoidalProfile(sketch, transGeom, centerX, centerY, depth, width, pitch, cutDirection, depthAlongX);
                    break;
            }
        }

        /// <summary>
        /// ISO Metric triangular thread - 60° V-thread (sharp).
        /// Standard thread angle: 60° included (30° each flank).
        /// Profile is a true triangle with sharp apex.
        /// </summary>
        private void DrawTriangularProfile(
            PlanarSketch sketch, TransientGeometry transGeom,
            double cx, double cy, double depth, double pitch,
            double cutDir, bool depthAlongX)
        {
            // ISO 60° V-thread: sharp triangle profile
            // For 60° thread, H = 0.866 * P, but we use the actual depth passed in
            // Width at base = pitch (full thread width)
            double hw = pitch / 2.0;  // Half of pitch for base width
            
            Point2d p1, p2, p3;
            
            if (depthAlongX)
            {
                // Depth along X, width along Y
                p1 = transGeom.CreatePoint2d(cx, cy - hw);                           // Base left
                p2 = transGeom.CreatePoint2d(cx, cy + hw);                           // Base right
                p3 = transGeom.CreatePoint2d(cx + cutDir * depth, cy);               // Sharp apex
            }
            else
            {
                // Depth along Y, width along X
                p1 = transGeom.CreatePoint2d(cx - hw, cy);                           // Base left
                p2 = transGeom.CreatePoint2d(cx + hw, cy);                           // Base right
                p3 = transGeom.CreatePoint2d(cx, cy + cutDir * depth);               // Apex
            }
            
            // Chain lines together using endpoints to ensure proper closure
            SketchLine line1 = sketch.SketchLines.AddByTwoPoints(p1, p2);
            SketchLine line2 = sketch.SketchLines.AddByTwoPoints(line1.EndSketchPoint, p3);
            sketch.SketchLines.AddByTwoPoints(line2.EndSketchPoint, line1.StartSketchPoint);
        }

        /// <summary>
        /// ISO/DIN Trapezoidal thread (Tr) - 30° included angle.
        /// Acme thread (29° for imperial).
        /// </summary>
        private void DrawTrapezoidalProfile(
            PlanarSketch sketch, TransientGeometry transGeom,
            double cx, double cy, double depth, double width, double pitch,
            double cutDir, bool depthAlongX)
        {
            // Trapezoidal: 30° flank angle (15° each side from vertical)
            // Root width = 0.366 * P, crest width = 0.366 * P
            // This creates the symmetric trapezoid
            double hw = width / 2.0;
            double flankAngle = 15.0 * PI / 180.0;  // 15° in radians
            double topOffset = depth * Math.Tan(flankAngle);
            double topHW = hw - topOffset;
            if (topHW < hw * 0.2) topHW = hw * 0.2;  // Minimum flat at top
            
            Point2d p1, p2, p3, p4;
            
            if (depthAlongX)
            {
                p1 = transGeom.CreatePoint2d(cx, cy - hw);                           // Base left
                p2 = transGeom.CreatePoint2d(cx, cy + hw);                           // Base right
                p3 = transGeom.CreatePoint2d(cx + cutDir * depth, cy + topHW);       // Top right
                p4 = transGeom.CreatePoint2d(cx + cutDir * depth, cy - topHW);       // Top left
            }
            else
            {
                p1 = transGeom.CreatePoint2d(cx - hw, cy);
                p2 = transGeom.CreatePoint2d(cx + hw, cy);
                p3 = transGeom.CreatePoint2d(cx + topHW, cy + cutDir * depth);
                p4 = transGeom.CreatePoint2d(cx - topHW, cy + cutDir * depth);
            }
            
            // Chain lines together using endpoints to ensure proper closure
            SketchLine line1 = sketch.SketchLines.AddByTwoPoints(p1, p2);
            SketchLine line2 = sketch.SketchLines.AddByTwoPoints(line1.EndSketchPoint, p3);
            SketchLine line3 = sketch.SketchLines.AddByTwoPoints(line2.EndSketchPoint, p4);
            sketch.SketchLines.AddByTwoPoints(line3.EndSketchPoint, line1.StartSketchPoint);
        }

        /// <summary>
        /// Square thread profile - 0° flank angle.
        /// Width at root = width at crest = 0.5 * P
        /// </summary>
        private void DrawSquareProfile(
            PlanarSketch sketch, TransientGeometry transGeom,
            double cx, double cy, double depth, double width,
            double cutDir, bool depthAlongX)
        {
            double hw = width / 2.0;
            Point2d c1, c2;
            
            if (depthAlongX)
            {
                if (cutDir > 0)
                {
                    c1 = transGeom.CreatePoint2d(cx, cy - hw);
                    c2 = transGeom.CreatePoint2d(cx + depth, cy + hw);
                }
                else
                {
                    c1 = transGeom.CreatePoint2d(cx - depth, cy - hw);
                    c2 = transGeom.CreatePoint2d(cx, cy + hw);
                }
            }
            else
            {
                if (cutDir > 0)
                {
                    c1 = transGeom.CreatePoint2d(cx - hw, cy);
                    c2 = transGeom.CreatePoint2d(cx + hw, cy + depth);
                }
                else
                {
                    c1 = transGeom.CreatePoint2d(cx - hw, cy - depth);
                    c2 = transGeom.CreatePoint2d(cx + hw, cy);
                }
            }
            
            sketch.SketchLines.AddAsTwoPointRectangle(c1, c2);
        }

        /// <summary>
        /// Gets a vector perpendicular to the given axis vector.
        /// </summary>
        private double[] GetPerpendicularVector(double[] axis)
        {
            double[] candidate = Math.Abs(axis[0]) < 0.9 ? new double[] { 1, 0, 0 } : new double[] { 0, 1, 0 };
            double[] perp = new double[]
            {
                axis[1] * candidate[2] - axis[2] * candidate[1],
                axis[2] * candidate[0] - axis[0] * candidate[2],
                axis[0] * candidate[1] - axis[1] * candidate[0]
            };
            double len = Math.Sqrt(perp[0] * perp[0] + perp[1] * perp[1] + perp[2] * perp[2]);
            if (len > 0.0001) { perp[0] /= len; perp[1] /= len; perp[2] /= len; }
            return perp;
        }

        /// <summary>
        /// Trims the coil body at a specific point along the axis.
        /// Creates a work plane perpendicular to the axis at the trim point, draws a large
        /// rectangle, extrudes it as a new body in the outward direction, then combines
        /// (cut) it from the coil body to cleanly remove all coil geometry past the trim point.
        /// </summary>
        private void TrimCoilAtPoint(
            PartComponentDefinition compDef,
            SurfaceBody coilBody,
            TransientGeometry transGeom,
            double[] axisVec,
            double[] trimDirection,
            double[] trimPoint,
            string featureName)
        {
            try
            {
                // Create a work point at the trim location
                Inventor.Point trimPt3D = transGeom.CreatePoint(trimPoint[0], trimPoint[1], trimPoint[2]);
                WorkPoint trimWorkPoint = compDef.WorkPoints.AddFixed(trimPt3D, false);
                
                // Create work plane perpendicular to axis at trim point
                // Find two perpendicular unit vectors in the trim plane
                double px = 1, py = 0, pz = 0;
                if (Math.Abs(axisVec[0]) > 0.9) { px = 0; py = 1; pz = 0; }
                double crossX = axisVec[1] * pz - axisVec[2] * py;
                double crossY = axisVec[2] * px - axisVec[0] * pz;
                double crossZ = axisVec[0] * py - axisVec[1] * px;
                double crossLen = Math.Sqrt(crossX * crossX + crossY * crossY + crossZ * crossZ);
                if (crossLen > 1e-9) { crossX /= crossLen; crossY /= crossLen; crossZ /= crossLen; }
                UnitVector xAxis = transGeom.CreateUnitVector(crossX, crossY, crossZ);
                
                double yX = axisVec[1] * crossZ - axisVec[2] * crossY;
                double yY = axisVec[2] * crossX - axisVec[0] * crossZ;
                double yZ = axisVec[0] * crossY - axisVec[1] * crossX;
                UnitVector yAxis = transGeom.CreateUnitVector(yX, yY, yZ);
                
                WorkPlane trimWorkPlane = compDef.WorkPlanes.AddFixed(trimPt3D, xAxis, yAxis, false);

                // Create a sketch on this plane with a large rectangle
                PlanarSketch trimSketch = compDef.Sketches.Add(trimWorkPlane);
                double halfSize = 10.0; // 10cm = 100mm
                Point2d corner1 = transGeom.CreatePoint2d(-halfSize, -halfSize);
                Point2d corner2 = transGeom.CreatePoint2d(halfSize, halfSize);
                trimSketch.SketchLines.AddAsTwoPointRectangle(corner1, corner2);

                Profile trimProfile = trimSketch.Profiles.AddForSolid();

                // Determine extrude direction based on trimDirection relative to axisVec
                // The plane normal is axisVec. Positive extrude goes in +axisVec direction.
                // Check if trimDirection aligns with +axisVec or -axisVec
                double dot = trimDirection[0] * axisVec[0] + trimDirection[1] * axisVec[1] + trimDirection[2] * axisVec[2];
                var extrudeDir = dot > 0
                    ? PartFeatureExtentDirectionEnum.kPositiveExtentDirection
                    : PartFeatureExtentDirectionEnum.kNegativeExtentDirection;

                ExtrudeFeature trimExtrude = compDef.Features.ExtrudeFeatures.AddByDistanceExtent(
                    trimProfile,
                    10.0,  // 10cm = 100mm
                    extrudeDir,
                    PartFeatureOperationEnum.kNewBodyOperation);

                trimExtrude.Name = featureName + "_Block";

                SurfaceBody trimBody = trimExtrude.SurfaceBodies[1];

                ObjectCollection trimToolBodies = _inventorApp.TransientObjects.CreateObjectCollection();
                trimToolBodies.Add(trimBody);
                CombineFeature trimCombine = compDef.Features.CombineFeatures.Add(
                    coilBody,
                    trimToolBodies,
                    PartFeatureOperationEnum.kCutOperation,
                    true);

                trimCombine.Name = featureName;
                try 
                { 
                    trimSketch.Visible = false; 
                    trimWorkPlane.Visible = false;
                    trimWorkPoint.Visible = false;
                    trimSketch.Name = featureName + "_Sketch"; 
                    trimWorkPlane.Name = featureName + "_Plane";
                    trimWorkPoint.Name = featureName + "_Point";
                } 
                catch { }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Coil trim failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Applies fillet to the thread root edges on the coil body.
        /// For internal threads: roots are at OUTER radius (further from axis)
        /// For external threads: roots are at INNER radius (closer to axis)
        /// </summary>
        private string FilletCoilEdges(PartComponentDefinition compDef, CoilFeature coil, WorkAxis cylinderAxis, double filletRadius, bool isInternal = true)
        {
            // Get axis geometry for distance calculations
            Inventor.Line axisLine = (Inventor.Line)cylinderAxis.Line;
            Inventor.Point axisPoint = axisLine.RootPoint;
            UnitVector axisDir = axisLine.Direction;
            
            // First pass: find all edges and their distances from axis
            var edgeDistances = new List<(Edge edge, double distance)>();
            double maxDistance = 0;
            double minDistance = double.MaxValue;
            
            foreach (Face face in coil.Faces)
            {
                foreach (Edge edge in face.Edges)
                {
                    try
                    {
                        // Get edge midpoint to calculate distance from axis
                        Inventor.Point midPt = edge.PointOnEdge;
                        
                        // Vector from axis to point
                        double vx = midPt.X - axisPoint.X;
                        double vy = midPt.Y - axisPoint.Y;
                        double vz = midPt.Z - axisPoint.Z;
                        
                        // Project onto axis direction
                        double dot = vx * axisDir.X + vy * axisDir.Y + vz * axisDir.Z;
                        
                        // Perpendicular component (distance from axis)
                        double px = vx - dot * axisDir.X;
                        double py = vy - dot * axisDir.Y;
                        double pz = vz - dot * axisDir.Z;
                        double dist = Math.Sqrt(px*px + py*py + pz*pz);
                        
                        edgeDistances.Add((edge, dist));
                        if (dist > maxDistance) maxDistance = dist;
                        if (dist < minDistance) minDistance = dist;
                    }
                    catch
                    {
                        // Skip edges we can't analyze
                    }
                }
            }
            
            if (edgeDistances.Count == 0)
            {
                return "No coil edges found";
            }
            
            // Second pass: select root edges based on thread type
            // For internal threads: roots are at OUTER radius (further from axis)
            // For external threads: roots are at INNER radius (closer to axis)
            EdgeCollection edgesToFillet = _inventorApp.TransientObjects.CreateEdgeCollection();
            double range = maxDistance - minDistance;
            
            foreach (var (edge, distance) in edgeDistances)
            {
                bool includeEdge;
                if (isInternal)
                {
                    // Internal thread: roots are at outer radius (top 15%)
                    double outerThreshold = maxDistance - range * 0.15;
                    includeEdge = distance >= outerThreshold;
                }
                else
                {
                    // External thread: roots are at inner radius (bottom 15%)
                    double innerThreshold = minDistance + range * 0.15;
                    includeEdge = distance <= innerThreshold;
                }
                
                if (includeEdge)
                {
                    try
                    {
                        edgesToFillet.Add(edge);
                    }
                    catch
                    {
                        // Edge already in collection
                    }
                }
            }
            
            if (edgesToFillet.Count == 0)
            {
                return "No root edges found to fillet";
            }
            
            // Create fillet on root edges
            FilletFeature fillet = compDef.Features.FilletFeatures.AddSimple(
                edgesToFillet,
                filletRadius);
            fillet.Name = $"{coil.Name}_RootFillet";
            return $"Filleted {edgesToFillet.Count} root edges, R={filletRadius*10:F3}mm";
        }

        /// <summary>
        /// Applies fillet to thread CREST edges AFTER cutting the thread.
        /// For internal threads: crests are at the inner radius (cylinder wall).
        /// For external threads: crests are at the outer radius (thread peaks).
        /// </summary>
        private string FilletThreadCrests(PartComponentDefinition compDef, WorkAxis cylinderAxis, double filletRadius, double cylinderRadius, double threadDepth, bool isInternal, string featurePrefix)
        {
            // Get axis geometry for distance calculations
            Inventor.Line axisLine = (Inventor.Line)cylinderAxis.Line;
            Inventor.Point axisPoint = axisLine.RootPoint;
            UnitVector axisDir = axisLine.Direction;
            
            // Determine the target radius for crests
            // For BOTH internal and external threads, crests are at the cylinder surface (profileRadius)
            // - Internal: thread crests point inward at the hole wall radius
            // - External: thread crests remain at the shaft surface radius (the coil cuts roots INTO the shaft)
            double crestRadius = cylinderRadius;
            
            // Find edges that are:
            // 1. Helical (not circular or linear)
            // 2. At approximately the crest radius
            EdgeCollection edgesToFillet = _inventorApp.TransientObjects.CreateEdgeCollection();
            int edgeCount = 0;
            
            foreach (SurfaceBody body in compDef.SurfaceBodies)
            {
                foreach (Edge edge in body.Edges)
                {
                    try
                    {
                        // Skip circular edges (end caps) and straight edges
                        if (edge.GeometryType == CurveTypeEnum.kCircleCurve ||
                            edge.GeometryType == CurveTypeEnum.kLineCurve)
                            continue;
                        
                        // Get edge midpoint distance from axis
                        Inventor.Point midPt = edge.PointOnEdge;
                        double vx = midPt.X - axisPoint.X;
                        double vy = midPt.Y - axisPoint.Y;
                        double vz = midPt.Z - axisPoint.Z;
                        double dot = vx * axisDir.X + vy * axisDir.Y + vz * axisDir.Z;
                        double px = vx - dot * axisDir.X;
                        double py = vy - dot * axisDir.Y;
                        double pz = vz - dot * axisDir.Z;
                        double dist = Math.Sqrt(px*px + py*py + pz*pz);
                        
                        // Check if edge is near the crest radius
                        // Allow 20% tolerance for thread profile variations
                        if (dist >= crestRadius * 0.85 && dist <= crestRadius * 1.15)
                        {
                            edgesToFillet.Add(edge);
                            edgeCount++;
                        }
                    }
                    catch
                    {
                        // Skip edges we can't analyze
                    }
                }
            }
            
            if (edgesToFillet.Count == 0)
            {
                return "[No crest edges found]";
            }
            
            // Create fillet on crest edges
            FilletFeature fillet = compDef.Features.FilletFeatures.AddSimple(
                edgesToFillet,
                filletRadius);
            fillet.Name = $"{featurePrefix}_CrestFillet";
            return $"[Crest: {edgeCount} edges]";
        }
    }
}
