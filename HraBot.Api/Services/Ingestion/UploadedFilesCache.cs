using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace HraBot.Api.Services.Ingestion
{
    public class UploadedFilesCache
    {
        private readonly string _cacheFilePath;
        private readonly HashSet<string> _uploadedFiles;

        public UploadedFilesCache(string cacheFilePath)
        {
            _cacheFilePath = cacheFilePath;
            _uploadedFiles = new HashSet<string>();
            LoadCache();
        }

        private void LoadCache()
        {
            if (File.Exists(_cacheFilePath))
            {
                var lines = File.ReadAllLines(_cacheFilePath);
                foreach (var line in lines)
                {
                    if (!string.IsNullOrWhiteSpace(line))
                        _uploadedFiles.Add(line.Trim());
                }
            }
        }

        public bool IsUploaded(string fileName)
        {
            return _uploadedFiles.Contains(fileName);
        }

        public void AddFile(string fileName)
        {
            if (_uploadedFiles.Add(fileName))
            {
                File.AppendAllLines(_cacheFilePath, new[] { fileName });
            }
        }

        public IEnumerable<string> GetAllUploadedFiles()
        {
            return _uploadedFiles.ToList();
        }
    }
}
