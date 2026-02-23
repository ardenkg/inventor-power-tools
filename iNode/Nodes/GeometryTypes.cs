// ============================================================================
// iNode Add-in for Autodesk Inventor 2026
// GeometryTypes.cs - Data types flowing through geometry ports
// ============================================================================

using System;
using System.Collections.Generic;

namespace iNode.Nodes
{
    /// <summary>
    /// Wraps an Inventor SurfaceBody for flowing through the node graph.
    /// This is the PRIMARY geometry data type — every geometry node produces one.
    /// The Body property holds a live transient SurfaceBody COM object created
    /// via TransientBRep, enabling real preview and boolean operations.
    /// </summary>
    public class BodyData
    {
        /// <summary>
        /// The Inventor SurfaceBody COM object (transient).
        /// Stored as object to allow graph infrastructure to work without Inventor.
        /// Cast to Inventor.SurfaceBody when interacting with the API.
        /// </summary>
        public object? Body { get; set; }

        /// <summary>Human-readable description (e.g., "Box", "Boolean Result").</summary>
        public string Description { get; set; } = "";

        /// <summary>The node that created this body.</summary>
        public Guid SourceNodeId { get; set; }

        /// <summary>
        /// Whether this body originated from the active part's existing geometry.
        /// Used during Apply to determine how to integrate the result.
        /// </summary>
        public bool IsFromActivePart { get; set; }

        /// <summary>
        /// Pending feature operations (fillet, chamfer) that cannot be performed
        /// on transient bodies. Applied during the Apply phase.
        /// </summary>
        public List<PendingOperation> PendingOperations { get; set; } = new();

        /// <summary>
        /// Creates a shallow clone with the same body reference
        /// but an independent pending operations list.
        /// </summary>
        public BodyData CloneMetadata()
        {
            return new BodyData
            {
                Body = Body,
                Description = Description,
                SourceNodeId = SourceNodeId,
                IsFromActivePart = IsFromActivePart,
                PendingOperations = new List<PendingOperation>(PendingOperations)
            };
        }

        public override string ToString() => Description;
    }

    /// <summary>
    /// Wraps a list of Inventor Face COM objects extracted from a SurfaceBody.
    /// </summary>
    public class FaceListData
    {
        /// <summary>List of Face COM objects.</summary>
        public List<object> Faces { get; set; } = new();

        /// <summary>The parent body these faces belong to.</summary>
        public BodyData? ParentBody { get; set; }

        /// <summary>Display description.</summary>
        public string Description => $"{Faces.Count} face(s)";

        public override string ToString() => Description;
    }

    /// <summary>
    /// Wraps a list of Inventor Edge COM objects extracted from a SurfaceBody.
    /// </summary>
    public class EdgeListData
    {
        /// <summary>List of Edge COM objects.</summary>
        public List<object> Edges { get; set; } = new();

        /// <summary>The parent body these edges belong to.</summary>
        public BodyData? ParentBody { get; set; }

        /// <summary>Display description.</summary>
        public string Description => $"{Edges.Count} edge(s)";

        public override string ToString() => Description;
    }

    // ========================================================================
    // Pending Operations — deferred to the Apply phase
    // ========================================================================

    /// <summary>
    /// Base class for operations that require real Inventor part features
    /// and cannot be performed on transient bodies.
    /// </summary>
    public abstract class PendingOperation
    {
        /// <summary>Display name for status/logging.</summary>
        public abstract string OperationName { get; }

        /// <summary>
        /// The ID of the node that CREATED this operation (e.g. the Fillet node),
        /// distinct from the body's SourceNodeId. Used for per-feature deletion.
        /// </summary>
        public Guid OriginNodeId { get; set; }
    }

    /// <summary>
    /// Records a fillet operation to be applied during the Apply phase.
    /// </summary>
    public class PendingFillet : PendingOperation
    {
        public override string OperationName => "Fillet";

        /// <summary>Fillet radius in mm (will be converted to cm at apply time).</summary>
        public double Radius { get; set; }

        /// <summary>Geometric snapshots of edges to fillet, for matching after body insertion.</summary>
        public List<EdgeSnapshot> EdgeSnapshots { get; set; } = new();

        /// <summary>
        /// Live Inventor Edge COM objects for preview highlighting.
        /// These are only valid on the transient body during preview — they become
        /// invalid after NonParametricBaseFeature insertion (use EdgeSnapshots then).
        /// </summary>
        public List<object> LiveEdges { get; set; } = new();
    }

