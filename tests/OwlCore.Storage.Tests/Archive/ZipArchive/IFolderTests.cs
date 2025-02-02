﻿using OwlCore.Storage.CommonTests;
using OwlCore.Storage.Archive;
using System.IO.Compression;
using OwlCore.Storage.SystemIO;

namespace OwlCore.Storage.Tests.Archive.ZipArchive;

[TestClass]
public class IFolderTests : CommonIModifiableFolderTests
{
    // Required for base class to perform common tests.
    public override async Task<IModifiableFolder> CreateModifiableFolderAsync()
    {
        var sourceFile = new SystemFile(CreateEmptyArchiveOnDisk());
        return new ZipArchiveFolder(sourceFile);
    }

    public override async Task<IModifiableFolder> CreateModifiableFolderWithItems(int fileCount, int folderCount)
    {
        var folder = await CreateModifiableFolderAsync();

        for (int i = 0; i < fileCount; i++)
        {
            await folder.CreateFileAsync($"{Guid.NewGuid()}");
        }

        for (int i = 0; i < folderCount; i++)
        {
            await folder.CreateFolderAsync($"{Guid.NewGuid()}");
        }

        return folder;
    }

    private static string CreateEmptyArchiveOnDisk()
    {
        // Create new archive on disk
        string archiveId = $"archiveTest_{Guid.NewGuid()}";
        var tempArchivePath = Path.Combine(Path.GetTempPath(), archiveId);

        var dir = Directory.CreateDirectory(tempArchivePath);
        ZipFile.CreateFromDirectory(tempArchivePath, tempArchivePath += ".zip");

        return tempArchivePath;
    }
}
