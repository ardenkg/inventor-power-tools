// ============================================================================
// iNode Add-in for Autodesk Inventor 2026
// InventorContext.cs - Provides Inventor API access during graph execution
// ============================================================================

using System;

using Inventor;

namespace iNode.Core
{
    /// <summary>
    /// Controls whether the graph is executing for live preview (transient)
    /// or for final commit (real Inventor features).
    /// </summary>
    public enum ExecutionMode
    {
        /// <summary>Produce transient bodies for visual preview only.</summary>
        Preview,

        /// <summary>Create real Inventor features in the document.</summary>
        Commit
    }

    /// <summary>
    /// Provides access to Inventor application objects during graph execution.
    /// Nodes that create geometry receive this context so they can build
    /// real transient SurfaceBody objects via TransientBRep.
    /// </summary>
    public class InventorContext
    {
        /// <summary>The Inventor application reference.</summary>
        public Inventor.Application App { get; }

        /// <summary>Helper for creating points, vectors, matrices.</summary>
        public TransientGeometry TG { get; }

        /// <summary>Helper for creating and operating on transient solid bodies.</summary>
        public TransientBRep TB { get; }

        /// <summary>The active part document (null if no part is open).</summary>
        public PartDocument? ActivePartDoc { get; }

        /// <summary>The active part's component definition (null if no part is open).</summary>
        public PartComponentDefinition? ActiveCompDef { get; }

        /// <summary>
        /// The target part document explicitly selected by the user.
        /// Falls back to ActivePartDoc if not set.
        /// </summary>
        public PartDocument? TargetPartDoc { get; }

        /// <summary>
        /// The target part's component definition.
        /// Falls back to ActiveCompDef if not set.
        /// </summary>
        public PartComponentDefinition? TargetCompDef { get; }

        /// <summary>Whether the context has a valid Inventor connection.</summary>
        public bool IsAvailable => App != null;

        /// <summary>Whether there is an active part document to work with.</summary>
        public bool HasActivePart => TargetPartDoc != null || ActivePartDoc != null;

        /// <summary>
        /// The execution mode: Preview (transient bodies) or Commit (real features).
        /// </summary>
        public ExecutionMode Mode { get; set; } = ExecutionMode.Preview;

        /// <summary>Whether we are in Commit mode (convenience property).</summary>
        public bool IsCommitting => Mode == ExecutionMode.Commit;

        /// <summary>Conversion factor from node units (mm) to Inventor internal (cm).</summary>
        public const double MM_TO_CM = 0.1;

        public InventorContext(Inventor.Application app)
            : this(app, null)
        {
        }

        /// <summary>
        /// Creates a context targeting a specific document.
        /// If targetDoc is null, falls back to the ActiveDocument.
        /// </summary>
        public InventorContext(Inventor.Application app, PartDocument? targetDoc)
        {
            App = app ?? throw new ArgumentNullException(nameof(app));
            TG = app.TransientGeometry;
            TB = app.TransientBRep;

            // Set target explicitly if provided
            if (targetDoc != null)
            {
                TargetPartDoc = targetDoc;
                TargetCompDef = targetDoc.ComponentDefinition;
            }

            try
            {
                var doc = app.ActiveDocument;
                if (doc?.DocumentType == DocumentTypeEnum.kPartDocumentObject)
                {
                    ActivePartDoc = (PartDocument)doc;
                    ActiveCompDef = ActivePartDoc.ComponentDefinition;
                }
            }
            catch { /* No active document or not a part */ }

            // If no explicit target, fallback to active
            if (TargetPartDoc == null)
            {
                TargetPartDoc = ActivePartDoc;
                TargetCompDef = ActiveCompDef;
            }
        }

        /// <summary>
        /// Converts a node-space point (mm) to an Inventor Point (cm).
        /// </summary>
        public Inventor.Point CreatePoint(double xMm, double yMm, double zMm)
        {
            return TG.CreatePoint(xMm * MM_TO_CM, yMm * MM_TO_CM, zMm * MM_TO_CM);
        }

        /// <summary>
        /// Converts a node-space vector (mm) to an Inventor Vector (cm).
        /// </summary>
        public Vector CreateVector(double xMm, double yMm, double zMm)
        {
            return TG.CreateVector(xMm * MM_TO_CM, yMm * MM_TO_CM, zMm * MM_TO_CM);
        }

        /// <summary>
        /// Creates a 2D point in Inventor units (cm) from node units (mm).
        /// </summary>
        public Point2d CreatePoint2d(double xMm, double yMm)
        {
            return TG.CreatePoint2d(xMm * MM_TO_CM, yMm * MM_TO_CM);
        }