    /// <summary>
    /// Records a chamfer operation to be applied during the Apply phase.
    /// </summary>
    public class PendingChamfer : PendingOperation
    {
        public override string OperationName => "Chamfer";

        /// <summary>Chamfer distance in mm.</summary>
        public double Distance { get; set; }

        /// <summary>Geometric snapshots of edges to chamfer.</summary>
        public List<EdgeSnapshot> EdgeSnapshots { get; set; } = new();

        /// <summary>
        /// Live Inventor Edge COM objects for preview highlighting.
        /// Valid only on the transient body during preview.
        /// </summary>
        public List<object> LiveEdges { get; set; } = new();
    }

    /// <summary>
    /// Records a color-face operation to be applied during the Apply phase.
    /// Colors specific faces on the committed body by matching FaceSnapshots.
    /// Uses the same Appearance asset approach as the ColoringTool add-in.
    /// </summary>
    public class PendingColorFaces : PendingOperation
    {
        public override string OperationName => "Color Faces";

        /// <summary>Target color RGB (0-255 each).</summary>
        public int Red { get; set; }
        public int Green { get; set; }
        public int Blue { get; set; }

        /// <summary>Geometric snapshots of faces to color, for matching after body insertion.</summary>
        public List<FaceSnapshot> FaceSnapshots { get; set; } = new();

        /// <summary>
        /// Live Face COM objects for preview highlighting.
        /// Only valid on the transient body.
        /// </summary>
        public List<object> LiveFaces { get; set; } = new();
    }

    /// <summary>
    /// Records a shell operation to be applied during the Apply phase.
    /// Shells the body with a specified wall thickness, optionally removing faces.
    /// </summary>
    public class PendingShell : PendingOperation
    {
        public override string OperationName => "Shell";

        /// <summary>Wall thickness in mm.</summary>
        public double Thickness { get; set; }

        /// <summary>Geometric snapshots of faces to remove (open faces).</summary>
        public List<FaceSnapshot> FaceSnapshots { get; set; } = new();

        /// <summary>Live Face COM objects for preview.</summary>
        public List<object> LiveFaces { get; set; } = new();
    }

    /// <summary>
    /// Records a hole operation to be applied during the Apply phase.
    /// </summary>
    public class PendingHole : PendingOperation
    {
        public override string OperationName => "Hole";

        /// <summary>Type: Drilled, Counterbore, Countersink, Tapped, CBore+Tapped, CSink+Tapped.</summary>
        public string HoleType { get; set; } = "Drilled";

        /// <summary>Extent: ThroughAll or ToDepth.</summary>
        public string Extent { get; set; } = "ThroughAll";

        /// <summary>Hole diameter in mm.</summary>
        public double Diameter { get; set; }

        /// <summary>Hole depth in mm (used when Extent=ToDepth).</summary>
        public double Depth { get; set; }

        /// <summary>Counterbore diameter in mm.</summary>
        public double CounterboreDiameter { get; set; }

        /// <summary>Counterbore depth in mm.</summary>
        public double CounterboreDepth { get; set; }

        /// <summary>Countersink diameter in mm.</summary>
        public double CountersinkDiameter { get; set; }

        /// <summary>Countersink angle in degrees (default 82).</summary>
        public double CountersinkAngle { get; set; } = 82.0;

        /// <summary>Thread standard for tapped holes.</summary>
        public string ThreadStandard { get; set; } = "ISO Metric profile";

        /// <summary>Thread designation for tapped holes.</summary>
        public string ThreadDesignation { get; set; } = "M8x1.25";

        /// <summary>Right-hand thread (for tapped holes).</summary>
        public bool IsRightHand { get; set; } = true;

        /// <summary>Geometric snapshots of faces where holes are placed.</summary>
        public List<FaceSnapshot> FaceSnapshots { get; set; } = new();

        /// <summary>Live Face COM objects.</summary>
        public List<object> LiveFaces { get; set; } = new();
    }

    /// <summary>
    /// Records a draft/taper operation to be applied during the Apply phase.
    /// </summary>
    public class PendingDraft : PendingOperation
    {
        public override string OperationName => "Draft";

        /// <summary>Draft angle in degrees.</summary>
        public double AngleDegrees { get; set; }

        /// <summary>Pull direction vector components.</summary>
        public double PullDirectionX { get; set; }
        public double PullDirectionY { get; set; }
        public double PullDirectionZ { get; set; }

