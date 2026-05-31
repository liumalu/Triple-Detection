using System;
using System.Drawing;
using System.IO;

namespace TripleDetection.App.Services.Detection
{
    public class ImageStorageService
    {
        private readonly string _okDir;
        private readonly string _ngDir;

        public ImageStorageService(string okDir, string ngDir)
        {
            _okDir = okDir;
            _ngDir = ngDir;
            CreateDirectories();
        }

        private void CreateDirectories()
        {
            if (!Directory.Exists(_okDir)) Directory.CreateDirectory(_okDir);
            if (!Directory.Exists(_ngDir)) Directory.CreateDirectory(_ngDir);
        }

        public void SaveImage(Bitmap image, bool isOK)
        {
            var dir = isOK ? _okDir : _ngDir;
            var filename = $"{DateTime.Now:yyyyMMdd_HHmmss_fff}_{(isOK ? "OK" : "NG")}.png";
            var path = Path.Combine(dir, filename);
            try
            {
                image.Save(path, System.Drawing.Imaging.ImageFormat.Png);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ImageStorageService: Failed to save image - {ex.Message}");
            }
        }
    }
}
