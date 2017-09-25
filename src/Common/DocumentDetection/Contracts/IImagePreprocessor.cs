using OpenCvSharp;

namespace Scangram.Common.DocumentDetection.Contracts
{
    public interface IImagePreProcessor
    {
        dynamic PreProcessImage(ref Mat image, Mat sourceImage);

        void CorrectContours(ContourResult contours, dynamic state);
    }
}
