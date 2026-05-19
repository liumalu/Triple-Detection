using Xunit;
using System.IO;
using System.Drawing;

namespace TripleDetection.App.Tests
{
    public class ImageStorageServiceTests
    {
        [Fact]
        public void SaveImage_SavesToOkDirectory_WhenIsOKTrue()
        {
            // Arrange
            var tempDir = Path.Combine(Path.GetTempPath(), "test_images_" + System.Guid.NewGuid().ToString());
            var okDir = Path.Combine(tempDir, "OK");
            var ngDir = Path.Combine(tempDir, "NG");
            var service = new ImageStorageService(okDir, ngDir);
            using var bitmap = new Bitmap(100, 100);

            // Act
            var path = service.SaveImage(bitmap, true);

            // Assert
            Assert.NotNull(path);
            Assert.StartsWith(okDir, path);
            Assert.True(File.Exists(path));

            // Cleanup
            Directory.Delete(tempDir, true);
        }

        [Fact]
        public void SaveImage_SavesToNgDirectory_WhenIsOKFalse()
        {
            // Arrange
            var tempDir = Path.Combine(Path.GetTempPath(), "test_images_" + System.Guid.NewGuid().ToString());
            var okDir = Path.Combine(tempDir, "OK");
            var ngDir = Path.Combine(tempDir, "NG");
            var service = new ImageStorageService(okDir, ngDir);
            using var bitmap = new Bitmap(100, 100);

            // Act
            var path = service.SaveImage(bitmap, false);

            // Assert
            Assert.NotNull(path);
            Assert.StartsWith(ngDir, path);
            Assert.True(File.Exists(path));

            // Cleanup
            Directory.Delete(tempDir, true);
        }
    }
}