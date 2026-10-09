using OwlCore.Storage.CommonTests;
using OwlCore.Storage.System.IO;
using System.Runtime.InteropServices;

namespace OwlCore.Storage.Tests.SystemIO;

[TestClass]
public class SystemFileTests : CommonIFileTests
{
    // Required for base class to perform common tests.
    public override async Task<IFile> CreateFileAsync()
    {
        var filePath = await GenerateRandomFile(256_000);
        return new SystemFile(filePath);
    }

    public override async Task<IFile?> CreateFileWithCreatedAtAsync(DateTime createdAt)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return null;

        var filePath = await GenerateRandomFile(256_000);
        File.SetCreationTimeUtc(filePath, createdAt);
        return new SystemFile(filePath);
    }

    public override async Task<IFile?> CreateFileWithLastModifiedAtAsync(DateTime lastModifiedAt)
    {
        var filePath = await GenerateRandomFile(256_000);
        File.SetLastWriteTimeUtc(filePath, lastModifiedAt);
        return new SystemFile(filePath);
    }

    public override async Task<IFile?> CreateFileWithLastAccessedAtAsync(DateTime lastAccessedAt)
    {
        var filePath = await GenerateRandomFile(256_000);
        File.SetLastAccessTimeUtc(filePath, lastAccessedAt);
        return new SystemFile(filePath);
    }

    private static async Task<string> GenerateRandomFile(int fileSize)
    {
        // Create
        var tempFilePath = Path.GetTempFileName();
        await using var tempFileStr = File.Create(tempFilePath);

        // Write
        tempFileStr.Position = 0;
        await tempFileStr.WriteAsync(GenerateRandomData(fileSize), 0, fileSize);

        return tempFilePath;
    }

    private static byte[] GenerateRandomData(int length)
    {
        var rand = new Random();
        var b = new byte[length];
        rand.NextBytes(b);

        return b;
    }
    
    [TestMethod]
    public async Task EnsureExistingFilesAreTruncatedOnOverwrite()
    {
        const byte originalByte = 0x0A;
        const byte newByte = 0x0B;
        const int originalSize = 1024;
        const int expectedNewSize = 512;
        
        var tempFilePath = Path.GetTempFileName();
        await using (var tempFileStr = File.Create(tempFilePath))
        {
            // Write initial pattern
            tempFileStr.Position = 0;
            var dataA = new byte[originalSize];
            Array.Fill(dataA, originalByte);
            await tempFileStr.WriteAsync(dataA, TestContext.CancellationToken);
        }

        SystemFile file = new(tempFilePath);

        await using (var streamA = await file.OpenWriteAsync(TestContext.CancellationToken))
        {
            // Write new, shorter pattern
            var dataB = new byte[expectedNewSize];
            Array.Fill(dataB, newByte);
            await streamA.WriteAsync(dataB, TestContext.CancellationToken);
        }
        
        await using (var streamB = await file.OpenReadAsync(TestContext.CancellationToken))
        {
            var dataC = new byte[originalSize];
            var newSize = await streamB.ReadAsync(dataC, TestContext.CancellationToken);
            Assert.AreEqual(expectedNewSize, newSize);
            
            foreach (var b in dataC) Assert.AreEqual(newByte, b);
        }
    }

    public TestContext TestContext { get; set; }
}
