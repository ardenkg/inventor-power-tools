// ============================================================================
// Threader Add-in for Autodesk Inventor 2026
// ThreadDataManager.cs - Manages Thread Data from Inventor's Thread Tables
// ============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using Inventor;

namespace Threader.Core
{
    /// <summary>
    /// Represents a thread standard with its properties.
    /// </summary>
    public class ThreadStandard
    {
        public string Designation { get; set; } = "";
        public double NominalDiameter { get; set; }  // in cm (Inventor internal units)
        public double Pitch { get; set; }             // in cm
        public double MajorDiameter { get; set; }     // in cm
        public double MinorDiameter { get; set; }     // in cm
        public double PitchDiameter { get; set; }     // in cm
        public string ThreadType { get; set; } = "";  // "ISO Metric profile"
        public bool IsInternal { get; set; }
        public string FullThreadName { get; set; } = "";
        
        /// <summary>
        /// Tap drill diameter - approximation for internal threads
        /// TapDrill ≈ (MinorDiameter + PitchDiameter) / 2
        /// </summary>
        public double TapDrillDiameter => (MinorDiameter + PitchDiameter) / 2.0;
        
        /// <summary>
        /// Display name for dropdown (e.g., "M8x1.25")
        /// </summary>
        public string DisplayName => Designation;
        
        /// <summary>
        /// Detailed description with dimensions
        /// </summary>
        public string Description => $"{Designation} - Pitch: {Pitch * 10:F2}mm, Major Ø: {MajorDiameter * 10:F2}mm";
    }

    /// <summary>
    /// Manages thread data from Inventor's internal thread tables.
    /// Queries ISO Metric Profile standards and filters by diameter.
    /// </summary>
    public class ThreadDataManager
    {
        #region Private Fields

        private readonly Inventor.Application _inventorApp;
        private readonly List<ThreadStandard> _allThreadStandards;
        private bool _isInitialized = false;

        #endregion

        #region Constructor

        public ThreadDataManager(Inventor.Application inventorApp)
        {
            _inventorApp = inventorApp ?? throw new ArgumentNullException(nameof(inventorApp));
            _allThreadStandards = new List<ThreadStandard>();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Initializes the thread data by loading from Inventor's thread tables.
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized) return;

            try
            {
                // Load ISO Metric thread data from built-in table
                LoadFallbackThreadData();
                _isInitialized = true;
                System.Diagnostics.Debug.WriteLine($"ThreadDataManager: Loaded {_allThreadStandards.Count} ISO Metric thread standards.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error initializing thread data: {ex.Message}");
                _isInitialized = true;
            }
        }

        /// <summary>
        /// Gets thread standards that match the given cylinder diameter.
        /// </summary>
        /// <param name="diameterCm">Cylinder diameter in centimeters (Inventor internal units)</param>
        /// <param name="tolerancePercent">Tolerance percentage for matching (default 10%)</param>
        /// <param name="isInternal">True for internal threads (holes), false for external threads (shafts)</param>
        /// <returns>List of matching thread standards</returns>
        public List<ThreadStandard> GetMatchingThreads(double diameterCm, double tolerancePercent = 10.0, bool isInternal = false)
        {
            if (!_isInitialized)
            {
                Initialize();
            }

            var tolerance = diameterCm * (tolerancePercent / 100.0);
            var minDiameter = diameterCm - tolerance;
            var maxDiameter = diameterCm + tolerance;

            // For internal threads (holes), match against the tap drill diameter
            // For external threads (shafts), match against the nominal/major diameter
            return _allThreadStandards
                .Where(t => t.IsInternal == isInternal)
                .Where(t => {
                    double compareDiameter = isInternal ? t.TapDrillDiameter : t.NominalDiameter;
                    return compareDiameter >= minDiameter && compareDiameter <= maxDiameter;
                })
                .OrderBy(t => {
                    double compareDiameter = isInternal ? t.TapDrillDiameter : t.NominalDiameter;
                    return Math.Abs(compareDiameter - diameterCm);
                })
                .ThenByDescending(t => t.Pitch)  // Prefer coarse thread (larger pitch) first
                .ToList();
        }

