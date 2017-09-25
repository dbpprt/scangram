using OpenCvSharp;

namespace Scangram.Common.DocumentDetection.Contracts
{
    public interface IImageExtractor
    {
        Mat Extract(Mat src, Point[] contours);
    }
}
