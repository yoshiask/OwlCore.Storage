﻿using OwlCore.Storage.CommonTests;
using OwlCore.Storage.Archive;
using System.IO.Compression;

namespace OwlCore.Storage.Tests.Archive.Zip;

[TestClass]
public class IFileTests : CommonIFileTests
{
    // Required for base class to perform common tests.
    public override Task<IFile> CreateFileAsync()
    {
        // Create new archive on disk
        string entryId = Guid.NewGuid().ToString();
        string archiveId = $"archiveTest_{Guid.NewGuid()}";
        var tempArchivePath = Path.Combine(Path.GetTempPath(), archiveId);

        var dir = Directory.CreateDirectory(tempArchivePath);
        using (var entryStream = File.Create(Path.Combine(tempArchivePath, entryId)))
        {
            var randomData = GenerateRandomData(256_000);
            entryStream.Write(randomData);
        }
        ZipFile.CreateFromDirectory(tempArchivePath, tempArchivePath + ".zip");

        ZipArchive archive = ZipFile.Open(tempArchivePath + ".zip", ZipArchiveMode.Update);
        var entry = archive.GetEntry(entryId);
        Assert.IsNotNull(entry);

        var file = new ZipEntryFile(entry, new ZipFolder(archive, new(archiveId, $"{Guid.NewGuid()}", true)));

        return Task.FromResult<IFile>(file);

        static byte[] GenerateRandomData(int length)
        {
            var rand = new Random();
            var b = new byte[length];
            rand.NextBytes(b);

            return b;
        }
    }
}