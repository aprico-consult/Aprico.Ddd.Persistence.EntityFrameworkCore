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
using System.Threading;
using System.Threading.Tasks;
using Aprico.AutoFixture.Xunit2;
using Aprico.Ddd.Abstractions;
using Aprico.Ddd.Persistence.EntityFrameworkCore.Dummies;
using AutoFixture.AutoMoq;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Aprico.Ddd.Persistence.EntityFrameworkCore;

public class DomainDrivenDbContextFixture
{
	[Fact]
	public async Task SaveChangesAsyncDispatchesEntityDomainEvents()
	{
		var domainEventDispatcherMock = new Mock<DummyDomainEventDispatcher> {
			CallBase = true
		}.As<IDomainEventDispatcher>();

		var dummy1 = new DummyAggregate().DoSomething();
		var dummy2 = new DummyAggregate().DoSomething();
		await using var context = new DummyDomainDrivenDbContext(domainEventDispatcherMock.Object);
		await context.AddAsync(dummy1);
		await context.AddAsync(dummy2);

		await context.SaveChangesAsync();

		domainEventDispatcherMock.Verify(m => m.DispatchEntityDomainEventsAsync(dummy1, It.IsAny<CancellationToken>()), Times.Once);
		domainEventDispatcherMock.Verify(m => m.DispatchEntityDomainEventsAsync(dummy2, It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	[SuppressMessage("ReSharper", "AccessToDisposedClosure")]
	public async Task SaveChangesAsyncSucceedsWhenReachingMaximumEventDispatchingIterationCount()
	{
		var services = new ServiceCollection();
		services.AddSingleton<IDomainEventHandler<DummyDomainEvent>, DummyDomainEventHandler>()
			.AddScoped<IDomainEventDispatcher>(static provider => new EntityDomainEventDispatcher(provider))
			.AddScoped<DomainDrivenDbContext, DummyDomainDrivenDbContext>();
		var serviceProvider = services.BuildServiceProvider();
		await using var context = serviceProvider.GetRequiredService<DomainDrivenDbContext>();

		var i = 1;
		var handledDomainEvents = new List<DummyDomainEvent>();
		DummyDomainEventHandler.Hook = async domainEvent => {
			handledDomainEvents.Should()
				.NotContain(domainEvent);
			handledDomainEvents.Add(domainEvent);
			await context.AddAsync(new DummyAggregate().DoSomething(i++ < context.MaximumEventDispatchingIterationCount));
		};
		await context.AddAsync(new DummyAggregate().DoSomething());

		await context.SaveChangesAsync();

		handledDomainEvents.Should()
			.HaveCount(context.MaximumEventDispatchingIterationCount);
	}

	[Theory]
	[AutoData<AutoMoqCustomization>]
	[SuppressMessage("ReSharper", "AccessToDisposedClosure")]
	public async Task SaveChangesAsyncThrowsWhenExceedingMaximumEventDispatchingIterationCount(IDomainEventDispatcher domainEventDispatcherMock)
	{
		// domainEventDispatcherMock will not remove events from entity's internal event queue
		// causing continuous event dispatching and eventually exceeding the max allowed iterations
		await using var context = new DummyDomainDrivenDbContext(domainEventDispatcherMock);
		await context.AddAsync(new DummyAggregate().DoSomething());
		await Invoking(() => context.SaveChangesAsync())
			.Should()
			.ThrowAsync<InvalidOperationException>()
			.WithMessage(DomainDrivenDbContext.MAX_DISPATCHING_ITERATIONS_EXCEEDED_ERROR_MESSAGE);
	}
}