        /// <summary>Geometric snapshots of faces to draft.</summary>
        public List<FaceSnapshot> FaceSnapshots { get; set; } = new();

        /// <summary>Live Face COM objects.</summary>
        public List<object> LiveFaces { get; set; } = new();
    }

    /// <summary>
    /// Records a thicken operation to be applied during the Apply phase.
    /// </summary>
    public class PendingThicken : PendingOperation
    {
        public override string OperationName => "Thicken";

        /// <summary>Thickness in mm (positive = outward, negative = inward).</summary>
        public double Thickness { get; set; }
    }

    /// <summary>
    /// Stores geometric properties of a face for re-matching after body insertion.
    /// Uses a centroid point on the face for proximity matching.
    /// </summary>
    public class FaceSnapshot
    {
        /// <summary>A point on the face (vertex centroid).</summary>
        public double PointX { get; set; }
        public double PointY { get; set; }
        public double PointZ { get; set; }

        /// <summary>
        /// Creates a snapshot from a live Inventor Face COM object.
        /// Uses vertex centroid for robust matching across transient → committed bodies.
        /// </summary>
        public static FaceSnapshot FromInventorFace(object faceObj)
        {
            dynamic face = faceObj;

            // Primary: compute centroid from face vertices (most reliable)
            try
            {
                double sumX = 0, sumY = 0, sumZ = 0;
                int count = 0;
                foreach (var vertex in face.Vertices)
                {
                    dynamic v = vertex;
                    var pt = v.Point;
                    sumX += (double)pt.X;
                    sumY += (double)pt.Y;
                    sumZ += (double)pt.Z;
                    count++;
                }
                if (count > 0)
                    return new FaceSnapshot { PointX = sumX / count, PointY = sumY / count, PointZ = sumZ / count };
            }
            catch { }

            // Fallback: UV evaluator midpoint
            var evaluator = face.Evaluator;
            double[] paramRange = new double[4];
            evaluator.GetParamRangeRect(ref paramRange);
            double midU = (paramRange[0] + paramRange[1]) / 2.0;
            double midV = (paramRange[2] + paramRange[3]) / 2.0;
            double[] paramArray = new double[] { midU, midV };
            double[] point = new double[3];
            evaluator.GetPointAtParam(ref paramArray, ref point);
            return new FaceSnapshot { PointX = point[0], PointY = point[1], PointZ = point[2] };
        }

        /// <summary>
        /// Finds the closest matching face on a body using vertex centroid comparison.
        /// Tolerance increased to 1.0 cm to handle reparameterization differences.
        /// </summary>
        public object? FindMatchingFace(object bodyObj, double tolerance = 1.0)
        {
            dynamic body = bodyObj;
            object? bestMatch = null;
            double bestDist = double.MaxValue;

            foreach (var faceObj in body.Faces)
            {
                try
                {
                    dynamic face = faceObj;
                    double fx = 0, fy = 0, fz = 0;
                    bool found = false;

                    // Primary: vertex centroid
                    try
                    {
                        double sumX = 0, sumY = 0, sumZ = 0;
                        int count = 0;
                        foreach (var vertex in face.Vertices)
                        {
                            dynamic v = vertex;
                            var pt = v.Point;
                            sumX += (double)pt.X;
                            sumY += (double)pt.Y;
                            sumZ += (double)pt.Z;
                            count++;
                        }
                        if (count > 0) { fx = sumX / count; fy = sumY / count; fz = sumZ / count; found = true; }
                    }
                    catch { }

                    // Fallback: UV evaluator  
                    if (!found)
                    {
                        try
                        {
                            var evaluator = face.Evaluator;
                            double[] paramRange = new double[4];
                            evaluator.GetParamRangeRect(ref paramRange);
                            double midU = (paramRange[0] + paramRange[1]) / 2.0;
                            double midV = (paramRange[2] + paramRange[3]) / 2.0;
                            double[] paramArray = new double[] { midU, midV };
                            double[] pt = new double[3];
                            evaluator.GetPointAtParam(ref paramArray, ref pt);
                            fx = pt[0]; fy = pt[1]; fz = pt[2]; found = true;
                        }
                        catch { }
                    }
                    if (!found) continue;

                    double dx = fx - PointX, dy = fy - PointY, dz = fz - PointZ;
                    double dist = Math.Sqrt(dx * dx + dy * dy + dz * dz);
                    if (dist < bestDist) { bestDist = dist; bestMatch = faceObj; }
                }
                catch { }
            }

