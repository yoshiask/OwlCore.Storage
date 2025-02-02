﻿using System.Threading;
using System.Threading.Tasks;

namespace OwlCore.Storage;

/// <summary>
/// Represents a storable resource that resides within a traversable folder structure.
/// </summary>
public interface IStorableChild : IStorable
{
    /// <summary>
    /// Gets the containing folder for this item, if any.
    /// </summary>
    public Task<IFolder?> GetParentAsync(CancellationToken cancellationToken = default); 
}