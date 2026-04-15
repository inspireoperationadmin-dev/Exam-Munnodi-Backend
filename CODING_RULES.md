# ScholarFlow — Coding Rules & Architecture Guide

Every developer working on this project must follow these rules without exception.
This document covers what to do, how to do it, and what is strictly forbidden.

---

## Table of Contents

1. [Project Structure Overview](#1-project-structure-overview)
2. [Module Ownership — Who Owns What](#2-module-ownership--who-owns-what)
3. [Cross-Module Communication](#3-cross-module-communication)
4. [Write Path — Commands & Repositories](#4-write-path--commands--repositories)
5. [Read Path — Queries & Dapper](#5-read-path--queries--dapper)
6. [Domain Entity Rules](#6-domain-entity-rules)
7. [Exception Handling](#7-exception-handling)
8. [Controller Rules](#8-controller-rules)
9. [DI Registration Rules](#9-di-registration-rules)
10. [The Absolute Don'ts](#10-the-absolute-donts)
11. [Checklist — Adding New Features](#11-checklist--adding-new-features)

---

## 1. Project Structure Overview

```
ScholarFlow.SharedKernel     — Zero deps. Primitives, exceptions, base interfaces.
ScholarFlow.Domain           — Entities, enums, events, value objects, repo interfaces.
ScholarFlow.Infrastructure   — EF Core, Dapper, JWT, repositories. Implements Domain interfaces.
Modules/
  ScholarFlow.Modules.Identity        — Register, Login
  ScholarFlow.Modules.UserProfiles    — StudentProfile, TeacherProfile lifecycle
  ScholarFlow.Modules.Academic        — Papers, Questions, Topics, Subjects
  ScholarFlow.Modules.Examination     — ExamSession start/submit/scoring
  ScholarFlow.Modules.Analytics       — Student performance tracking
ScholarFlow.WebAPI           — Controllers, filters, exception handler. No business logic.
```

**Dependency direction — never reversed:**
```
WebAPI → Modules → Infrastructure → Domain → SharedKernel
```

Modules never reference each other. Ever.

---

## 2. Module Ownership — Who Owns What

Each module is responsible for reading and writing only its own tables.

| Module | Owns (Write) | Can Read via Dapper JOIN |
|---|---|---|
| **Identity** | `ApplicationUser` (via UserManager) | Nothing else |
| **UserProfiles** | `StudentProfile`, `TeacherProfile`, `StudentTeacherConnection`, `StudentSubjectSelection` | `ApplicationUser` (read-only, for display) |
| **Academic** | `Paper`, `Question`, `Option`, `Explanation`, `ExplanationSection`, `Topic`, `SubTopic`, `Subject`, `AcademicStream`, `SubjectStream` | `TeacherProfile` (read-only, for display) |
| **Examination** | `ExamSession`, `ExamSessionQuestion`, `UserResponse` | `Paper`, `Question`, `Option` (read-only, load for scoring) |
| **Analytics** | `StudentSubjectPerformance`, `StudentSubTopicPerformance`, `StudentQuestionHistory` | Any table (read-only, for aggregated reports) |

> **Rule:** "Owns" means you can create, update, delete via EF repositories.
> "Can Read via Dapper JOIN" means read-only flat DTO projections only — no state change.

---

## 3. Cross-Module Communication

Modules communicate in two ways only. No exceptions.

### 3.1 Integration Events — for async notifications

Use when: one module needs to notify another that something happened.
The publisher does not care who is listening.

**Step 1 — Define the event in SharedKernel:**
```csharp
// SharedKernel/IntegrationEvents/PaperPublishedIntegrationEvent.cs
public sealed record PaperPublishedIntegrationEvent(
    Guid EventId,
    DateTime OccurredOn,
    Guid PaperId,
    Guid SubjectId,
    bool IsPublic
) : IIntegrationEvent;
```

**Step 2 — Publish from the source module's command handler:**
```csharp
// Academic module
await publisher.Publish(new PaperPublishedIntegrationEvent(...), ct);
```

**Step 3 — Handle in the target module:**
```csharp
// Examination module
public sealed class PaperPublishedIntegrationEventHandler(...)
    : INotificationHandler<PaperPublishedIntegrationEvent>
{
    public async Task Handle(PaperPublishedIntegrationEvent e, CancellationToken ct)
    {
        // idempotency guard first — always
        if (await repo.ExistsAsync(e.PaperId, ct)) return;
        // ...
    }
}
```

> Every integration event handler **must** have an idempotency guard as the first line.

---

### 3.2 Module Public API — for sync read during business logic

Use when: a command handler needs data from another module's tables to make a decision.
Direct DbSet access across modules is forbidden.

**Step 1 — Define a public interface inside the owning module:**
```csharp
// Academic/Public/IAcademicApi.cs  (access modifier: public)
public interface IAcademicApi
{
    Task<bool> PaperExistsAsync(Guid paperId, CancellationToken ct);
    Task<PaperSummary?> GetPaperSummaryAsync(Guid paperId, CancellationToken ct);
}

public sealed record PaperSummary(Guid Id, string Title, bool IsPublic, Guid? CreatedByTeacherId);
```

**Step 2 — Implement internally inside the same module:**
```csharp
// Academic/Public/AcademicApi.cs  (access modifier: internal sealed)
internal sealed class AcademicApi(IApplicationDbContext db) : IAcademicApi
{
    public Task<bool> PaperExistsAsync(Guid paperId, CancellationToken ct)
        => db.Papers.AnyAsync(p => p.Id == paperId, ct);

    public async Task<PaperSummary?> GetPaperSummaryAsync(Guid paperId, CancellationToken ct)
    {
        var paper = await db.Papers.FindAsync([paperId], ct);
        return paper is null ? null
            : new PaperSummary(paper.Id, paper.Title, paper.IsPublic, paper.CreatedByTeacherId);
    }
}
```

**Step 3 — Register in the module's DependencyInjection.cs:**
```csharp
services.AddScoped<IAcademicApi, AcademicApi>();
```

**Step 4 — Inject in any other module's handler:**
```csharp
// Examination module command handler
public sealed class StartExamCommandHandler(IAcademicApi academicApi, ...)
{
    public async Task Handle(StartExamCommand request, CancellationToken ct)
    {
        var paper = await academicApi.GetPaperSummaryAsync(request.PaperId, ct);
        if (paper is null) throw new NotFoundException("Paper not found.");
        // ...
    }
}
```

**Why this pattern?**
When migrating to microservices, only `AcademicApi.cs` changes (HTTP call instead of DB call).
All handlers that inject `IAcademicApi` remain untouched.

---

### 3.3 When to use which

| Situation | Use |
|---|---|
| Exam completed → update analytics | Integration Event |
| User registered → create profile | Integration Event |
| Start exam → check paper exists | Module Public API |
| Submit answer → verify option belongs to question | Module Public API |
| Display report with data from 3 modules | Dapper JOIN in Analytics module query |

---

## 4. Write Path — Commands & Repositories

### 4.1 Command structure

Every write operation is a MediatR command. One folder per command.

```
Commands/
  CreatePaper/
    CreatePaperCommand.cs
    CreatePaperCommandHandler.cs
    CreatePaperCommandValidator.cs
```

**Command — a plain record:**
```csharp
public sealed record CreatePaperCommand(
    string Title,
    Guid SubjectId,
    PaperType Type
) : IRequest<Guid>;
```

**Handler — throw exceptions, never return Result<T>:**
```csharp
public sealed class CreatePaperCommandHandler(
    IPaperRepository paperRepo,
    ICurrentUser currentUser)
    : IRequestHandler<CreatePaperCommand, Guid>
{
    public async Task<Guid> Handle(CreatePaperCommand request, CancellationToken ct)
    {
        // 1. Business rule checks — throw AppException subclass on violation
        var existing = await paperRepo.GetByTitleAsync(request.Title, ct);
        if (existing is not null)
            throw new ConflictException("A paper with this title already exists.");

        // 2. Create domain entity via factory method — never use `new Entity()`
        var paper = Paper.Create(
            title: request.Title,
            subjectId: request.SubjectId,
            type: request.Type,
            createdByUserId: currentUser.UserId);

        // 3. Persist via repository — never inject IApplicationDbContext in handlers
        await paperRepo.AddAsync(paper, ct);
        await paperRepo.SaveChangesAsync(ct);

        return paper.Id;
    }
}
```

**Validator:**
```csharp
public sealed class CreatePaperCommandValidator : AbstractValidator<CreatePaperCommand>
{
    public CreatePaperCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.SubjectId).NotEmpty();
    }
}
```

### 4.2 Repository rules

**Interface — in Domain layer:**
```csharp
// Domain/Interfaces/Repositories/IPaperRepository.cs
public interface IPaperRepository
{
    Task<Paper?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Paper?> GetByTitleAsync(string title, CancellationToken ct = default);
    Task AddAsync(Paper paper, CancellationToken ct = default);
    void Update(Paper paper);
    Task SaveChangesAsync(CancellationToken ct = default);
}
```

**Implementation — in Infrastructure, class name must start with `Ef`:**
```csharp
// Infrastructure/Persistence/Repositories/EfPaperRepository.cs
internal sealed class EfPaperRepository(ApplicationDbContext db) : IPaperRepository
{
    public Task<Paper?> GetByIdAsync(Guid id, CancellationToken ct)
        => db.Papers.FirstOrDefaultAsync(p => p.Id == id, ct);

    public Task<Paper?> GetByTitleAsync(string title, CancellationToken ct)
        => db.Papers.FirstOrDefaultAsync(p => p.Title == title, ct);

    public async Task AddAsync(Paper paper, CancellationToken ct)
        => await db.Papers.AddAsync(paper, ct);

    public void Update(Paper paper)
        => db.Papers.Update(paper);

    public Task SaveChangesAsync(CancellationToken ct)
        => db.SaveChangesAsync(ct);
}
```

> Scrutor auto-discovers all `Ef*` classes in the Repositories namespace.
> No manual DI registration needed when you name the class starting with `Ef`.

---

## 5. Read Path — Queries & Dapper

### 5.1 Query structure

```
Queries/
  GetPaperById/
    GetPaperByIdQuery.cs
    GetPaperByIdQueryHandler.cs
```

**Query:**
```csharp
public sealed record GetPaperByIdQuery(Guid PaperId) : IRequest<PaperDetailDto>;
```

**Handler — always use Dapper + ISqlConnectionFactory:**
```csharp
public sealed class GetPaperByIdQueryHandler(ISqlConnectionFactory sql)
    : IRequestHandler<GetPaperByIdQuery, PaperDetailDto>
{
    public async Task<PaperDetailDto> Handle(GetPaperByIdQuery request, CancellationToken ct)
    {
        using var conn = sql.CreateConnection();

        var dto = await conn.QueryFirstOrDefaultAsync<PaperDetailDto>("""
            SELECT
                p.Id,
                p.Title,
                p.Type,
                p.Medium,
                p.Year,
                s.Name AS SubjectName
            FROM Papers p
            JOIN Subjects s ON s.Id = p.SubjectId
            WHERE p.Id = @PaperId
              AND p.IsDeleted = 0
            """, new { request.PaperId });

        if (dto is null)
            throw new NotFoundException($"Paper {request.PaperId} not found.");

        return dto;
    }
}
```

### 5.2 Dapper JOIN rules

| JOIN | Allowed? | Reason |
|---|---|---|
| Own module's tables | ✅ Always | Same module boundary |
| Reference data (Subjects, Topics, Streams) | ✅ Always | Shared read-only lookup data |
| Other module's tables for display | ✅ Read-only flat DTO only | Dapper never loads domain entities |
| Other module's tables in a command handler | ❌ Never | Use Module Public API instead |

> **Key rule:** Dapper is for READ. It returns flat DTOs. It never loads a domain entity.
> If you need data from another module to MAKE A DECISION in a command — use Module Public API, not Dapper.

### 5.3 DTO rules

- DTOs live in the module that owns the query.
- DTOs are `sealed record` types.
- DTOs never contain domain entities or other DTOs as properties — flat only.
- No `[JsonIgnore]` or `[JsonProperty]` on DTOs — keep them plain.

```csharp
// ✅ Correct
public sealed record PaperDetailDto(
    Guid Id,
    string Title,
    string SubjectName,
    int QuestionCount);

// ❌ Wrong — nested entity
public sealed record PaperDetailDto(
    Guid Id,
    Paper Paper,           // never
    Subject Subject);      // never
```

---

## 6. Domain Entity Rules

### 6.1 Private constructor + static factory

```csharp
public class Paper : AuditableEntity
{
    // ✅ Private — prevents raw `new Paper()` from outside
    private Paper() { }

    // ✅ Static factory — only valid creation path
    public static Paper Create(string title, Guid subjectId, ...) => new()
    {
        Id = Guid.NewGuid(),
        Title = title,
        SubjectId = subjectId,
        // ...
    };
}
```

### 6.2 Private setters — state only through domain methods

```csharp
// ✅ Private setter
public TeacherRegistrationStatus Status { get; private set; }

// ✅ State change only through a domain method
public void Approve()
{
    if (Status != TeacherRegistrationStatus.Pending)
        throw new DomainException("Only pending profiles can be approved.");

    Status = TeacherRegistrationStatus.Accepted;
    ReviewedAt = DateTime.UtcNow;
    Raise(new TeacherApprovedDomainEvent(...));
}
```

### 6.3 Domain events — raise from inside the aggregate

```csharp
// Inside ExamSession
public ExamScore Complete(decimal negativeMarkValue)
{
    // ... scoring logic ...
    Raise(new ExamSessionCompletedDomainEvent(...));  // ✅ raised from inside
    return score;
}
```

### 6.4 Which base class to use

| Class | Use when |
|---|---|
| `AggregateRoot` | Raises domain events, not soft-deleted (e.g. `ExamSession`, `TeacherProfile`) |
| `AuditableEntity` | Admin-managed reference data, soft-deletable (e.g. `Paper`, `Question`, `Subject`) |
| `AuditableAggregateRoot` | Needs domain events + full audit + soft delete |

### 6.5 DomainException vs AppException

| Exception | Where | When |
|---|---|---|
| `DomainException` | Inside aggregate domain methods | Business invariant violated within the entity |
| `NotFoundException` | Command/query handlers | Entity not found in DB |
| `ConflictException` | Command handlers | Duplicate data |
| `ForbiddenException` | Command handlers | Auth role/ownership check failed |
| `BadRequestException` | Command handlers | Invalid request state (not caught by validator) |

```csharp
// ✅ DomainException — inside entity
public void Approve()
{
    if (Status != Pending) throw new DomainException("...");
}

// ✅ NotFoundException — in handler
var paper = await repo.GetByIdAsync(id, ct)
    ?? throw new NotFoundException("Paper not found.");

// ❌ Never throw Exception or ApplicationException directly
```

---

## 7. Exception Handling

All exceptions bubble up to `GlobalExceptionHandler`. Never use try-catch in handlers or controllers.

| Exception Type | HTTP Status | Response code |
|---|---|---|
| `ValidationException` (FluentValidation) | 400 | `VALIDATION_ERROR` |
| `BadRequestException` | 400 | `BAD_REQUEST` |
| `UnauthorizedException` | 401 | `UNAUTHORIZED` |
| `ForbiddenException` | 403 | `FORBIDDEN` |
| `NotFoundException` | 404 | `NOT_FOUND` |
| `ConflictException` | 409 | `CONFLICT` |
| Anything else | 500 | `INTERNAL_ERROR` |

**Do not wrap handler logic in try-catch.** Let exceptions propagate — the global handler manages all HTTP mapping.

---

## 8. Controller Rules

Controllers are thin. Zero business logic. Zero try-catch. One line per endpoint.

```csharp
[ApiController]
[Route("api/papers")]
[Authorize]
public sealed class PapersController(IMediator mediator) : ControllerBase
{
    [HttpPost]
    [Authorize(Roles = AppRole.Admin + "," + AppRole.Teacher)]
    public async Task<IActionResult> Create([FromBody] CreatePaperCommand command, CancellationToken ct)
        => Ok(await mediator.Send(command, ct));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        => Ok(await mediator.Send(new GetPaperByIdQuery(id), ct));
}
```

**Rules:**
- One controller per module (e.g. `PapersController`, `ExamController`)
- Controllers live in `ScholarFlow.WebAPI/Controllers/`
- `Ok(await mediator.Send(...))` — this is the only pattern
- Inject `ICurrentUser` for userId — never parse JWT claims manually in controllers
- `ApiResponseFilter` wraps `Ok()` results automatically — do not manually wrap

---

## 9. DI Registration Rules

Each module has its own `DependencyInjection.cs` with one extension method.

```csharp
// Modules/ScholarFlow.Modules.Academic/DependencyInjection.cs
public static class DependencyInjection
{
    public static IServiceCollection AddAcademicModule(this IServiceCollection services)
    {
        // MediatR handlers from this assembly
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(CreatePaperCommand).Assembly));

        // FluentValidation validators from this assembly
        services.AddValidatorsFromAssembly(typeof(CreatePaperCommand).Assembly);

        // Module Public API — if this module exposes one
        services.AddScoped<IAcademicApi, AcademicApi>();

        return services;
    }
}
```

> `ValidationBehavior<,>` is registered **once** in `AddInfrastructure()`. Never register it in a module.
> New EF repositories are auto-discovered by Scrutor — no manual registration needed (class name must start with `Ef`).

---

## 10. The Absolute Don'ts

### Module isolation
```csharp
// ❌ Module referencing another module's namespace
using ScholarFlow.Modules.Academic.Commands;   // from Examination module

// ❌ Injecting another module's handler or service directly
public class StartExamHandler(CreatePaperCommandHandler paperHandler)  // NEVER

// ❌ Accessing another module's DbSet in a command handler
public class StartExamHandler(IApplicationDbContext db)
{
    var paper = await db.Papers.FindAsync(id);  // ❌ use IAcademicApi instead
}
```

### Domain entity violations
```csharp
// ❌ Creating entity with `new`
var paper = new Paper { Title = "..." };        // NEVER — use Paper.Create()

// ❌ Setting private property from outside
paper.Status = ExamSessionStatus.Completed;     // won't compile, but never try

// ❌ Loading domain entity via Dapper
var session = conn.QueryFirstOrDefault<ExamSession>("SELECT ...");  // NEVER
// Dapper returns flat DTOs only — domain entities have private setters, Dapper can't hydrate them
```

### Handler anti-patterns
```csharp
// ❌ try-catch in handlers
try { ... } catch (Exception ex) { return Result.Failure(ex.Message); }  // NEVER

// ❌ Returning Result<T> from handlers — throw exceptions instead
public async Task<Result<Guid>> Handle(...)  // NEVER in this project

// ❌ Business logic in controllers
[HttpPost]
public async Task<IActionResult> Create(CreatePaperCommand cmd)
{
    if (cmd.Title.Length > 100) return BadRequest(...);  // NEVER — put in validator
    var paper = new Paper(...);                           // NEVER — put in handler
    await db.Papers.AddAsync(paper);                     // NEVER — put in repo
}
```

### Infrastructure leaking up
```csharp
// ❌ Injecting ApplicationDbContext directly in a module handler
public sealed class CreatePaperHandler(ApplicationDbContext db)  // NEVER
// Use IPaperRepository or IApplicationDbContext (interface), never the concrete class

// ❌ Injecting EfPaperRepository directly
public sealed class CreatePaperHandler(EfPaperRepository repo)   // NEVER
// Always inject the interface: IPaperRepository
```

---

## 11. Checklist — Adding New Features

### New Command in existing module
- [ ] `Commands/XxxCommand.cs` — `sealed record`, implements `IRequest<T>`
- [ ] `Commands/XxxCommandValidator.cs` — `AbstractValidator<XxxCommand>`
- [ ] `Commands/XxxCommandHandler.cs` — `IRequestHandler<XxxCommand, T>`
- [ ] Controller endpoint — one liner `Ok(await mediator.Send(command, ct))`
- [ ] No DI changes — MediatR auto-discovers from assembly

### New Query in existing module
- [ ] `Queries/GetXxx/GetXxxQuery.cs` — `sealed record`, implements `IRequest<T>`
- [ ] `Queries/GetXxx/GetXxxQueryHandler.cs` — inject `ISqlConnectionFactory`, use Dapper
- [ ] `DTOs/XxxDto.cs` — `sealed record`, flat properties only
- [ ] Controller endpoint

### New Repository
- [ ] `Domain/Interfaces/Repositories/IXxxRepository.cs`
- [ ] `Infrastructure/Persistence/Repositories/EfXxxRepository.cs` — class name **must** start with `Ef`
- [ ] `DbSet<Xxx>` added to `IApplicationDbContext` and `ApplicationDbContext`
- [ ] `Infrastructure/Persistence/Configurations/XxxConfiguration.cs`
- [ ] Run migration: `dotnet ef migrations add XxxMigration`

### New cross-module data need (command handler)
- [ ] Define `IXxxModuleApi` interface + DTO in the owning module's `Public/` folder
- [ ] Implement `XxxModuleApi` as `internal sealed` in the same module
- [ ] Register in owning module's `DependencyInjection.cs`
- [ ] Inject `IXxxModuleApi` in the consuming module's handler

### New cross-module notification
- [ ] Define integration event `sealed record` in `SharedKernel/IntegrationEvents/`
- [ ] `publisher.Publish(new XxxIntegrationEvent(...), ct)` in source module handler
- [ ] `INotificationHandler<XxxIntegrationEvent>` in target module's `EventHandlers/`
- [ ] Idempotency guard as first line in handler

### New Module
- [ ] Create project `ScholarFlow.Modules.Xxx`
- [ ] Add project references: SharedKernel, Domain, Infrastructure
- [ ] Create `DependencyInjection.cs` with `AddXxxModule()`
- [ ] Register in `Program.cs`: `builder.Services.AddXxxModule()`
- [ ] Add project reference in `ScholarFlow.WebAPI.csproj`