        /// <summary>
        /// Gets all thread standards for the specified type (internal or external), 
        /// sorted by nominal diameter, with a recommended marker for the closest match.
        /// </summary>
        /// <param name="isInternal">True for internal threads (holes), false for external (shafts).</param>
        /// <param name="currentDiameterCm">The current cylinder diameter in cm, used to mark recommended sizes.</param>
        /// <returns>Tuple containing the list of threads and the index of the recommended thread.</returns>
        public (List<ThreadStandard> Threads, int RecommendedIndex) GetAllThreadsForType(bool isInternal, double currentDiameterCm)
        {
            if (!_isInitialized)
            {
                Initialize();
            }

            // Filter by internal/external and get unique designations (avoid duplicates from coarse/fine)
            var threads = _allThreadStandards
                .Where(t => t.IsInternal == isInternal)
                .OrderBy(t => t.NominalDiameter)
                .ThenByDescending(t => t.Pitch)  // Coarse thread (larger pitch) first
                .ToList();

            // Find the recommended index (closest match to current diameter)
            int recommendedIndex = 0;
            double minDiff = double.MaxValue;
            
            for (int i = 0; i < threads.Count; i++)
            {
                double compareDiameter = isInternal ? threads[i].TapDrillDiameter : threads[i].NominalDiameter;
                double diff = Math.Abs(compareDiameter - currentDiameterCm);
                if (diff < minDiff)
                {
                    minDiff = diff;
                    recommendedIndex = i;
                }
            }

            return (threads, recommendedIndex);
        }

        /// <summary>
        /// Gets unique M sizes (nominal diameters) available for the specified thread type.
        /// Returns list of M size strings (e.g., "M3", "M4", "M5") with recommended index.
        /// </summary>
        public (List<string> Sizes, List<double> Diameters, int RecommendedIndex) GetAvailableMSizes(bool isInternal, double currentDiameterCm)
        {
            if (!_isInitialized)
            {
                Initialize();
            }

            // Get unique nominal diameters (rounded to avoid floating point duplicates)
            var uniqueDiameters = _allThreadStandards
                .Where(t => t.IsInternal == isInternal)
                .Select(t => Math.Round(t.NominalDiameter, 4))
                .Distinct()
                .OrderBy(d => d)
                .ToList();

            // Create M size labels - show decimal only when not a whole number
            var sizes = uniqueDiameters.Select(d => {
                double mm = d * 10;
                if (Math.Abs(mm - Math.Round(mm)) < 0.01)
                    return $"M{mm:F0}";
                else
                    return $"M{mm:G3}";
            }).ToList();

            // Find recommended index
            int recommendedIndex = 0;
            double minDiff = double.MaxValue;
            
            for (int i = 0; i < uniqueDiameters.Count; i++)
            {
                double compareDiameter = uniqueDiameters[i];
                double diff = Math.Abs(compareDiameter - currentDiameterCm);
                if (diff < minDiff)
                {
                    minDiff = diff;
                    recommendedIndex = i;
                }
            }

            return (sizes, uniqueDiameters, recommendedIndex);
        }

        /// <summary>
        /// Gets available pitch options for a specific M size.
        /// </summary>
        public List<ThreadStandard> GetPitchOptionsForSize(double nominalDiameterCm, bool isInternal)
        {
            if (!_isInitialized)
            {
                Initialize();
            }

            return _allThreadStandards
                .Where(t => t.IsInternal == isInternal && Math.Abs(t.NominalDiameter - nominalDiameterCm) < 0.001)
                .OrderByDescending(t => t.Pitch)  // Coarse (larger pitch) first
                .ToList();
        }

        /// <summary>
        /// Gets all available thread standards.
        /// </summary>
        public List<ThreadStandard> GetAllThreadStandards()
        {
            if (!_isInitialized)
            {
                Initialize();
            }

            return _allThreadStandards.ToList();
        }

