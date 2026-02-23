// ============================================================================
// iNode Add-in for Autodesk Inventor 2026
// DraftNode.cs - Add draft/taper angle to faces (pending operation)
// ============================================================================

using System;
using System.Collections.Generic;
using iNode.Core;

namespace iNode.Nodes
{
    /// <summary>
    /// Adds draft (taper) angle to selected faces.
    /// Critical for injection molding design.
    /// This is a pending operation — applied during commit.
    /// </summary>
    public class DraftNode : Node
    {
        public override string TypeName => "Draft";
        public override string Title => "Draft";
        public override string Category => "Operations";

        public DraftNode()
        {
            AddInput("Body", "Body", PortDataType.Geometry, null);
            AddInput("Faces", "Faces to Draft", PortDataType.Face, null);
            AddInput("Angle", "Draft Angle (\u00B0)", PortDataType.Number, 2.0);
            AddInput("PullDirection", "Pull Direction", PortDataType.Point3D, (0.0, 1.0, 0.0));
            AddOutput("Body", "Body", PortDataType.Geometry);
        }

        protected override void Compute()
        {
            var bodyVal = GetInput("Body")!.GetEffectiveValue();
            var facesVal = GetInput("Faces")?.GetEffectiveValue() as FaceListData;
            var angle = GetInput("Angle")!.GetDouble(2.0);

            if (bodyVal is not BodyData srcBody)
            {
                HasError = true;
                ErrorMessage = "No body connected";
                return;
            }
            if (angle <= 0 || angle >= 90)
            {
                HasError = true;
                ErrorMessage = "Draft angle must be between 0\u00B0 and 90\u00B0";
                return;
            }
            if (facesVal == null || facesVal.Faces.Count == 0)
            {
                HasError = true;
                ErrorMessage = "No faces selected for draft";
                return;
            }

            var faceSnapshots = new List<FaceSnapshot>();
            var liveFaces = new List<object>();
            foreach (var f in facesVal.Faces)
            {
                try
                {
                    faceSnapshots.Add(FaceSnapshot.FromInventorFace(f));
                    liveFaces.Add(f);
                }
                catch { }
            }

            var pullDir = GetInput("PullDirection")!.GetPoint3D();

            var result = srcBody.CloneMetadata();
            result.Description = $"Draft ({angle:F1}\u00B0)";
            result.SourceNodeId = Id;
            result.PendingOperations.Add(new PendingDraft
            {
                AngleDegrees = angle,
                PullDirectionX = pullDir.X,
                PullDirectionY = pullDir.Y,
                PullDirectionZ = pullDir.Z,
                FaceSnapshots = faceSnapshots,
                LiveFaces = liveFaces,
                OriginNodeId = Id
            });

            GetOutput("Body")!.Value = result;
        }
    }
}
