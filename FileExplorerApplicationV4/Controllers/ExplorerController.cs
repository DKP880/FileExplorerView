using FileExplorer.Models;
using FileExplorer.Services;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Xml;
using FluentFTP;
using Renci.SshNet;
using System.Threading.Tasks;

namespace FileExplorer.Controllers
{
    public class ExplorerController : Controller
    {
        private readonly JsonDataService _dataService;
        private static readonly string[] AllowedExtensions = { ".xml", ".json", ".txt", ".pdf", ".jpeg", ".jpg", ".png" };

        public ExplorerController(JsonDataService dataService)
        {
            _dataService = dataService;
        }

         private string FormatSize(long bytes)
        {
            string[] suf = { "B", "KB", "MB", "GB", "TB" };
            int i;
            double dblSByte = bytes;
            for (i = 0; i < suf.Length && bytes >= 1024; i++, bytes /= 1024)
            {
                dblSByte = bytes / 1024.0;
            }

            return String.Format("{0:0.##} {1}", dblSByte, suf[i]);
        }

        public IActionResult Index()
        {
            var username = HttpContext.Session.GetString("Username");
            if (string.IsNullOrEmpty(username))
            {
                return RedirectToAction("Login", "Account");
            }
            var user = _dataService.GetUserByUsername(username);
            return View(user);
        }

        [HttpGet]
        public IActionResult GetLocalFiles(string path)
        {
            var files = new List<FileViewModel>();
            try
            {
                if (Directory.Exists(path))
                {
                    var directoryInfo = new DirectoryInfo(path);
                    var fileInfos = directoryInfo.GetFiles()
                        .Where(f => AllowedExtensions.Contains(f.Extension.ToLowerInvariant()));

                    foreach (var fileInfo in fileInfos)
                    {
                        files.Add(new FileViewModel
                        {
                            Name = fileInfo.Name,
                            Size = fileInfo.Length,
                            LastModified = fileInfo.LastWriteTime,
                            FullPath = fileInfo.FullName,
                            Provider = "Local"
                        });
                    }
                }
                else if (System.IO.File.Exists(path))
                {
                    var fileInfo = new FileInfo(path);
                    if (AllowedExtensions.Contains(fileInfo.Extension.ToLowerInvariant()))
                    {
                        files.Add(new FileViewModel
                        {
                            Name = fileInfo.Name,
                            Size = fileInfo.Length,
                            LastModified = fileInfo.LastWriteTime,
                            FullPath = fileInfo.FullName,
                            Provider = "Local"
                        });
                    }
                }
                else
                {
                    return BadRequest($"Path not found: {path}");
                }
            }
            catch (Exception ex)
            {
                return BadRequest($"Error accessing local path: {ex.Message}");
            }
            return PartialView("_FileListPartial", files);
        }

        [HttpPost]
        public async Task<IActionResult> AddVirtualFile(string filePath)
        {
            var username = HttpContext.Session.GetString("Username");
            if (string.IsNullOrEmpty(username))
            {
                return Unauthorized("You must be logged in.");
            }

            if (string.IsNullOrWhiteSpace(filePath))
            {
                return BadRequest("File path cannot be empty.");
            }

            try
            {
                await _dataService.AddVirtualFileForUserAsync(username, filePath);
                return Ok("File path added successfully.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred while saving the file path: {ex.Message}");
            }
        }

        [HttpPost]
        public IActionResult GetFtpFiles(string host, string username, string password, string remoteDir)
        {
            var files = new List<FileViewModel>();
            try
            {
                using (var client = new FtpClient(host, username, password))
                {
                    client.Connect();
                    foreach (var item in client.GetListing(remoteDir))
                    {
                        if (item.Type == FtpObjectType.File && AllowedExtensions.Contains(Path.GetExtension(item.Name).ToLowerInvariant()))
                        {
                            files.Add(new FileViewModel
                            {
                                Name = item.Name,
                                Size = item.Size,
                                LastModified = item.Modified,
                                FullPath = item.FullName,
                                Provider = "FTP",
                                Host = host,
                                FtpUsername = username,
                                FtpPassword = password
                            });
                        }
                    }
                }
            }
            catch (Exception ex)    
            {
                return BadRequest($"FTP Error: {ex.Message}");
            }
            return PartialView("_FileListPartial", files);
        }

