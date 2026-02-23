// ============================================================================
// iNode Add-in for Autodesk Inventor 2026
// WorkPlaneNode.cs - Selects a reference plane for sketch-based operations
// ============================================================================

using System;
using System.Collections.Generic;
using iNode.Core;

namespace iNode.Nodes
{
    /// <summary>
    /// Selects a reference plane (XY, XZ, YZ, or face) with optional offset.
    /// Outputs a PlaneData that sketch nodes consume.
    /// </summary>
    public class WorkPlaneNode : Node
    {
        public override string TypeName => "WorkPlane";
        public override string Title => "Work Plane";
        public override string Category => "Input";

        /// <summary>
        /// Which base plane: "XY", "XZ", "YZ", or "Face".
        /// </summary>
        public string Plane { get; set; } = "XY";

        public WorkPlaneNode()
        {
            AddInput("Offset", "Offset (mm)", PortDataType.Number, 0.0);
            AddOptionalInput("Face", "Face", PortDataType.Face, null);
            AddOutput("Plane", "Plane", PortDataType.WorkPlane);
        }

        protected override void Compute()
        {
            double offsetMm = GetInput("Offset")!.GetDouble(0.0);

            try
            {
                PlaneData? plane = null;

                switch (Plane)
                {
                    case "XZ":
                        plane = PlaneData.XZ(offsetMm);
                        break;
                    case "YZ":
                        plane = PlaneData.YZ(offsetMm);
                        break;
                    case "Face":
                        plane = ComputeFromFace(offsetMm);
                        break;
                    default: // "XY"
                        plane = PlaneData.XY(offsetMm);
                        break;
                }

                if (plane == null)
                {
                    HasError = true;
                    ErrorMessage = "Could not resolve plane";
                    return;
                }

                GetOutput("Plane")!.Value = plane;
            }
            catch (Exception ex)
            {
                HasError = true;
                ErrorMessage = $"Work plane failed: {ex.Message}";
            }
        }

        private PlaneData? ComputeFromFace(double offsetMm)
        {
            var faceInput = GetInput("Face")?.Value;
            if (faceInput == null)
            {
                HasError = true;
                ErrorMessage = "Face mode requires a face input";
                return null;
            }

            // Accept a Face from FaceListData or direct face reference
            object? faceObj = null;
            if (faceInput is FaceListData faceList && faceList.Faces.Count > 0)
                faceObj = faceList.Faces[0];
            else
                faceObj = faceInput;

            if (faceObj == null)
            {
                HasError = true;
                ErrorMessage = "No face provided";
                return null;
            }

            try
            {
                dynamic face = faceObj;

                // Compute face centroid from vertices
                double sumX = 0, sumY = 0, sumZ = 0;
                int count = 0;
                foreach (var vertex in face.Vertices)
                {
                    dynamic v = vertex;
                    dynamic pt = v.Point;
                    sumX += (double)pt.X;
                    sumY += (double)pt.Y;
                    sumZ += (double)pt.Z;
                    count++;
                }

                if (count == 0)
                {
                    HasError = true;
                    ErrorMessage = "Face has no vertices";
                    return null;
                }

                double cx = sumX / count;
                double cy = sumY / count;
                double cz = sumZ / count;

                // Get face normal at the centroid via evaluator
                var evaluator = face.Evaluator;
                double[] paramRange = new double[4];
                evaluator.GetParamRangeRect(ref paramRange);
                double midU = (paramRange[0] + paramRange[1]) / 2.0;
                double midV = (paramRange[2] + paramRange[3]) / 2.0;
                double[] param = { midU, midV };
                double[] normal = new double[3];
                evaluator.GetNormal(ref param, ref normal);

                double nx = normal[0], ny = normal[1], nz = normal[2];

                // Compute local X and Y axes from the normal
                // Pick a reference vector that isn't parallel to the normal
                double refX = 0, refY = 0, refZ = 1;
                if (Math.Abs(nz) > 0.9)
                {
                    refX = 1; refY = 0; refZ = 0; // use X if normal is ~Z
                }

                // xAxis = cross(normal, ref), then normalize
                double axX = ny * refZ - nz * refY;
                double axY = nz * refX - nx * refZ;
                double axZ = nx * refY - ny * refX;
                double axLen = Math.Sqrt(axX * axX + axY * axY + axZ * axZ);
                if (axLen < 1e-10)
                {
                    HasError = true;
                    ErrorMessage = "Degenerate face normal";
                    return null;
                }
                axX /= axLen; axY /= axLen; axZ /= axLen;

                // yAxis = cross(normal, xAxis)
                double ayX = ny * axZ - nz * axY;
                double ayY = nz * axX - nx * axZ;
                double ayZ = nx * axY - ny * axX;

                // Apply offset along normal
                double offCm = offsetMm * 0.1;
                cx += nx * offCm;
                cy += ny * offCm;
                cz += nz * offCm;

                return new PlaneData
                {
                    Name = "Face Plane",
                    StandardPlaneIndex = 0, // custom
                    OriginX = cx, OriginY = cy, OriginZ = cz,
                    NormalX = nx, NormalY = ny, NormalZ = nz,
                    XAxisX = axX, XAxisY = axY, XAxisZ = axZ,
                    YAxisX = ayX, YAxisY = ayY, YAxisZ = ayZ,
                    OffsetCm = offCm,
                    FaceRef = faceObj
                };
            }
            catch (Exception ex)
            {
                HasError = true;
                ErrorMessage = $"Face plane extraction failed: {ex.Message}";
                return null;
            }
        }

        public override string? GetDisplaySummary() => Plane;

        public override List<ParameterDescriptor> GetEditableParameters()
        {
            return new List<ParameterDescriptor>
            {
                new ParameterDescriptor
                {
                    Label = "Plane",
                    Key = "Plane",
                    Value = Plane,
                    Choices = new[] { "XY", "XZ", "YZ", "Face" },
                    DisplayOnNode = Plane
                }
            };
        }

        public override Dictionary<string, object?> GetParameters()
        {
            return new Dictionary<string, object?> { ["Plane"] = Plane };
        }

        public override void SetParameters(Dictionary<string, object?> parameters)
        {
            if (parameters.TryGetValue("Plane", out var p) && p is string s)
            {
                if (s == "XY" || s == "XZ" || s == "YZ" || s == "Face")
                    Plane = s;
            }
        }
    }
}
