using System.Dynamic;
using System.Linq;
using OpenCvSharp;
using Scangram.Common.DocumentDetection.Contracts;

namespace Scangram.Common.DocumentDetection.Preprocessors
{
    public class ResizeImagePreProcessor : IImagePreProcessor
    {
        private readonly int _height;

        public ResizeImagePreProcessor(int height)
        {
            _height = height;
        }

        public dynamic PreProcessImage(ref Mat image, Mat sourceImage)
        {
            if (image.Height > _height)
            {
                var ratio = (double)_height / image.Height;
                Cv2.Resize(image, image, new Size(ratio * image.Width, _height));

                dynamic result = new ExpandoObject();
                result.Ratio = ratio;

                return result;
            }

            return null;
        }

        public void CorrectContours(ContourResult contours, dynamic state)
        {
            if (state != null)
            {
                contours.Points = contours.Points.Select(__ => new Point(__.X / (double)state.Ratio, __.Y / (double)state.Ratio)).ToArray();
            }
        }
    }
}
