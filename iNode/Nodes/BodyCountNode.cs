// ============================================================================
// iNode Add-in for Autodesk Inventor 2026
// BodyCountNode.cs - Count faces, edges, vertices of a body
// ============================================================================

using System;
using iNode.Core;

namespace iNode.Nodes
{
    /// <summary>
    /// Counts the topology elements of a body: faces, edges, vertices.
    /// </summary>
    public class BodyCountNode : Node
    {
        public override string TypeName => "BodyCount";
        public override string Title => "Body Count";
        public override string Category => "Topology";

        public BodyCountNode()
        {
            AddInput("Body", "Body", PortDataType.Geometry, null);
            AddOutput("Faces", "Face Count", PortDataType.Number);
            AddOutput("Edges", "Edge Count", PortDataType.Number);
            AddOutput("Vertices", "Vertex Count", PortDataType.Number);
        }

        protected override void Compute()
        {
            var bodyVal = GetInput("Body")!.GetEffectiveValue();

            if (bodyVal is not BodyData srcBody || srcBody.Body == null)
            {
                HasError = true;
                ErrorMessage = "No body connected";
                return;
            }

            try
            {
                dynamic body = srcBody.Body;
                int faceCount = 0, edgeCount = 0, vertexCount = 0;

                foreach (var _ in body.Faces) faceCount++;
                foreach (var _ in body.Edges) edgeCount++;
                foreach (var _ in body.Vertices) vertexCount++;

                GetOutput("Faces")!.Value = (double)faceCount;
                GetOutput("Edges")!.Value = (double)edgeCount;
                GetOutput("Vertices")!.Value = (double)vertexCount;
            }
            catch (Exception ex)
            {
                HasError = true;
                ErrorMessage = $"Count failed: {ex.Message}";
            }
        }
    }
}
