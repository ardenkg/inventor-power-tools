// ============================================================================
// iNode Add-in for Autodesk Inventor 2026
// DeconstructBodyNode.cs - Extract faces and edges from a body
// ============================================================================

using System;
using System.Collections.Generic;
using Inventor;
using iNode.Core;

namespace iNode.Nodes
{
    /// <summary>
    /// Decomposes a SurfaceBody into its topological components:
    /// all faces and all edges. Connect the outputs to SelectFace/SelectEdge
    /// nodes to pick specific faces or edges for operations like fillet or chamfer.
    ///
    /// Think of this like Grasshopper's Deconstruct Brep — it exposes
    /// the topology of the body so you can work with individual elements.
    /// </summary>
    public class DeconstructBodyNode : Node
    {
        public override string TypeName => "DeconstructBody";
        public override string Title => "Deconstruct Body";
        public override string Category => "Topology";

        public DeconstructBodyNode()
        {
            AddInput("Body", "Body", PortDataType.Geometry, null);
            AddOutput("Body", "Body", PortDataType.Geometry);
            AddOutput("Faces", "Faces", PortDataType.Face);
            AddOutput("Edges", "Edges", PortDataType.Edge);
            AddOutput("FaceCount", "Face Count", PortDataType.Number);
            AddOutput("EdgeCount", "Edge Count", PortDataType.Number);
        }

        protected override void Compute()
        {
            var bodyVal = GetInput("Body")!.GetEffectiveValue();

            if (bodyVal is not BodyData bodyData)
            {
                HasError = true;
                ErrorMessage = "No body connected";
                return;
            }

            // Pass through the body unchanged
            GetOutput("Body")!.Value = bodyData;

            if (bodyData.Body == null)
            {
                GetOutput("Faces")!.Value = new FaceListData { ParentBody = bodyData };
                GetOutput("Edges")!.Value = new EdgeListData { ParentBody = bodyData };
                GetOutput("FaceCount")!.Value = 0.0;
                GetOutput("EdgeCount")!.Value = 0.0;
                return;
            }

            try
            {
                dynamic body = bodyData.Body;

                // Extract all faces
                var faceList = new FaceListData { ParentBody = bodyData };
                foreach (var face in body.Faces)
                {
                    faceList.Faces.Add(face);
                }

                // Extract all edges
                var edgeList = new EdgeListData { ParentBody = bodyData };
                foreach (var edge in body.Edges)
                {
                    edgeList.Edges.Add(edge);
                }

                GetOutput("Faces")!.Value = faceList;
                GetOutput("Edges")!.Value = edgeList;
                GetOutput("FaceCount")!.Value = (double)faceList.Faces.Count;
                GetOutput("EdgeCount")!.Value = (double)edgeList.Edges.Count;
            }
            catch (Exception ex)
            {
                HasError = true;
                ErrorMessage = $"Deconstruct failed: {ex.Message}";
            }
        }
    }
}
