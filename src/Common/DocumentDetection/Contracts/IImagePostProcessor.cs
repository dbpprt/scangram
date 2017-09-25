using OpenCvSharp;

namespace Scangram.Common.DocumentDetection.Contracts
{
    public interface IImagePostProcessor
    {
        void PostProcessImage(ref Mat image, Mat sourceImage);
    }
}
