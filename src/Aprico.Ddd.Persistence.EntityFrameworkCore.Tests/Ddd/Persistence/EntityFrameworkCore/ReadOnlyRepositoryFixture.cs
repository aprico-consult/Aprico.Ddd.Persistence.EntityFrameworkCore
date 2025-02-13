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
using System.Threading.Tasks;
using Aprico.Ddd.Persistence.EntityFrameworkCore.Dummies;
using AutoFixture.Xunit2;

namespace Aprico.Ddd.Persistence.EntityFrameworkCore;

public abstract class ReadOnlyRepositoryFixture
{
	#region Nested Type: FindByIdAsync

	public class FindByIdAsync : ReadOnlyRepositoryFixture
	{
		[Theory]
		[AutoData]
		public async Task ReturnsEntity(DummyAggregate dummy)
		{
			await using var context = new DummyDbContext();
			await context.AddAsync(dummy);

			var sut = new DummyReadOnlyRepository(context);

			(await sut.FindByIdAsync(dummy.Id)).Should()
				.Be(dummy);
		}

		[Theory]
		[AutoData]
		public async Task ReturnsNull(Guid id)
		{
			await using var context = new DummyDbContext();

			var sut = new DummyReadOnlyRepository(context);

			(await sut.FindByIdAsync(id)).Should()
				.BeNull();
		}
	}

	#endregion

	#region Nested Type: GetByIdAsync

	public class GetByIdAsync : ReadOnlyRepositoryFixture
	{
		[Theory]
		[AutoData]
		public async Task ReturnsEntity(DummyAggregate dummy)
		{
			await using var context = new DummyDbContext();
			await context.AddAsync(dummy);

			var sut = new DummyReadOnlyRepository(context);

			(await sut.GetByIdAsync(dummy.Id)).Should()
				.Be(dummy);
		}

		[Theory]
		[AutoData]
		public async Task ThrowsEntityNotFoundException(Guid id)
		{
			await using var context = new DummyDbContext();

			var sut = new DummyReadOnlyRepository(context);

			await Invoking(async () => await sut.GetByIdAsync(id))
				.Should()
				.ThrowAsync<EntityNotFoundException>()
				.WithMessage($"Entity '{nameof(DummyAggregate)} {{ Id: {id:D} }}' not found.");
		}
	}

	#endregion
}
