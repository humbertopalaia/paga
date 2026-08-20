# Implementation Plan: MVP 2 — Auth and Users API

## Overview

Implements JWT authentication (login, refresh with rotation, logout), administrative CRUD for users,
global exception handler producing ProblemDetails, and the current-user identity service. Builds on
the mvp-1 foundation (entities, DbContext, BcryptPasswordHasher, seed).

## Tasks

- [x] 1. Domain exceptions and application abstractions
  - [x] 1.1 Create domain exception hierarchy in Paga.Application
    - Create `Paga.Application/Exceptions/DomainException.cs` (abstract base)
    - Create `Paga.Application/Exceptions/NotFoundException.cs`
    - Create `Paga.Application/Exceptions/ConflictException.cs`
    - Create `Paga.Application/Exceptions/AuthenticationException.cs`
    - _Requirements: 6.1, 6.3, 6.4, 6.5, 6.6_

  - [x] 1.2 Create ICurrentUserService interface in Paga.Application
    - Create `Paga.Application/Abstractions/ICurrentUserService.cs` with `Guid UserId` property
    - _Requirements: 5.1_

  - [x] 1.3 Create ITokenService interface in Paga.Application
    - Create `Paga.Application/Abstractions/ITokenService.cs` with `GenerateAccessToken` and `GenerateRefreshToken` methods
    - _Requirements: 1.2, 1.3, 3.7_

  - [x] 1.4 Create IAuthService interface and TokenResponse record in Paga.Application
    - Create `Paga.Application/Abstractions/IAuthService.cs` with `LoginAsync`, `RefreshAsync`, `LogoutAsync`
    - Create `Paga.Application/DTOs/TokenResponse.cs` record
    - _Requirements: 2.1, 3.1, 4.1_

  - [x] 1.5 Create IUserService interface in Paga.Application
    - Create `Paga.Application/Abstractions/IUserService.cs` with `GetAllAsync`, `GetByIdAsync`, `CreateAsync`, `UpdateAsync`, `DeleteAsync`
    - _Requirements: 7.1, 8.1, 9.1, 10.1, 11.1_

- [x] 2. DTOs, validators, and pagination helper
  - [x] 2.1 Create auth DTOs
    - Create `Paga.Application/DTOs/LoginRequest.cs`
    - Create `Paga.Application/DTOs/RefreshRequest.cs`
    - Create `Paga.Application/DTOs/LogoutRequest.cs`
    - _Requirements: 2.1, 3.1, 4.1_

  - [x] 2.2 Create user DTOs
    - Create `Paga.Application/DTOs/CreateUserRequest.cs`
    - Create `Paga.Application/DTOs/UpdateUserRequest.cs`
    - Create `Paga.Application/DTOs/UserResponse.cs`
    - Create `Paga.Application/DTOs/UserFilter.cs`
    - _Requirements: 7.1, 7.5, 9.1, 10.1_

  - [x] 2.3 Create FluentValidation validators
    - Create `Paga.Application/Validators/LoginRequestValidator.cs`
    - Create `Paga.Application/Validators/RefreshRequestValidator.cs`
    - Create `Paga.Application/Validators/LogoutRequestValidator.cs`
    - Create `Paga.Application/Validators/CreateUserRequestValidator.cs`
    - Create `Paga.Application/Validators/UpdateUserRequestValidator.cs`
    - All validation messages in pt-BR
    - _Requirements: 2.6, 9.4, 9.5, 10.6_

  - [x] 2.4 Create pagination helper (PagedResult + ToPagedResultAsync)
    - Create `Paga.Application/Common/PagedResult.cs` record
    - Create `Paga.Application/Common/PaginationExtensions.cs` with `ToPagedResultAsync<T>` extension
    - Clamp pageNumber ≥ 1, pageSize 1..100
    - _Requirements: 7.4_

