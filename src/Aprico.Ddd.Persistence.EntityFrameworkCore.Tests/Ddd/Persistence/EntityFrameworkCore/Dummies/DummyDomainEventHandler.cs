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

namespace Aprico.Ddd.Persistence.EntityFrameworkCore.Dummies;

[SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix")]
public class DummyDomainEventHandler : IDomainEventHandler<DummyDomainEvent>
{
	internal static Func<DummyDomainEvent, Task>? Hook { get; set; }

	#region IDomainEventHandler<DummyDomainEvent> Members

	[SuppressMessage("ReSharper", "UseConfigureAwaitFalse")]
	public async Task HandleAsync(DummyDomainEvent domainEvent, CancellationToken cancellationToken = default)
	{
		if (Hook is not null) await Hook.Invoke(domainEvent);
	}

	#endregion
}
