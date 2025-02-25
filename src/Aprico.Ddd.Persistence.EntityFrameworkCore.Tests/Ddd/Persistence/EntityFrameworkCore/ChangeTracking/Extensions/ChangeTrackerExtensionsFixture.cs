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

using System.Threading.Tasks;
using Aprico.Ddd.Persistence.EntityFrameworkCore.Dummies;
using Aprico.Persistence.EntityFrameworkCore.Dummies;

namespace Aprico.Ddd.Persistence.EntityFrameworkCore.ChangeTracking.Extensions;

public class ChangeTrackerExtensionsFixture
{
	[Fact]
	public async Task GetTrackedEntitiesHavingDomainEventsReturnsEntitiesThatDidEnqueueDomainEvents()
	{
		var dummy1 = new DummyAggregate().DoSomething();
		var dummy2 = new DummyAggregate();
		await using var context = new DummyDbContext();
		await context.AddAsync(dummy1);
		await context.AddAsync(dummy2);

		context.ChangeTracker.GetTrackedEntitiesHavingDomainEvents()
			.Should()
			.BeEquivalentTo([dummy1]);
	}

	[Fact]
	public async Task GetTrackedEntitiesHavingDomainEventsReturnsNothingWhenEntitiesDidNotEnqueueDomainEvents()
	{
		var dummy1 = new DummyAggregate();
		var dummy2 = new DummyAggregate();
		await using var context = new DummyDbContext();
		await context.AddAsync(dummy1);
		await context.AddAsync(dummy2);

		context.ChangeTracker.GetTrackedEntitiesHavingDomainEvents()
			.Should()
			.BeEmpty();
	}
}