- [x] 3. Domain entity updates and infrastructure services
  - [x] 3.1 Add mutation methods to User and RefreshToken entities
    - Add `Update(string name, string email, string? passwordHash)` method to `User`
    - Add `Revoke()` method to `RefreshToken`
    - Keep private setters intact
    - _Requirements: 10.1, 10.2, 10.3, 4.1_

  - [x] 3.2 Implement TokenService in Paga.Infrastructure
    - Create `Paga.Infrastructure/Security/TokenService.cs` implementing `ITokenService`
    - Access token: HMAC-SHA256, claims `sub` and `email`, 30 min expiration
    - Refresh token: 32 bytes `RandomNumberGenerator`, Base64Url encoded
    - _Requirements: 1.2, 1.3, 3.7_

  - [x] 3.3 Implement AuthService in Paga.Infrastructure
    - Create `Paga.Infrastructure/Services/AuthService.cs` implementing `IAuthService`
    - Login: find user by email, verify password, generate tokens, persist refresh token (7 days)
    - Refresh: validate token, revoke old, generate new pair, persist
    - Logout: revoke token for userId, idempotent if not found/already revoked
    - Throw `AuthenticationException` on failures
    - _Requirements: 2.1, 2.2, 2.3, 2.4, 2.5, 3.1, 3.2, 3.3, 3.4, 3.5, 4.1, 4.2, 4.3_

  - [x] 3.4 Implement UserService in Paga.Infrastructure
    - Create `Paga.Infrastructure/Services/UserService.cs` implementing `IUserService`
    - GetAll: filter by name/email (case-insensitive Contains), project to UserResponse, paginate
    - GetById: project to UserResponse or throw NotFoundException
    - Create: validate email uniqueness, generate Guid + UTC CreatedAt, hash password, persist
    - Update: find tracked entity, update fields, hash new password if provided, check email uniqueness
    - Delete: find entity or throw NotFoundException, remove (cascade)
    - _Requirements: 7.1, 7.2, 7.3, 7.4, 7.5, 8.1, 8.2, 8.3, 9.1, 9.2, 9.3, 9.4, 9.6, 10.1, 10.2, 10.3, 10.4, 10.5, 11.1, 11.2, 11.3_

- [x] 4. Checkpoint - Core services
  - Ensure `dotnet build` passes for the entire solution, ask the user if questions arise.

- [x] 5. API layer (controllers, exception handler, middleware)
  - [x] 5.1 Implement CurrentUserService in Paga.Api
    - Create `Paga.Api/Services/CurrentUserService.cs` implementing `ICurrentUserService`
    - Extract `sub` claim from HttpContext.User, parse as Guid
    - Throw InvalidOperationException if missing/invalid
    - _Requirements: 5.1, 5.2, 5.3_

  - [x] 5.2 Implement GlobalExceptionHandler in Paga.Api
    - Create `Paga.Api/ExceptionHandling/GlobalExceptionHandler.cs` implementing `IExceptionHandler`
    - Map: ValidationException → 400 with errors dict, AuthenticationException → 401, NotFoundException → 404, ConflictException → 409, others → 500
    - Log Error for 500s with full exception, LogWarning for domain exceptions
    - Never expose stack trace or internal details in 500 response
    - Messages in pt-BR for validation and conflict
    - _Requirements: 6.1, 6.2, 6.3, 6.4, 6.5, 6.6, 6.7_

  - [x] 5.3 Configure JWT middleware and DI in Program.cs
    - Add `Jwt:Key` validation (fail-fast, min 32 chars)
    - Register Authentication with JwtBearer (HMAC-SHA256, ClockSkew zero, ValidateLifetime true)
    - Register Authorization
    - Register Controllers
    - Register FluentValidation auto-validation with assembly scanning
    - Register ExceptionHandler, HttpContextAccessor
    - Register ITokenService, IAuthService, IUserService, ICurrentUserService (scoped)
    - Pipeline: UseExceptionHandler, UseAuthentication, UseAuthorization, MapControllers
    - Add `Jwt` and `RefreshToken` sections to appsettings.json
    - _Requirements: 1.1, 1.4, 1.5, 1.6, 1.7_

  - [x] 5.4 Implement AuthController
    - Create `Paga.Api/Controllers/AuthController.cs`
    - `POST /api/auth/login` [AllowAnonymous] → 200 TokenResponse
    - `POST /api/auth/refresh` [AllowAnonymous] → 200 TokenResponse
    - `POST /api/auth/logout` [Authorize] → 200 (uses ICurrentUserService for userId)
    - _Requirements: 2.1, 2.5, 3.1, 3.6, 4.1, 4.4_

  - [x] 5.5 Implement UsersController
    - Create `Paga.Api/Controllers/UsersController.cs` with `[Authorize]`
    - `GET /api/users` → 200 PagedResult
    - `GET /api/users/{id}` → 200 UserResponse
    - `POST /api/users` → 201 CreatedAtAction
    - `PUT /api/users/{id}` → 200 UserResponse
    - `DELETE /api/users/{id}` → 204 NoContent
    - _Requirements: 7.1, 7.6, 8.1, 8.4, 9.1, 9.7, 10.1, 10.7, 11.1, 11.4_

