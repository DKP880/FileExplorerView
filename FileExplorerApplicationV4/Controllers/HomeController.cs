//using FileExplorerApplicationV4.Models;
//using FileExplorerApplicationV4.Services;
//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Mvc;
//using Newtonsoft.Json;

//namespace FileExplorerMVC.Controllers
//{
//    [Authorize]
//    public class HomeController : Controller
//    {
//        private readonly IAuthService _authService;
//        private readonly IFileService _fileService;

//        public HomeController(IAuthService authService, IFileService fileService)
//        {
//            _authService = authService;
//            _fileService = fileService;
//        }

//        public async Task<IActionResult> Home()
//        {
//            var username = User.Identity?.Name ?? "";

//            // Get virtual folders from AuthService
//            var virtualFolders = await _authService.GetUserVirtualFoldersAsync(username);

//            // Load hardcoded JSON file
//            var jsonPath = Path.Combine(Directory.GetCurrentDirectory(), "Data", "USER_CREDENTIALS.json");
//            var json = await System.IO.File.ReadAllTextAsync(jsonPath);
//            var allUsers = JsonConvert.DeserializeObject<UserJsonModel>(json);

//            // Find current user's credentials
//            var user = allUsers?.Users.FirstOrDefault(u => u.Username == username);
//            var ftpCredentials = user?.FtpCredentials ?? new List<FtpCredential>();

//            // Pass data to view
//            ViewBag.VirtualFolders = virtualFolders;
//            ViewBag.FtpCredentials = ftpCredentials;

//            return View();
//        }

//        [HttpPost]
//        public async Task<IActionResult> AddFolder(AddFolderModel model)
//        {
//            if (!ModelState.IsValid)
//                return Json(new { success = false, message = "Invalid input data." });

//            if (!Directory.Exists(model.Path))
//                return Json(new { success = false, message = "Directory path does not exist." });

//            var username = User.Identity?.Name ?? "";
//            var folder = new VirtualFolder
//            {
//                Name = model.Name,
//                Path = model.Path
//            };

//            await ((AuthService)_authService).AddVirtualFolderAsync(username, folder);

//            return Json(new { success = true, message = "Folder added successfully." });
//        }

//        [HttpGet]
//        public async Task<IActionResult> GetFiles(string folderId, string path = "")
//        {
//            var username = User.Identity?.Name ?? "";
//            var virtualFolders = await _authService.GetUserVirtualFoldersAsync(username);
//            var folder = virtualFolders.FirstOrDefault(f => f.Id == folderId);

//            if (folder == null)
//                return Json(new { success = false, message = "Folder not found." });

//            var targetPath = string.IsNullOrEmpty(path) ? folder.Path : Path.Combine(folder.Path, path);
//            var files = await _fileService.GetFilesAsync(targetPath);

//            var result = files.Select(f => new
//            {
//                f.Name,
//                f.Path,
//                f.IsDirectory,
//                f.Extension,
//                LastModified = f.LastModified.ToString("MM/dd/yyyy, h:mm:ss tt"),
//                Size = f.IsDirectory ? "" : _fileService.FormatFileSize(f.Size)
//            });

//            return Json(new { success = true, files = result });
//        }

//        [HttpGet]
//        public async Task<IActionResult> ViewFile(string filePath, string source = "local")
//        {
//            FileContent? fileContent = null;

//            if (source == "local")
//            {
//                fileContent = await _fileService.GetFileContentAsync(filePath);
//            }

//            if (fileContent == null)
//                return Json(new { success = false, message = "Unable to load file content." });

//            return Json(new
//            {
//                success = true,
//                content = fileContent.Content,
//                fileName = fileContent.FileName,
//                fileType = fileContent.FileType,
//                filePath = fileContent.FilePath
//            });
//        }

//        [HttpGet]
//        public async Task<IActionResult> GetXmlTagDetails(string filePath, int lineNumber)
//        {
//            try
//            {
//                var tagDetail = await _fileService.GetXmlTagDetailsAsync(filePath, lineNumber);

//                if (tagDetail == null)
//                    return Json(new { success = false, message = "Unable to get tag details." });

//                return Json(new
//                {
//                    success = true,
//                    tagName = tagDetail.TagName,
//                    xpath = tagDetail.XPath,
//                    content = tagDetail.Content,
//                    attributes = tagDetail.Attributes
//                });
//            }
//            catch
//            {
//                return Json(new { success = false, message = "Error processing XML file." });
//            }
//        }

//        [HttpPost]
//        public IActionResult CopyToClipboard([FromBody] CopyRequest request)
//        {
//            return Json(new { success = true, text = request.Text });
//        }
//    }

//    // Helper classes for JSON deserialization
//    public class UserJsonModel
//    {
//        public List<UserItem> Users { get; set; } = new();
//    }

//    public class UserItem
//    {
//        public string Username { get; set; } = "";
//        public string Password { get; set; } = "";
//        public List<VirtualFolder> VirtualFolders { get; set; } = new();
//        public List<FtpCredential> FtpCredentials { get; set; } = new();
//    }

//    public class CopyRequest
//    {
//        public string Text { get; set; } = string.Empty;
//    }
//}
