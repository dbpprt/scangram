using System.Collections.Generic;
using OpenCvSharp;
using Scangram.Common.DocumentDetection.Contracts;

namespace Scangram.Common.DocumentDetection.Scorer
{
    class FourEdgesScorer : IResultScorer
    {
        public void Score(IList<ContourResult> results, Mat preProcessedImage, Mat sourceImage)
        {
            for (var i = results.Count - 1; i >= 0; i--)
            {
                if (results[i].Points.Length != 4)
                {
                    results.RemoveAt(i);
                }
            }
        }
    }
}
