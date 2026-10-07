using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace HQStudio.Services.BugReport
{
    /// <summary>
    /// Хранит токен GitHub, зашифрованный DPAPI (привязка к текущему пользователю Windows).
    /// </summary>
    public sealed class GitHubTokenStore
    {
        private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("HQStudio.GitHubToken.v1");

        private readonly string _path;

        public GitHubTokenStore(string? path = null)
        {
            _path = path ?? BugReportConfig.TokenPath;
        }

        public string FilePath => _path;

        public string? Load()
        {
            try
            {
                if (!File.Exists(_path)) return null;

                var protectedBytes = File.ReadAllBytes(_path);
                var plain = ProtectedData.Unprotect(protectedBytes, Entropy, DataProtectionScope.CurrentUser);
                var token = Encoding.UTF8.GetString(plain).Trim();
                return token.Length == 0 ? null : token;
            }
            catch (CryptographicException)
            {
                // Файл от другого пользователя/машины или повреждён: токен всё равно непригоден.
                Delete();
                return null;
            }
            catch (IOException)
            {
                return null;
            }
            catch (UnauthorizedAccessException)
            {
                return null;
            }
        }

        public void Save(string token)
        {
            var directory = Path.GetDirectoryName(_path);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            var protectedBytes = ProtectedData.Protect(
                Encoding.UTF8.GetBytes(token), Entropy, DataProtectionScope.CurrentUser);
            File.WriteAllBytes(_path, protectedBytes);
        }

        public void Delete()
        {
            try
            {
                if (File.Exists(_path)) File.Delete(_path);
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }
}
