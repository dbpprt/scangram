using System.Collections.Generic;
using OpenCvSharp;
using Scangram.Common.DocumentDetection.Contracts;

namespace Scangram.Common.DocumentDetection.Detectors
{
    abstract class BaseDetector : IContourDetector
    {
        public int Score { get; }

        protected BaseDetector(int score)
        {
            Score = score;
        }

        public abstract IEnumerable<ContourResult> DetectDocumentContours(Mat image, Mat sourceImage);
    }
}
