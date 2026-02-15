using AssistenteIaApi.Domain.Entities;
using AssistenteIaApi.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace AssistenteIaApi.Infrastructure.Persistence.Orm;

public class AssistenteIaApiDbContext : DbContext
{
    public AssistenteIaApiDbContext(DbContextOptions<AssistenteIaApiDbContext> options) : base(options)
    {
    }

    public DbSet<AiTask> Tasks => Set<AiTask>();
    public DbSet<TaskAttempt> TaskAttempts => Set<TaskAttempt>();
    public DbSet<TaskArtifact> TaskArtifacts => Set<TaskArtifact>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AiTask>(entity =>
        {
            var domainTypeConverter = new EnumToStringConverter<DomainType>();
            var capabilityTypeConverter = new EnumToStringConverter<CapabilityType>();
            var executionTypeConverter = new EnumToStringConverter<TaskExecutionType>();

            entity.ToTable("Tasks");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.TenantId).HasMaxLength(100).IsRequired();
            entity.Property(x => x.DomainType).HasConversion(domainTypeConverter).HasMaxLength(80).IsRequired();
            entity.Property(x => x.CapabilityType).HasConversion(capabilityTypeConverter).HasMaxLength(80).IsRequired();
            entity.Property(x => x.TaskExecutionType).HasConversion(executionTypeConverter).HasMaxLength(80).IsRequired();
            entity.Property(x => x.PayloadJson).HasColumnType("text").IsRequired();
            entity.Property(x => x.IdempotencyKey).HasMaxLength(120).IsRequired();
            entity.Property(x => x.LockedBy).HasMaxLength(120);
            entity.Property(x => x.LastError).HasColumnType("text");
            entity.HasIndex(x => new { x.TenantId, x.IdempotencyKey }).IsUnique();
            entity.HasMany(x => x.Attempts).WithOne(x => x.Task).HasForeignKey(x => x.TaskId).OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(x => x.Artifacts).WithOne(x => x.Task).HasForeignKey(x => x.TaskId).OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(x => x.OutboxMessages).WithOne(x => x.Task).HasForeignKey(x => x.TaskId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<TaskAttempt>(entity =>
        {
            entity.ToTable("TaskAttempts");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Model).HasMaxLength(120);
            entity.Property(x => x.Cost).HasPrecision(18, 6);
            entity.Property(x => x.ErrorCode).HasMaxLength(60);
            entity.Property(x => x.ErrorDetail).HasColumnType("text");
        });

        modelBuilder.Entity<TaskArtifact>(entity =>
        {
            entity.ToTable("TaskArtifacts");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Kind).HasMaxLength(40).IsRequired();
            entity.Property(x => x.Uri).HasColumnType("text");
            entity.Property(x => x.Content).HasColumnType("text");
        });

        modelBuilder.Entity<OutboxMessage>(entity =>
        {
            entity.ToTable("OutboxMessages");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.EventType).HasMaxLength(120).IsRequired();
            entity.Property(x => x.PayloadJson).HasColumnType("text").IsRequired();
        });
    }
}
