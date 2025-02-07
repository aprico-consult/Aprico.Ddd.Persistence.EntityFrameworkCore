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

using System.Threading;
using System.Threading.Tasks;
using Aprico.AutoFixture.Xunit2;
using Aprico.Ddd.Abstractions;
using Aprico.Ddd.Persistence.EntityFrameworkCore.Dummies;
using Aprico.Moq.Extensions;
using AutoFixture.AutoMoq;
using Moq;

namespace Aprico.Ddd.Persistence.EntityFrameworkCore;

public class DomainDrivenDbContextFixture
{
	[Theory]
	[AutoData<AutoMoqCustomization>]
	public async Task SaveChangesAsyncDispatchesEntityDomainEvents(IDomainEventDispatcher domainEventDispatcher)
	{
		var dummy1 = new Aggregate().DoSomething();
		var dummy2 = new Aggregate().DoSomething();
		await using var context = new DomainDrivenAggregateDbContext(domainEventDispatcher);
		await context.AddAsync(dummy1);
		await context.AddAsync(dummy2);

		await context.SaveChangesAsync();

		domainEventDispatcher.AsMock()
			.Verify(m => m.DispatchEntityDomainEventsAsync(dummy1, It.IsAny<CancellationToken>()), Times.Once);
		domainEventDispatcher.AsMock()
			.Verify(m => m.DispatchEntityDomainEventsAsync(dummy2, It.IsAny<CancellationToken>()), Times.Once);
	}
}
