using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using TransactionDispatch.Application.Options;
using TransactionDispatch.Infrastructure.IO;
using Xunit;

namespace TransactionDispatch.Infrastructure.Tests.IO
{
    public class LocalFileProviderTests : IDisposable
    {
        private readonly string _tempDir;

        public LocalFileProviderTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "td-tests-" + Guid.NewGuid());
            Directory.CreateDirectory(_tempDir);
        }

        private static LocalFileProvider CreateProvider(string[]? types = null, long maxBytes = 200 * 1024)
        {
            var fileTypes = Options.Create(new AllowedFileTypesOptions { AllowedFileTypes = types ?? new[] { "xml" } });
            var settings = Options.Create(new FileProviderOptions { MaxFileSizeBytes = maxBytes });
            return new LocalFileProvider(fileTypes, settings);
        }

        [Fact]
        public async Task EnumerateFilesAsync_Returns_OnlyAllowedFiles()
        {
            // Arrange
            var allowed = CreateFile("a.xml");
            var notAllowed = CreateFile("b.txt");
            var provider = CreateProvider(new[] { "xml" });

            // Act
            var results = new List<string>();
            await foreach (var f in provider.EnumerateFilesAsync(_tempDir))
                results.Add(Path.GetFileName(f));

            // Assert
            Assert.Single(results);
            Assert.Contains(Path.GetFileName(allowed), results);
            Assert.DoesNotContain(Path.GetFileName(notAllowed), results);
        }

        [Fact]
        public async Task OpenReadAsync_ReturnsReadableStream()
        {
            var file = CreateFile("a.xml", "hello");
            var provider = CreateProvider();

            await using var stream = await provider.OpenReadAsync(file);
            Assert.True(stream.CanRead);
        }

        [Fact]
        public async Task DeleteAsync_RemovesFile()
        {
            var file = CreateFile("delete.xml");
            var provider = CreateProvider();

            await provider.DeleteAsync(file);

            Assert.False(File.Exists(file));
        }

        [Fact]
        public async Task IsFileAvailableAsync_ReturnsFalse_WhenLocked()
        {
            var file = CreateFile("lock.xml", "data");
            var provider = CreateProvider();

            using var fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.None);
            var result = await provider.IsFileAvailableAsync(file);

            Assert.False(result);
        }

        private string CreateFile(string name, string content = "x")
        {
            var path = Path.Combine(_tempDir, name);
            File.WriteAllText(path, content);
            return path;
        }

        public void Dispose()
        {
            try { Directory.Delete(_tempDir, true); } catch {  }
        }
    }
}
