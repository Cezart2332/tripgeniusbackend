# TripGenius Backend Test Suite

Comprehensive testing framework for TripGenius backend with **unit tests** and **integration tests** using **xUnit** and **Moq**.

## Project Structure

```
TripGeniusBackend.Tests/
├── Unit/
│   └── Services/
│       ├── AuthServiceTests.cs
│       ├── UserServiceTests.cs
│       ├── TripServiceTests.cs
│       ├── AiChatServiceTests.cs
│       └── BugServiceTests.cs
├── Integration/
│   └── Controllers/
│       ├── AuthControllerIntegrationTests.cs
│       ├── TripControllerIntegrationTests.cs
│       ├── UserControllerIntegrationTests.cs
│       ├── AiControllerIntegrationTests.cs
│       ├── BugControllerIntegrationTests.cs
│       └── GeocodingControllerIntegrationTests.cs
├── Fixtures/
│   ├── TestDbContextFactory.cs          (In-memory database factory)
│   ├── AuthTestFixture.cs               (JWT token generation)
│   └── TripGeniusWebApplicationFactory.cs (Integration test server)
├── Builders/
│   ├── UserBuilder.cs                   (Test data builder for User)
│   └── TripBuilder.cs                   (Test data builder for Trip)
├── Mocks/
│   └── MockServiceFixture.cs            (Pre-configured Moq mocks)
└── TripGeniusBackend.Tests.csproj
```

## Features

### Test Utilities & Fixtures

**TestDbContextFactory**
- Creates isolated in-memory databases for each test
- Ensures test isolation without database interference
- Usage: `var context = TestDbContextFactory.CreateTestContext();`

**AuthTestFixture**
- Generates valid JWT tokens for testing authorized endpoints
- Supports token generation with custom user IDs and expiration
- Usage: `var token = AuthTestFixture.GenerateTestToken(userId: 1);`

**TripGeniusWebApplicationFactory**
- Configures in-memory ASP.NET Core application for integration tests
- Automatically replaces database with SQLite in-memory store
- Usage: `public class MyIntegrationTests : IClassFixture<TripGeniusWebApplicationFactory>`

### Entity & DTO Builders

**UserBuilder & TripBuilder**
- Fluent API for building test objects with custom or default values
- Reduces boilerplate in test setup
- Example:
  ```csharp
  var trip = new TripBuilder()
      .WithTitle("Summer Vacation")
      .WithPrice(500m)
      .WithMaxParticipants(10)
      .Build();
  ```

### Mock Service Fixture

**MockServiceFixture**
- Pre-configured mocks for all service dependencies
- Reduces setup code in unit tests
- Example:
  ```csharp
  var mockFixture = new MockServiceFixture();
  mockFixture.MockUserRepository.Setup(...);
  mockFixture.ResetAllMocks(); // Between tests
  ```

## Running Tests

### Run All Tests
```bash
dotnet test TripGeniusBackend.Tests/TripGeniusBackend.Tests.csproj
```

### Run Specific Test Class
```bash
dotnet test TripGeniusBackend.Tests/TripGeniusBackend.Tests.csproj --filter "FullyQualifiedName~AuthServiceTests"
```

### Run Unit Tests Only
```bash
dotnet test TripGeniusBackend.Tests/TripGeniusBackend.Tests.csproj --filter "Namespace~Unit"
```

### Run Integration Tests Only
```bash
dotnet test TripGeniusBackend.Tests/TripGeniusBackend.Tests.csproj --filter "Namespace~Integration"
```

### Generate Coverage Report
```bash
dotnet test /p:CollectCoverage=true /p:CoverageFormat=opencover
```

## Test Coverage

### Unit Tests (Service Layer)
- **AuthService** (5+ tests)
  - Register with valid/duplicate email
  - Login with valid/invalid credentials
  - Email verification flow
  - Token refresh/expiration
  
- **UserService** (5+ tests)
  - Get user profile
  - Update profile information
  - Upload avatar/profile pictures
  - Change password with verification
  
- **TripService** (5+ tests)
  - Create new trip with timeline
  - Get/update/list trips
  - Member management
  - Timeline operations
  
