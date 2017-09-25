using System;
using System.Collections.Generic;
using System.Linq;
using OpenCvSharp;
using Scangram.Common.DocumentDetection.Contracts;

namespace Scangram.Common.DocumentDetection.Scorer
{
    class HoughLinesScorer : IResultScorer
    {
        public void Score(IList<ContourResult> results, Mat preProcessedImage, Mat sourceImage)
        {
            var lines = Cv2.HoughLinesP(preProcessedImage, 0.02, Math.PI / 500, 10, 100, 100);
            var scoringResults = new List<(double, ContourResult)>();

            for (var i = results.Count - 1; i >= 0; i--)
            {
                var lineCount = 0;

                foreach (var line in lines)
                {
                    if (Cv2.PointPolygonTest(results[i].Points, line.P1, false) >= 0 &&
                        Cv2.PointPolygonTest(results[i].Points, line.P2, false) >= 0)
                    {
                        lineCount++;
                    }
                }

                var lineAreaRatio = Cv2.ContourArea(results[i].Points) / lineCount;
                scoringResults.Add((lineAreaRatio, results[i]));
            }

            var topScoringResults = scoringResults.OrderBy(_ => _.Item1)
                .Take(5)
                .ToList();

            for (var i = topScoringResults.Count - 1; i >= 0; i--)
            {
                topScoringResults[i].Item2.Score += i;
            }
        }
    }
}
