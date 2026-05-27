using DeployPilot.Persistence.Entities;
using DeployPilot.Shared;
using Microsoft.EntityFrameworkCore;

namespace DeployPilot.Persistence;

public sealed class DeployPilotDbContext(DbContextOptions<DeployPilotDbContext> options) : DbContext(options)
{
    public DbSet<OrganizationEntity> Organizations => Set<OrganizationEntity>();

    public DbSet<UserAccountEntity> Users => Set<UserAccountEntity>();

    public DbSet<ApplicationEntity> Applications => Set<ApplicationEntity>();

    public DbSet<ModuleEntity> Modules => Set<ModuleEntity>();

    public DbSet<RepositoryEntity> Repositories => Set<RepositoryEntity>();

    public DbSet<BuildTemplateEntity> BuildTemplates => Set<BuildTemplateEntity>();

    public DbSet<BuildJobEntity> BuildJobs => Set<BuildJobEntity>();

    public DbSet<BuildEventEntity> BuildEvents => Set<BuildEventEntity>();

    public DbSet<VersionEntity> Versions => Set<VersionEntity>();

    public DbSet<ArtifactEntity> Artifacts => Set<ArtifactEntity>();

    public DbSet<InstallationEntity> Installations => Set<InstallationEntity>();

    public DbSet<IntegrityCheckEntity> IntegrityChecks => Set<IntegrityCheckEntity>();

    public DbSet<AgentNodeEntity> AgentNodes => Set<AgentNodeEntity>();

    public DbSet<SettingEntity> Settings => Set<SettingEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<OrganizationEntity>(entity =>
        {
            entity.ToTable("organizations");
            entity.HasKey(item => item.Id);
            entity.HasIndex(item => item.Slug).IsUnique();
            entity.Property(item => item.Name).HasMaxLength(160).IsRequired();
            entity.Property(item => item.Slug).HasMaxLength(120).IsRequired();
        });

