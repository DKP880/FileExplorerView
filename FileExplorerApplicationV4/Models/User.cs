//using System.ComponentModel.DataAnnotations;

//namespace FileExplorerApplicationV4.Models
//{
//    public class LoginModel
//    {
//        [Required]
//        public string Username { get; set; } = string.Empty;

//        [Required]
//        [DataType(DataType.Password)]
//        public string Password { get; set; } = string.Empty;
//    }

//    public class User
//    {
//        public string Name { get; set; } = string.Empty;
//        public string Username { get; set; } = string.Empty;
//        public string Password { get; set; } = string.Empty;
//        public DateTime LastAccessed { get; set; }
//        public List<VirtualFolder> VirtualFolders { get; set; } = new();
//    }

//    public class VirtualFolder
//    {
//        public string Id { get; set; } = Guid.NewGuid().ToString();
//        public string Name { get; set; } = string.Empty;
//        public string Path { get; set; } = string.Empty;
//        public DateTime CreatedAt { get; set; } = DateTime.Now;
//    }

//    public class FileItem
//    {
//        public string Name { get; set; } = string.Empty;
//        public string Path { get; set; } = string.Empty;
//        public long Size { get; set; }
//        public DateTime LastModified { get; set; }
//        public string Extension { get; set; } = string.Empty;
//        public bool IsDirectory { get; set; }
//    }

//    public class FtpCredential
//    {
//        public string Id { get; set; } = Guid.NewGuid().ToString();
//        public string Username { get; set; } = string.Empty;
//        public string Name { get; set; } = string.Empty;
//        public string Host { get; set; } = string.Empty;
//        public int Port { get; set; } = 21;
//        public string FtpUsername { get; set; } = string.Empty;
//        public string FtpPassword { get; set; } = string.Empty;
//        public string ConnectionType { get; set; } = "FTP"; // FTP or SFTP
//    }

//    public class FileContent
//    {
//        public string Content { get; set; } = string.Empty;
//        public string FilePath { get; set; } = string.Empty;
//        public string FileName { get; set; } = string.Empty;
//        public string FileType { get; set; } = string.Empty;
//    }

//    public class AddFolderModel
//    {
//        [Required]
//        public string Name { get; set; } = string.Empty;

//        [Required]
//        public string Path { get; set; } = string.Empty;
//    }

//    public class FtpConnectionModel
//    {
//        [Required]
//        public string Name { get; set; } = string.Empty;

//        [Required]
//        public string Host { get; set; } = string.Empty;

//        public int Port { get; set; } = 21;

//        [Required]
//        public string Username { get; set; } = string.Empty;

//        [Required]
//        public string Password { get; set; } = string.Empty;

//        [Required]
//        public string ConnectionType { get; set; } = "FTP";
//    }

//    public class TagDetail
//    {
//        public string TagName { get; set; } = string.Empty;
//        public string XPath { get; set; } = string.Empty;
//        public string Content { get; set; } = string.Empty;
//        public Dictionary<string, string> Attributes { get; set; } = new();
//    }
//}
using System.Text.Json.Serialization;

namespace FileExplorer.Models
{
    public class User
    {
        [JsonPropertyName("Username")]
        public string Username { get; set; } = string.Empty;

        [JsonPropertyName("PasswordHash")]
        public string PasswordHash { get; set; } = string.Empty;

        [JsonPropertyName("Explorers")]
        public List<Explorer> Explorers { get; set; } = new List<Explorer>();
    }
}