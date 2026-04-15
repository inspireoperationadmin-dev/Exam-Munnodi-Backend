# ScholarFlow — Architecture Documentation

## Table of Contents
1. [Overview](#1-overview)
2. [Technology Stack](#2-technology-stack)
3. [Solution Structure](#3-solution-structure)
4. [Layer Architecture](#4-layer-architecture)
5. [Dependency Rules](#5-dependency-rules)
6. [SharedKernel](#6-sharedkernel)
7. [Domain Layer](#7-domain-layer)
8. [Infrastructure Layer](#8-infrastructure-layer)
9. [Modules](#9-modules)
10. [WebAPI Layer](#10-webapi-layer)
11. [Coding Patterns](#11-coding-patterns)
12. [Data Flow — Request Lifecycle](#12-data-flow--request-lifecycle)
13. [Event Architecture](#13-event-architecture)
14. [Adding New Features — Checklist](#14-adding-new-features--checklist)

---

## 1. Overview

ScholarFlow is built as a **Modular Monolith** using **Tactical Domain-Driven Design (DDD)**. The system is physically one deployable unit (single process) but logically divided into independent modules that communicate only through events — never through direct code references to each other.

```
┌─────────────────────────────────────────────────────────┐
│                    ScholarFlow.WebAPI                   │
│              (HTTP entry point, thin controllers)       │
├──────────┬──────────┬────────────┬──────────┬──────────-┤
│ Identity │ UserProf │  Academic  │  Examin  │ Analytics │
│  Module  │  Module  │   Module   │  Module  │  Module   │
├──────────┴──────────┴────────────┴──────────┴───────────┤
│                 ScholarFlow.Infrastructure               │
│      (EF Core, Repositories, TokenService, Seeder)      │
├─────────────────────────────────────────────────────────┤
│                   ScholarFlow.Domain                    │
│       (Entities, Enums, Events, Interfaces, VOs)        │
├─────────────────────────────────────────────────────────┤
│                 ScholarFlow.SharedKernel                │
│    (Primitives, Exceptions, Behaviors, IRepository)     │
└─────────────────────────────────────────────────────────┘
```

---

## 2. Technology Stack

| Concern              | Technology                          |
|----------------------|-------------------------------------|
| Framework            | .NET 10, ASP.NET Core               |
| Language             | C# 13 (primary constructors, collection expressions) |
| ORM                  | EF Core 10.0.2 (writes + aggregate loading) |
| Raw SQL reads        | Dapper 2.1.66 (query handlers → flat DTOs) |
| Mediator / CQRS      | MediatR 12.4.1                      |
| Validation           | FluentValidation 11.11.0            |
| Authentication       | ASP.NET Core Identity + JWT Bearer  |
| JWT generation       | System.IdentityModel.Tokens.Jwt 8.9 |
| DI scanning          | Scrutor 7.0.0                       |
| API documentation    | Scalar + OpenAPI                    |
| Database             | SQL Server (LocalDB for dev)        |

---

## 3. Solution Structure

```
j:\Asp.Net\SSSSS\
│
├── ScholarFlow.SharedKernel/          # Zero project refs — pure abstractions
│   ├── Primitives/
│   │   ├── IDomainEvent.cs
│   │   ├── IIntegrationEvent.cs
│   │   ├── IHasDomainEvents.cs
│   │   └── Result.cs
│   ├── Behaviors/
│   │   └── ValidationBehavior.cs
│   ├── Exceptions/
│   │   ├── AppException.cs            # abstract base
│   │   ├── BadRequestException.cs     # 400
│   │   ├── ConflictException.cs       # 409
│   │   ├── NotFoundException.cs       # 404
│   │   ├── UnauthorizedException.cs   # 401
│   │   └── ForbiddenException.cs      # 403
│   ├── Repositories/
│   │   ├── IRepository.cs             # generic base (reserved)
│   │   └── IUnitOfWork.cs
│   └── IntegrationEvents/
│       └── UserRegisteredIntegrationEvent.cs
│
├── ScholarFlow.Domain/                # Core business rules — no framework deps
│   ├── Entities/
│   │   ├── Base/
│   │   │   ├── AggregateRoot.cs       # Id + domain events
│   │   │   ├── AuditableEntity.cs     # Id + IAuditable + ISoftDeletable
│   │   │   └── AuditableAggregateRoot.cs  # Id + all three
│   │   ├── ApplicationUser.cs         # extends IdentityUser<Guid>
│   │   ├── StudentProfile.cs
│   │   ├── TeacherProfile.cs
│   │   ├── AcademicStream.cs
│   │   ├── Subject.cs
│   │   ├── SubjectStream.cs           # join: Subject ↔ AcademicStream
│   │   ├── Topic.cs
│   │   ├── SubTopic.cs
│   │   ├── Paper.cs
│   │   ├── Question.cs
│   │   ├── Option.cs
│   │   ├── Explanation.cs
│   │   ├── ExplanationSection.cs
│   │   ├── ExamSession.cs             # aggregate root — rich scoring logic
│   │   ├── ExamSessionQuestion.cs
│   │   ├── UserResponse.cs
│   │   ├── StudentTeacherConnection.cs
│   │   ├── StudentSubjectSelection.cs
│   │   ├── StudentQuestionHistory.cs
│   │   ├── StudentSubjectPerformance.cs
│   │   └── StudentSubTopicPerformance.cs
│   ├── Enums/
│   │   ├── AppRole.cs                 # string constants (not enum)
│   │   ├── PaperType.cs
│   │   ├── PaperMedium.cs
│   │   ├── ExamSitting.cs
│   │   ├── ExamSessionStatus.cs
│   │   ├── ResponseStatus.cs
│   │   ├── ExplanationType.cs
│   │   ├── TeacherRegistrationStatus.cs
│   │   └── ConnectionStatus.cs
│   ├── Events/                        # domain events raised by aggregates
│   │   ├── ExamSessionCompletedDomainEvent.cs
│   │   ├── TeacherApprovedDomainEvent.cs
│   │   └── TeacherRejectedDomainEvent.cs
│   ├── Exceptions/
│   │   └── DomainException.cs         # invariant violations inside aggregates
│   ├── ValueObjects/
│   │   └── ExamScore.cs               # sealed record, immutable
│   └── Interfaces/
│       ├── IApplicationDbContext.cs   # EF DbSet contracts (Infrastructure impl)
│       ├── IAuditable.cs
│       ├── ISoftDeletable.cs
│       ├── ISqlConnectionFactory.cs
│       └── Repositories/
│           ├── IStudentProfileRepository.cs
│           └── ITeacherProfileRepository.cs
│
├── ScholarFlow.Infrastructure/        # EF Core, Repos, JWT, Seeder
│   ├── DependencyInjection.cs         # AddInfrastructure() — all DI wiring
│   ├── Persistence/
│   │   ├── ApplicationDbContext.cs    # IdentityDbContext + soft delete + audit + event dispatch
│   │   ├── ApplicationDbContextFactory.cs  # IDesignTimeDbContextFactory (migrations)
│   │   ├── DataSeeder.cs              # Roles + SuperAdmin (idempotent)
│   │   ├── SqlConnectionFactory.cs    # ISqlConnectionFactory impl
│   │   ├── Configurations/            # IEntityTypeConfiguration<T> per entity (21 files)
│   │   └── Repositories/
│   │       ├── EfStudentProfileRepository.cs
│   │       └── EfTeacherProfileRepository.cs
│   ├── Services/
│   │   ├── ITokenService.cs
│   │   └── TokenService.cs            # JWT generation via SymmetricSecurityKey
│   └── Settings/
│       └── JwtSettings.cs             # bound to appsettings "JwtSettings" section
│
├── Modules/
│   ├── ScholarFlow.Modules.Identity/
│   │   ├── DependencyInjection.cs
│   │   ├── Commands/
│   │   │   ├── Register/
│   │   │   │   ├── RegisterCommand.cs
│   │   │   │   ├── RegisterCommandHandler.cs
│   │   │   │   └── RegisterCommandValidator.cs
│   │   │   └── Login/
│   │   │       ├── LoginCommand.cs
│   │   │       ├── LoginCommandHandler.cs
│   │   │       └── LoginCommandValidator.cs
│   │   └── DTOs/
│   │       └── AuthResponse.cs
│   │
│   ├── ScholarFlow.Modules.UserProfiles/
│   │   ├── DependencyInjection.cs
│   │   └── EventHandlers/
│   │       └── UserRegisteredIntegrationEventHandler.cs
│   │
│   ├── ScholarFlow.Modules.Academic/
│   │   └── DependencyInjection.cs     # stub — ready for feature development
│   │
│   ├── ScholarFlow.Modules.Examination/
│   │   └── DependencyInjection.cs     # stub — ready for feature development
│   │
│   └── ScholarFlow.Modules.Analytics/
│       └── DependencyInjection.cs     # stub — ready for feature development
│
└── ScholarFlow.WebAPI/                # Entry point — thin, no business logic
    ├── Program.cs
    ├── appsettings.json
    ├── Controllers/
    │   └── AuthController.cs
    ├── Filters/
    │   └── ApiResponseFilter.cs       # IActionFilter — wraps OK results in ApiResponse
    ├── Infrastructure/
    │   └── GlobalExceptionHandler.cs  # IExceptionHandler — maps exceptions to HTTP
    └── Models/
        └── ApiResponse.cs             # { success, data, error } envelope
```

---

## 4. Layer Architecture

### Dependency Direction (strict — never reversed)

```
WebAPI  →  Modules  →  Infrastructure  →  Domain  →  SharedKernel
```

- **SharedKernel** — no project references, only NuGet (MediatR, FluentValidation)
- **Domain** — no NuGet infrastructure packages; depends only on SharedKernel + EF Core abstractions
- **Infrastructure** — implements Domain interfaces; knows about EF Core, Dapper, Identity
- **Modules** — know Domain + Infrastructure interfaces; never reference each other
- **WebAPI** — knows all modules; composes everything in `Program.cs`

### What Each Layer Owns

| Layer | Owns | Does NOT own |
|---|---|---|
| SharedKernel | Primitives, base exceptions, base behaviors | Business logic, entities |
| Domain | Entities, enums, events, value objects, interfaces | Framework code, SQL |
| Infrastructure | DbContext, repositories, services, migrations | Business rules |
| Module | Commands, queries, validators, event handlers | Other module's entities |
| WebAPI | Controllers, filters, exception handler | Any business logic |

---

## 5. Dependency Rules

### Module Boundary Rules
- Modules **never reference each other** directly
- Cross-module communication happens **only through integration events** in SharedKernel
- Each module reads/writes **only its own domain entities** through its own repositories
- Shared read-only reference data (e.g. `AcademicStream`, `Subject`) may be read by any module

### Integration Event Contract
```
SharedKernel/IntegrationEvents/   ←  neutral ground
        ↑ published by             ↑ consumed by
   Identity Module            UserProfiles Module
```

The event record lives in SharedKernel so no module needs to reference another.

---

## 6. SharedKernel

### Primitives

**`IDomainEvent`** — marker for events raised within an aggregate (same transaction):
```csharp
public interface IDomainEvent : INotification
{
    Guid EventId { get; }
    DateTime OccurredOn { get; }
}
```

**`IIntegrationEvent`** — marker for events crossing module boundaries:
```csharp
public interface IIntegrationEvent : INotification
{
    Guid EventId { get; }
    DateTime OccurredOn { get; }
}
```

**`IHasDomainEvents`** — implemented by aggregate roots to expose raised events:
```csharp
public interface IHasDomainEvents
{
    IReadOnlyList<IDomainEvent> DomainEvents { get; }
    void ClearDomainEvents();
}
```

**`Result<T>`** — functional return type for operations that may fail without throwing:
```csharp
Result<T>.Success(value)
Result<T>.Failure("error message")
```

### Exception Hierarchy

All application exceptions extend `AppException` which carries `Code` (string) and `StatusCode` (int):

```
AppException (abstract)
  ├── BadRequestException   400  code: BAD_REQUEST     optional Details[]
  ├── NotFoundException     404  code: NOT_FOUND
  ├── UnauthorizedException 401  code: UNAUTHORIZED
  ├── ForbiddenException    403  code: FORBIDDEN
  └── ConflictException     409  code: CONFLICT
```

`DomainException` (in Domain layer) is separate — thrown only inside aggregates for invariant violations. It is NOT an AppException.

### ValidationBehavior

Registered **once** in `AddInfrastructure()` as an open MediatR pipeline behavior. Every `IRequest<T>` that has a registered `IValidator<T>` is automatically validated before the handler runs.

```csharp
// Throws FluentValidation.ValidationException on failure
// GlobalExceptionHandler catches it and returns 400 with details[]
```

---

## 7. Domain Layer

### Base Classes

Three base classes cover all use cases — no separate `BaseEntity`:

| Class | Id | Domain Events | IAuditable | ISoftDeletable |
|---|---|---|---|---|
| `AggregateRoot` | ✅ | ✅ | ✗ | ✗ |
| `AuditableEntity` | ✅ | ✗ | ✅ | ✅ |
| `AuditableAggregateRoot` | ✅ | ✅ | ✅ | ✅ |

**When to use which:**
- `AggregateRoot` — transactional aggregates that raise events but are not soft-deleted (e.g. `ExamSession`, `TeacherProfile`)
- `AuditableEntity` — reference/lookup data managed by admins (e.g. `Subject`, `Topic`, `Paper`)
- `AuditableAggregateRoot` — aggregates that need full audit + soft delete + events

### DDD Patterns Applied to Every Entity

**1. Private parameterless constructor** — prevents raw `new Entity()` from outside:
```csharp
private TeacherProfile() { }
```

**2. Static factory method** — the only way to create a valid entity:
```csharp
public static TeacherProfile Create(Guid userId, string fullName, ...) => new() { ... };
```

**3. Private setters** — state can only change through domain methods:
```csharp
public TeacherRegistrationStatus Status { get; private set; }
```

**4. Domain methods with business rules** — invariants enforced inside the aggregate:
```csharp
public void Approve()
{
    if (Status != TeacherRegistrationStatus.Pending)
        throw new DomainException("Only pending profiles can be approved.");
    Status = TeacherRegistrationStatus.Accepted;
    Raise(new TeacherApprovedDomainEvent(...));
}
```

**5. Domain events raised from within** — side effects declared, not executed:
```csharp
protected void Raise(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);
```

### Value Objects

`ExamScore` is a `sealed record` — immutable, self-validating, no identity:

```csharp
public sealed record ExamScore
{
    public decimal Percentage { get; }
    public decimal ObtainedMarks { get; }
    public decimal TotalMarks { get; }

    public static ExamScore Calculate(decimal obtainedMarks, decimal totalMarks) { ... }
    public bool IsPassing(decimal passMark = 40m) => Percentage >= passMark;
}
```

### Enums — Stored as Strings

All enums use `.HasConversion<string>()` in EF configuration. This makes the database human-readable and avoids integer migration issues when enum values are reordered.

`AppRole` is a **static constants class**, not an enum:
```csharp
public static class AppRole
{
    public const string SuperAdmin = "SuperAdmin";
    public const string Admin      = "Admin";
    public const string Teacher    = "Teacher";
    public const string Student    = "Student";
}
```

### Domain Events (3 defined)

| Event | Raised By | Purpose |
|---|---|---|
| `ExamSessionCompletedDomainEvent` | `ExamSession.Complete()` | Trigger analytics update |
| `TeacherApprovedDomainEvent` | `TeacherProfile.Approve()` | Notify teacher, update status |
| `TeacherRejectedDomainEvent` | `TeacherProfile.Reject()` | Notify teacher with reason |

### Repository Interfaces (in Domain/Interfaces/Repositories/)

Repository interfaces live in Domain so modules can depend on them without referencing Infrastructure:

```csharp
public interface IStudentProfileRepository
{
    Task<StudentProfile?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<StudentProfile?> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<bool> ExistsByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task AddAsync(StudentProfile profile, CancellationToken ct = default);
    void Update(StudentProfile profile);
    Task SaveChangesAsync(CancellationToken ct = default);
}
```

---

## 8. Infrastructure Layer

### ApplicationDbContext

Extends `IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>`.

Three automatic behaviors wired into `SaveChangesAsync`:

**1. Audit trail** — sets `CreatedAt` on add, `UpdatedAt` on modify:
```csharp
// Applied to all entities implementing IAuditable
if (entry.State == EntityState.Added) entry.Entity.CreatedAt = now;
if (entry.State == EntityState.Modified) entry.Entity.UpdatedAt = now;
```

**2. Soft delete interception** — converts EF `Deleted` state to `Modified + IsDeleted = true`:
```csharp
// Applied to all entities implementing ISoftDeletable
entry.State = EntityState.Modified;
entry.Entity.IsDeleted = true;
entry.Entity.DeletedAt = now;
```

**3. Domain event dispatch** — collects events before save, dispatches after:
```csharp
// Collect before save (while entities are still tracked)
var aggregates = ChangeTracker.Entries<IHasDomainEvents>()...

// Save first
var result = await base.SaveChangesAsync(...);

// Then publish (transaction committed, safe to publish)
foreach (var domainEvent in events)
    await _mediator.Publish(domainEvent, cancellationToken);
```

**Global query filter** — `IsDeleted = false` automatically applied to all `ISoftDeletable` entities via reflection in `OnModelCreating`. No need to add `.Where(x => !x.IsDeleted)` in queries.

### EF Core Configurations

Every entity has its own `IEntityTypeConfiguration<T>` file in `Persistence/Configurations/`. Key rules applied:

- All string properties have explicit `MaxLength`
- All decimal properties have explicit `HasPrecision`
- All enums use `.HasConversion<string>()`
- All backing fields (private collections) use `UsePropertyAccessMode(PropertyAccessMode.Field)`
- Ignored computed/value object properties use `.Ignore()`
- Strategic composite indexes on common query patterns

### Repositories

**EF Core repositories** (`Ef*Repository`) — used for:
- Loading aggregates for write operations
- Simple key-based lookups (`GetById`, `GetByUserId`)
- All writes (Add, Update, Delete, SaveChanges)

**Dapper** — used for:
- Complex read queries that return **flat DTOs** (not domain entities)
- Query handlers inject `ISqlConnectionFactory` directly
- Never used to hydrate domain aggregates (private setters would not be set)

```
Write path:  Command Handler → I*Repository → Ef*Repository → EF Core → SQL Server
Read path:   Query Handler   → ISqlConnectionFactory → Dapper → SQL Server → DTO
```

### Scrutor DI — Scan + Decorate Pattern

```csharp
// Scan: auto-registers every Ef* class as its I*Repository interface
services.Scan(scan => scan
    .FromAssemblyOf<ApplicationDbContext>()
    .AddClasses(c => c.InNamespaceOf<EfStudentProfileRepository>()
                      .Where(t => t.Name.StartsWith("Ef")))
    .AsImplementedInterfaces()
    .WithScopedLifetime());
```

When a new repository is added (`EfPaperRepository` implementing `IPaperRepository`), the Scan picks it up automatically — no manual registration needed.

### DependencyInjection.cs — Full Registration Map

```
AddInfrastructure()
  ├── AddDbContext<ApplicationDbContext>
  ├── AddScoped<IApplicationDbContext>
  ├── AddIdentity<ApplicationUser, IdentityRole<Guid>>
  │     └── AddEntityFrameworkStores + AddDefaultTokenProviders
  ├── Configure<JwtSettings>
  ├── AddScoped<ITokenService, TokenService>
  ├── AddSingleton<ISqlConnectionFactory, SqlConnectionFactory>
  ├── Scan → registers all Ef* repos as their I* interfaces (scoped)
  └── AddMediatR → registers ValidationBehavior<,> (open behavior, once globally)
```

---

## 9. Modules

Each module follows the same internal structure and rules:

```
Module/
  ├── DependencyInjection.cs      — AddXxxModule() extension method
  ├── Commands/                   — write operations (IRequest<T>)
  │   └── Xxx/
  │       ├── XxxCommand.cs
  │       ├── XxxCommandHandler.cs
  │       └── XxxCommandValidator.cs
  ├── Queries/                    — read operations (IRequest<T>)
  │   └── GetXxx/
  │       ├── GetXxxQuery.cs
  │       └── GetXxxQueryHandler.cs
  ├── EventHandlers/              — INotificationHandler<IIntegrationEvent>
  └── DTOs/                       — module-scoped response records
```

### Module DI Registration Pattern

```csharp
public static IServiceCollection AddXxxModule(this IServiceCollection services)
{
    // Register MediatR handlers from this assembly
    services.AddMediatR(cfg =>
        cfg.RegisterServicesFromAssembly(typeof(SomeCommand).Assembly));

    // Register FluentValidation validators from this assembly
    services.AddValidatorsFromAssembly(typeof(SomeCommand).Assembly);

    return services;
}
// NOTE: ValidationBehavior is NOT added here — it is registered once in AddInfrastructure()
```

### Identity Module

**Responsibility:** User account creation and authentication only.

| Handler | Input | Output | Key Behavior |
|---|---|---|---|
| `RegisterCommandHandler` | email, password, role, profile fields | `AuthResponse` | Creates Identity user, publishes `UserRegisteredIntegrationEvent`, returns JWT |
| `LoginCommandHandler` | email, password | `AuthResponse` | CheckPasswordSignIn, lockout on failure, returns JWT |

**What Identity does NOT do:**
- Create `StudentProfile` or `TeacherProfile` — that is UserProfiles' responsibility
- Generate `TeacherCode` — that is UserProfiles' responsibility
- Access any domain entity DbSets directly

### UserProfiles Module

**Responsibility:** Domain profile lifecycle (Student and Teacher profiles).

| Handler | Trigger | Key Behavior |
|---|---|---|
| `UserRegisteredIntegrationEventHandler` | `UserRegisteredIntegrationEvent` | Creates StudentProfile or TeacherProfile; generates TeacherCode; idempotent |

**Idempotency pattern:**
```csharp
if (await studentRepo.ExistsByUserIdAsync(e.UserId, ct)) return;
```

### Academic, Examination, Analytics Modules

Currently scaffolded stubs. Each has a `DependencyInjection.cs` with `AddXxxModule()` already wired in `Program.cs`. When features are added, handlers are auto-discovered — no changes to DI needed.

---

## 10. WebAPI Layer

### Program.cs — Composition Root

```
AddInfrastructure()         DbContext, Identity, JWT, Repos, ValidationBehavior
AddIdentityModule()         Register + Login handlers + validators
AddUserProfilesModule()     UserRegistered event handler
AddAcademicModule()         (stub)
AddExaminationModule()      (stub)
AddAnalyticsModule()        (stub)
AddAuthentication()         JwtBearer with token validation params
AddExceptionHandler()       GlobalExceptionHandler
AddControllers()            with ApiResponseFilter
AddOpenApi()                Scalar UI at /scalar
```

### Uniform API Response Envelope

Every response from every endpoint is wrapped in the same structure:

**Success:**
```json
{
  "success": true,
  "data": { ... },
  "error": null
}
```

**Failure:**
```json
{
  "success": false,
  "data": null,
  "error": {
    "code": "CONFLICT",
    "message": "An account with this email already exists.",
    "details": null
  }
}
```

**Validation failure:**
```json
{
  "success": false,
  "data": null,
  "error": {
    "code": "VALIDATION_ERROR",
    "message": "One or more validation errors occurred.",
    "details": [
      "Email is required.",
      "Password must be at least 8 characters."
    ]
  }
}
```

This is applied automatically:
- **Success wrapping** — `ApiResponseFilter` (IActionFilter) intercepts `OkObjectResult` / `OkResult` / `CreatedAtActionResult`
- **Error wrapping** — `GlobalExceptionHandler` (IExceptionHandler) catches all unhandled exceptions

### Controller Pattern

Controllers are thin — zero business logic, zero try-catch:
```csharp
[ApiController]
[Route("api/auth")]
public sealed class AuthController(IMediator mediator) : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register(
        [FromBody] RegisterCommand command, CancellationToken ct)
        => Ok(await mediator.Send(command, ct));
}
```

---

## 11. Coding Patterns

### C# 13 Features Used Throughout

| Feature | Where Used |
|---|---|
| Primary constructors | All exception classes, handlers, services, controllers |
| Collection expressions `[..spread]` | GlobalExceptionHandler, error detail lists |
| Pattern matching switch expressions | GlobalExceptionHandler, ApiResponseFilter |
| `sealed record` | DTOs, value objects, integration events, domain events |
| Target-typed `new()` | Entity factory methods |

### Command / Query Handler Pattern

```csharp
// Command — returns domain result directly (not Result<T>)
// Throws AppException subclass on failure — caught by GlobalExceptionHandler
public sealed class XxxCommandHandler(IDependency dep) : IRequestHandler<XxxCommand, XxxResponse>
{
    public async Task<XxxResponse> Handle(XxxCommand request, CancellationToken ct)
    {
        // validate business rules → throw specific AppException if violated
        // execute domain logic
        // persist via repository
        // return response DTO
    }
}
```

### Validator Pattern

```csharp
public sealed class XxxCommandValidator : AbstractValidator<XxxCommand>
{
    public XxxCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();

        // Conditional rules
        When(x => x.Role == AppRole.Teacher, () =>
        {
            RuleFor(x => x.SubjectId).NotEmpty();
        });
    }
}
```

### Repository Pattern

```csharp
// Interface in Domain — modules depend on this, not on EF
public interface IXxxRepository
{
    Task<Xxx?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(Xxx entity, CancellationToken ct = default);
    void Update(Xxx entity);
    Task SaveChangesAsync(CancellationToken ct = default);
}

// Implementation in Infrastructure — registered via Scrutor Scan
public sealed class EfXxxRepository(ApplicationDbContext db) : IXxxRepository { ... }
```

### Event Handler Pattern

```csharp
// Integration event handler — cross-module communication
public sealed class XxxIntegrationEventHandler(IXxxRepository repo)
    : INotificationHandler<XxxIntegrationEvent>
{
    public async Task Handle(XxxIntegrationEvent e, CancellationToken ct)
    {
        // idempotency guard first
        if (await repo.ExistsAsync(e.SomeId, ct)) return;

        // create domain entity
        var entity = XxxEntity.Create(...);
        await repo.AddAsync(entity, ct);
        await repo.SaveChangesAsync(ct);
    }
}
```

---

## 12. Data Flow — Request Lifecycle

### POST /api/auth/register

```
1.  HTTP POST → AuthController.Register()
2.  mediator.Send(RegisterCommand)
3.  ValidationBehavior → RegisterCommandValidator.Validate()
      ✗ fails → throws ValidationException
      ✓ passes → continues
4.  RegisterCommandHandler.Handle()
    a. userManager.FindByEmailAsync() → exists? throw ConflictException
    b. userManager.CreateAsync(user, password)
    c. userManager.AddToRoleAsync(user, role)
    d. publisher.Publish(UserRegisteredIntegrationEvent)
         → UserRegisteredIntegrationEventHandler.Handle()
              → ExistsByUserIdAsync() → skip if already created (idempotent)
              → StudentProfile.Create() or TeacherProfile.Create()
              → repo.AddAsync() + repo.SaveChangesAsync()
    e. tokenService.GenerateToken()
    f. return AuthResponse
5.  ApiResponseFilter wraps in { success: true, data: AuthResponse }
6.  HTTP 200 OK
```

### Exception Path

```
Any AppException thrown anywhere in the pipeline
  → GlobalExceptionHandler.TryHandleAsync()
  → maps to { success: false, error: { code, message, details? } }
  → HTTP status code from exception.StatusCode
```

---

## 13. Event Architecture

### Domain Events vs Integration Events

| | Domain Event | Integration Event |
|---|---|---|
| Interface | `IDomainEvent` | `IIntegrationEvent` |
| Scope | Within one aggregate's transaction | Across module boundaries |
| Location | `Domain/Events/` | `SharedKernel/IntegrationEvents/` |
| Published by | `ApplicationDbContext` after `SaveChanges` | Command handlers via `IPublisher` |
| Consumed by | Handlers in any module (same DB transaction context) | Handlers in target module |
| Example | `ExamSessionCompletedDomainEvent` | `UserRegisteredIntegrationEvent` |

### Domain Event Dispatch Flow

```
CommandHandler → repo.SaveChangesAsync()
  → ApplicationDbContext.SaveChangesAsync()
      1. ApplyAuditInfo()
      2. Collect IHasDomainEvents aggregates (before save)
      3. base.SaveChangesAsync() ← actual DB write
      4. foreach domainEvent → mediator.Publish(domainEvent)
```

Domain events are dispatched **after** the DB write succeeds — handlers see committed data.

### Current Integration Events

| Event | Published By | Consumed By | Fields |
|---|---|---|---|
| `UserRegisteredIntegrationEvent` | Identity module | UserProfiles module | UserId, Email, Role, FullName, PhoneNumber, SubjectId?, Qualification?, Bio? |

---

## 14. Adding New Features — Checklist

### Adding a new Command to an existing module

- [ ] Create `XxxCommand.cs` (record implementing `IRequest<TResponse>`)
- [ ] Create `XxxCommandValidator.cs` (AbstractValidator<XxxCommand>)
- [ ] Create `XxxCommandHandler.cs` (IRequestHandler<XxxCommand, TResponse>)
- [ ] Add endpoint to controller (one-liner: `Ok(await mediator.Send(command, ct))`)
- [ ] No DI changes needed — Scrutor + MediatR auto-discover

### Adding a new Repository

- [ ] Add interface to `Domain/Interfaces/Repositories/IXxxRepository.cs`
- [ ] Add implementation to `Infrastructure/Persistence/Repositories/EfXxxRepository.cs`
- [ ] No manual DI registration — Scrutor Scan picks up all `Ef*` classes automatically
- [ ] Add `DbSet<Xxx>` to `IApplicationDbContext` and `ApplicationDbContext`
- [ ] Create EF configuration `Persistence/Configurations/XxxConfiguration.cs`
- [ ] Run `dotnet ef migrations add XxxMigration`

### Adding a new Module

- [ ] Add new project `ScholarFlow.Modules.Xxx`
- [ ] Reference SharedKernel, Domain, Infrastructure in `.csproj`
- [ ] Create `DependencyInjection.cs` with `AddXxxModule()`
- [ ] Add `builder.Services.AddXxxModule()` in `Program.cs`
- [ ] Add project reference in `ScholarFlow.WebAPI.csproj`

### Adding cross-module communication

- [ ] Define integration event record in `SharedKernel/IntegrationEvents/`
- [ ] Publisher module: `await publisher.Publish(new XxxIntegrationEvent(...), ct)`
- [ ] Consumer module: create `XxxIntegrationEventHandler : INotificationHandler<XxxIntegrationEvent>`
- [ ] No routing config — MediatR dispatches in-process automatically
