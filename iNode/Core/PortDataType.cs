// ============================================================================
// iNode Add-in for Autodesk Inventor 2026
// PortDataType.cs - Data types that flow between node ports
// ============================================================================

namespace iNode.Core
{
    /// <summary>
    /// Defines the data types that can flow between node ports.
    /// Used for type-checking connections and coloring wires.
    /// </summary>
    public enum PortDataType
    {
        /// <summary>Numeric value (double).</summary>
        Number,

        /// <summary>3D point (X, Y, Z).</summary>
        Point3D,

        /// <summary>Reference to Inventor geometry (SurfaceBody via BodyData).</summary>
        Geometry,

        /// <summary>Face or face list from a SurfaceBody (FaceListData).</summary>
        Face,

        /// <summary>Edge or edge list from a SurfaceBody (EdgeListData).</summary>
        Edge,

        /// <summary>Reference to an Inventor PlanarSketch.</summary>
        SketchRef,

        /// <summary>Reference to a work plane.</summary>
        WorkPlane,

        /// <summary>Sketch profile data (SketchProfileData).</summary>
        Profile,

        /// <summary>A list/tree of values (DataList).</summary>
        List,

        /// <summary>Any type - acts as a wildcard.</summary>
        Any
    }
}