- [x] 6. Checkpoint - Full API build
  - Ensure `dotnet build` passes for the entire solution and the API starts without runtime errors, ask the user if questions arise.

- [x] 7. Unit tests
  - [x] 7.1 Write unit tests for TokenService
    - `TokenServiceTests`: JWT claims correctness (sub, email, exp), 30 min expiration, refresh token length ≥ 43 chars, 100 refresh tokens are unique
    - _Requirements: 12.1, 12.2_

  - [x] 7.2 Write unit tests for AuthService
    - `AuthServiceTests`: login success returns TokenResponse, login email not found throws AuthenticationException, login wrong password throws AuthenticationException, refresh valid rotates, refresh expired throws, refresh revoked throws, refresh not found throws, logout idempotent
    - Mock IPasswordHasher, ITokenService, PagaDbContext (or use in-memory)
    - _Requirements: 12.3, 12.4_

  - [x] 7.3 Write unit tests for UserService
    - `UserServiceTests`: create with duplicate email throws ConflictException, create valid returns UserResponse, update with password updates hash, update without password preserves hash, update email duplicate throws ConflictException, delete existing removes, delete non-existing throws NotFoundException
    - _Requirements: 12.6_

  - [x] 7.4 Write unit tests for validators
    - `CreateUserRequestValidatorTests`: name empty fails, email empty fails, email invalid format fails, password empty fails, password < 6 chars fails, all valid passes
    - `UpdateUserRequestValidatorTests`: name empty fails, email invalid fails, password present < 6 chars fails, password null passes, all valid passes
    - `LoginRequestValidatorTests`: email empty fails, password empty fails, valid passes
    - _Requirements: 12.5_

  - [x] 7.5 Write unit tests for GlobalExceptionHandler
    - `GlobalExceptionHandlerTests`: ValidationException → 400 with errors, NotFoundException → 404, ConflictException → 409, AuthenticationException → 401, generic Exception → 500 without stack trace
    - _Requirements: 6.1, 6.3, 6.4, 6.5_

- [x] 8. Checkpoint - Unit tests green
  - Ensure `dotnet test --filter "FullyQualifiedName~Unit"` passes, ask the user if questions arise.