        [HttpPost]
        public IActionResult GetSftpFiles(string host, string username, string password, string remoteDir)
        {
            var files = new List<FileViewModel>();
            try
            {
                using (var client = new SftpClient(host, username, password))
                {
                    client.Connect();
                    var fileList = client.ListDirectory(remoteDir);
                    foreach (var file in fileList)
                    {
                        if (!file.IsDirectory && AllowedExtensions.Contains(Path.GetExtension(file.Name).ToLowerInvariant()))
                        {
                            files.Add(new FileViewModel
                            {
                                Name = file.Name,
                                Size = file.Length,
                                LastModified = file.LastWriteTime,
                                FullPath = file.FullName,
                                Provider = "SFTP",
                                Host = host,
                                FtpUsername = username,
                                FtpPassword = password
                            });
                        }
                    }
                    client.Disconnect();
                }
            }
            catch (Exception ex)
            {
                return BadRequest($"SFTP Error: {ex.Message}");
            }
            return PartialView("_FileListPartial", files);
        }

        [HttpPost]
        public async Task<IActionResult> ViewFile(string provider, string fullPath, string? host, string? username, string? password)
        {
            try
            {
                byte[] fileBytes;

                // Step 1: Get the raw file bytes from the correct source (Local, FTP, or SFTP)
                switch (provider)
                {
                    case "Local":
                        fileBytes = await System.IO.File.ReadAllBytesAsync(fullPath);
                        break;
                    case "FTP":
                        using (var ftpClient = new AsyncFtpClient(host, username, password))
                        {
                            await ftpClient.Connect();
                            fileBytes = await ftpClient.DownloadBytes(fullPath, default);
                        }
                        break;
                    case "SFTP":
                        using (var sftpClient = new SftpClient(host, username, password))
                        {
                            await sftpClient.ConnectAsync(default);
                            using (var ms = new MemoryStream())
                            {
                                await Task.Run(() => sftpClient.DownloadFile(fullPath, ms));
                                fileBytes = ms.ToArray();
                            }
                            sftpClient.Disconnect();
                        }
                        break;
                    default:
                        return BadRequest("Unsupported provider");
                }

                // Step 2: Now that we have the bytes, check the extension and return the correct type
                string extension = Path.GetExtension(fullPath).ToLowerInvariant();

                if (extension == ".pdf" || extension == ".jpeg" || extension == ".jpg" || extension == ".png")
                {
              
                    string mimeType = extension switch
                    {
                        ".pdf" => "application/pdf",
                        ".jpeg" => "image/jpeg",
                        ".jpg" => "image/jpeg",
                        ".png" => "image/png",
                        _ => "application/octet-stream"
                    };
                    return File(fileBytes, mimeType);
                }
                else 
                {
                    string content = Encoding.UTF8.GetString(fileBytes);
                    return Content(content, "text/plain; charset=utf-8");
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error reading file: {ex.Message}");
            }
        }
        [HttpPost]
        public IActionResult GetXPathFromSelection(string xmlContent, string selectedText)
        {
            try
            {
                if (string.IsNullOrEmpty(xmlContent)) return BadRequest("XML content is missing.");

                var doc = new XmlDocument();
                doc.LoadXml(xmlContent);

                // Case 1: The user has selected specific text
                if (!string.IsNullOrWhiteSpace(selectedText))
                {
                    var cleanSelectedText = selectedText.Trim();
                    XmlNode bestMatch = null;

                    
                    foreach (XmlNode node in doc.SelectNodes("//*"))
                    {
                        if (node.OuterXml.Contains(cleanSelectedText))
                        {
                            if (bestMatch == null || node.OuterXml.Length < bestMatch.OuterXml.Length)
                            {
                                bestMatch = node;
                            }
                        }
                    }

                    if (bestMatch != null)
                    {
                        string path = GetElementXPath(bestMatch);
                        return Content(path);
                    }

                    return Content("XPath could not be determined for the selection.");
                }
                // Case 2: Nothing is selected, so we return all paths
                else
                {
                    var allPaths = new List<string>();
                    if (doc.DocumentElement != null)
                    {
                        GetAllXPaths(doc.DocumentElement, allPaths);
                    }
                    return Content(string.Join("\n", allPaths.Distinct().OrderBy(p => p.Length).ThenBy(p => p)));
                }
            }
            catch (XmlException ex)
            {
                return BadRequest("Invalid XML format: " + ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occurred while calculating XPath: " + ex.Message);
            }
        }

        
        private string GetElementXPath(XmlNode element)
        {
            var path = new Stack<string>();
            while (element != null && element.NodeType == XmlNodeType.Element)
            {
                // Using LocalName ignores namespaces (e.g., gets "singer" from "foo:singer")
                path.Push(element.LocalName);
                element = element.ParentNode;
            }
            return "/" + string.Join("/", path);
        }

        private void GetAllXPaths(XmlNode node, List<string> paths)
        {
            if (node.NodeType == XmlNodeType.Element)
            {
                paths.Add(GetElementXPath(node));
            }

            foreach (XmlNode child in node.ChildNodes)
            {
                if (child.NodeType == XmlNodeType.Element)
                {
                    GetAllXPaths(child, paths);
                }
            }
        }
    }
}