        /// <summary>
        /// Creates a translation matrix from a displacement in node units (mm).
        /// </summary>
        public Matrix CreateTranslationMatrix(double dxMm, double dyMm, double dzMm)
        {
            var matrix = TG.CreateMatrix();
            var vec = TG.CreateVector(dxMm * MM_TO_CM, dyMm * MM_TO_CM, dzMm * MM_TO_CM);
            matrix.SetTranslation(vec);
            return matrix;
        }

        /// <summary>
        /// Creates a rotation matrix around an axis through a point.
        /// </summary>
        public Matrix CreateRotationMatrix(
            double originXMm, double originYMm, double originZMm,
            double axisX, double axisY, double axisZ,
            double angleRadians)
        {
            var matrix = TG.CreateMatrix();
            var origin = TG.CreatePoint(originXMm * MM_TO_CM, originYMm * MM_TO_CM, originZMm * MM_TO_CM);
            var axis = TG.CreateVector(axisX, axisY, axisZ);
            axis.Normalize();
            matrix.SetToRotation(angleRadians, axis, origin);
            return matrix;
        }

        /// <summary>
        /// Creates a uniform scale matrix around a center point.
        /// </summary>
        public Matrix CreateScaleMatrix(double centerXMm, double centerYMm, double centerZMm, double factor)
        {
            var matrix = TG.CreateMatrix();

            // Scale matrix: translate to origin, scale, translate back
            double cx = centerXMm * MM_TO_CM;
            double cy = centerYMm * MM_TO_CM;
            double cz = centerZMm * MM_TO_CM;

            // Build the 4x4 matrix manually
            // Scale relative to center: M = T(c) * S(f) * T(-c)
            matrix.Cell[1, 1] = factor; matrix.Cell[1, 2] = 0;      matrix.Cell[1, 3] = 0;      matrix.Cell[1, 4] = cx * (1 - factor);
            matrix.Cell[2, 1] = 0;      matrix.Cell[2, 2] = factor; matrix.Cell[2, 3] = 0;      matrix.Cell[2, 4] = cy * (1 - factor);
            matrix.Cell[3, 1] = 0;      matrix.Cell[3, 2] = 0;      matrix.Cell[3, 3] = factor; matrix.Cell[3, 4] = cz * (1 - factor);
            matrix.Cell[4, 1] = 0;      matrix.Cell[4, 2] = 0;      matrix.Cell[4, 3] = 0;      matrix.Cell[4, 4] = 1;

            return matrix;
        }

        /// <summary>
        /// Creates a mirror/reflection matrix across a plane defined by a point and normal.
        /// </summary>
        public Matrix CreateMirrorMatrix(
            double pointXMm, double pointYMm, double pointZMm,
            double normalX, double normalY, double normalZ)
        {
            var matrix = TG.CreateMatrix();

            double px = pointXMm * MM_TO_CM;
            double py = pointYMm * MM_TO_CM;
            double pz = pointZMm * MM_TO_CM;

            // Normalize the normal vector
            double len = Math.Sqrt(normalX * normalX + normalY * normalY + normalZ * normalZ);
            if (len < 1e-10) { normalX = 0; normalY = 0; normalZ = 1; len = 1; }
            double nx = normalX / len, ny = normalY / len, nz = normalZ / len;

            // Reflection matrix: I - 2 * n * n^T, translated to the plane point
            double d = px * nx + py * ny + pz * nz;

            matrix.Cell[1, 1] = 1 - 2 * nx * nx;  matrix.Cell[1, 2] = -2 * nx * ny;      matrix.Cell[1, 3] = -2 * nx * nz;      matrix.Cell[1, 4] = 2 * nx * d;
            matrix.Cell[2, 1] = -2 * ny * nx;      matrix.Cell[2, 2] = 1 - 2 * ny * ny;  matrix.Cell[2, 3] = -2 * ny * nz;      matrix.Cell[2, 4] = 2 * ny * d;
            matrix.Cell[3, 1] = -2 * nz * nx;      matrix.Cell[3, 2] = -2 * nz * ny;      matrix.Cell[3, 3] = 1 - 2 * nz * nz;  matrix.Cell[3, 4] = 2 * nz * d;
            matrix.Cell[4, 1] = 0;                  matrix.Cell[4, 2] = 0;                  matrix.Cell[4, 3] = 0;                  matrix.Cell[4, 4] = 1;

            return matrix;
        }
    }
}
