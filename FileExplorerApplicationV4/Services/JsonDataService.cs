using FileExplorer.Models;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Threading; // 1. Add this using statement for SemaphoreSlim

namespace FileExplorer.Services
{
    public class JsonDataService
    {
        private List<User> _users;
        private readonly string _filePath;

        private static readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        public JsonDataService(IWebHostEnvironment env)
        {
            _filePath = Path.Combine(env.ContentRootPath, "Data", "users.json");

            var json = File.ReadAllText(_filePath);
            _users = JsonSerializer.Deserialize<List<User>>(json) ?? new List<User>();
        }

        public User? GetUserByUsername(string username)
        {
            return _users.FirstOrDefault(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
        }

        public async Task AddVirtualFileForUserAsync(string username, string filePath)
        {
            await _semaphore.WaitAsync();
            try
            {
                var jsonString = await File.ReadAllTextAsync(_filePath);
                var users = JsonSerializer.Deserialize<List<User>>(jsonString) ?? new List<User>();

                var userToUpdate = users.FirstOrDefault(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
                if (userToUpdate == null)
                {
                    throw new Exception("User not found.");
                }

                var localProvider = userToUpdate.Explorers.FirstOrDefault(e => e.Provider == "Local");
                if (localProvider == null)
                {
                    localProvider = new Explorer { Provider = "Local", RootPaths = new List<string>() };
                    userToUpdate.Explorers.Add(localProvider);
                }

                if (localProvider.RootPaths == null)
                {
                    localProvider.RootPaths = new List<string>();
                }

                if (!localProvider.RootPaths.Contains(filePath))
                {
                    localProvider.RootPaths.Add(filePath);

                    var options = new JsonSerializerOptions { WriteIndented = true };
                    var updatedJsonString = JsonSerializer.Serialize(users, options);

                    await File.WriteAllTextAsync(_filePath, updatedJsonString);

                    _users = users;
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}

