﻿using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

#pragma warning disable CS1998

namespace OwlCore.Storage.SystemIO.Compression;

/// <summary>
/// A folder implementation wrapping a <see cref="ZipArchive"/>.
/// </summary>
public class ZipArchiveFolder : IAddressableFolder, IModifiableFolder, IFolderCanFastGetItem
{
    /// <summary>
    /// The directory separator as defined by the ZIP standard.
    /// This is constant no matter the operating system (see 4.4.17.1).
    /// https://pkware.cachefly.net/webdocs/casestudies/APPNOTE.TXT
    /// </summary>
    const char ZIP_DIRECTORY_SEPARATOR = '/';

    private readonly ZipArchive _archive;
    private readonly Dictionary<string, ZipArchiveFolder> _virtualFolders = new();

    /// <summary>
    /// Creates a new instance of <see cref="ZipArchiveFolder"/>.
    /// </summary>
    /// <param name="id">A unique and consistent identifier for this file or folder.</param>
    /// <param name="name">The name of the file or folder, with the extension (if any).</param>
    /// <param name="archive">An existing ZIP archive which is provided as contents of the folder.</param>
    /// <param name="path">The relative path inside the ZIP archive. Leave empty for the root folder.</param>
    public ZipArchiveFolder(string id, string name, ZipArchive archive, string path = "")
    {
        Id = id;
        Name = name;
        Path = NormalizeEnding(path);

        _archive = archive;
    }

    /// <inheritdoc/>
    public string Id { get; }

    /// <inheritdoc/>
    public string Name { get; }

    /// <inheritdoc/>
    public string Path { get; }

    /// <inheritdoc/>
    public async Task<IAddressableFile> CreateCopyOfAsync(IFile fileToCopy, bool overwrite = false, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var srcStream = await fileToCopy.OpenStreamAsync(cancellationToken: cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();

        if (srcStream.CanSeek)
            srcStream.Seek(0, SeekOrigin.Begin);
        else if (srcStream.Position == 0)
            throw new InvalidOperationException("The opened file stream is not at position 0 and cannot be seeked. Unable to copy.");

        var existingEntry = _archive.GetEntry(fileToCopy.Id);
        if (!overwrite && existingEntry is not null)
            return new ZipArchiveEntryFile(existingEntry, this);

        var copy = await CreateFileAsync(fileToCopy.Name, overwrite, cancellationToken);
        using var dstStream = await copy.OpenStreamAsync(FileAccess.Write, cancellationToken);
        await srcStream.CopyToAsync(dstStream, 81920, cancellationToken);
        srcStream.Position = 0;

        return copy;
    }

    /// <inheritdoc/>
    public Task<IAddressableFile> CreateFileAsync(string name, bool overwrite = false, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        string realSubPath = Path + name;

        ZipArchiveEntry? entry = _archive.GetEntry(realSubPath);

        if (overwrite && entry is not null)
        {
            entry.Delete();
            entry = null;
        }

        entry ??= _archive.CreateEntry(realSubPath);

        return Task.FromResult<IAddressableFile>(new ZipArchiveEntryFile(entry, this));
    }

    /// <inheritdoc/>
    public async Task<IAddressableFolder> CreateFolderAsync(string name, bool overwrite = false, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        string subPath = NormalizeEnding(Path + name);
        bool exists = _virtualFolders.TryGetValue(subPath, out ZipArchiveFolder? folder);

        if (overwrite && exists)
        {
            await DeleteAsync(folder, cancellationToken);
            folder = null;
        }

        if (folder is null)
        {
            folder = new ZipArchiveFolder(name, name, _archive, subPath);
            _virtualFolders[folder.Path] = folder;
        }

        return folder;
    }

    /// <inheritdoc/>
    public Task DeleteAsync(IAddressableStorable item, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (item is ZipArchiveFolder folder)
        {
            // NOTE: Should this be recursive, or should it
            // throw if the virtual folder isn't empty?

            if (_archive.Entries.Any(e => e.FullName.StartsWith(folder.Path)))
                throw new IOException("The directory specified by path is not empty.");

            _virtualFolders.Remove(folder.Id);
        }
        else
        {
            var entry = _archive.GetEntry(item.Path);
            entry?.Delete();
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<IFolderWatcher> GetFolderWatcherAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult<IFolderWatcher>(new ZipArchiveFolderWatcher(this));
    }

    /// <inheritdoc/>
    public Task<IAddressableStorable> GetItemAsync(string id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        IAddressableStorable item;

        string itemPath = Path + id;

        var entry = _archive.GetEntry(itemPath);
        if (entry is not null)
        {
            item = new ZipArchiveEntryFile(entry, this);
        }
        else
        {
            itemPath = NormalizeEnding(itemPath);
            if (_virtualFolders.TryGetValue(itemPath, out var existingFolder))
            item = existingFolder;
        else
                item = new ZipArchiveFolder(id, id, _archive, itemPath);
        }

        return Task.FromResult(item);
    }

    /// <inheritdoc/>
    public async IAsyncEnumerable<IAddressableStorable> GetItemsAsync(StorableType type = StorableType.All, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (type == StorableType.None)
            throw new ArgumentOutOfRangeException(nameof(type), $"{nameof(StorableType)}.{type} is not valid here.");

        if (type.HasFlag(StorableType.File))
        {
            foreach (var entry in _archive.Entries.Where(e => e.FullName.StartsWith(Path)))
            {
                cancellationToken.ThrowIfCancellationRequested();

                yield return new ZipArchiveEntryFile(entry, this);
            }
        }

        if (type.HasFlag(StorableType.Folder))
        {
            foreach (var virtualFolder in _virtualFolders.Values.Where(e => e.Path.StartsWith(Path)))
            {
                cancellationToken.ThrowIfCancellationRequested();

                yield return virtualFolder;
            }
        }
    }

    /// <inheritdoc/>
    public Task<IFolder?> GetParentAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult<IFolder?>(null);
    }

    /// <inheritdoc/>
    public async Task<IAddressableFile> MoveFromAsync(IAddressableFile fileToMove, IModifiableFolder source, bool overwrite = false, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Zip archives can't move files around, so we have to take
        // the slower manual path every time.
        var file = await CreateCopyOfAsync(fileToMove, overwrite, cancellationToken);
        await source.DeleteAsync(fileToMove, cancellationToken);

        return file;
    }

    private static string NormalizeEnding(string path)
    {
        return path.Length == 0 || path[path.Length - 1] == ZIP_DIRECTORY_SEPARATOR
            ? path
            : path + ZIP_DIRECTORY_SEPARATOR;
    }
}
