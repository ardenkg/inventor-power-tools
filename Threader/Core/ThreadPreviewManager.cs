// ============================================================================
// Threader Add-in for Autodesk Inventor 2026
// ThreadPreviewManager.cs - Manages Thread Preview using ClientGraphics
// ============================================================================

using System;
using System.Collections.Generic;
using Inventor;

namespace Threader.Core
{
    /// <summary>
    /// Manages the preview visualization of threads using ClientGraphics.
    /// Shows a helical path preview before the actual thread is created.
    /// </summary>
    public class ThreadPreviewManager
    {
        #region Private Fields

        private readonly Inventor.Application _inventorApp;
        private readonly string _clientGraphicsId = "Threader_Preview";
        
        private ClientGraphics? _clientGraphics;
        private GraphicsDataSets? _graphicsDataSets;
        private GraphicsNode? _previewNode;
        private bool _isPreviewActive = false;

        #endregion

        #region Constructor

        public ThreadPreviewManager(Inventor.Application inventorApp)
        {
            _inventorApp = inventorApp ?? throw new ArgumentNullException(nameof(inventorApp));
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets whether a preview is currently active.
        /// </summary>
        public bool IsPreviewActive => _isPreviewActive;

        #endregion

        #region Public Methods

        /// <summary>
        /// Shows a preview of the thread on the specified cylinder.
        /// </summary>
        /// <param name="cylinderInfo">Information about the selected cylinder.</param>
        /// <param name="threadStandard">The thread standard to preview.</param>
        /// <param name="threadLengthCm">Optional thread length override.</param>
        /// <param name="rightHand">True for right-hand thread.</param>
        /// <param name="startFromEnd">If true, start thread from the end (top) of the cylinder.</param>
        public void ShowPreview(
            CylinderInfo cylinderInfo, 
            ThreadStandard threadStandard,
            double? threadLengthCm = null,
            bool rightHand = true,
            bool startFromEnd = false)
        {
            if (cylinderInfo == null || threadStandard == null || !cylinderInfo.IsValid)
                return;

            try
            {
                // Clear any existing preview
                ClearPreview();

                // Support both direct part editing and in-place editing within an assembly
                var doc = (_inventorApp.ActiveEditDocument as PartDocument)
                          ?? (_inventorApp.ActiveDocument as PartDocument);
                if (doc == null) return;

                var compDef = doc.ComponentDefinition;
                var transGeom = _inventorApp.TransientGeometry;

                // Create client graphics collection for the preview (non-transacting so it doesn't participate in undo)
                _graphicsDataSets = doc.GraphicsDataSetsCollection.AddNonTransacting(_clientGraphicsId);
                _clientGraphics = compDef.ClientGraphicsCollection.AddNonTransacting(_clientGraphicsId);

                // Create the preview node
                _previewNode = _clientGraphics.AddNode(1);

                // Calculate thread parameters
                double pitch = threadStandard.Pitch;
                double majorRadius = threadStandard.MajorDiameter / 2.0;
                double minorRadius = threadStandard.MinorDiameter / 2.0;
                double pitchRadius = threadStandard.PitchDiameter / 2.0;
                double threadLength = threadLengthCm ?? cylinderInfo.LengthCm;
                int revolutions = (int)Math.Ceiling(threadLength / pitch);
                int pointsPerRevolution = 36;  // 10 degrees per point

                // Generate helix points for the thread visualization
                var helixPoints = GenerateHelixPoints(
                    cylinderInfo,
                    pitchRadius,
                    pitch,
                    threadLength,
                    revolutions,
                    pointsPerRevolution,
                    rightHand,
                    startFromEnd,
                    transGeom);

                // Create line strips to show the helix
                if (helixPoints.Count > 1)
                {
                    // Create coordinate set from helix points
                    var coordSet = _graphicsDataSets.CreateCoordinateSet(1);
                    
                    // Convert list to array for Inventor API
                    var coordArray = new double[helixPoints.Count * 3];
                    for (int i = 0; i < helixPoints.Count; i++)
                    {
                        coordArray[i * 3] = helixPoints[i].X;
                        coordArray[i * 3 + 1] = helixPoints[i].Y;
                        coordArray[i * 3 + 2] = helixPoints[i].Z;
                    }
                    coordSet.PutCoordinates(ref coordArray);

                    // Create line strip graphics
                    var lineStripGraphics = _previewNode.AddLineStripGraphics();
                    lineStripGraphics.CoordinateSet = coordSet;

                    // Set line appearance
                    lineStripGraphics.LineWeight = 2.0;
                    lineStripGraphics.BurnThrough = true;

                    // Create color set for the lines (cyan/teal color)
                    var colorSet = _graphicsDataSets.CreateColorSet(2);
                    var colorArray = new byte[helixPoints.Count * 3];
                    for (int i = 0; i < helixPoints.Count; i++)
                    {
                        colorArray[i * 3] = 0;      // R
                        colorArray[i * 3 + 1] = 204; // G
                        colorArray[i * 3 + 2] = 255; // B
                    }
                    colorSet.PutColors(ref colorArray);
                    lineStripGraphics.ColorSet = colorSet;
                }

                // Add major and minor diameter circles at the thread start position
                AddDiameterIndicators(cylinderInfo, majorRadius, minorRadius, threadLength, startFromEnd, transGeom);

                // Update the view
                _inventorApp.ActiveView.Update();
                _isPreviewActive = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error showing preview: {ex.Message}");
                ClearPreview();
            }
        }

        /// <summary>
        /// Clears any existing preview from the model.
        /// </summary>
        public void ClearPreview()
        {
            try
            {
                // Support both direct part editing and in-place editing within an assembly
                var doc = (_inventorApp.ActiveEditDocument as PartDocument)
                          ?? (_inventorApp.ActiveDocument as PartDocument);
                if (doc == null) return;

                var compDef = doc.ComponentDefinition;

                // Remove client graphics
                try
                {
                    var cg = compDef.ClientGraphicsCollection[_clientGraphicsId];
                    cg.Delete();
                }
                catch { }

                // Remove graphics data sets
                try
                {
                    var gds = doc.GraphicsDataSetsCollection[_clientGraphicsId];
                    gds.Delete();
                }
                catch { }

                _clientGraphics = null;
                _graphicsDataSets = null;
                _previewNode = null;
                _isPreviewActive = false;

                // Update the view
                try
                {
                    _inventorApp.ActiveView.Update();
                }
                catch { }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error clearing preview: {ex.Message}");
                _isPreviewActive = false;
            }
        }

        /// <summary>
        /// Updates the preview with new parameters without fully recreating it.
        /// </summary>
        public void UpdatePreview(
            CylinderInfo cylinderInfo, 
            ThreadStandard threadStandard,
            double? threadLengthCm = null,
            bool rightHand = true,
            bool startFromEnd = false)
        {
            // For simplicity, just recreate the preview
            ShowPreview(cylinderInfo, threadStandard, threadLengthCm, rightHand, startFromEnd);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Generates helix points for the thread visualization.
        /// </summary>
        private List<Inventor.Point> GenerateHelixPoints(
            CylinderInfo cylinderInfo,
            double radius,
            double pitch,
            double length,
            int revolutions,
            int pointsPerRevolution,
            bool rightHand,
            bool startFromEnd,
            TransientGeometry transGeom)
        {
            var points = new List<Inventor.Point>();

            // Use the actual start point of the cylindrical face (not the mathematical base point)
            var basePoint = cylinderInfo.StartPoint;
            var axisVector = cylinderInfo.AxisVector;

            // Normalize axis vector
            double axisLength = Math.Sqrt(axisVector[0] * axisVector[0] + 
                                          axisVector[1] * axisVector[1] + 
                                          axisVector[2] * axisVector[2]);
            double[] normAxis = new double[3];
            normAxis[0] = axisVector[0] / axisLength;
            normAxis[1] = axisVector[1] / axisLength;
            normAxis[2] = axisVector[2] / axisLength;
            
            // Determine start point based on startFromEnd flag
            double[] startPoint = new double[3];
            double axisDirection = 1.0;
            if (startFromEnd)
            {
                // Start from the end of the cylinder, move towards start
                double cylLength = cylinderInfo.LengthCm;
                startPoint[0] = basePoint[0] + cylLength * normAxis[0];
                startPoint[1] = basePoint[1] + cylLength * normAxis[1];
                startPoint[2] = basePoint[2] + cylLength * normAxis[2];
                axisDirection = -1.0;  // Move backwards along axis
            }
            else
            {
                // Start from the beginning of the cylinder
                startPoint[0] = basePoint[0];
                startPoint[1] = basePoint[1];
                startPoint[2] = basePoint[2];
            }

            // Calculate perpendicular vectors for the helix
            double[] perpVec1 = GetPerpendicularVector(normAxis);
            double[] perpVec2 = CrossProduct(normAxis, perpVec1);

            // Generate helix points
            int totalPoints = revolutions * pointsPerRevolution;
            double angleStep = 2.0 * Math.PI / pointsPerRevolution;
            double heightStep = pitch / pointsPerRevolution;
            int direction = rightHand ? 1 : -1;

            for (int i = 0; i <= totalPoints; i++)
            {
                double angle = i * angleStep * direction;
                double height = i * heightStep;

                // Stop if we've exceeded the length
                if (height > length) break;

                // Calculate point position using start point (with axis direction for end-start)
                double x = startPoint[0] + 
                           height * axisDirection * normAxis[0] + 
                           radius * (Math.Cos(angle) * perpVec1[0] + Math.Sin(angle) * perpVec2[0]);
                double y = startPoint[1] + 
                           height * axisDirection * normAxis[1] + 
                           radius * (Math.Cos(angle) * perpVec1[1] + Math.Sin(angle) * perpVec2[1]);
                double z = startPoint[2] + 
                           height * axisDirection * normAxis[2] + 
                           radius * (Math.Cos(angle) * perpVec1[2] + Math.Sin(angle) * perpVec2[2]);

                points.Add(transGeom.CreatePoint(x, y, z));
            }

            return points;
        }

        /// <summary>
        /// Adds diameter indicator circles at the thread start position.
        /// </summary>
        private void AddDiameterIndicators(
            CylinderInfo cylinderInfo,
            double majorRadius,
            double minorRadius,
            double threadLength,
            bool startFromEnd,
            TransientGeometry transGeom)
        {
            if (_previewNode == null || _graphicsDataSets == null) return;

            try
            {
                // Get base point and axis
                var basePoint = cylinderInfo.StartPoint;
                var axisVector = cylinderInfo.AxisVector;

                // Normalize axis
                double axisLength = Math.Sqrt(axisVector[0] * axisVector[0] + 
                                              axisVector[1] * axisVector[1] + 
                                              axisVector[2] * axisVector[2]);
                double[] normAxis = new double[3];
                normAxis[0] = axisVector[0] / axisLength;
                normAxis[1] = axisVector[1] / axisLength;
                normAxis[2] = axisVector[2] / axisLength;
                
                // Determine indicator position based on startFromEnd flag
                double[] indicatorPoint = new double[3];
                if (startFromEnd)
                {
                    // Place indicators at the end of the cylinder
                    double cylLength = cylinderInfo.LengthCm;
                    indicatorPoint[0] = basePoint[0] + cylLength * normAxis[0];
                    indicatorPoint[1] = basePoint[1] + cylLength * normAxis[1];
                    indicatorPoint[2] = basePoint[2] + cylLength * normAxis[2];
                }
                else
                {
                    // Place indicators at the start of the cylinder
                    indicatorPoint[0] = basePoint[0];
                    indicatorPoint[1] = basePoint[1];
                    indicatorPoint[2] = basePoint[2];
                }

                double[] perpVec1 = GetPerpendicularVector(normAxis);
                double[] perpVec2 = CrossProduct(normAxis, perpVec1);

                // Create major diameter circle (red)
                var majorPoints = GenerateCirclePoints(indicatorPoint, perpVec1, perpVec2, majorRadius, 36, transGeom);
                if (majorPoints.Count > 0)
                {
                    var majorCoordSet = _graphicsDataSets.CreateCoordinateSet(3);
                    var majorCoordArray = new double[majorPoints.Count * 3];
                    for (int i = 0; i < majorPoints.Count; i++)
                    {
                        majorCoordArray[i * 3] = majorPoints[i].X;
                        majorCoordArray[i * 3 + 1] = majorPoints[i].Y;
                        majorCoordArray[i * 3 + 2] = majorPoints[i].Z;
                    }
                    majorCoordSet.PutCoordinates(ref majorCoordArray);

                    var majorLine = _previewNode.AddLineStripGraphics();
                    majorLine.CoordinateSet = majorCoordSet;
                    majorLine.LineWeight = 3.0;
                    majorLine.BurnThrough = true;

                    // Red color for major diameter
                    var majorColorSet = _graphicsDataSets.CreateColorSet(4);
                    var majorColorArray = new byte[majorPoints.Count * 3];
                    for (int i = 0; i < majorPoints.Count; i++)
                    {
                        majorColorArray[i * 3] = 255;    // R
                        majorColorArray[i * 3 + 1] = 50; // G
                        majorColorArray[i * 3 + 2] = 50; // B
                    }
                    majorColorSet.PutColors(ref majorColorArray);
                    majorLine.ColorSet = majorColorSet;
                }

                // Create minor diameter circle (green)
                var minorPoints = GenerateCirclePoints(indicatorPoint, perpVec1, perpVec2, minorRadius, 36, transGeom);
                if (minorPoints.Count > 0)
                {
                    var minorCoordSet = _graphicsDataSets.CreateCoordinateSet(5);
                    var minorCoordArray = new double[minorPoints.Count * 3];
                    for (int i = 0; i < minorPoints.Count; i++)
                    {
                        minorCoordArray[i * 3] = minorPoints[i].X;
                        minorCoordArray[i * 3 + 1] = minorPoints[i].Y;
                        minorCoordArray[i * 3 + 2] = minorPoints[i].Z;
                    }
                    minorCoordSet.PutCoordinates(ref minorCoordArray);

                    var minorLine = _previewNode.AddLineStripGraphics();
                    minorLine.CoordinateSet = minorCoordSet;
                    minorLine.LineWeight = 3.0;
                    minorLine.BurnThrough = true;

                    // Green color for minor diameter
                    var minorColorSet = _graphicsDataSets.CreateColorSet(6);
                    var minorColorArray = new byte[minorPoints.Count * 3];
                    for (int i = 0; i < minorPoints.Count; i++)
                    {
                        minorColorArray[i * 3] = 50;     // R
                        minorColorArray[i * 3 + 1] = 255; // G
                        minorColorArray[i * 3 + 2] = 50;  // B
                    }
                    minorColorSet.PutColors(ref minorColorArray);
                    minorLine.ColorSet = minorColorSet;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error adding diameter indicators: {ex.Message}");
            }
        }

        /// <summary>
        /// Generates circle points for visualization.
        /// </summary>
        private List<Inventor.Point> GenerateCirclePoints(
            double[] center,
            double[] perpVec1,
            double[] perpVec2,
            double radius,
            int numPoints,
            TransientGeometry transGeom)
        {
            var points = new List<Inventor.Point>();

            for (int i = 0; i <= numPoints; i++)
            {
                double angle = 2.0 * Math.PI * i / numPoints;
                double x = center[0] + radius * (Math.Cos(angle) * perpVec1[0] + Math.Sin(angle) * perpVec2[0]);
                double y = center[1] + radius * (Math.Cos(angle) * perpVec1[1] + Math.Sin(angle) * perpVec2[1]);
                double z = center[2] + radius * (Math.Cos(angle) * perpVec1[2] + Math.Sin(angle) * perpVec2[2]);

                points.Add(transGeom.CreatePoint(x, y, z));
            }

            return points;
        }

        /// <summary>
        /// Gets a vector perpendicular to the given axis.
        /// </summary>
        private double[] GetPerpendicularVector(double[] axis)
        {
            double[] result = new double[3];

            // Find a non-parallel reference vector
            if (Math.Abs(axis[0]) < 0.9)
            {
                // Use X axis as reference
                result[0] = 1; result[1] = 0; result[2] = 0;
            }
            else
            {
                // Use Y axis as reference
                result[0] = 0; result[1] = 1; result[2] = 0;
            }

            // Calculate perpendicular via cross product
            var perp = CrossProduct(axis, result);

            // Normalize
            double length = Math.Sqrt(perp[0] * perp[0] + perp[1] * perp[1] + perp[2] * perp[2]);
            perp[0] /= length;
            perp[1] /= length;
            perp[2] /= length;

            return perp;
        }

        /// <summary>
        /// Calculates cross product of two vectors.
        /// </summary>
        private double[] CrossProduct(double[] a, double[] b)
        {
            return new double[]
            {
                a[1] * b[2] - a[2] * b[1],
                a[2] * b[0] - a[0] * b[2],
                a[0] * b[1] - a[1] * b[0]
            };
        }

        #endregion
    }
}
