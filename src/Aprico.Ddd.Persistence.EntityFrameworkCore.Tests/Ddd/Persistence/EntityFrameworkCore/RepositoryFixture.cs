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

using System.Threading.Tasks;
using Aprico.AutoFixture.Xunit2;
using Aprico.Ddd.Persistence.EntityFrameworkCore.Dummies;
using AutoFixture.AutoMoq;
using Moq;
using Moq.Protected;

namespace Aprico.Ddd.Persistence.EntityFrameworkCore;

public class RepositoryFixture
{
	[Theory]
	[AutoData<AutoMoqCustomization>]
	public async Task AddAsyncCallsDispatchOnAddedToRepository(DummyRepository sut)
	{
		var aggregateMock = new Mock<DummyAggregate>();
		aggregateMock.Protected()
			.Setup("OnAddedToRepository")
			.Verifiable();

		await sut.AddAsync(aggregateMock.Object);

		aggregateMock.VerifyAll();
	}

	[Theory]
	[AutoData<AutoMoqCustomization>]
	public async Task RemoveAsyncCallsDispatchOnRemovedFromRepository(DummyRepository sut)
	{
		var aggregateMock = new Mock<DummyAggregate>();
		aggregateMock.Protected()
			.Setup("OnRemovedFromRepository")
			.Verifiable();

		await sut.RemoveAsync(aggregateMock.Object);

		aggregateMock.VerifyAll();
	}
}