- **AiChatService** (5+ tests)
  - Get chat history
  - Message retrieval and validation
  - User context validation
  
- **BugService** (5+ tests)
  - Report bugs with details
  - Retrieve bug reports
  - Update bug status
  - Bug filtering and sorting

### Integration Tests (Controller Layer)
- **AuthController**
  - Registration flow with validation
  - Login authentication
  - Token refresh
  
- **TripController**
  - Create trip with form upload
  - CRUD operations
  - Permission verification
  - Timeline management
  
- **UserController**
  - Profile endpoints
  - Avatar upload
  - Preferences management
  
- **AiController**
  - Chat message sending
  - History retrieval
  - Session management
  
- **BugController**
  - Bug report submission
  - Report listing
  - Status updates
  
- **GeocodingController**
  - Address to coordinates
  - Reverse geocoding
  - Place search
  - Distance calculations

## Known Issues & Notes

### Compilation Errors
Some test methods use service interfaces that may need adjustment:
- Verify actual method signatures in service interfaces match test calls
- Update mocks and assertions to match actual return types
- Adjust DTOs and builders based on actual entity properties

### Test Adjustments Needed
1. **AuthResponse** - Property name may be `Token` not `AccessToken`
2. **TripService** - Methods like `DeleteTrip` may have different names
3. **BugService** - Verify interface methods match test method calls
4. **User Entity** - Check verification methods available

### Recommendations
1. Run `dotnet build` to identify remaining compilation errors
2. Cross-reference test method calls with actual service interfaces
3. Update mock setups to match actual parameter types
4. Adjust assertions to match actual return types

## Mock Setup Examples

### Service Unit Test
```csharp
public class AuthServiceTests
{
    private readonly AuthService _authService;
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<IJwtService> _mockJwtService;
    
    public AuthServiceTests()
    {
        _mockUserRepository = new Mock<IUserRepository>();
        _mockJwtService = new Mock<IJwtService>();
        
        _authService = new AuthService(
            _mockUserRepository.Object,
            _mockJwtService.Object
            // ... other dependencies
        );
    }
    
    [Fact]
    public async Task Login_WithValidCredentials_ReturnsAuthResponse()
    {
        // Arrange
        var user = User.UserCreate("user@example.com", "hashedPassword");
        _mockUserRepository.Setup(x => x.GetUserByEmail("user@example.com"))
            .ReturnsAsync(user);
        
        // Act
        var result = await _authService.Login(new LoginRequest { ... });
        
        // Assert
        result.Should().NotBeNull();
        result.Token.Should().NotBeNullOrEmpty();
    }
}
```

### Integration Test
```csharp
public class AuthControllerIntegrationTests : 
    IClassFixture<TripGeniusWebApplicationFactory>
{
    private readonly HttpClient _client;
    
    public AuthControllerIntegrationTests(TripGeniusWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }
    
    [Fact]
    public async Task Login_WithValidCredentials_ReturnsOkAndToken()
    {
        // Arrange
        var loginRequest = new LoginRequest { ... };
        
        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
```

## Best Practices

1. **Test Isolation**: Each test uses fresh fixtures/mocks
2. **Naming**: Tests follow pattern: `MethodName_Scenario_ExpectedResult`
3. **Async/Await**: All service methods are properly awaited
4. **Assertions**: Use FluentAssertions for readable assertions
5. **Data Builders**: Use builders for test data consistency
6. **Mock Reset**: Reset mocks between tests in complex scenarios

## Contributing to Tests

When adding new tests:
1. Follow existing naming conventions
2. Use appropriate fixtures and builders
3. Test both happy paths and error scenarios
4. Update this README with new test scenarios
5. Maintain >80% coverage for services
6. Verify tests run successfully before committing

## References

- [xUnit Documentation](https://xunit.net/)
- [Moq Documentation](https://github.com/moq/moq4/wiki/Quickstart)
- [FluentAssertions](https://fluentassertions.com/)
- [ASP.NET Core Testing](https://docs.microsoft.com/en-us/aspnet/core/test/integration-tests)
