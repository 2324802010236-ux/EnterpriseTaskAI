using EnterpriseTask.Domain.Entities;
using EnterpriseTask.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseTask.Infrastructure.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<EmployeeProfile> EmployeeProfiles => Set<EmployeeProfile>();
    public DbSet<WorkTask> WorkTasks => Set<WorkTask>();
    public DbSet<TaskAssignment> TaskAssignments => Set<TaskAssignment>();
    public DbSet<TaskComment> TaskComments => Set<TaskComment>();
    public DbSet<TaskStatusHistory> TaskStatusHistories => Set<TaskStatusHistory>();
    public DbSet<ChatRoom> ChatRooms => Set<ChatRoom>();
    public DbSet<ChatRoomMember> ChatRoomMembers => Set<ChatRoomMember>();
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<AiTaskSuggestion> AiTaskSuggestions => Set<AiTaskSuggestion>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        ConfigureApplicationUser(builder);
        ConfigureCompany(builder);
        ConfigureDepartment(builder);
        ConfigureEmployeeProfile(builder);
        ConfigureWorkTask(builder);
        ConfigureTaskAssignment(builder);
        ConfigureTaskComment(builder);
        ConfigureTaskStatusHistory(builder);
        ConfigureChatRoom(builder);
        ConfigureChatRoomMember(builder);
        ConfigureChatMessage(builder);
        ConfigureNotification(builder);
        ConfigureAiTaskSuggestion(builder);
        ConfigureAuditLog(builder);
    }

    private static void ConfigureApplicationUser(ModelBuilder builder)
    {
        builder.Entity<ApplicationUser>(entity =>
        {
            entity.ToTable("AspNetUsers");
            entity.Property(x => x.FullName).HasMaxLength(150);
            entity.Property(x => x.Position).HasMaxLength(150);
            entity.HasIndex(x => new { x.CompanyId, x.DepartmentId });
        });
    }

    private static void ConfigureCompany(ModelBuilder builder)
    {
        builder.Entity<Company>(entity =>
        {
            entity.ToTable("Companies");
            entity.Property(x => x.Name).HasMaxLength(200);
            entity.Property(x => x.TaxCode).HasMaxLength(50);
            entity.Property(x => x.Email).HasMaxLength(256);
            entity.Property(x => x.Phone).HasMaxLength(30);
            entity.Property(x => x.Address).HasMaxLength(500);
            entity.Property(x => x.Industry).HasMaxLength(150);
            entity.Property(x => x.Status).HasConversion<int>();
            entity.HasIndex(x => x.Email).IsUnique();

            entity.HasMany(x => x.Departments)
                .WithOne(x => x.Company)
                .HasForeignKey(x => x.CompanyId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(x => x.EmployeeProfiles)
                .WithOne(x => x.Company)
                .HasForeignKey(x => x.CompanyId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(x => x.WorkTasks)
                .WithOne(x => x.Company)
                .HasForeignKey(x => x.CompanyId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureDepartment(ModelBuilder builder)
    {
        builder.Entity<Department>(entity =>
        {
            entity.ToTable("Departments");
            entity.Property(x => x.Name).HasMaxLength(150);
            entity.Property(x => x.Description).HasMaxLength(1000);
            entity.Property(x => x.FunctionDescription).HasMaxLength(2000);
            entity.Property(x => x.ManagerUserId).HasMaxLength(450);
            entity.HasIndex(x => new { x.CompanyId, x.Name }).IsUnique();

            entity.HasMany(x => x.EmployeeProfiles)
                .WithOne(x => x.Department)
                .HasForeignKey(x => x.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(x => x.WorkTasks)
                .WithOne(x => x.AssignedDepartment)
                .HasForeignKey(x => x.AssignedDepartmentId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureEmployeeProfile(ModelBuilder builder)
    {
        builder.Entity<EmployeeProfile>(entity =>
        {
            entity.ToTable("EmployeeProfiles");
            entity.Property(x => x.UserId).HasMaxLength(450);
            entity.Property(x => x.EmployeeCode).HasMaxLength(50);
            entity.Property(x => x.FullName).HasMaxLength(150);
            entity.Property(x => x.Phone).HasMaxLength(30);
            entity.Property(x => x.Position).HasMaxLength(150);
            entity.Property(x => x.Skills).HasMaxLength(2000);
            entity.Property(x => x.CapacityNote).HasMaxLength(2000);
            entity.HasIndex(x => new { x.CompanyId, x.UserId }).IsUnique();
        });
    }

    private static void ConfigureWorkTask(ModelBuilder builder)
    {
        builder.Entity<WorkTask>(entity =>
        {
            entity.ToTable("WorkTasks");
            entity.Property(x => x.Title).HasMaxLength(250);
            entity.Property(x => x.Description).HasMaxLength(4000);
            entity.Property(x => x.AiSummary).HasMaxLength(2000);
            entity.Property(x => x.CreatedByUserId).HasMaxLength(450);
            entity.Property(x => x.Status).HasConversion<int>();
            entity.Property(x => x.Priority).HasConversion<int>();
            entity.HasIndex(x => new { x.CompanyId, x.Status });
            entity.HasIndex(x => new { x.CompanyId, x.DueDate });

            entity.HasMany(x => x.Assignments)
                .WithOne(x => x.WorkTask)
                .HasForeignKey(x => x.WorkTaskId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(x => x.Comments)
                .WithOne(x => x.WorkTask)
                .HasForeignKey(x => x.WorkTaskId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(x => x.StatusHistories)
                .WithOne(x => x.WorkTask)
                .HasForeignKey(x => x.WorkTaskId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureTaskAssignment(ModelBuilder builder)
    {
        builder.Entity<TaskAssignment>(entity =>
        {
            entity.ToTable("TaskAssignments");
            entity.Property(x => x.TargetType).HasConversion<int>();
            entity.Property(x => x.AssignedToUserId).HasMaxLength(450);
            entity.Property(x => x.AssignedByUserId).HasMaxLength(450);
            entity.Property(x => x.Note).HasMaxLength(1000);

            entity.HasOne(x => x.AssignedToDepartment)
                .WithMany()
                .HasForeignKey(x => x.AssignedToDepartmentId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureTaskComment(ModelBuilder builder)
    {
        builder.Entity<TaskComment>(entity =>
        {
            entity.ToTable("TaskComments");
            entity.Property(x => x.UserId).HasMaxLength(450);
            entity.Property(x => x.Content).HasMaxLength(4000);
        });
    }

    private static void ConfigureTaskStatusHistory(ModelBuilder builder)
    {
        builder.Entity<TaskStatusHistory>(entity =>
        {
            entity.ToTable("TaskStatusHistories");
            entity.Property(x => x.ChangedByUserId).HasMaxLength(450);
            entity.Property(x => x.FromStatus).HasConversion<int>();
            entity.Property(x => x.ToStatus).HasConversion<int>();
            entity.Property(x => x.Note).HasMaxLength(1000);
        });
    }

    private static void ConfigureChatRoom(ModelBuilder builder)
    {
        builder.Entity<ChatRoom>(entity =>
        {
            entity.ToTable("ChatRooms");
            entity.Property(x => x.Name).HasMaxLength(200);
            entity.Property(x => x.Type).HasConversion<int>();
            entity.Property(x => x.CreatedByUserId).HasMaxLength(450);
            entity.HasIndex(x => new { x.CompanyId, x.Type });

            entity.HasOne(x => x.Department)
                .WithMany()
                .HasForeignKey(x => x.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.WorkTask)
                .WithMany()
                .HasForeignKey(x => x.WorkTaskId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(x => x.Members)
                .WithOne(x => x.ChatRoom)
                .HasForeignKey(x => x.ChatRoomId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(x => x.Messages)
                .WithOne(x => x.ChatRoom)
                .HasForeignKey(x => x.ChatRoomId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureChatRoomMember(ModelBuilder builder)
    {
        builder.Entity<ChatRoomMember>(entity =>
        {
            entity.ToTable("ChatRoomMembers");
            entity.Property(x => x.UserId).HasMaxLength(450);
            entity.HasIndex(x => new { x.ChatRoomId, x.UserId }).IsUnique();
        });
    }

    private static void ConfigureChatMessage(ModelBuilder builder)
    {
        builder.Entity<ChatMessage>(entity =>
        {
            entity.ToTable("ChatMessages");
            entity.Property(x => x.SenderUserId).HasMaxLength(450);
            entity.Property(x => x.Content).HasMaxLength(2000);
        });
    }

    private static void ConfigureNotification(ModelBuilder builder)
    {
        builder.Entity<Notification>(entity =>
        {
            entity.ToTable("Notifications");
            entity.Property(x => x.UserId).HasMaxLength(450);
            entity.Property(x => x.Title).HasMaxLength(200);
            entity.Property(x => x.Message).HasMaxLength(1000);
            entity.Property(x => x.Type).HasConversion<int>();
            entity.HasIndex(x => new { x.CompanyId, x.UserId, x.IsRead });
        });
    }

    private static void ConfigureAiTaskSuggestion(ModelBuilder builder)
    {
        builder.Entity<AiTaskSuggestion>(entity =>
        {
            entity.ToTable("AiTaskSuggestions");
            entity.Property(x => x.RequestedByUserId).HasMaxLength(450);
            entity.Property(x => x.InputText).HasMaxLength(4000);
            entity.Property(x => x.SuggestedUserId).HasMaxLength(450);
            entity.Property(x => x.Summary).HasMaxLength(2000);
            entity.Property(x => x.Reason).HasMaxLength(2000);
            entity.Property(x => x.Score).HasPrecision(5, 2);
        });
    }

    private static void ConfigureAuditLog(ModelBuilder builder)
    {
        builder.Entity<AuditLog>(entity =>
        {
            entity.ToTable("AuditLogs");
            entity.Property(x => x.UserId).HasMaxLength(450);
            entity.Property(x => x.Action).HasMaxLength(150);
            entity.Property(x => x.EntityName).HasMaxLength(150);
            entity.Property(x => x.EntityId).HasMaxLength(100);
            entity.Property(x => x.Details).HasMaxLength(4000);
            entity.Property(x => x.IpAddress).HasMaxLength(45);
            entity.HasIndex(x => new { x.CompanyId, x.CreatedAt });
        });
    }
}
