#region Copyright & License

// Copyright © 2024 - 2025 Aprico Consultants
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
// http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

#endregion

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Aprico.Ddd.Persistence.EntityFrameworkCore.ChangeTracking.Extensions;

/// <summary>Provides extension methods for the <see cref="ChangeTracker"/> to facilitate working with tracked entities.</summary>
[SuppressMessage("ReSharper", "MemberCanBeInternal", Justification = "Public API.")]
public static class ChangeTrackerExtensions
{
	/// <summary>Retrieves an array of tracked entities that have queued domain events from the specified <see cref="ChangeTracker"/>.</summary>
	/// <param name="changeTracker">The <see cref="ChangeTracker"/> instance to inspect for tracked entities with domain events.</param>
	/// <returns>An array of <see cref="Entity"/> objects that have queued domain events.</returns>
	public static Entity[] GetTrackedEntitiesHavingDomainEvents(this ChangeTracker changeTracker)
	{
		ArgumentNullException.ThrowIfNull(changeTracker);
		return changeTracker.Entries()
			.Select(static c => c.Entity)
			.OfType<Entity>()
			.Where(static e => e.HasQueuedDomainEvents)
			.ToArray();
	}
}
