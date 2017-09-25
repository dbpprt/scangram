using OpenCvSharp;

namespace Scangram.Common.DocumentDetection
{
    public class ContourResult
    {
        public double Score { get; set; }

        public Point[] Points { get; set; }
    }
}
