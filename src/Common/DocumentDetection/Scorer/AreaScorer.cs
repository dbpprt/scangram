using System.Collections.Generic;
using OpenCvSharp;
using Scangram.Common.DocumentDetection.Contracts;

namespace Scangram.Common.DocumentDetection.Scorer
{
    class AreaScorer : IResultScorer
    {
        private readonly int _maxSizeTolerancePixels;

        public AreaScorer(int maxSizeTolerancePixels)
        {
            _maxSizeTolerancePixels = maxSizeTolerancePixels;
        }

        public void Score(IList<ContourResult> results, Mat preProcessedImage, Mat sourceImage)
        {
            var sourceArea = sourceImage.Height * sourceImage.Width;
            var maxToleranceArea = (sourceImage.Height - _maxSizeTolerancePixels) *
                                (sourceImage.Width - _maxSizeTolerancePixels);
            
            var minToleranceArea = 0.25 * maxToleranceArea;
            
            for (var i = results.Count - 1; i >= 0; i--)
            {
                var contourArea = Cv2.ContourArea(results[i].Points);

                if (contourArea >= maxToleranceArea || contourArea < minToleranceArea)
                {
                    results.RemoveAt(i);
                    continue;
                }

                var contourRatio = sourceArea / contourArea;
                results[i].Score -= contourRatio;
            }
        }
    }
}
