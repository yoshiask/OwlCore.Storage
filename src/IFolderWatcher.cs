﻿using System;
using System.Collections.Specialized;

namespace OwlCore.Storage;

/// <summary>
/// A disposable object which can notify of changes to the folder.
/// </summary>
public interface IFolderWatcher : INotifyCollectionChanged, IDisposable, IAsyncDisposable
{
    /// <summary>
    /// The folder being watched for changes.
    /// </summary>
    public IMutableFolder Folder { get; }
}