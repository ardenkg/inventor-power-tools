// ============================================================================
// CaseSelection Add-in for Autodesk Inventor 2026
// TopologyAnalyzer.cs - Topology Analysis Algorithms (Robust Implementation)
// ============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using Inventor;

namespace CaseSelection.Core
{
    /// <summary>
    /// Enumeration for edge convexity classification.
    /// </summary>
    public enum EdgeConvexity
    {
        /// <summary>Edge is convex (outward fold, like a mountain ridge)</summary>
        Convex,
        /// <summary>Edge is concave (inward fold, like a valley)</summary>
        Concave,
        /// <summary>Edge is tangent (smooth transition between faces)</summary>
        Tangent,
        /// <summary>Could not determine edge convexity</summary>
        Unknown
    }

    /// <summary>
    /// Provides topology analysis algorithms for face selection.
    /// Implements robust selection filters including Pocket/Boss detection using
    /// proper edge convexity calculations based on cross/dot product math.
    /// </summary>
    public class TopologyAnalyzer
    {
        #region Constants

        /// <summary>
        /// Threshold for determining if two faces are tangent (dot product > 0.9999).
        /// This corresponds to approximately 0.8 degrees.
        /// </summary>
        private const double TANGENT_THRESHOLD = 0.9999;

        /// <summary>
        /// Tolerance for zero comparisons in geometric calculations.
        /// </summary>
        private const double ZERO_TOLERANCE = 1e-8;

        /// <summary>
        /// General tolerance for geometric calculations.
        /// </summary>
        private const double TOLERANCE = 1e-6;

        #endregion

        #region Private Fields

        private readonly Inventor.Application _inventorApp;

        #endregion

        #region Constructor

