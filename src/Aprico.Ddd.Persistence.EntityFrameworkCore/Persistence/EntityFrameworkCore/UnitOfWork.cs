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
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Aprico.Persistence.EntityFrameworkCore;

/// <summary>
/// Implements a unit of work pattern for EntityFrameworkCore, managing database transactions and changes for a single
/// <see cref="DbContext"/>. This class provides a mechanism to automatically persist database modifications and ensure
/// transactional integrity across a set of operations.
/// </summary>
/// <remarks>
/// When <see cref="IUnitOfWork.FlushAsync"/> is invoked, it will call
/// <see cref="DbContext.SaveChangesAsync(CancellationToken)"/>, automatically saving pending changes made to the
/// <see cref="DbContext"/> to the database. The class ensures that all database operations are executed within a single,
/// consistent transaction.
/// </remarks>
/// <example>
/// Here is a typical workflow for using a <see cref="UnitOfWork"/>:
/// <code><![CDATA[
/// using(var unitOfWork = new UnitOfWork(dbContext))
/// {
///   ... perform some DBContext change operations ...
///   await unitOfWork.FlushAsync();
///   ... perform more DBContext change operations ...
///   await unitOfWork.CommitAsync();
/// }
/// ]]></code>
/// </example>
[SuppressMessage("ReSharper", "MemberCanBeInternal", Justification = "Public API.")]
public class UnitOfWork : AbstractUnitOfWork
{
	public UnitOfWork(DbContext dbContext)
	{
		_dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
		_transaction = dbContext.Database.BeginTransaction();
		DbTransaction = (_transaction as RelationalTransaction)?.GetDbTransaction();
	}

	#region Base Class Member Overrides

	protected override async Task CommitAsyncCore(CancellationToken cancellationToken = new())
	{
		await _transaction.CommitAsync(cancellationToken);
		_committed = true;
	}

	public override DbTransaction? DbTransaction { get; }

	protected override void Dispose(bool disposing)
	{
		if (!_committed) _transaction.Rollback();
		if (!disposing) _transaction.Dispose();
		base.Dispose(disposing);
	}

	public override Task FlushAsync(CancellationToken cancellationToken = new())
	{
		return _dbContext.SaveChangesAsync(cancellationToken);
	}

	#endregion

	private readonly DbContext _dbContext;
	private readonly IDbContextTransaction _transaction;
	private bool _committed;
}
