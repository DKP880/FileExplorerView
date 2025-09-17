namespace FileExplorer.Models
{
    public class FileViewModel
    {
        public string Name { get; set; } = string.Empty;
        public long Size { get; set; }
        public DateTime LastModified { get; set; }
        public string FullPath { get; set; } = string.Empty;
        public string Provider { get; set; } = string.Empty;
        public string? Host { get; set; }
        public string? FtpUsername { get; set; }
        public string? FtpPassword { get; set; }
    }
}