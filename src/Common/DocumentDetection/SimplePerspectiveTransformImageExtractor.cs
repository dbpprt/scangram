using System.Collections.Generic;
using System.Linq;
using OpenCvSharp;
using Scangram.Common.DocumentDetection.Contracts;

namespace Scangram.Common.DocumentDetection
{
    public class SimplePerspectiveTransformImageExtractor : IImageExtractor
    {
        private static IEnumerable<Point2f> CornerSort(Point[] points)
        {
            var tl = points.OrderBy(_ => _.X + _.Y).First();
            var br = points.OrderByDescending(_ => _.X + _.Y).First();
            var tr = points.OrderBy(_ => _.X - _.Y).First();
            var bl = points.OrderByDescending(_ => _.X - _.Y).First();

            return new List<Point2f>
            {
                new Point2f(tl.X, tl.Y),
                new Point2f(tr.X, tr.Y),
                new Point2f(br.X, br.Y),
                new Point2f(bl.X, bl.Y)
            };
        }
        
        public Mat Extract(Mat src, Point[] contours)
        {
            var edges = CornerSort(contours);
            var boundingRectangle = Cv2.BoundingRect(contours);

            var result = new Mat();

            var inputMatrix = new List<Point2f>();
            inputMatrix.AddRange(edges.Select(_ => new Point2f(_.X, _.Y)));

            var outputMatrix = new List<Point2f>
            {
                new Point2f(0, 0),
                new Point2f(0, boundingRectangle.Height),
                new Point2f(boundingRectangle.Width, boundingRectangle.Height),
                new Point2f(boundingRectangle.Width, 0)
            };

            using (var transformation = Cv2.GetPerspectiveTransform(inputMatrix, outputMatrix))
            {
                Cv2.WarpPerspective(src, result, transformation, new Size(boundingRectangle.Width, boundingRectangle.Height));
            }

            return result;
        }
    }
}
