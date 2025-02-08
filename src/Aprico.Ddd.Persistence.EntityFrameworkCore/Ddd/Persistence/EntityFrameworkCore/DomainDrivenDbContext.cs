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
using Aprico.Linq.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Aprico.Ddd.Persistence.EntityFrameworkCore;

/// <summary>
/// Represents a Domain-Driven Design (DDD) specialized <see cref="DbContext"/> that integrates with a domain event
/// dispatcher for dispatching domain events to their respective handlers.
/// </summary>
/// <param name="options">
/// Configuration <see cref="DbContextOptions"/>> options for instantiating this
/// <see cref="DomainDrivenDbContext"/> instance.
/// </param>
/// <param name="domainEventDispatcher">The domain event dispatcher used to dispatch domain events raised by tracked entities.</param>
/// <seealso href="https://github.com/JonPSmith/EfCore.GenericEventRunner">EfCore.GenericEventRunner</seealso>
public class DomainDrivenDbContext(DbContextOptions options, IDomainEventDispatcher domainEventDispatcher) : DbContext(options)
{
	#region Base Class Member Overrides

	/// <summary>Saves all changes made in this context to the database.</summary>
	/// <param name="acceptAllChangesOnSuccess">
	/// Indicates whether <see cref="ChangeTracker.AcceptAllChanges"/> is called after the
	/// changes have been sent successfully to the database.
	/// </param>
	/// <returns>The number of state entries written to the database.</returns>
	/// <remarks>
	/// <para>
	/// This synchronous method internally invokes the asynchronous version of
	/// <see cref="SaveChangesAsync(bool, CancellationToken)"/> and blocks the calling thread until the operation completes.
	/// </para>
	/// <para>
	/// This method should typically be used in scenarios where asynchronous operations are not supported or required. Blocking
	/// the calling thread may impact performance and scalability, especially in high-concurrency environments.
	/// </para>
	/// </remarks>
	public override int SaveChanges(bool acceptAllChangesOnSuccess)
	{
		return SaveChangesAsync(acceptAllChangesOnSuccess, CancellationToken.None)
			.GetAwaiter()
			.GetResult();
	}

	/// <summary>
	/// Asynchronously saves all changes made in this context to the database. This method also dispatches any domain events
	/// raised by the entities being tracked in the context.
	/// </summary>
	/// <param name="acceptAllChangesOnSuccess">
	/// Indicates whether <see cref="ChangeTracker.AcceptAllChanges"/> is called after the
	/// changes have been sent successfully to the database.
	/// </param>
	/// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
	/// <returns>
	/// A task that represents the asynchronous save operation. The task result contains the number of state entries written
	/// to the database.
	/// </returns>
	/// <exception cref="InvalidOperationException">
	/// Thrown when the event dispatching process exceeds the maximum iteration count
	/// defined by <see cref="MaximumEventDispatchingIterationCount"/>. This may indicate potential circular dependencies in domain
	/// events that require attention.
	/// </exception>
	/// <remarks>
	/// This method employs an iterative approach to dispatching domain events raised by entities being tracked in the
	/// context. After each pass, it checks for any further domain events triggered by the handling of previous ones, repeating the
	/// process until no more events remain. To safeguard against infinite loops, which might occur due to circular dependencies
	/// between domain events, a maximum iteration count is enforced. If this limit is exceeded, an exception is thrown to indicate a
	/// possible issue in the event pipeline or insufficient iteration allowance.
	/// </remarks>
	public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
	{
		var iterationCount = 1;
		var trackedEntities = ChangeTracker.GetTrackedEntitiesHavingDomainEvents();
		while (trackedEntities.Length != 0)
		{
			if (iterationCount++ > MaximumEventDispatchingIterationCount) throw new InvalidOperationException(MAX_DISPATCHING_ITERATIONS_EXCEEDED_ERROR_MESSAGE);
			await trackedEntities.ForEachAsync(entity => domainEventDispatcher.DispatchEntityDomainEventsAsync(entity, cancellationToken))
				.ConfigureAwait(continueOnCapturedContext: false);
			trackedEntities = ChangeTracker.GetTrackedEntitiesHavingDomainEvents();
		}
		return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken)
			.ConfigureAwait(continueOnCapturedContext: false);
	}

	#endregion

	/// <summary>
	/// Gets or sets the maximum number of iterations allowed for dispatching domain events to prevent potential infinite
	/// loops caused by events raising additional events.
	/// </summary>
	[SuppressMessage("ReSharper", "PropertyCanBeMadeInitOnly.Global", Justification = "Public API.")]
	public int MaximumEventDispatchingIterationCount { get; set; } = 7;

	internal const string MAX_DISPATCHING_ITERATIONS_EXCEEDED_ERROR_MESSAGE = $"The domain event dispatching process exceeded the maximum allowed iteration count. "
		+ $"This may indicate circular dependencies in domain events that need resolution or that the {nameof(MaximumEventDispatchingIterationCount)} value should be increased.";
}
