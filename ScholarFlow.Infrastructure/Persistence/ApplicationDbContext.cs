using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ScholarFlow.Domain.Entities;
using ScholarFlow.Domain.Interfaces;
using ScholarFlow.SharedKernel.Primitives;
using System.Linq.Expressions;

namespace ScholarFlow.Infrastructure.Persistence;

public class ApplicationDbContext
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>, IApplicationDbContext
{
    private readonly IMediator? _mediator;

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, IMediator? mediator = null)
        : base(options)
    {
        _mediator = mediator;
    }

    // ── DbSets ───────────────────────────────────────────────────────────────

    public new DbSet<ApplicationUser>          Users                        { get; set; }
    public DbSet<AcademicStream>               Streams                      { get; set; }
    public DbSet<Subject>                      Subjects                     { get; set; }
    public DbSet<SubjectStream>                SubjectStreams                { get; set; }
    public DbSet<Topic>                        Topics                       { get; set; }
    public DbSet<SubTopic>                     SubTopics                    { get; set; }
    public DbSet<Paper>                        Papers                       { get; set; }
    public DbSet<Question>                     Questions                    { get; set; }
    public DbSet<Option>                       Options                      { get; set; }
    public DbSet<Explanation>                  Explanations                 { get; set; }
    public DbSet<ExplanationSection>           ExplanationSections          { get; set; }
    public DbSet<ExamSession>                  ExamSessions                 { get; set; }
    public DbSet<ExamSessionQuestion>          ExamSessionQuestions         { get; set; }
    public DbSet<UserResponse>                 UserResponses                { get; set; }
    public DbSet<StudentProfile>               StudentProfiles              { get; set; }
    public DbSet<StudentStudyActivity>         StudentStudyActivities       { get; set; }
    public DbSet<StudentNotificationPreference> StudentNotificationPreferences { get; set; }
    public DbSet<TeacherProfile>               TeacherProfiles              { get; set; }
    public DbSet<StudentSubjectSelection>      StudentSubjectSelections     { get; set; }
    public DbSet<StudentTeacherConnection>     StudentTeacherConnections    { get; set; }
    public DbSet<StudentSubTopicPerformance>   StudentSubTopicPerformances  { get; set; }
    public DbSet<StudentSubjectPerformance>    StudentSubjectPerformances   { get; set; }
    public DbSet<StudentQuestionProgress>      StudentQuestionProgresses    { get; set; }
    public DbSet<OtpCode>                      OtpCodes                     { get; set; }
    public DbSet<NotificationDevice>           NotificationDevices          { get; set; }
    public DbSet<SubscriptionPlan>             SubscriptionPlans            { get; set; }
    public DbSet<StudentSubscription>          StudentSubscriptions         { get; set; }
    public DbSet<SubscriptionPayment>          SubscriptionPayments         { get; set; }
    public DbSet<StudentSubscriptionUsage>     StudentSubscriptionUsages    { get; set; }
    public DbSet<SubscriptionPlanPriceChange>  SubscriptionPlanPriceChanges { get; set; }

    // ── Model ────────────────────────────────────────────────────────────────

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            // Global soft-delete filter
            if (typeof(ISoftDeletable).IsAssignableFrom(entityType.ClrType))
            {
                var param  = Expression.Parameter(entityType.ClrType, "e");
                var prop   = Expression.Property(param, nameof(ISoftDeletable.IsDeleted));
                var filter = Expression.Lambda(Expression.Equal(prop, Expression.Constant(false)), param);
                modelBuilder.Entity(entityType.ClrType).HasQueryFilter(filter);
            }

            // Ignore DomainEvents navigation on all aggregates
            if (typeof(IHasDomainEvents).IsAssignableFrom(entityType.ClrType))
                modelBuilder.Entity(entityType.ClrType).Ignore(nameof(IHasDomainEvents.DomainEvents));
        }
    }

    // ── Save + Audit + Domain Events ─────────────────────────────────────────

    public override async Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        ApplyAuditInfo();

        // Collect before save so events aren't lost after ClearDomainEvents()
        var aggregates = ChangeTracker
            .Entries<IHasDomainEvents>()
            .Where(e => e.Entity.DomainEvents.Count > 0)
            .Select(e => e.Entity)
            .ToList();

        var result = await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);

        if (_mediator is not null)
        {
            foreach (var aggregate in aggregates)
            {
                var events = aggregate.DomainEvents.ToList();
                aggregate.ClearDomainEvents();
                foreach (var domainEvent in events)
                    await _mediator.Publish(domainEvent, cancellationToken);
            }
        }

        return result;
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ApplyAuditInfo();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    private void ApplyAuditInfo()
    {
        var now = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries<IAuditable>())
        {
            if (entry.State == EntityState.Added)   entry.Entity.CreatedAt = now;
            if (entry.State == EntityState.Modified) entry.Entity.UpdatedAt = now;
        }

        foreach (var entry in ChangeTracker.Entries<ISoftDeletable>()
                     .Where(e => e.State == EntityState.Deleted))
        {
            entry.State = EntityState.Modified;
            entry.Entity.IsDeleted  = true;
            entry.Entity.DeletedAt  = now;
        }
    }
}
