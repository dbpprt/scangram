using System.Collections.Generic;
using OpenCvSharp;

namespace Scangram.Common.DocumentDetection.Detectors
{
    class SimpleContourDetector : BaseDetector
    {
        public SimpleContourDetector() 
            : base(15)
        {
        }

        public override IEnumerable<ContourResult> DetectDocumentContours(Mat image, Mat sourceImage)
        {
            var contours = Cv2
                .FindContoursAsArray(image, RetrievalModes.Tree, ContourApproximationModes.ApproxSimple);

            var results = new List<ContourResult>();

            foreach (var contour in contours)
            {
                var peri = Cv2.ArcLength(contour, true);
                var simplifiedContour = Cv2.ApproxPolyDP(contour, 0.03 * peri, true);

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
