using Xunit;
using TripleDetection.App.Models;

namespace TripleDetection.App.Tests
{
    public class DetectionResultTests
    {
        [Fact]
        public void DetectionResult_DefaultValues_AreCorrect()
        {
            var result = new DetectionResult();

            Assert.False(result.IsOK);
            Assert.Equal(string.Empty, result.CodeInfo);
            Assert.Equal(0, result.CharCount);
            Assert.Equal(0.0, result.Confidence);
            Assert.Null(result.ImagePath);
        }

        [Fact]
        public void DetectionResult_CanSetProperties()
        {
            var result = new DetectionResult
            {
                IsOK = true,
                CodeInfo = "ABC123",
                CharCount = 6,
                Confidence = 0.95,
                ImagePath = @"D:\Images\OK\test.png",
                DetectionTime = System.DateTime.Now
            };

            Assert.True(result.IsOK);
            Assert.Equal("ABC123", result.CodeInfo);
            Assert.Equal(6, result.CharCount);
            Assert.Equal(0.95, result.Confidence);
            Assert.Equal(@"D:\Images\OK\test.png", result.ImagePath);
        }
    }
}