using Xunit;
using TripleDetection.Models;

namespace TripleDetection.App.Tests
{
    public class DetectionResultTests
    {
        [Fact]
        public void DetectionResult_DefaultValues_AreCorrect()
        {
            var result = new DetectionResult();

            Assert.False(result.IsOK);
            Assert.Null(result.BatchNumber);
            Assert.Null(result.ProductionDate);
            Assert.Null(result.ExpirationDate);
            Assert.Null(result.ImagePath);
        }

        [Fact]
        public void DetectionResult_CanSetProperties()
        {
            var result = new DetectionResult
            {
                IsOK = true,
                BatchNumber = "BATCH001",
                ProductionDate = "20250530",
                ExpirationDate = "20260530",
                ImagePath = @"D:\Images\OK\test.png",
                DetectionTime = System.DateTime.Now
            };

            Assert.True(result.IsOK);
            Assert.Equal("BATCH001", result.BatchNumber);
            Assert.Equal("20250530", result.ProductionDate);
            Assert.Equal("20260530", result.ExpirationDate);
            Assert.Equal(@"D:\Images\OK\test.png", result.ImagePath);
        }
    }
}