        /// <summary>
        /// Creates a new TopologyAnalyzer instance.
        /// </summary>
        public TopologyAnalyzer(Inventor.Application inventorApp)
        {
            _inventorApp = inventorApp ?? throw new ArgumentNullException(nameof(inventorApp));
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Gets faces based on the specified filter type starting from a seed face.
        /// </summary>
        public List<Face> GetFaces(Face seedFace, SelectionFilterType filterType)
        {
            if (seedFace == null)
                return new List<Face>();

            try
            {
                return filterType switch
                {
                    SelectionFilterType.SingleFace => GetSingleFace(seedFace),
                    SelectionFilterType.TangentFaces => GetTangentFaces(seedFace),
                    SelectionFilterType.BossPocketFaces => GetBossPocketFaces(seedFace),
                    SelectionFilterType.AdjacentFaces => GetAdjacentFaces(seedFace),
                    SelectionFilterType.FeatureFaces => GetFeatureFaces(seedFace),
                    SelectionFilterType.ConnectedBlendFaces => GetConnectedBlendFaces(seedFace),
                    _ => GetSingleFace(seedFace)
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in GetFaces: {ex.Message}");
                return new List<Face> { seedFace };
            }
        }

        #endregion

        #region Single Face

        /// <summary>
        /// Returns just the clicked face.
        /// </summary>
        private List<Face> GetSingleFace(Face seedFace)
        {
            return new List<Face> { seedFace };
        }

        #endregion

        #region Tangent Faces

        /// <summary>
        /// Gets all faces that are tangentially connected to the seed face.
        /// Uses Inventor's TangentiallyConnectedFaces API for accuracy.
        /// </summary>
        private List<Face> GetTangentFaces(Face seedFace)
        {
            var result = new HashSet<Face> { seedFace };

            try
            {
                // Use Inventor's built-in TangentiallyConnectedFaces property
                var tangentFaces = seedFace.TangentiallyConnectedFaces;
                if (tangentFaces != null)
                {
                    foreach (Face face in tangentFaces)
                    {
                        result.Add(face);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting tangent faces via API: {ex.Message}");
                // Fallback to edge-based detection
                return GetTangentFacesViaEdges(seedFace);
            }

            return result.ToList();
        }

        /// <summary>
        /// Fallback method: Gets tangent faces by analyzing edges.
        /// </summary>
        private List<Face> GetTangentFacesViaEdges(Face seedFace)
        {
            var result = new HashSet<Face>();
            var visited = new HashSet<Face>();
            var queue = new Queue<Face>();

            queue.Enqueue(seedFace);

            while (queue.Count > 0)
            {
                var currentFace = queue.Dequeue();

                if (visited.Contains(currentFace))
                    continue;

                visited.Add(currentFace);
                result.Add(currentFace);

                foreach (Edge edge in currentFace.Edges)
                {
                    var convexity = GetEdgeConvexity(edge, currentFace);
                    if (convexity == EdgeConvexity.Tangent)
                    {
                        foreach (Face neighbor in edge.Faces)
                        {
                            if (!ReferenceEquals(neighbor, currentFace) && !visited.Contains(neighbor))
                            {
                                queue.Enqueue(neighbor);
                            }
                        }
                    }
                }
            }

            return result.ToList();
        }

        #endregion

        #region Adjacent Faces

        /// <summary>
        /// Gets all faces that share an edge with the seed face.
        /// Iterates all edges of the seed face and collects connected faces.
        /// </summary>
        private List<Face> GetAdjacentFaces(Face seedFace)
        {
            var result = new HashSet<Face> { seedFace };

            try
            {
                foreach (Edge edge in seedFace.Edges)
                {
                    foreach (Face face in edge.Faces)
                    {
                        if (!ReferenceEquals(face, seedFace))
                        {
                            result.Add(face);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting adjacent faces: {ex.Message}");
            }

            return result.ToList();
        }

        #endregion

        #region Boss/Pocket Faces

        /// <summary>
        /// Gets all faces that form a boss or pocket feature.
        /// Primary: Uses feature-based detection via CreatedByFeature, then expands
        /// through adjacent blend/fillet faces to capture the full pocket including fillets.
        /// Fallback: Edge convexity BFS that stops at boundary edges.
        /// </summary>
        private List<Face> GetBossPocketFaces(Face seedFace)
        {
            var result = new HashSet<Face> { seedFace };

            try
            {
                // Primary: Feature-based approach - most reliable for Inventor
                var feature = seedFace.CreatedByFeature;
                if (feature != null)
                {
                    try
                    {
                        var featureFaces = feature.Faces;
                        if (featureFaces != null)
                        {
                            // Collect all feature faces into a lookup set
                            var featureFaceSet = new HashSet<Face>();
                            foreach (Face face in featureFaces)
                            {
                                featureFaceSet.Add(face);
                            }

                            // Only select faces topologically connected to the seed face
                            if (featureFaceSet.Count > 1)
                            {
                                var connected = GetConnectedSubset(seedFace, featureFaceSet);
                                if (connected.Count > 1)
                                {
                                    result = connected;
                                    System.Diagnostics.Debug.WriteLine($"Boss/Pocket: Feature-connected found {result.Count} faces from feature '{feature.Name}'");
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Feature-based detection failed: {ex.Message}");
                    }
                }

                // Expand through adjacent blend/fillet faces
                // Fillets applied to a pocket belong to a different feature (FilletFeature),
                // so the initial feature-based pass misses them. We walk outward from
                // the current result boundary, absorbing blend faces and any pocket-feature
                // faces reachable on the other side of those blends.
                // Always try expansion when we have a feature — even if connected subset
                // is only 1 face (walls may be fully separated by fillets).
                string? seedFeatureName = null;
                try { seedFeatureName = feature?.Name; } catch { }
                ExpandThroughBlendFaces(result, seedFeatureName);

                if (result.Count > 1)
                {
                    return result.ToList();
                }

                // Fallback: Edge convexity BFS
                result.Clear();
                result.Add(seedFace);

                // Determine dominant edge type around the seed face
                int concaveCount = 0;
                int convexCount = 0;
                int tangentCount = 0;

                foreach (Edge edge in seedFace.Edges)
                {
                    var convexity = GetEdgeConvexity(edge, seedFace);
                    if (convexity == EdgeConvexity.Concave) concaveCount++;
                    else if (convexity == EdgeConvexity.Convex) convexCount++;
                    else if (convexity == EdgeConvexity.Tangent) tangentCount++;
                }

                // If all edges are tangent (filleted pocket), use concave+tangent flood fill
                // stopping only at convex edges. Limited to 100 faces to prevent runaway.
                if (concaveCount == 0 && convexCount == 0 && tangentCount > 0)
                {
                    var visited = new HashSet<Face>();
                    var queue = new Queue<Face>();
                    queue.Enqueue(seedFace);

                    while (queue.Count > 0 && result.Count < 100)
                    {
                        var currentFace = queue.Dequeue();
                        if (visited.Contains(currentFace)) continue;
                        visited.Add(currentFace);
                        result.Add(currentFace);

                        foreach (Edge edge in currentFace.Edges)
                        {
                            var convexity = GetEdgeConvexity(edge, currentFace);
                            // Follow tangent and concave edges, stop at convex
                            if (convexity != EdgeConvexity.Convex)
                            {
                                foreach (Face neighbor in edge.Faces)
                                {
                                    if (!ReferenceEquals(neighbor, currentFace) && !visited.Contains(neighbor))
                                    {
                                        queue.Enqueue(neighbor);
                                    }
                                }
                            }
                        }
                    }

                    // After tangent flood, expand through blends too
                    if (result.Count > 1)
                    {
                        ExpandThroughBlendFaces(result, null);
                    }
                }
                else
                {
                    // Standard edge convexity BFS
                    EdgeConvexity targetConvexity = concaveCount >= convexCount
                        ? EdgeConvexity.Concave
                        : EdgeConvexity.Convex;

                    var visited = new HashSet<Face>();
                    var queue = new Queue<Face>();
                    queue.Enqueue(seedFace);

                    while (queue.Count > 0)
                    {
                        var currentFace = queue.Dequeue();
                        if (visited.Contains(currentFace)) continue;
                        visited.Add(currentFace);
                        result.Add(currentFace);

                        foreach (Edge edge in currentFace.Edges)
                        {
                            var convexity = GetEdgeConvexity(edge, currentFace);

                            if (convexity == targetConvexity)
                            {
                                foreach (Face neighbor in edge.Faces)
                                {
                                    if (!ReferenceEquals(neighbor, currentFace) && !visited.Contains(neighbor))
                                    {
                                        queue.Enqueue(neighbor);
                                    }
                                }
                            }
                            else if (convexity == EdgeConvexity.Tangent)
                            {
                                foreach (Face neighbor in edge.Faces)
                                {
                                    if (!ReferenceEquals(neighbor, currentFace) && !visited.Contains(neighbor))
                                    {
                                        bool hasMatchingEdge = false;
                                        foreach (Edge neighborEdge in neighbor.Edges)
                                        {
                                            var nc = GetEdgeConvexity(neighborEdge, neighbor);
                                            if (nc == targetConvexity)
                                            {
                                                hasMatchingEdge = true;
                                                break;
                                            }
                                        }
                                        if (hasMatchingEdge)
                                        {
                                            queue.Enqueue(neighbor);
                                        }
                                    }
                                }
                            }
                        }
                    }

                    // After edge BFS, expand through blends too
                    if (result.Count > 1)
                    {
                        ExpandThroughBlendFaces(result, null);
                    }
                }

                System.Diagnostics.Debug.WriteLine($"Boss/Pocket: Edge-based found {result.Count} faces");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in GetBossPocketFaces: {ex.Message}");
                if (!result.Contains(seedFace))
                    result.Add(seedFace);
            }

            return result.ToList();
        }

        /// <summary>
        /// Expands a face set by walking into adjacent blend/fillet faces.
        /// When a pocket has fillets applied, those fillet faces belong to a different
        /// feature. This method bridges across fillet features to capture the full pocket.
        /// It also walks from fillet faces back into faces of the original pocket feature
        /// that may have been disconnected from the initial set by fillet operations.
        /// </summary>
        private void ExpandThroughBlendFaces(HashSet<Face> result, string? seedFeatureName)
        {
            bool expanded = true;
            int iterations = 0;
            const int maxIterations = 10;

            while (expanded && iterations < maxIterations)
            {
                expanded = false;
                iterations++;

                var toAdd = new List<Face>();

                foreach (var face in result.ToList())
                {
                    foreach (Edge edge in face.Edges)
                    {
                        foreach (Face neighbor in edge.Faces)
                        {
                            if (ReferenceEquals(neighbor, face) || result.Contains(neighbor))
                                continue;

                            // Add adjacent blend faces (fillets/chamfers)
                            if (IsBlendFace(neighbor))
                            {
                                toAdd.Add(neighbor);
                                continue;
                            }

                            // If we have a seed feature name, also reclaim faces that belong
                            // to it but were separated by fillet operations.
                            // Compare by feature name (COM-safe — ReferenceEquals fails
                            // across different RCW wrappers for the same COM object).
                            if (seedFeatureName != null)
                            {
                                try
                                {
                                    var nf = neighbor.CreatedByFeature;
                                    if (nf != null && nf.Name == seedFeatureName)
                                    {
                                        toAdd.Add(neighbor);
                                    }
                                }
                                catch { }
                            }
                        }
                    }
                }

                foreach (var f in toAdd)
                {
                    if (result.Add(f))
                        expanded = true;
                }
            }

            System.Diagnostics.Debug.WriteLine($"Boss/Pocket: After blend expansion = {result.Count} faces ({iterations} iterations)");
        }

        #endregion

        #region Feature Faces

        /// <summary>
        /// Gets all faces created by the same feature as the seed face.
        /// Uses the CreatedByFeature property to find the parent feature,
        /// then iterates through all faces in that feature.
        /// </summary>
        private List<Face> GetFeatureFaces(Face seedFace)
        {
            var result = new HashSet<Face> { seedFace };

            try
            {
                // Get the feature that created this face
                var feature = seedFace.CreatedByFeature;
                if (feature == null)
                {
                    System.Diagnostics.Debug.WriteLine("No CreatedByFeature found for seed face");
                    return result.ToList();
                }

                // Get the surface body that contains the seed face
                SurfaceBody? targetBody = null;
                try
                {
                    // Try to get the parent body from the face
                    foreach (Edge edge in seedFace.Edges)
                    {
                        foreach (Face f in edge.Faces)
                        {
                            // Get parent body through the face's containing body
                            var body = GetFaceBody(f);
                            if (body != null)
                            {
                                targetBody = body;
                                break;
                            }
                        }
                        if (targetBody != null) break;
                    }
                }
                catch { }

                // Get all faces from the feature
                try
                {
                    var featureFaces = feature.Faces;
                    if (featureFaces != null)
                    {
                        foreach (Face face in featureFaces)
                        {
                            // If we have a target body, only add faces from that body
                            if (targetBody != null)
                            {
                                var faceBody = GetFaceBody(face);
                                if (faceBody != null && ReferenceEquals(faceBody, targetBody))
                                {
                                    result.Add(face);
                                }
                            }
                            else
                            {
                                // No target body filter, add all faces
                                result.Add(face);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error iterating feature faces: {ex.Message}");
                }

                System.Diagnostics.Debug.WriteLine($"Feature faces found: {result.Count} for feature: {feature.Name}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting feature faces: {ex.Message}");
            }

            return result.ToList();
        }

        /// <summary>
        /// Gets the surface body that contains a face.
        /// </summary>
        private SurfaceBody? GetFaceBody(Face face)
        {
            try
            {
                // The face is part of a SurfaceBody - we need to find it
                // by checking its edges and finding the common body
                foreach (Edge edge in face.Edges)
                {
                    // Edges belong to bodies via the EdgeLoop -> Face -> Body chain
                    // But we can also check edge.Parent which should give us the body
                    try
                    {
                        var parent = edge.Parent;
                        if (parent is SurfaceBody body)
                        {
                            return body;
                        }
                    }
                    catch { }
                }
            }
            catch { }
            return null;
        }

        #endregion

        #region Edge Convexity Calculation

        /// <summary>
        /// Calculates the convexity of an edge relative to a reference face.
        /// 
        /// Algorithm:
        /// 1. Get surface normals for both faces at the edge midpoint
        /// 2. Get the edge tangent vector at the midpoint (oriented relative to Face1's loop)
        /// 3. Calculate cross product of (Normal1 × Normal2) = IntersectionVector
        /// 4. Calculate dot product of (IntersectionVector · EdgeTangent)
        /// 5. Evaluate:
        ///    - Near zero: Tangent (faces are parallel/smooth)
        ///    - Positive: Convex (outward bend like mountain peak)
        ///    - Negative: Concave (inward bend like valley)
        /// </summary>
        /// <param name="edge">The edge to analyze</param>
        /// <param name="referenceFace">The face from which we're looking at the edge</param>
        /// <returns>The edge convexity classification</returns>
        public EdgeConvexity GetEdgeConvexity(Edge edge, Face referenceFace)
        {
            try
            {
                // Get both faces connected by this edge
                var faces = new List<Face>();
                foreach (Face f in edge.Faces)
                {
                    faces.Add(f);
                }

                if (faces.Count != 2)
                    return EdgeConvexity.Unknown;

                Face face1 = referenceFace;
                Face face2 = faces[0];
                if (ReferenceEquals(faces[0], referenceFace))
                {
                    face2 = faces[1];
                }

                // Get edge midpoint parameter and position
                double minParam = 0, maxParam = 0;
                edge.Evaluator.GetParamExtents(out minParam, out maxParam);
                double midParam = (minParam + maxParam) / 2.0;
                double[] midParamArray = new double[] { midParam };

                // Get 3D point at edge midpoint
                double[] edgeMidPoint = new double[3];
                edge.Evaluator.GetPointAtParam(ref midParamArray, ref edgeMidPoint);

                // Step 1: Get surface normals at the edge midpoint
                double[]? normal1 = GetFaceNormalAtPoint(face1, edgeMidPoint);
                double[]? normal2 = GetFaceNormalAtPoint(face2, edgeMidPoint);

                if (normal1 == null || normal2 == null)
                    return EdgeConvexity.Unknown;

                // Normalize the normals
                Normalize(normal1);
                Normalize(normal2);

                // Check if faces are tangent (normals parallel or anti-parallel)
                double normalDot = Math.Abs(DotProduct(normal1, normal2));
                if (normalDot > TANGENT_THRESHOLD)
                {
                    return EdgeConvexity.Tangent;
                }

                // Step 2: Get edge tangent at midpoint
                double[] edgeTangent = new double[3];
                edge.Evaluator.GetTangent(ref midParamArray, ref edgeTangent);
                Normalize(edgeTangent);

                // Step 3: Calculate cross product of normals (intersection vector)
                double[] intersectionVector = CrossProduct(normal1, normal2);
                Normalize(intersectionVector);

                // Step 4: Calculate dot product of intersection vector and edge tangent
                double dotResult = DotProduct(intersectionVector, edgeTangent);

                // Step 5: Evaluate the result
                if (Math.Abs(dotResult) < ZERO_TOLERANCE)
                {
                    // Edge tangent is perpendicular to intersection vector - tangent case
                    return EdgeConvexity.Tangent;
                }
                else if (dotResult > 0)
                {
                    // Positive: Convex edge (outward fold)
                    return EdgeConvexity.Convex;
                }
                else
                {
                    // Negative: Concave edge (inward fold)
                    return EdgeConvexity.Concave;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error calculating edge convexity: {ex.Message}");
                return EdgeConvexity.Unknown;
            }
        }

        /// <summary>
        /// Alternative convexity check using centroid-based method.
        /// This is a simpler fallback that works well for most cases.
        /// </summary>
        public EdgeConvexity GetEdgeConvexitySimple(Edge edge, Face referenceFace)
        {
            try
            {
                var faces = new List<Face>();
                foreach (Face f in edge.Faces)
                {
                    faces.Add(f);
                }

                if (faces.Count != 2)
                    return EdgeConvexity.Unknown;

                Face face1 = referenceFace;
                Face face2 = ReferenceEquals(faces[0], referenceFace) ? faces[1] : faces[0];

                // Get edge midpoint
                double minParam = 0, maxParam = 0;
                edge.Evaluator.GetParamExtents(out minParam, out maxParam);
                double midParam = (minParam + maxParam) / 2.0;
                double[] midParamArray = new double[] { midParam };
                double[] edgeMidPoint = new double[3];
                edge.Evaluator.GetPointAtParam(ref midParamArray, ref edgeMidPoint);

                // Get normals at edge midpoint
                double[]? normal1 = GetFaceNormalAtPoint(face1, edgeMidPoint);
                double[]? normal2 = GetFaceNormalAtPoint(face2, edgeMidPoint);

                if (normal1 == null || normal2 == null)
                    return EdgeConvexity.Unknown;

                Normalize(normal1);
                Normalize(normal2);

                // Check tangency
                double normalDot = DotProduct(normal1, normal2);
                if (Math.Abs(normalDot) > TANGENT_THRESHOLD)
                {
                    return EdgeConvexity.Tangent;
                }

                // Calculate a point slightly offset from face1's surface
                // along its normal, then check which side of face2 it's on
                double offset = 0.001; // 1mm offset
                double[] testPoint = new double[]
                {
                    edgeMidPoint[0] + normal1[0] * offset,
                    edgeMidPoint[1] + normal1[1] * offset,
                    edgeMidPoint[2] + normal1[2] * offset
                };

                // Vector from edge midpoint to test point
                double[] toTestPoint = new double[]
                {
                    testPoint[0] - edgeMidPoint[0],
                    testPoint[1] - edgeMidPoint[1],
                    testPoint[2] - edgeMidPoint[2]
                };

                // Check if test point is on the same side as face2's normal
                double dot = DotProduct(toTestPoint, normal2);

                if (Math.Abs(dot) < ZERO_TOLERANCE)
                {
                    return EdgeConvexity.Tangent;
                }
                else if (dot > 0)
                {
                    // Test point is on the outside of face2 - convex
                    return EdgeConvexity.Convex;
                }
                else
                {
                    // Test point is on the inside of face2 - concave
                    return EdgeConvexity.Concave;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in simple convexity check: {ex.Message}");
                return EdgeConvexity.Unknown;
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Gets the face normal at a specific 3D point.
        /// </summary>
        private double[]? GetFaceNormalAtPoint(Face face, double[] point3d)
        {
            try
            {
                var evaluator = face.Evaluator;

                // Find the parameter on the face closest to the given point
                double[] param = new double[2];
                double[] closestPoints = new double[3];
                double[] maxDeviations = new double[1];
                SolutionNatureEnum[] solutionNatures = new SolutionNatureEnum[1];

                evaluator.GetParamAtPoint(ref point3d, ref param, ref closestPoints, ref maxDeviations, ref solutionNatures);

                // Get the normal at that parameter
                double[] normal = new double[3];
                evaluator.GetNormal(ref param, ref normal);

                return normal;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting face normal: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Calculates the dot product of two 3D vectors.
        /// </summary>
        private double DotProduct(double[] v1, double[] v2)
        {
            return v1[0] * v2[0] + v1[1] * v2[1] + v1[2] * v2[2];
        }

        /// <summary>
        /// Calculates the cross product of two 3D vectors.
        /// Result = v1 × v2
        /// </summary>
        private double[] CrossProduct(double[] v1, double[] v2)
        {
            return new double[]
            {
                v1[1] * v2[2] - v1[2] * v2[1],
                v1[2] * v2[0] - v1[0] * v2[2],
                v1[0] * v2[1] - v1[1] * v2[0]
            };
        }

        /// <summary>
        /// Normalizes a 3D vector in place.
        /// </summary>
        private void Normalize(double[] v)
        {
            double length = Math.Sqrt(v[0] * v[0] + v[1] * v[1] + v[2] * v[2]);
            if (length > TOLERANCE)
            {
                v[0] /= length;
                v[1] /= length;
                v[2] /= length;
            }
        }

        /// <summary>
        /// Calculates the magnitude (length) of a 3D vector.
        /// </summary>
        private double Magnitude(double[] v)
        {
            return Math.Sqrt(v[0] * v[0] + v[1] * v[1] + v[2] * v[2]);
        }

        /// <summary>
        /// From a seed face, BFS through shared edges to find all topologically
        /// connected faces within the given candidate set. This ensures we only
        /// select faces that are physically adjacent to the clicked face, not
        /// disconnected faces that happen to belong to the same feature.
        /// </summary>
        private HashSet<Face> GetConnectedSubset(Face seedFace, HashSet<Face> candidateSet)
        {
            var connected = new HashSet<Face> { seedFace };
            var queue = new Queue<Face>();
            queue.Enqueue(seedFace);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                foreach (Edge edge in current.Edges)
                {
                    foreach (Face neighbor in edge.Faces)
                    {
                        if (!ReferenceEquals(neighbor, current) && !connected.Contains(neighbor) && candidateSet.Contains(neighbor))
                        {
                            connected.Add(neighbor);
                            queue.Enqueue(neighbor);
                        }
                    }
                }
            }

            return connected;
        }

        #endregion

        #region Connected Blend Faces

        /// <summary>
        /// Gets all connected blend (fillet/chamfer) faces starting from a seed face.
        /// A blend face is identified by having a cylindrical, conical, spherical, or toroidal surface.
        /// Connected blends share edges with other blend faces.
        /// </summary>
        private List<Face> GetConnectedBlendFaces(Face seedFace)
        {
            var result = new HashSet<Face>();

            try
            {
                // Check if seed face is a blend face
                if (!IsBlendFace(seedFace))
                {
                    // If not a blend face, just return the single face
                    return new List<Face> { seedFace };
                }

                // Start with the seed face
                result.Add(seedFace);

                // Use BFS to find all connected blend faces
                var queue = new Queue<Face>();
                queue.Enqueue(seedFace);

                while (queue.Count > 0)
                {
                    var currentFace = queue.Dequeue();

                    // Get all edges of this face
                    foreach (Edge edge in currentFace.Edges)
                    {
                        // Get adjacent faces (usually 2 faces per edge)
                        foreach (Face adjacentFace in edge.Faces)
                        {
                            // Skip if we already processed this face
                            if (result.Contains(adjacentFace))
                                continue;

                            // Check if adjacent face is also a blend face
                            if (IsBlendFace(adjacentFace))
                            {
                                result.Add(adjacentFace);
                                queue.Enqueue(adjacentFace);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in GetConnectedBlendFaces: {ex.Message}");
                if (!result.Contains(seedFace))
                    result.Add(seedFace);
            }

            return result.ToList();
        }

        /// <summary>
        /// Determines if a face is a blend (fillet/chamfer) face based on its surface geometry.
        /// Blend faces typically have cylindrical, conical, spherical, or toroidal surfaces.
        /// </summary>
        private bool IsBlendFace(Face face)
        {
            try
            {
                var surfaceType = face.SurfaceType;

                // Blend faces are typically:
                // - Cylindrical (constant radius fillet)
                // - Conical (variable radius fillet)
                // - Spherical (ball corners)
                // - Toroidal (rolling ball blend at corners)
                // - B-spline surfaces can also be blends (variable radius)

                switch (surfaceType)
                {
                    case SurfaceTypeEnum.kCylinderSurface:
                        // Most common fillet type - cylindrical
                        return true;

                    case SurfaceTypeEnum.kConeSurface:
                        // Variable radius fillet or chamfer edge
                        return true;

                    case SurfaceTypeEnum.kSphereSurface:
                        // Ball corners
                        return true;

                    case SurfaceTypeEnum.kTorusSurface:
                        // Rolling ball corners or complex blends
                        return true;

                    case SurfaceTypeEnum.kBSplineSurface:
                        // Could be a variable radius blend - check if it looks like one
                        // B-spline blends typically have 2 edges that are curves
                        // and connect to planar or cylindrical surfaces
                        return IsLikelyBSplineBlend(face);

                    default:
                        return false;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error checking if blend face: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Heuristic check to determine if a B-spline surface is likely a blend.
        /// B-spline blends typically have curved edges on opposite sides connecting
        /// to other blend faces or planar/cylindrical faces.
        /// </summary>
        private bool IsLikelyBSplineBlend(Face face)
        {
            try
            {
                int curvedEdgeCount = 0;
                int straightEdgeCount = 0;

                foreach (Edge edge in face.Edges)
                {
                    var curveType = edge.GeometryType;
                    if (curveType == CurveTypeEnum.kLineCurve || curveType == CurveTypeEnum.kLineSegmentCurve)
                    {
                        straightEdgeCount++;
                    }
                    else
                    {
                        curvedEdgeCount++;
                    }
                }

                // Typical blend has 2 straight edges (the "sides") and 2 curved edges (along the blend)
                // Or could have 4 curved edges for corner blends
                // If mostly curved edges, likely a blend
                if (curvedEdgeCount >= 2 && curvedEdgeCount >= straightEdgeCount)
                {
                    // Additional check: adjacent faces should include blend-like surfaces
                    int adjacentBlendCount = 0;
                    foreach (Edge edge in face.Edges)
                    {
                        foreach (Face adjFace in edge.Faces)
                        {
                            if (adjFace.Equals(face)) continue;
                            
                            var adjType = adjFace.SurfaceType;
                            if (adjType == SurfaceTypeEnum.kCylinderSurface ||
                                adjType == SurfaceTypeEnum.kConeSurface ||
                                adjType == SurfaceTypeEnum.kSphereSurface ||
                                adjType == SurfaceTypeEnum.kTorusSurface)
                            {
                                adjacentBlendCount++;
                            }
                        }
                    }
                    
                    // If at least one adjacent face is a known blend type, this is likely a blend too
                    return adjacentBlendCount > 0;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        #endregion
    }
}