        modelBuilder.Entity<UserAccountEntity>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.DisplayName).HasMaxLength(160).IsRequired();
            entity.Property(item => item.Email).HasMaxLength(320).IsRequired();
            entity.Property(item => item.Role).HasMaxLength(80).IsRequired();
        });

        modelBuilder.Entity<ApplicationEntity>(entity =>
        {
            entity.ToTable("applications");
            entity.HasKey(item => item.Id);
            entity.HasIndex(item => new { item.OrganizationId, item.Slug }).IsUnique();
            entity.Property(item => item.Name).HasMaxLength(160).IsRequired();
            entity.Property(item => item.Slug).HasMaxLength(120).IsRequired();
        });

        modelBuilder.Entity<ModuleEntity>(entity =>
        {
            entity.ToTable("modules");
            entity.HasKey(item => item.Id);
            entity.HasIndex(item => new { item.ApplicationId, item.Name }).IsUnique();
            entity.Property(item => item.Name).HasMaxLength(160).IsRequired();
            entity.Property(item => item.ExecutableName).HasMaxLength(260).IsRequired();
            entity.Property(item => item.InstallPath).IsRequired();
        });

        modelBuilder.Entity<RepositoryEntity>(entity =>
        {
            entity.ToTable("repositories");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Name).HasMaxLength(160).IsRequired();
            entity.Property(item => item.RemoteUrl).IsRequired();
            entity.Property(item => item.DefaultBranch).HasMaxLength(160).IsRequired();
            entity.Property(item => item.Technology).HasConversion<string>().HasMaxLength(80).IsRequired();
            entity.Property(item => item.ProjectPath).IsRequired();
        });

        modelBuilder.Entity<BuildTemplateEntity>(entity =>
        {
            entity.ToTable("build_templates");
            entity.HasKey(item => item.Id);
            entity.HasIndex(item => item.Technology);
            entity.Property(item => item.Technology).HasConversion<string>().HasMaxLength(80).IsRequired();
            entity.Property(item => item.Name).HasMaxLength(160).IsRequired();
            entity.Property(item => item.ScriptPath).IsRequired();
            entity.Property(item => item.Description).IsRequired();
        });

        modelBuilder.Entity<BuildJobEntity>(entity =>
        {
            entity.ToTable("build_jobs");
            entity.HasKey(item => item.Id);
            entity.HasIndex(item => new { item.OrganizationId, item.RepositoryId, item.ModuleId, item.Status });
            entity.Property(item => item.Status).HasConversion<string>().HasMaxLength(40).IsRequired();
            entity.Property(item => item.RequestedBy).HasMaxLength(160).IsRequired();
            entity.Property(item => item.RequestedVersion).HasMaxLength(64);
            entity.Property(item => item.RequestedSha).HasMaxLength(64);
        });

        modelBuilder.Entity<BuildEventEntity>(entity =>
        {
            entity.ToTable("build_events");
            entity.HasKey(item => item.Id);
            entity.HasIndex(item => item.BuildJobId);
            entity.Property(item => item.Level).HasConversion<string>().HasMaxLength(40).IsRequired();
            entity.Property(item => item.Message).IsRequired();
        });

        modelBuilder.Entity<VersionEntity>(entity =>
        {
            entity.ToTable("versions");
            entity.HasKey(item => item.Id);
            entity.HasIndex(item => new { item.ModuleId, item.Version, item.GitSha }).IsUnique();
            entity.Property(item => item.Version).HasMaxLength(64).IsRequired();
            entity.Property(item => item.GitSha).HasMaxLength(64).IsRequired();
            entity.Property(item => item.Changelog).IsRequired();
        });

        modelBuilder.Entity<ArtifactEntity>(entity =>
        {
            entity.ToTable("artifacts");
            entity.HasKey(item => item.Id);
            entity.HasIndex(item => item.VersionId);
            entity.Property(item => item.FileName).HasMaxLength(260).IsRequired();
            entity.Property(item => item.RelativePath).IsRequired();
            entity.Property(item => item.Sha256).HasMaxLength(64).IsRequired();
        });

        modelBuilder.Entity<InstallationEntity>(entity =>
        {
            entity.ToTable("installations");
            entity.HasKey(item => item.Id);
            entity.HasIndex(item => new { item.OrganizationId, item.ModuleId, item.MachineName });
            entity.Property(item => item.MachineName).HasMaxLength(160).IsRequired();
            entity.Property(item => item.InstalledVersion).HasMaxLength(64).IsRequired();
            entity.Property(item => item.CurrentSha).HasMaxLength(64);
        });

        modelBuilder.Entity<IntegrityCheckEntity>(entity =>
        {
            entity.ToTable("integrity_checks");
            entity.HasKey(item => item.Id);
            entity.HasIndex(item => item.InstallationId);
            entity.Property(item => item.Details).IsRequired();
        });

        modelBuilder.Entity<AgentNodeEntity>(entity =>
        {
            entity.ToTable("agent_nodes");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Name).HasMaxLength(160).IsRequired();
            entity.Property(item => item.MachineName).HasMaxLength(160).IsRequired();
            entity.Property(item => item.Version).HasMaxLength(64).IsRequired();
        });

        modelBuilder.Entity<SettingEntity>(entity =>
        {
            entity.ToTable("settings");
            entity.HasKey(item => item.Key);
            entity.Property(item => item.Key).HasMaxLength(160);
            entity.Property(item => item.Value).IsRequired();
        });

        SeedBuildTemplates(modelBuilder);
    }

    private static void SeedBuildTemplates(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BuildTemplateEntity>().HasData(
            new BuildTemplateEntity { Id = Guid.Parse("11111111-1111-1111-1111-111111111111"), Name = "MSBuild classic", Technology = BuildTechnology.MsBuildClassic, ScriptPath = "recipes/msbuild-classic.ps1", Description = "Builds legacy Visual Studio solutions.", IsEnabled = true },
            new BuildTemplateEntity { Id = Guid.Parse("22222222-2222-2222-2222-222222222222"), Name = ".NET SDK", Technology = BuildTechnology.DotNetSdk, ScriptPath = "recipes/dotnet-sdk.ps1", Description = "Builds SDK-style projects.", IsEnabled = true },
            new BuildTemplateEntity { Id = Guid.Parse("33333333-3333-3333-3333-333333333333"), Name = "C# WinForms", Technology = BuildTechnology.CSharpWinForms, ScriptPath = "recipes/csharp-winforms.ps1", Description = "Builds C# Windows Forms applications.", IsEnabled = true },
            new BuildTemplateEntity { Id = Guid.Parse("44444444-4444-4444-4444-444444444444"), Name = "VB.NET WinForms", Technology = BuildTechnology.VbNetWinForms, ScriptPath = "recipes/vbnet-winforms.ps1", Description = "Builds VB.NET Windows Forms applications.", IsEnabled = true },
            new BuildTemplateEntity { Id = Guid.Parse("55555555-5555-5555-5555-555555555555"), Name = "FoxPro", Technology = BuildTechnology.FoxPro, ScriptPath = "recipes/foxpro.ps1", Description = "Runs a configurable FoxPro build command.", IsEnabled = true },
            new BuildTemplateEntity { Id = Guid.Parse("66666666-6666-6666-6666-666666666666"), Name = "Custom command", Technology = BuildTechnology.CustomCommand, ScriptPath = "recipes/custom-command.ps1", Description = "Runs a custom build command.", IsEnabled = true });
    }
}
