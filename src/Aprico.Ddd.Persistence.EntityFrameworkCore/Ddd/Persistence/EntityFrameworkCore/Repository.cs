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
using System.Threading;
using System.Threading.Tasks;
using Aprico.Ddd.Abstractions;
using Aprico.Ddd.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Aprico.Ddd.Persistence.EntityFrameworkCore;

/// <summary>
/// Represents a repository that provides both read and write operations for entities of type
/// <typeparamref name="TEntity"/> with a primary key of type <typeparamref name="TKey"/>.
/// </summary>
/// <typeparam name="TEntity">
/// The type of the entity managed by the repository, which must inherit from
/// <see cref="AggregateRoot{TKey}"/>.
/// </typeparam>
/// <typeparam name="TKey">The type of the entity's primary key, which must be a value type.</typeparam>
public class Repository<TEntity, TKey> : ReadOnlyRepository<TEntity, TKey>, IRepository<TEntity, TKey>
	where TEntity : AggregateRoot<TKey>
	where TKey : struct
{
	/// <summary>
	/// Initializes a new instance of the <see cref="Repository{TEntity, TKey}"/> class using the specified
	/// <see cref="DbContext"/>.
	/// </summary>
	/// <param name="dbContext">
	/// The <see cref="DbContext"/> used for managing and interacting with entity data. This parameter cannot
	/// be null.
	/// </param>
	protected Repository(DbContext dbContext) : base(dbContext) { }

	#region IRepository<TEntity,TKey> Members

	/// <inheritdoc/>
	public async Task AddAsync(TEntity entity, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(entity);
		await DbSet.AddAsync(entity, cancellationToken);
		this.DispatchOnAddedToRepository(entity);
	}

	/// <inheritdoc/>
	public Task RemoveAsync(TEntity entity, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(entity);
		DbSet.Remove(entity);
		this.DispatchOnRemovedFromRepository(entity);
		return Task.CompletedTask;
	}

	#endregion
}
