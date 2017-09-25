using System.Collections.Generic;
using OpenCvSharp;

namespace Scangram.Common.DocumentDetection.Contracts
{
    public interface IResultScorer
    {
        void Score(IList<ContourResult> results, Mat preProcessedImage, Mat sourceImage);
    }
}
