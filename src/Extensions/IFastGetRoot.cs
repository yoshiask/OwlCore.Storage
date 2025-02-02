﻿using System.Threading.Tasks;

namespace OwlCore.Storage;

/// <summary>
/// Provides a fast-path for the <see cref="StorableChildExtensions.GetRootAsync(IStorableChild)"/> extension method.
/// </summary>
public interface IFastGetRoot : IStorableChild
{
    /// <summary>
    /// Retrieves the root of this storable item. If this item IS the root, null will be returned instead.
    /// </summary>
    /// <returns>The root of this storable item</returns>
    public Task<IFolder?> GetRootAsync();
}
