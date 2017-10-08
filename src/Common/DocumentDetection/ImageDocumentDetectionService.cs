using System.Collections.Generic;
using System.IO;
using System.Linq;
using ImageMagick;
using OpenCvSharp;
using Scangram.Common.DocumentDetection.Contracts;

namespace Scangram.Common.DocumentDetection
{
    public class ImageDocumentDetectionService
    {
        private readonly IImagePreProcessor[] _preProcessors;
        private readonly IImageExtractor _extractor;
        private readonly IContourDetector[] _contourDetectors;
        private readonly IResultScorer[] _scorers;

        public ImageDocumentDetectionService(
            IImagePreProcessor[] preProcessors,
            IImageExtractor extractor,
            IContourDetector[] contourDetectors,
            IResultScorer[] scorers,
            IImagePostProcessor[] postProcessors)
        {
            _preProcessors = preProcessors;
            _extractor = extractor;
            _contourDetectors = contourDetectors;
            _scorers = scorers;
        }

        public void ProcessImageInteractive(string path)
        {
            using (var image = new Mat(path))
            {
                var preProcessedImage = image.Clone().CvtColor(ColorConversionCodes.BGR2GRAY);

                try
                {
                    var states = new List<dynamic>();
                    foreach (var preprocessor in _preProcessors)
                    {
                        states.Add(preprocessor.PreProcessImage(ref preProcessedImage, image));
                    }

                    var contourResults = new List<ContourResult>();
                    foreach (var boundaryDetector in _contourDetectors)
                    {
                        contourResults.AddRange(boundaryDetector.DetectDocumentContours(preProcessedImage, image));
                    }

                    for (var i = 0; i < _preProcessors.Length; i++)
                    {
                        foreach (var contourResult in contourResults)
                        {
                            _preProcessors[i].CorrectContours(contourResult, states[i]);
                        }
                    }

                    //foreach (var contourResult in contourResults)
                    //{
                    //    Cv2.DrawContours(image, new List<IEnumerable<Point>> { contourResult.Points }, -1, Scalar.Pink, 2);
                    //}

                    using (new Window(WindowMode.Normal, preProcessedImage))
                    {
                        Cv2.WaitKey();
                    }

                    foreach (var scorer in _scorers)
                    {
                        scorer.Score(contourResults, preProcessedImage, image);
                    }

                    var contour = contourResults.OrderByDescending(_ => _.Score)
                        .FirstOrDefault();

                    if (contour != null)
                    {
                        Cv2.DrawContours(preProcessedImage, new List<IEnumerable<Point>> { contour.Points }, -1, Scalar.White, 2);

                        using (var resultImage = _extractor.Extract(image, contour.Points))
                        {
                            using (new Window(WindowMode.Normal, image))
                            using (new Window(WindowMode.Normal, preProcessedImage))
                            using (new Window(WindowMode.Normal, resultImage))
                            {
                                Cv2.WaitKey();
                            }
                        }
                    }
                }
                finally
                {
                    preProcessedImage.Dispose();
                }
            }
        }

        public Stream ProcessStream(Stream stream)
        {
            //using (var image = new MagickImage(stream, new MagickReadSettings(new JpegReadDefines())))
            //{
            //    var size = new MagickGeometry(2000, 2000) { IgnoreAspectRatio = false };

            //    image.Resize(size);
            //    image.Format = MagickFormat.Jpeg;

            //    image.Write(resizedStream);
            //}

            using (var image = Mat.FromStream(stream, ImreadModes.Color))
            {
                var preProcessedImage = image.Clone().CvtColor(ColorConversionCodes.BGR2GRAY);

                try
                {
                    var states = new List<dynamic>();
                    foreach (var preprocessor in _preProcessors)
                    {
                        states.Add(preprocessor.PreProcessImage(ref preProcessedImage, image));
                    }

                    var contourResults = new List<ContourResult>();
                    foreach (var boundaryDetector in _contourDetectors)
                    {
                        contourResults.AddRange(boundaryDetector.DetectDocumentContours(preProcessedImage, image));
                    }

                    for (var i = 0; i < _preProcessors.Length; i++)
                    {
                        foreach (var contourResult in contourResults)
                        {
                            _preProcessors[i].CorrectContours(contourResult, states[i]);
                        }
                    }

                    foreach (var scorer in _scorers)
                    {
                        scorer.Score(contourResults, preProcessedImage, image);
                    }

                    var contour = contourResults.OrderByDescending(_ => _.Score)
                        .FirstOrDefault();

                    if (contour != null)
                    {
                        Cv2.DrawContours(preProcessedImage, new List<IEnumerable<Point>> { contour.Points }, -1, Scalar.White, 2);

                        using (var resultImage = _extractor.Extract(image, contour.Points))
                        {
                            return resultImage.ToMemoryStream(".jpg", new ImageEncodingParam(ImwriteFlags.JpegQuality, 95));
                        }
                    }
                }
                finally
                {
                    preProcessedImage.Dispose();
                }
            }

            return null;
        }
    }
}
