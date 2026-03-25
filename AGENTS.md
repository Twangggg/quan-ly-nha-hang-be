# FoodHub Backend - Agent Guidelines

## Project Overview
- **Framework**: .NET 9.0 (ASP.NET Core)
- **Architecture**: Clean Architecture (Domain → Application → Infrastructure → WebAPI)
- **ORM**: Entity Framework Core 9
- **Authentication**: JWT Bearer
- **Logging**: Serilog
- **Testing**: xUnit + FluentAssertions + Moq + AutoFixture
- **Real-time**: SignalR Hubs

## Solution Structure
```
FoodHub_BE/
├── FoodHub.Domain/         # Entities, Enums, Domain Services
├── FoodHub.Application/    # Commands, Queries, Interfaces, Behaviors
├── FoodHub.Infrastructure/ # EF Core, Redis, External Services
├── FoodHub.WebAPI/         # Controllers, Middleware, Extensions
└── FoodHub.Tests/          # Unit & Integration Tests
```

## Build, Run & Test Commands
```bash
# Build
dotnet build                # Build solution
dotnet build FoodHub.sln    # Specific project

# Run
dotnet run --project FoodHub.WebAPI
dotnet run --project FoodHub.WebAPI --urls "http://localhost:5000"

# Database
dotnet ef migrations add <Name> --project FoodHub.Infrastructure  # Add migration
dotnet ef database update  --project FoodHub.Infrastructure          # Apply migrations

# Tests
dotnet test                 # Run all tests
dotnet test --filter "FullyQualifiedName~CreateOrderTests"  # Run specific test
dotnet test --verbosity normal
```

## Code Style

### EditorConfig
Follow `.editorconfig` rules (enforced by IDE):
- **Indent**: 4 spaces
- **Private fields**: `_camelCase` (with underscore prefix)
- **Interfaces**: `I` prefix (e.g., `IOrderRepository`)
- **Braces**: Allman style (newline before open brace)
- **System usings**: Sort first (`dotnet_sort_system_directives_first = true`)

### Naming
- **Classes/Methods**: PascalCase (`CreateOrderCommand`)
- **Properties**: PascalCase (`OrderId`)
- **Variables**: camelCase (`var orderId`)
- **Files**: PascalCase (`CreateOrderCommand.cs`)

### C# Patterns
```csharp
// Command
public class CreateOrderCommand : IRequest<Result<Guid>>, IMustBeActive
{
    public Guid TableId { get; set; }
    public string? Note { get; set; }
}

// Handler
public class CreateOrderHandler : IRequestHandler<CreateOrderCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateOrderCommand request, CancellationToken ct)
    {
        // Implementation
    }
}
```

### MediatR Commands/Queries Structure
```
Features/<Feature>/
├── Commands/
│   └── <Action>/
│       ├── <Action>Command.cs
│       └── <Action>Handler.cs
└── Queries/
    └── <Action>/
        ├── <Action>Query.cs
        └── <Action>Handler.cs
```

## Layer Dependencies
- **Domain**: No dependencies (pure business entities)
- **Application**: Depends on Domain (Commands, Queries, Interfaces)
- **Infrastructure**: Depends on Application (EF Core, Redis implementations)
- **WebAPI**: Depends on Application (Controllers, Middleware)

## Common Patterns

### Repository Pattern
```csharp
public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(Guid id);
    Task<Guid> CreateAsync(Order order);
}

public class OrderRepository : IOrderRepository
{
    private readonly AppDbContext _context;
    public OrderRepository(AppDbContext context) => _context = context;
}
```

### Result Pattern
```csharp
public class Result<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public List<string> Errors { get; }
    
    public static Result<T> Success(T value) => ...
    public static Result<T> Failure(List<string> errors) => ...
}
```

### Validation
Use FluentValidation or DataAnnotations on Commands:
```csharp
public class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderCommandValidator()
    {
        RuleFor(x => x.TableId).NotEmpty();
    }
}
```

### Authorization
Implement `IAuthorizationRequirement` and use `[Authorize]` attribute:
```csharp
public class MustBeActive : IAuthorizationRequirement
{
    // Requirements
}
```

### Error Handling
Use `ExceptionMiddleware` for centralized error handling (already implemented).

## Testing
```csharp
public class CreateOrderTests
{
    [Fact]
    public async Task Handle_ValidCommand_ReturnsSuccess()
    {
        // Arrange
        var command = new CreateOrderCommand { TableId = Guid.NewGuid() };
        
        // Act
        var result = await _handler.Handle(command, CancellationToken.None);
        
        // Assert
        result.IsSuccess.Should().BeTrue();
    }
}
```

## Real-time (SignalR)
Hubs defined in `FoodHub.Infrastructure`:
- `KdsHub` - Kitchen Display System
- `BillingHub` - Billing updates
- `TableStatusHub` - Table status changes

## Configuration
- Environment variables in `.env` (loaded via DotNetEnv)
- Secrets via `dotnet user-secrets`
- Serilog configured in `Program.cs`

## API Documentation
- Swagger available at `/swagger` in Development
- Versioning via `Asp.Versioning.Mvc`