            return bestDist < tolerance ? bestMatch : null;
        }
    }

    /// <summary>
    /// Stores geometric properties of an edge for re-matching after body insertion.
    /// When a transient body is inserted into the part via NonParametricBaseFeature,
    /// edge COM references become invalid. This snapshot allows finding the
    /// corresponding edge on the feature body by geometric proximity.
    /// </summary>
    public class EdgeSnapshot
    {
        /// <summary>A point on the edge (typically the midpoint).</summary>
        public double PointX { get; set; }
        public double PointY { get; set; }
        public double PointZ { get; set; }

        /// <summary>
        /// Creates a snapshot from a live Inventor Edge COM object.
        /// </summary>
        public static EdgeSnapshot FromInventorEdge(object edgeObj)
        {
            // Use dynamic dispatch to access COM properties
            dynamic edge = edgeObj;
            var pt = edge.PointOnEdge;
            return new EdgeSnapshot
            {
                PointX = (double)pt.X,
                PointY = (double)pt.Y,
                PointZ = (double)pt.Z
            };
        }

        /// <summary>
        /// Finds the closest matching edge on a body by comparing PointOnEdge.
        /// Returns the matching edge COM object, or null if no close match.
        /// </summary>
        public object? FindMatchingEdge(object bodyObj, double tolerance = 0.05)
        {
            dynamic body = bodyObj;
            object? bestMatch = null;
            double bestDist = double.MaxValue;

            foreach (var edgeObj in body.Edges)
            {
                dynamic edge = edgeObj;
                var pt = edge.PointOnEdge;
                double dx = (double)pt.X - PointX;
                double dy = (double)pt.Y - PointY;
                double dz = (double)pt.Z - PointZ;
                double dist = Math.Sqrt(dx * dx + dy * dy + dz * dz);

                if (dist < bestDist)
                {
                    bestDist = dist;
                    bestMatch = edgeObj;
                }
            }

            return bestDist < tolerance ? bestMatch : null;
        }
    }

    // ========================================================================
    // Sketch & Work Plane Reference Types
    // ========================================================================

    /// <summary>
    /// Wraps a reference to an Inventor PlanarSketch for flowing through the node graph.
    /// Extrude and Revolve nodes consume this to create parametric features.
    /// </summary>
    public class SketchRefData
    {
        /// <summary>The Inventor PlanarSketch COM object.</summary>
        public object? Sketch { get; set; }

        /// <summary>Name of the sketch for display.</summary>
        public string SketchName { get; set; } = "";

        /// <summary>Number of profiles in the sketch.</summary>
        public int ProfileCount { get; set; }

        /// <summary>Display description.</summary>
        public string Description => string.IsNullOrEmpty(SketchName)
            ? "Sketch Reference"
            : $"Sketch: {SketchName} ({ProfileCount} profile(s))";

        public override string ToString() => Description;
    }

    /// <summary>
    /// Wraps a reference to an Inventor WorkPlane for flowing through the node graph.
    /// </summary>
    public class WorkPlaneRefData
    {
        /// <summary>The Inventor WorkPlane COM object.</summary>
        public object? WorkPlane { get; set; }

        /// <summary>Name for display.</summary>
        public string PlaneName { get; set; } = "";

        public string Description => string.IsNullOrEmpty(PlaneName)
            ? "Work Plane"
            : $"Plane: {PlaneName}";

        public override string ToString() => Description;
    }

    // ========================================================================
    // Plane & Sketch Profile Data (for sketch-based geometry creation)
    // ========================================================================

    /// <summary>
    /// Describes a reference plane in 3D space — origin + orthonormal axes.
    /// Used to position sketches for extrude/revolve operations.
    /// All coordinates are in cm (Inventor internal units).
    /// </summary>
    public class PlaneData
    {
        public double OriginX { get; set; }
        public double OriginY { get; set; }
        public double OriginZ { get; set; }
        public double NormalX { get; set; }
        public double NormalY { get; set; }
        public double NormalZ { get; set; }
        public double XAxisX { get; set; }
        public double XAxisY { get; set; }
        public double XAxisZ { get; set; }
        public double YAxisX { get; set; }
        public double YAxisY { get; set; }
        public double YAxisZ { get; set; }
        public string Name { get; set; } = "Plane";

        /// <summary>Index of standard work plane (1=YZ, 2=XZ, 3=XY), or 0 for custom.</summary>
        public int StandardPlaneIndex { get; set; }