        /// <summary>
        /// Gets a thread standard by its designation.
        /// </summary>
        public ThreadStandard? GetThreadByDesignation(string designation)
        {
            if (!_isInitialized)
            {
                Initialize();
            }

            return _allThreadStandards.FirstOrDefault(t => 
                t.Designation.Equals(designation, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Gets a thread standard by designation and type.
        /// </summary>
        public ThreadStandard? GetThreadByDesignation(string designation, bool isInternal)
        {
            if (!_isInitialized)
            {
                Initialize();
            }

            return _allThreadStandards.FirstOrDefault(t => 
                t.Designation.Equals(designation, StringComparison.OrdinalIgnoreCase) && 
                t.IsInternal == isInternal);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Loads fallback ISO Metric thread data when Inventor tables aren't accessible.
        /// Data based on ISO 261 and ISO 262 standards.
        /// </summary>
        private void LoadFallbackThreadData()
        {
            _allThreadStandards.Clear();

            // ISO Metric Coarse Thread (ISO 261) - Most common sizes
            // Dimensions in millimeters, converted to centimeters for Inventor
            var isoMetricCoarse = new[]
            {
                // (Designation, NominalDia_mm, Pitch_mm, MinorDia_mm, PitchDia_mm)
                ("M1", 1.0, 0.25, 0.729, 0.838),
                ("M1.2", 1.2, 0.25, 0.929, 1.038),
                ("M1.4", 1.4, 0.3, 1.075, 1.205),
                ("M1.6", 1.6, 0.35, 1.221, 1.373),
                ("M2", 2.0, 0.4, 1.567, 1.740),
                ("M2.5", 2.5, 0.45, 2.013, 2.208),
                ("M3", 3.0, 0.5, 2.459, 2.675),
                ("M3.5", 3.5, 0.6, 2.850, 3.110),
                ("M4", 4.0, 0.7, 3.242, 3.545),
                ("M5", 5.0, 0.8, 4.134, 4.480),
                ("M6", 6.0, 1.0, 4.917, 5.350),
                ("M7", 7.0, 1.0, 5.917, 6.350),
                ("M8", 8.0, 1.25, 6.647, 7.188),
                ("M10", 10.0, 1.5, 8.376, 9.026),
                ("M12", 12.0, 1.75, 10.106, 10.863),
                ("M14", 14.0, 2.0, 11.835, 12.701),
                ("M16", 16.0, 2.0, 13.835, 14.701),
                ("M18", 18.0, 2.5, 15.294, 16.376),
                ("M20", 20.0, 2.5, 17.294, 18.376),
                ("M22", 22.0, 2.5, 19.294, 20.376),
                ("M24", 24.0, 3.0, 20.752, 22.051),
                ("M27", 27.0, 3.0, 23.752, 25.051),
                ("M30", 30.0, 3.5, 26.211, 27.727),
                ("M33", 33.0, 3.5, 29.211, 30.727),
                ("M36", 36.0, 4.0, 31.670, 33.402),
                ("M39", 39.0, 4.0, 34.670, 36.402),
                ("M42", 42.0, 4.5, 37.129, 39.077),
                ("M45", 45.0, 4.5, 40.129, 42.077),
                ("M48", 48.0, 5.0, 42.587, 44.752),
                ("M52", 52.0, 5.0, 46.587, 48.752),
                ("M56", 56.0, 5.5, 50.046, 52.428),
                ("M60", 60.0, 5.5, 54.046, 56.428),
                ("M64", 64.0, 6.0, 57.505, 60.103),
            };

            // ISO Metric Fine Thread - Common sizes
            var isoMetricFine = new[]
            {
                ("M8x1", 8.0, 1.0, 6.917, 7.350),
                ("M10x1", 10.0, 1.0, 8.917, 9.350),
                ("M10x1.25", 10.0, 1.25, 8.647, 9.188),
                ("M12x1", 12.0, 1.0, 10.917, 11.350),
                ("M12x1.25", 12.0, 1.25, 10.647, 11.188),
                ("M12x1.5", 12.0, 1.5, 10.376, 11.026),
                ("M14x1.5", 14.0, 1.5, 12.376, 13.026),
                ("M16x1", 16.0, 1.0, 14.917, 15.350),
                ("M16x1.5", 16.0, 1.5, 14.376, 15.026),
                ("M18x1.5", 18.0, 1.5, 16.376, 17.026),
                ("M18x2", 18.0, 2.0, 15.835, 16.701),
                ("M20x1.5", 20.0, 1.5, 18.376, 19.026),
                ("M20x2", 20.0, 2.0, 17.835, 18.701),
                ("M22x1.5", 22.0, 1.5, 20.376, 21.026),
                ("M22x2", 22.0, 2.0, 19.835, 20.701),
                ("M24x1.5", 24.0, 1.5, 22.376, 23.026),
                ("M24x2", 24.0, 2.0, 21.835, 22.701),
                ("M27x1.5", 27.0, 1.5, 25.376, 26.026),
                ("M27x2", 27.0, 2.0, 24.835, 25.701),
                ("M30x1.5", 30.0, 1.5, 28.376, 29.026),
                ("M30x2", 30.0, 2.0, 27.835, 28.701),
                ("M30x3", 30.0, 3.0, 26.752, 28.051),
                ("M33x1.5", 33.0, 1.5, 31.376, 32.026),
                ("M33x2", 33.0, 2.0, 30.835, 31.701),
                ("M33x3", 33.0, 3.0, 29.752, 31.051),
                ("M36x1.5", 36.0, 1.5, 34.376, 35.026),
                ("M36x2", 36.0, 2.0, 33.835, 34.701),
                ("M36x3", 36.0, 3.0, 32.752, 34.051),
            };

            // Add coarse threads (external and internal)
            foreach (var (designation, nomDia, pitch, minorDia, pitchDia) in isoMetricCoarse)
            {
                _allThreadStandards.Add(new ThreadStandard
                {
                    Designation = designation,
                    NominalDiameter = nomDia / 10.0,  // Convert mm to cm
                    Pitch = pitch / 10.0,
                    MajorDiameter = nomDia / 10.0,
                    MinorDiameter = minorDia / 10.0,
                    PitchDiameter = pitchDia / 10.0,
                    ThreadType = "ISO Metric profile",
                    IsInternal = false,
                    FullThreadName = $"ISO Metric profile - {designation}"
                });

                // Add internal thread version
                _allThreadStandards.Add(new ThreadStandard
                {
                    Designation = designation,
                    NominalDiameter = nomDia / 10.0,
                    Pitch = pitch / 10.0,
                    MajorDiameter = nomDia / 10.0,
                    MinorDiameter = minorDia / 10.0,
                    PitchDiameter = pitchDia / 10.0,
                    ThreadType = "ISO Metric profile",
                    IsInternal = true,
                    FullThreadName = $"ISO Metric profile - {designation} (Internal)"
                });
            }

            // Add fine threads (external and internal)
            foreach (var (designation, nomDia, pitch, minorDia, pitchDia) in isoMetricFine)
            {
                _allThreadStandards.Add(new ThreadStandard
                {
                    Designation = designation,
                    NominalDiameter = nomDia / 10.0,
                    Pitch = pitch / 10.0,
                    MajorDiameter = nomDia / 10.0,
                    MinorDiameter = minorDia / 10.0,
                    PitchDiameter = pitchDia / 10.0,
                    ThreadType = "ISO Metric profile",
                    IsInternal = false,
                    FullThreadName = $"ISO Metric profile - {designation}"
                });

                // Add internal thread version
                _allThreadStandards.Add(new ThreadStandard
                {
                    Designation = designation,
                    NominalDiameter = nomDia / 10.0,
                    Pitch = pitch / 10.0,
                    MajorDiameter = nomDia / 10.0,
                    MinorDiameter = minorDia / 10.0,
                    PitchDiameter = pitchDia / 10.0,
                    ThreadType = "ISO Metric profile",
                    IsInternal = true,
                    FullThreadName = $"ISO Metric profile - {designation} (Internal)"
                });
            }

            System.Diagnostics.Debug.WriteLine($"Loaded {_allThreadStandards.Count} fallback thread standards.");
        }

        #endregion
    }
}
