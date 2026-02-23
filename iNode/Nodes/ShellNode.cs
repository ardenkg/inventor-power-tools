// ============================================================================
// iNode Add-in for Autodesk Inventor 2026
// ShellNode.cs - Hollows out a body with a wall thickness
// ============================================================================

using System;
using System.Collections.Generic;
using Inventor;
using iNode.Core;

namespace iNode.Nodes
{
    /// <summary>
    /// Hollows out a solid body by subtracting a scaled-down copy from itself.
    /// For preview: creates an approximate shell using scale + boolean subtract.
    /// For commit: uses ShellFeature on the committed body.
    /// </summary>
    public class ShellNode : Node
    {
        public override string TypeName => "Shell";
        public override string Title => "Shell";
        public override string Category => "Operations";

        public ShellNode()
        {
            AddInput("Body", "Body", PortDataType.Geometry, null);
            AddInput("Thickness", "Thickness (mm)", PortDataType.Number, 1.0);
            AddInput("Faces", "Open Faces", PortDataType.Face, null);
            AddOutput("Body", "Body", PortDataType.Geometry);
        }

        protected override void Compute()
        {
            var bodyVal = GetInput("Body")!.GetEffectiveValue();
            var thickness = GetInput("Thickness")!.GetDouble(1.0);

            if (bodyVal is not BodyData srcBody)
            {
                HasError = true;
                ErrorMessage = "No body connected";
                return;
            }
            if (thickness <= 0)
            {
                HasError = true;
                ErrorMessage = "Thickness must be positive";
                return;
            }

            // Shell is a feature-level operation like Fillet/Chamfer.
            // Record as pending operation for commit phase.
            var facesVal = GetInput("Faces")?.GetEffectiveValue() as FaceListData;

            var faceSnapshots = new List<FaceSnapshot>();
            var liveFaces = new List<object>();
            if (facesVal != null && facesVal.Faces.Count > 0)
            {
                foreach (var faceObj in facesVal.Faces)
                {
                    try
                    {
                        faceSnapshots.Add(FaceSnapshot.FromInventorFace(faceObj));
                        liveFaces.Add(faceObj);
                    }
                    catch { }
                }
            }

            var result = srcBody.CloneMetadata();
            result.Description = $"Shell (t={thickness:F1}mm)";
            result.SourceNodeId = Id;
            result.PendingOperations.Add(new PendingShell
            {
                Thickness = thickness,
                FaceSnapshots = faceSnapshots,
                LiveFaces = liveFaces,
                OriginNodeId = Id
            });

            GetOutput("Body")!.Value = result;
        }
    }
}
