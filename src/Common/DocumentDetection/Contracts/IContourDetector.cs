using System.Collections.Generic;
using OpenCvSharp;

namespace Scangram.Common.DocumentDetection.Contracts
{
    public interface IContourDetector
    {
        IEnumerable<ContourResult> DetectDocumentContours(Mat image, Mat sourceImage);
    }
}
