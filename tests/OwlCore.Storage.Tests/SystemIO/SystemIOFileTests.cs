using OwlCore.Storage.SystemIO;

namespace OwlCore.Storage.Tests.SystemIO
{
    [TestClass]
    public class IFileTests : CommonIFileTests
    {
        // Required for base class to perform common tests.
        public override async Task<IFile> CreateInstance_ValidParameters()
        {
            var filePath = await GenerateRandomFile(256_000);
            return new SystemFile(filePath);
            
            static async Task<string> GenerateRandomFile(int fileSize)
            {
                // Create
                var tempFilePath = Path.GetTempFileName();
                await using var tempFileStr = File.Create(tempFilePath);

                // Write
                tempFileStr.Position = 0;
                await tempFileStr.WriteAsync(GenerateRandomData(fileSize), 0, fileSize);

                return tempFilePath;
            }

            static byte[] GenerateRandomData(int length)
            {
                var rand = new Random();
                var b = new byte[length];
                rand.NextBytes(b);

                return b;
            }
        }

        // Required for base class to perform common tests.
        public override Task<IFile> CreateInstance_InvalidParameters()
        {
            return Task.FromResult<IFile>(new SystemFile(Guid.NewGuid().ToString()));
        }
    }
}