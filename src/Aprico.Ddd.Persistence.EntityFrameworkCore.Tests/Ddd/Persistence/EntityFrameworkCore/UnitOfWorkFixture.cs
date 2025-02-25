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
using System.Linq;
using System.Threading.Tasks;
using Aprico.AutoFixture.Xunit2;
using Aprico.Ddd.Persistence.EntityFrameworkCore.Dummies;
using AutoFixture.AutoMoq;
using Microsoft.EntityFrameworkCore;

namespace Aprico.Ddd.Persistence.EntityFrameworkCore;

public class UnitOfWorkFixture
{
	[Theory]
	[AutoData<AutoMoqCustomization>]
	public void CannotEnlistMultipleTimes(DummySqliteDbContext dbContext)
	{
		Invoking(
				() => {
					using (new UnitOfWork(dbContext))
					using (new UnitOfWork(dbContext)) { }
				})
			.Should()
			.Throw<InvalidOperationException>()
			.WithMessage("The connection is already in a transaction and cannot participate in another transaction.");
	}

	[Theory]
	[AutoData<AutoMoqCustomization>]
	public async Task RollbacksImplicitly(DummySqliteDbContext dbContext, DummyAggregate dummyAggregate)
	{
		await EnsureDatabaseCreatedAsync(dbContext);
		using (new UnitOfWork(dbContext))
		{
			await dbContext.AddAsync(dummyAggregate);
		}

		dbContext.Dummies.SingleOrDefault()
			.Should()
			.BeNull();
	}

	[Theory]
	[AutoData<AutoMoqCustomization>]
	public async Task SaveChangesOnCommit(DummySqliteDbContext dbContext, DummyAggregate dummyAggregate)
	{
		dummyAggregate.Id.Should()
			.Be(Guid.Empty);
		await EnsureDatabaseCreatedAsync(dbContext);
		using (var unitOfWork = new UnitOfWork(dbContext))
		{
			await dbContext.AddAsync(dummyAggregate);
			await unitOfWork.CommitAsync();
		}
		dummyAggregate.Id.Should()
			.NotBe(Guid.Empty);

		dbContext.Dummies.Single()
			.Should()
			.BeEquivalentTo(dummyAggregate);
	}

	private static async Task EnsureDatabaseCreatedAsync(DummySqliteDbContext dbContext)
	{
		await dbContext.Database.OpenConnectionAsync();
		await dbContext.Database.EnsureCreatedAsync();
	}
}
