using System;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Diagnostics;

namespace TripleDetection.App.Services.Detection
{
    public class ImageStorageService
    {
        private string _okDir;
        private string _ngDir;

        public ImageStorageService(string okDir, string ngDir)
        {
            _okDir = CreateDirIfNotExists(okDir);
            _ngDir = CreateDirIfNotExists(ngDir);
        }

        private string CreateDirIfNotExists(string dir)
        {
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            return dir;
        }

        public string SaveImage(Bitmap image, bool isOK)
        {
            string dir = isOK ? _okDir : _ngDir;
            string filename = $"{DateTime.Now:yyyyMMdd_HHmmss_fff}.png";
            string fullPath = Path.Combine(dir, filename);

            try
            {
                image.Save(fullPath, ImageFormat.Png);
                return fullPath;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to save image: {ex.Message}");
                return null;
            }
        }
    }
}