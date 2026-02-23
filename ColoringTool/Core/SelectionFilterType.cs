// ============================================================================
// ColoringTool Add-in for Autodesk Inventor 2026
// SelectionFilterType.cs - Enumeration for filter types
// ============================================================================

namespace ColoringTool.Core
{
    /// <summary>
    /// Enumeration of available selection filter types.
    /// </summary>
    public enum SelectionFilterType
    {
        /// <summary>
        /// Select only the clicked face.
        /// </summary>
        SingleFace = 0,

        /// <summary>
        /// Select all faces that are tangent (smooth) connected to the seed face.
        /// </summary>
        TangentFaces = 1,

        /// <summary>
        /// Select all faces that form a boss or pocket feature.
        /// </summary>
        BossPocketFaces = 2,

        /// <summary>
        /// Select all faces that share an edge with the seed face.
        /// </summary>
        AdjacentFaces = 3,

        /// <summary>
        /// Select all faces created by the same feature as the seed face.
        /// </summary>
        FeatureFaces = 4,

        /// <summary>
        /// Select all blend (fillet/chamfer) faces that are connected.
        /// </summary>
        ConnectedBlendFaces = 5
    }
}
