﻿// <copyright file="FixedUpdateEventSystem.cs" company="BovineLabs">
// Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Samples.MultiWorld
{
    using BovineLabs.Event;
    using Unity.Entities;

    /// <summary>
    /// The FixedUpdateEventSystem.
    /// </summary>
    [DisableAutoCreation]
    public class FixedUpdateEventSystem : EventSystem
    {
        /// <inheritdoc/>
        protected override WorldMode Mode => WorldMode.DefaultWorldName;
    }
}