        /// <summary>Offset from the standard plane in cm (0 = on the plane).</summary>
        public double OffsetCm { get; set; }

        /// <summary>Optional face COM reference for face-based planes.</summary>
        public object? FaceRef { get; set; }

        public override string ToString() => Name;

        /// <summary>Convert a 2D point in sketch coordinates (mm) to 3D world point (cm).</summary>
        public void To3D(double xMm, double yMm, out double wx, out double wy, out double wz)
        {
            double xCm = xMm * 0.1;
            double yCm = yMm * 0.1;
            wx = OriginX + xCm * XAxisX + yCm * YAxisX;
            wy = OriginY + xCm * XAxisY + yCm * YAxisY;
            wz = OriginZ + xCm * XAxisZ + yCm * YAxisZ;
        }

        public static PlaneData XY(double offsetMm = 0) => new PlaneData
        {
            Name = offsetMm == 0 ? "XY Plane" : $"XY + {offsetMm}mm",
            StandardPlaneIndex = 3,
            OriginX = 0, OriginY = 0, OriginZ = offsetMm * 0.1,
            NormalX = 0, NormalY = 0, NormalZ = 1,
            XAxisX = 1, XAxisY = 0, XAxisZ = 0,
            YAxisX = 0, YAxisY = 1, YAxisZ = 0,
            OffsetCm = offsetMm * 0.1
        };

        public static PlaneData XZ(double offsetMm = 0) => new PlaneData
        {
            Name = offsetMm == 0 ? "XZ Plane" : $"XZ + {offsetMm}mm",
            StandardPlaneIndex = 2,
            OriginX = 0, OriginY = offsetMm * 0.1, OriginZ = 0,
            NormalX = 0, NormalY = 1, NormalZ = 0,
            XAxisX = 1, XAxisY = 0, XAxisZ = 0,
            YAxisX = 0, YAxisY = 0, YAxisZ = 1,
            OffsetCm = offsetMm * 0.1
        };

        public static PlaneData YZ(double offsetMm = 0) => new PlaneData
        {
            Name = offsetMm == 0 ? "YZ Plane" : $"YZ + {offsetMm}mm",
            StandardPlaneIndex = 1,
            OriginX = offsetMm * 0.1, OriginY = 0, OriginZ = 0,
            NormalX = 1, NormalY = 0, NormalZ = 0,
            XAxisX = 0, XAxisY = 1, XAxisZ = 0,
            YAxisX = 0, YAxisY = 0, YAxisZ = 1,
            OffsetCm = offsetMm * 0.1
        };
    }

    /// <summary>
    /// Describes a 2D sketch profile on a plane — a collection of curves
    /// that form a closed profile for extrusion/revolution.
    /// All dimensions are in mm (user units).
    /// </summary>
    public class SketchProfileData
    {
        public PlaneData Plane { get; set; } = PlaneData.XY();
        public List<ProfileCurve> Curves { get; set; } = new();
        public string Description => Curves.Count == 1
            ? Curves[0].Describe()
            : $"{Curves.Count} curve(s)";
        public override string ToString() => Description;
    }

    /// <summary>Base class for 2D sketch profile curves. Dimensions in mm.</summary>
    public abstract class ProfileCurve
    {
        public abstract string Describe();
    }

    /// <summary>A circle in sketch space (center X/Y in mm, radius in mm).</summary>
    public class CircleProfileCurve : ProfileCurve
    {
        public double CenterX { get; set; }
        public double CenterY { get; set; }
        public double Radius { get; set; }
        public override string Describe() => $"Circle(R={Radius:F1}mm)";
    }

    /// <summary>A rectangle in sketch space (center X/Y, width, height — all in mm).</summary>
    public class RectangleProfileCurve : ProfileCurve
    {
        public double CenterX { get; set; }
        public double CenterY { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public override string Describe() => $"Rect({Width:F1}\u00D7{Height:F1}mm)";
    }

    /// <summary>A line in sketch space (start/end X/Y in mm).</summary>
    public class LineProfileCurve : ProfileCurve
    {
        public double StartX { get; set; }
        public double StartY { get; set; }
        public double EndX { get; set; }
        public double EndY { get; set; }
        public override string Describe() => $"Line({StartX:F1},{StartY:F1}→{EndX:F1},{EndY:F1})";
    }

