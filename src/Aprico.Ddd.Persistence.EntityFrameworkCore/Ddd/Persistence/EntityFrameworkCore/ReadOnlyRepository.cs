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
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Aprico.Ddd.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Aprico.Ddd.Persistence.EntityFrameworkCore;

/// <summary>
/// Provides a read-only repository implementation for accessing entities in a data context, enabling querying operations
/// while restricting modification capabilities.
/// </summary>
/// <typeparam name="TEntity">
/// The type of the entity to be managed by the repository. Must inherit from
/// <see cref="AggregateRoot{TKey}"/>.
/// </typeparam>
/// <typeparam name="TKey">The type of the primary key used by the entity. Must be a value type.</typeparam>
public class ReadOnlyRepository<TEntity, TKey> : IQueryableReadOnlyRepository<TEntity, TKey>
	where TEntity : AggregateRoot<TKey>
	where TKey : struct
{
	/// <summary>
	/// Initializes a new instance of the <see cref="ReadOnlyRepository{TEntity, TKey}"/> class associated to the specified
	/// database context.
	/// </summary>
	/// <param name="dbContext">
	/// The <see cref="DbContext"/> instance used for managing and querying entity data. This parameter cannot
	/// be null.
	/// </param>
	/// <exception cref="ArgumentNullException">Thrown when the <paramref name="dbContext"/> is null.</exception>
	protected ReadOnlyRepository(DbContext dbContext)
	{
		ArgumentNullException.ThrowIfNull(dbContext);
		DbContext = dbContext;
	}

	#region IQueryableReadOnlyRepository<TEntity,TKey> Members

	/// <inheritdoc/>
	public async Task<TEntity?> FindByIdAsync(TKey id, CancellationToken cancellationToken = default)
	{
		// await, instead of directly returning the ValueTask returned by DbSet.FindAsync, to convert the ValueTask to Task
		return await DbSet.FindAsync([id], cancellationToken);
	}

	/// <inheritdoc/>
	public async Task<TEntity> GetByIdAsync(TKey id, CancellationToken cancellationToken = default)
	{
		return (await FindByIdAsync(id, cancellationToken)).UnlessEntityIsNotFound(() => $"{nameof(Entity<TKey>.Id)}: {id}");
	}

	/// <inheritdoc/>
	public Task<bool> ExistsAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
	{
		return DbSet.AnyAsync(predicate, cancellationToken);
	}

	/// <inheritdoc/>
	public async Task<IEnumerable<TEntity?>> FindAllAsync(CancellationToken cancellationToken = default)
	{
		return await DbSet.ToListAsync(cancellationToken);
	}

	/// <inheritdoc/>
	public IQueryable<TEntity> CreateQuery()
	{
		return DbSet;
	}

	#endregion

	/// <summary>
	/// Gets the <see cref="DbContext"/> instance associated with this repository. This database context is used to interact
	/// with the underlying data store.
	/// </summary>
	[SuppressMessage("ReSharper", "MemberCanBePrivate.Global", Justification = "Public API.")]
	public DbContext DbContext { get; }

	/// <summary>
	/// Gets the <see cref="DbSet{TEntity}"/> representing the collection of all entities of type
	/// <typeparamref name="TEntity"/> in the context, enabling querying and interaction with the data store.
	/// </summary>
	protected DbSet<TEntity> DbSet => DbContext.Set<TEntity>();
}
