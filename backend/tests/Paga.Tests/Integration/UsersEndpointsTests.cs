using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Paga.Application.Common;
using Paga.Application.DTOs;
using Paga.Tests.Integration.Fixtures;

namespace Paga.Tests.Integration;

/// <summary>
/// Integration tests for the /api/users endpoints.
/// Validates CRUD operations, filtering, pagination, error handling, and authentication.
/// </summary>
[Collection("Integration")]
public class UsersEndpointsTests : IntegrationTestBase
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public UsersEndpointsTests(PostgresFixture fixture) : base(fixture)
    {
    }

    #region GET /api/users

    [Fact]
    public async Task GetAll_ShouldReturn200WithEnvelope_WhenAuthenticated()
    {
        // Arrange
        using var client = await AuthenticateAsync();

        // Act
        var response = await client.GetAsync("/api/users");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        json.TryGetProperty("items", out _).Should().BeTrue();
        json.TryGetProperty("pageNumber", out _).Should().BeTrue();
        json.TryGetProperty("pageSize", out _).Should().BeTrue();
        json.TryGetProperty("totalCount", out _).Should().BeTrue();
        json.TryGetProperty("totalPages", out _).Should().BeTrue();
    }

    [Fact]
    public async Task GetAll_ShouldFilterByName_WhenNameProvided()
    {
        // Arrange
        using var client = await AuthenticateAsync();
        var uniqueName = $"FilterName_{Guid.NewGuid():N}";
        var createPayload = new
        {
            name = uniqueName,
            email = $"filtername_{Guid.NewGuid():N}@test.com",
            password = "Test123!"
        };
        var createResponse = await client.PostAsJsonAsync("/api/users", createPayload);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // Act
        var response = await client.GetAsync($"/api/users?name={uniqueName[..10]}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var items = json.GetProperty("items");
        items.GetArrayLength().Should().BeGreaterThanOrEqualTo(1);

        var found = items.EnumerateArray().Any(u =>
            u.GetProperty("name").GetString()!.Contains(uniqueName, StringComparison.OrdinalIgnoreCase));
        found.Should().BeTrue();
    }

    [Fact]
    public async Task GetAll_ShouldFilterByEmail_WhenEmailProvided()
    {
        // Arrange
        using var client = await AuthenticateAsync();

        // Act — filter by partial admin email
        var response = await client.GetAsync("/api/users?email=palaia");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var items = json.GetProperty("items");
        items.GetArrayLength().Should().BeGreaterThanOrEqualTo(1);

        var found = items.EnumerateArray().Any(u =>
            u.GetProperty("email").GetString()!.Contains("palaia", StringComparison.OrdinalIgnoreCase));
        found.Should().BeTrue();
    }

    #endregion

    #region GET /api/users/{id}

    [Fact]
    public async Task GetById_ShouldReturn200_WhenUserExists()
    {
        // Arrange
        using var client = await AuthenticateAsync();
        var createPayload = new
        {
            name = "GetById Test",
            email = $"getbyid_{Guid.NewGuid():N}@test.com",
            password = "Test123!"
        };
        var createResponse = await client.PostAsJsonAsync("/api/users", createPayload);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<UserResponse>(JsonOptions);

        // Act
        var response = await client.GetAsync($"/api/users/{created!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var user = await response.Content.ReadFromJsonAsync<UserResponse>(JsonOptions);
        user.Should().NotBeNull();
        user!.Id.Should().Be(created.Id);
        user.Name.Should().Be(createPayload.name);
        user.Email.Should().Be(createPayload.email);
    }

    [Fact]
    public async Task GetById_ShouldReturn404_WhenIdNotFound()
    {
        // Arrange
        using var client = await AuthenticateAsync();
        var randomId = Guid.NewGuid();

        // Act
        var response = await client.GetAsync($"/api/users/{randomId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region POST /api/users

    [Fact]
    public async Task Create_ShouldReturn201_WhenPayloadValid()
    {
        // Arrange
        using var client = await AuthenticateAsync();
        var payload = new
        {
            name = "New User",
            email = $"create201_{Guid.NewGuid():N}@test.com",
            password = "Secure123!"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/users", payload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();

        var user = await response.Content.ReadFromJsonAsync<UserResponse>(JsonOptions);
        user.Should().NotBeNull();
        user!.Id.Should().NotBeEmpty();
        user.Name.Should().Be(payload.name);
        user.Email.Should().Be(payload.email);
        user.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task Create_ShouldReturn409_WhenEmailDuplicate()
    {
        // Arrange
        using var client = await AuthenticateAsync();
        var payload = new
        {
            name = "Duplicate Email",
            email = PagaApiFactory.AdminEmail,
            password = "Secure123!"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/users", payload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Create_ShouldReturn400_WhenPayloadInvalid()
    {
        // Arrange
        using var client = await AuthenticateAsync();
        var payload = new
        {
            name = "",
            email = "bad",
            password = ""
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/users", payload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        json.TryGetProperty("errors", out _).Should().BeTrue();
    }

    #endregion

    #region PUT /api/users/{id}

    [Fact]
    public async Task Update_ShouldReturn200_WhenPayloadValid()
    {
        // Arrange
        using var client = await AuthenticateAsync();
        var createPayload = new
        {
            name = "Before Update",
            email = $"update200_{Guid.NewGuid():N}@test.com",
            password = "Test123!"
        };
        var createResponse = await client.PostAsJsonAsync("/api/users", createPayload);
        var created = await createResponse.Content.ReadFromJsonAsync<UserResponse>(JsonOptions);

        var updatePayload = new
        {
            name = "After Update",
            email = createPayload.email
        };

        // Act
        var response = await client.PutAsJsonAsync($"/api/users/{created!.Id}", updatePayload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<UserResponse>(JsonOptions);
        updated!.Name.Should().Be("After Update");
        updated.Email.Should().Be(createPayload.email);
    }

    [Fact]
    public async Task Update_ShouldUpdatePassword_WhenPasswordProvided()
    {
        // Arrange
        using var client = await AuthenticateAsync();
        var email = $"updatepw_{Guid.NewGuid():N}@test.com";
        var createPayload = new
        {
            name = "Password Update",
            email,
            password = "OldPass123!"
        };
        var createResponse = await client.PostAsJsonAsync("/api/users", createPayload);
        var created = await createResponse.Content.ReadFromJsonAsync<UserResponse>(JsonOptions);

        var updatePayload = new
        {
            name = "Password Update",
            email,
            password = "NewPass456!"
        };

        // Act
        var response = await client.PutAsJsonAsync($"/api/users/{created!.Id}", updatePayload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify new password works by logging in
        using var loginClient = Factory.CreateClient();
        var loginResponse = await loginClient.PostAsJsonAsync("/api/auth/login", new
        {
            email,
            password = "NewPass456!"
        });
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Update_ShouldPreserveHash_WhenPasswordNotProvided()
    {
        // Arrange
        using var client = await AuthenticateAsync();
        var email = $"preservepw_{Guid.NewGuid():N}@test.com";
        var originalPassword = "Original123!";
        var createPayload = new
        {
            name = "Preserve Hash",
            email,
            password = originalPassword
        };
        var createResponse = await client.PostAsJsonAsync("/api/users", createPayload);
        var created = await createResponse.Content.ReadFromJsonAsync<UserResponse>(JsonOptions);

        var updatePayload = new
        {
            name = "Preserve Hash Updated",
            email
        };

        // Act
        var response = await client.PutAsJsonAsync($"/api/users/{created!.Id}", updatePayload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify original password still works
        using var loginClient = Factory.CreateClient();
        var loginResponse = await loginClient.PostAsJsonAsync("/api/auth/login", new
        {
            email,
            password = originalPassword
        });
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Update_ShouldReturn409_WhenEmailDuplicate()
    {
        // Arrange
        using var client = await AuthenticateAsync();
        var createPayload = new
        {
            name = "Update Conflict",
            email = $"updateconflict_{Guid.NewGuid():N}@test.com",
            password = "Test123!"
        };
        var createResponse = await client.PostAsJsonAsync("/api/users", createPayload);
        var created = await createResponse.Content.ReadFromJsonAsync<UserResponse>(JsonOptions);

        var updatePayload = new
        {
            name = "Update Conflict",
            email = PagaApiFactory.AdminEmail // duplicate of seeded admin
        };

        // Act
        var response = await client.PutAsJsonAsync($"/api/users/{created!.Id}", updatePayload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Update_ShouldReturn404_WhenIdNotFound()
    {
        // Arrange
        using var client = await AuthenticateAsync();
        var randomId = Guid.NewGuid();
        var updatePayload = new
        {
            name = "Ghost",
            email = $"ghost_{Guid.NewGuid():N}@test.com"
        };

        // Act
        var response = await client.PutAsJsonAsync($"/api/users/{randomId}", updatePayload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region DELETE /api/users/{id}

    [Fact]
    public async Task Delete_ShouldReturn204_WhenUserExists()
    {
        // Arrange
        using var client = await AuthenticateAsync();
        var createPayload = new
        {
            name = "To Delete",
            email = $"delete204_{Guid.NewGuid():N}@test.com",
            password = "Test123!"
        };
        var createResponse = await client.PostAsJsonAsync("/api/users", createPayload);
        var created = await createResponse.Content.ReadFromJsonAsync<UserResponse>(JsonOptions);

        // Act
        var response = await client.DeleteAsync($"/api/users/{created!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Delete_ShouldReturn404_WhenIdNotFound()
    {
        // Arrange
        using var client = await AuthenticateAsync();
        var randomId = Guid.NewGuid();

        // Act
        var response = await client.DeleteAsync($"/api/users/{randomId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region Authentication required (401)

    [Fact]
    public async Task AllEndpoints_ShouldReturn401_WithoutToken()
    {
        // Arrange — use unauthenticated client
        var randomId = Guid.NewGuid();

        // Act & Assert — GET list
        var getAll = await Client.GetAsync("/api/users");
        getAll.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        // GET by id
        var getById = await Client.GetAsync($"/api/users/{randomId}");
        getById.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        // POST
        var post = await Client.PostAsJsonAsync("/api/users", new
        {
            name = "Unauthorized",
            email = "unauth@test.com",
            password = "Test123!"
        });
        post.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        // PUT
        var put = await Client.PutAsJsonAsync($"/api/users/{randomId}", new
        {
            name = "Unauthorized",
            email = "unauth@test.com"
        });
        put.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        // DELETE
        var delete = await Client.DeleteAsync($"/api/users/{randomId}");
        delete.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion
}
