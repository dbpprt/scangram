using System.Collections.Generic;
using OpenCvSharp;

namespace Scangram.Common.DocumentDetection.Detectors
{
    class ConvexHullContourDetector : BaseDetector
    {
        private readonly double _epsilon;

        public ConvexHullContourDetector(int score, double epsilon) 
            : base(score)
        {
            _epsilon = epsilon;
        }

        public override IEnumerable<ContourResult> DetectDocumentContours(Mat image, Mat sourceImage)
        {
            var contours = Cv2
                .FindContoursAsArray(image, RetrievalModes.Tree, ContourApproximationModes.ApproxSimple);

            var results = new List<ContourResult>();

            foreach (var contour in contours)
            {
                var convexHull = Cv2.ConvexHull(contour);
                var peri = Cv2.ArcLength(convexHull, true);
                var simplifiedContour = Cv2.ApproxPolyDP(convexHull, _epsilon * peri, true);

                if (simplifiedContour != null)
                {
                    results.Add(new ContourResult
                    {
                        Score = Score,
                        Points = simplifiedContour
                    });
                }
            }

            return results;
        }
    }
}
