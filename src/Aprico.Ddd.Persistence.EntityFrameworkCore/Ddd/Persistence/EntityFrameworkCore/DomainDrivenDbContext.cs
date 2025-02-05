#region region Copyright & License

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
using System.Threading;
using System.Threading.Tasks;
using Aprico.Ddd.Abstractions;
using Aprico.Ddd.Persistence.EntityFrameworkCore.ChangeTracking.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Aprico.Ddd.Persistence.EntityFrameworkCore;

/// <seealso href="https://github.com/JonPSmith/EfCore.GenericEventRunner">EfCore.GenericEventRunner</seealso>
public class DomainDrivenDbContext : DbContext
{
	public DomainDrivenDbContext(DbContextOptions options, IDomainEventDispatcher domainEventDispatcher) : base(options)
	{
		ArgumentNullException.ThrowIfNull(domainEventDispatcher);
		DomainEventDispatcher = domainEventDispatcher;
		MaximumEventDispatchingIterationCount = 7;
	}

	#region Base Class Member Overrides

	public override int SaveChanges(bool acceptAllChangesOnSuccess)
	{
		return SaveChangesAsync(acceptAllChangesOnSuccess, CancellationToken.None)
			.GetAwaiter()
			.GetResult();
	}

	[SuppressMessage("ReSharper", "LoopCanBePartlyConvertedToQuery")]
	public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
	{
		var iterationCount = 1;
		var trackedEntities = ChangeTracker.GetTrackedEntitiesHavingDomainEvents();
		while (trackedEntities.Length != 0)
		{
			// @formatter:max_line_length 300
			if (iterationCount++ > MaximumEventDispatchingIterationCount)
				throw new InvalidOperationException($"The domain event dispatching loop exceeded its maximum allowed iteration count. This might indicate circular dependencies among the domain events that need to be fixed or that you need to increase {nameof(MaximumEventDispatchingIterationCount)}.");
			// @formatter:max_line_length restore
			foreach (var entity in trackedEntities)
				// TODO rename DispatchAsync to DispatchEntityDomainEventsAsync
				await DomainEventDispatcher.DispatchAsync(entity, cancellationToken)
					.ConfigureAwait(continueOnCapturedContext: false);
			trackedEntities = ChangeTracker.GetTrackedEntitiesHavingDomainEvents();
		}
		return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken)
			.ConfigureAwait(continueOnCapturedContext: false);
	}

	#endregion

	public IDomainEventDispatcher DomainEventDispatcher { get; }

	public int MaximumEventDispatchingIterationCount { get; set; }
}
