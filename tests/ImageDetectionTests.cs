using System;
using System.IO;
using System.Linq;
using Scangram.Common.DocumentDetection;
using Scangram.Common.DocumentDetection.Contracts;
using Scangram.Common.DocumentDetection.Detectors;
using Scangram.Common.DocumentDetection.Preprocessors;
using Scangram.Common.DocumentDetection.Scorer;
using Xunit;

namespace Scangram.Tests
{
    public class ImageDetectionTests
    {
        private ImageDocumentDetectionService _service;

        public ImageDetectionTests()
        {
            _service = new ImageDocumentDetectionService(
                new IImagePreProcessor[] { new ResizeImagePreProcessor(1600), new SimpleCannyImagePreProcessor() },
                new SimplePerspectiveTransformImageExtractor(),
                new IContourDetector[]
                {
                    new SimpleContourDetector(20, 0.005), new SimpleContourDetector(15, 0.03),
                    new ConvexHullContourDetector(10, 0.03)
                },
                new IResultScorer[]
                    {new FourEdgesScorer(), new ConvexityScorer(), new AreaScorer(10), new HoughLinesScorer()},
                new IImagePostProcessor[] { });
        }

        [Fact]
        public void MemoryTest()
        {
            using (var file = File.OpenRead("./images/7.jpg"))
            {
                for (var i = 0; i < 100; i++)
                {
                    var result = _service.ProcessStream(file);

                    result?.Dispose();
                }
            }
        }

        [Fact]
        public void MemoryTestWithLargeImage()
        {
            using (var file = File.OpenRead("./images/12.jpg"))
            {
                for (var i = 0; i < 100; i++)
                {
                    var result = _service.ProcessStream(file);

                    result?.Dispose();
                }
            }
        }

        [Fact]
        public void ParallelMemoryTestWithLargeImage()
        {

            Enumerable.Range(0, 100)
                .AsParallel()
                .WithDegreeOfParallelism(4)
                .ForAll(_ =>
                {
                    using (var file = File.OpenRead("./images/12.jpg"))
                    {
                        var result = _service.ProcessStream(file);
                        result?.Dispose();
                    }
                });

        }

        [Fact]
        public void ParallelMemoryTestWithAllImages()
        {

            Enumerable.Range(0, 100)
                .AsParallel()
                .WithDegreeOfParallelism(8)
                .ForAll(_ =>
                {
                    Directory.EnumerateFiles("./images/", "*.jpg").ToList().ForEach(fileName =>
                    {
                        using (var file = File.OpenRead(fileName))
                        {
                            var result = _service.ProcessStream(file);
                            result?.Dispose();
                        }
                    });
                });

        }
    }
}