    /// <summary>A regular polygon in sketch space (center, radius, sides — all in mm).</summary>
    public class PolygonProfileCurve : ProfileCurve
    {
        public double CenterX { get; set; }
        public double CenterY { get; set; }
        public double Radius { get; set; }
        public int Sides { get; set; } = 6;
        public override string Describe() => $"Polygon({Sides} sides, R={Radius:F1}mm)";
    }

    /// <summary>A slot (obround/stadium) profile in sketch space.</summary>
    public class SlotProfileCurve : ProfileCurve
    {
        public double CenterX { get; set; }
        public double CenterY { get; set; }
        public double Length { get; set; }
        public double Width { get; set; }
        public override string Describe() => $"Slot({Length:F1}\u00D7{Width:F1}mm)";
    }

    /// <summary>An ellipse in sketch space (center, major/minor radii — all in mm).</summary>
    public class EllipseProfileCurve : ProfileCurve
    {
        public double CenterX { get; set; }
        public double CenterY { get; set; }
        public double MajorRadius { get; set; }
        public double MinorRadius { get; set; }
        public override string Describe() => $"Ellipse({MajorRadius:F1}\u00D7{MinorRadius:F1}mm)";
    }

    // ========================================================================
    // Legacy GeometryData — kept for backward compatibility with old workflows
    // ========================================================================

    /// <summary>
    /// [LEGACY] Data class carrying geometry creation parameters.
    /// Replaced by BodyData in the new transient body execution model.
    /// Kept for backward compatibility with serialized workflows.
    /// </summary>
    public class GeometryData
    {
        public string Type { get; set; } = "";
        public double Width { get; set; }
        public double Height { get; set; }
        public double Depth { get; set; }
        public double Radius { get; set; }
        public double CenterX { get; set; }
        public double CenterY { get; set; }
        public double CenterZ { get; set; }
        public object? FeatureReference { get; set; }
        public double MoveX { get; set; }
        public double MoveY { get; set; }
        public double MoveZ { get; set; }
        public int Count { get; set; }
        public double Spacing { get; set; }
        public string Direction { get; set; } = "X";
        public string BooleanOperation { get; set; } = "";
        public GeometryData? BooleanOperandA { get; set; }
        public GeometryData? BooleanOperandB { get; set; }
    }

    // ====================================================================
    // Data List / Tree Types
    // ====================================================================

    /// <summary>
    /// A generic list of values that can flow through the node graph.
    /// Supports lists of any type (numbers, points, bodies, etc.).
    /// Can be nested (list of lists) for tree-like structures.
    /// </summary>
    public class DataList
    {
        /// <summary>The items in this list.</summary>
        public List<object?> Items { get; set; } = new();

        /// <summary>Number of items in the list.</summary>
        public int Count => Items.Count;

        /// <summary>Description for display.</summary>
        public string Description => $"List [{Count} items]";

        /// <summary>Creates a DataList from a single item (graft).</summary>
        public static DataList FromItem(object? item)
        {
            var list = new DataList();
            list.Items.Add(item);
            return list;
        }

        /// <summary>Creates a DataList from multiple items.</summary>
        public static DataList FromItems(params object?[] items)
        {
            var list = new DataList();
            list.Items.AddRange(items);
            return list;
        }

        /// <summary>
        /// Flattens this list: if items are DataLists, their items are inlined.
        /// Returns a new flat DataList.
        /// </summary>
        public DataList Flatten()
        {
            var result = new DataList();
            FlattenInto(result.Items, Items);
            return result;
        }

        private static void FlattenInto(List<object?> target, List<object?> source)
        {
            foreach (var item in source)
            {
                if (item is DataList subList)
                    FlattenInto(target, subList.Items);
                else
                    target.Add(item);
            }
        }

        /// <summary>
        /// Grafts this list: wraps each item in its own DataList.
        /// </summary>
        public DataList Graft()
        {
            var result = new DataList();
            foreach (var item in Items)
                result.Items.Add(DataList.FromItem(item));
            return result;
        }

        /// <summary>
        /// Reverses the list order. Returns a new DataList.
        /// </summary>
        public DataList Reverse()
        {
            var result = new DataList();
            for (int i = Items.Count - 1; i >= 0; i--)
                result.Items.Add(Items[i]);
            return result;
        }

        /// <summary>Gets item at index (clamped to valid range).</summary>
        public object? GetItem(int index)
        {
            if (Items.Count == 0) return null;
            index = Math.Max(0, Math.Min(index, Items.Count - 1));
            return Items[index];
        }
    }
}