- [x] 9. Integration tests
  - [x] 9.1 Create test infrastructure (PagaApiFactory + helpers)
    - Create or update `PagaApiFactory` with Testcontainers PostgreSQL, seed admin with known password
    - Create `AuthenticateAsync` helper that logs in the seeded admin and returns authenticated HttpClient
    - _Requirements: 13.8_

  - [x] 9.2 Write integration tests for auth endpoints
    - `AuthEndpointsTests`: login admin → 200, login wrong email → 401, login wrong password → 401, login invalid payload → 400, refresh valid → 200 new pair, refresh revoked → 401, refresh expired → 401, logout revokes, logout idempotent, protected endpoints without token → 401
    - _Requirements: 13.1, 13.2, 13.3, 13.4_

  - [x] 9.3 Write integration tests for users endpoints
    - `UsersEndpointsTests`: GET list → 200 envelope, GET filter name, GET filter email, GET by id → 200, GET missing id → 404, POST → 201, POST duplicate email → 409, POST invalid → 400, PUT → 200, PUT with password, PUT without password preserves hash, PUT duplicate email → 409, PUT missing → 404, DELETE → 204, DELETE missing → 404, all without token → 401
    - _Requirements: 13.5, 13.6, 13.7_

- [ ] 10. Property-based tests (FsCheck)
  - [ ]* 10.1 Write property test for token claims correctness
    - **Property 1: Token claims correctness**
    - Generate random userId (Guid) and email, verify decoded JWT has correct `sub`, `email`, and `exp` = issue + 30 min
    - **Validates: Requirements 1.2, 1.3**

  - [ ]* 10.2 Write property test for refresh token entropy
    - **Property 2: Refresh token entropy and uniqueness**
    - Generate 100 refresh tokens, verify all distinct, each ≥ 43 chars
    - **Validates: Requirements 3.7**

  - [ ]* 10.3 Write property test for pagination envelope consistency
    - **Property 10: Pagination envelope consistency**
    - Generate random pageNumber [1..100] and pageSize [1..100], populate N users, verify envelope fields
    - **Validates: Requirements 7.4**

  - [ ]* 10.4 Write property test for password hash round-trip on creation
    - **Property 12: Password hash round-trip on creation**
    - Generate random passwords [6..50 chars], create user, verify Verify(password, storedHash) == true
    - **Validates: Requirements 9.2**

  - [ ]* 10.5 Write property test for update without password preserves hash
    - **Property 13: Update without password preserves hash**
    - Generate update without password, verify hash unchanged
    - **Validates: Requirements 10.3**

  - [ ]* 10.6 Write property test for server-generated identity
    - **Property 14: Server-generated identity**
    - Generate Id and CreatedAt in request body, verify server ignores them
    - **Validates: Requirements 9.6**

- [x] 11. Final checkpoint
  - Ensure `dotnet build` and `dotnet test` both pass with zero failures, ask the user if questions arise.

## Notes

- Tasks marked with `*` are optional and can be skipped for faster MVP delivery
- Each task references specific requirements for traceability
- Checkpoints ensure incremental validation
- Property tests validate universal correctness properties from the design document
- Unit tests validate specific examples and edge cases
- Existing infrastructure from mvp-1 (entities, DbContext, BcryptPasswordHasher, DatabaseSeeder) is not recreated
- FsCheck.Xunit and Moq packages need to be added to Paga.Tests.csproj
- FluentValidation.DependencyInjectionExtensions package needed for auto-validation in Program.cs

## Task Dependency Graph

```json
{
  "waves": [
    { "id": 0, "tasks": ["1.1", "1.2", "1.3", "1.4", "1.5"] },
    { "id": 1, "tasks": ["2.1", "2.2", "2.3", "2.4", "3.1"] },
    { "id": 2, "tasks": ["3.2", "3.3", "3.4", "5.1"] },
    { "id": 3, "tasks": ["5.2", "5.3"] },
    { "id": 4, "tasks": ["5.4", "5.5"] },
    { "id": 5, "tasks": ["7.1", "7.2", "7.3", "7.4", "7.5"] },
    { "id": 6, "tasks": ["9.1"] },
    { "id": 7, "tasks": ["9.2", "9.3"] },
    { "id": 8, "tasks": ["10.1", "10.2", "10.3", "10.4", "10.5", "10.6"] }
  ]
}
```
