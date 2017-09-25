using System.Collections.Generic;
using OpenCvSharp;

namespace Scangram.Common.DocumentDetection.Detectors
{
    class ConvexHullContourDetector : BaseDetector
    {
        public ConvexHullContourDetector() 
            : base(10)
        {
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
                var simplifiedContour = Cv2.ApproxPolyDP(convexHull, 0.03 * peri, true);

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
