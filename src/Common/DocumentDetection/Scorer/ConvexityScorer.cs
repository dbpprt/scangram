using System.Collections.Generic;
using OpenCvSharp;
using Scangram.Common.DocumentDetection.Contracts;

namespace Scangram.Common.DocumentDetection.Scorer
{
    public class ConvexityScorer : IResultScorer
    {
        public void Score(IList<ContourResult> results, Mat preProcessedImage, Mat sourceImage)
        {
            for (var i = results.Count - 1; i >= 0; i--)
            {
                if (!Cv2.IsContourConvex(results[i].Points))
                {
                    results.RemoveAt(i);
                }
            }
        }
    }
}
