using System.Linq;
using OpenCvSharp;
using Scangram.Common.DocumentDetection.Contracts;

namespace Scangram.Common.DocumentDetection.Preprocessors
{
    class SimpleCannyImagePreProcessor : IImagePreProcessor
    {
        public dynamic PreProcessImage(ref Mat image, Mat sourceImage)
        {
            var copy = new Mat();

            try
            {
                Cv2.BilateralFilter(image, copy, 9, 75, 75);
                Cv2.AdaptiveThreshold(copy, copy, 255, AdaptiveThresholdTypes.GaussianC, ThresholdTypes.Binary, 115, 4);
                Cv2.MedianBlur(copy, copy, 11);
                Cv2.CopyMakeBorder(copy, copy, 5, 5, 5, 5, BorderTypes.Constant, Scalar.Black);

                // TODO: Dispose new Mat()
                var otsu = Cv2.Threshold(copy, new Mat(), 0, 255, ThresholdTypes.Binary | ThresholdTypes.Otsu);
                Cv2.Canny(copy, copy, otsu, otsu * 2, 3, true);
            }
            catch
            {
                copy.Dispose();
                throw;
            }

            image.Dispose();
            image = copy;

            return null;
        }

        public void CorrectContours(ContourResult contours, dynamic state)
        {
            contours.Points = contours.Points.Select(__ => new Point(__.X - 5, __.Y - 5)).ToArray();
            // TODO: Correct area
        }
    }
}
