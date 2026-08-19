using Paga.Tests.Integration.Fixtures;

namespace Paga.Tests.Integration;

/// <summary>
/// Defines the "Integration" collection so the PostgreSQL container is shared
/// across all test classes that belong to this collection.
/// </summary>
[CollectionDefinition("Integration")]
public class IntegrationTestCollection : ICollectionFixture<PostgresFixture>
{
}
