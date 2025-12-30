using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Pomelo.EntityFrameworkCore.MySql.Scaffolding.Internal;
using lccnet_competition.Models;

namespace lccnet_competition.Data;

public partial class Lccnet20251124PythonContext : DbContext
{
    public Lccnet20251124PythonContext()
    {
    }

    public Lccnet20251124PythonContext(DbContextOptions<Lccnet20251124PythonContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Account> Accounts { get; set; }

    public virtual DbSet<Efmigrationshistory> Efmigrationshistories { get; set; }

    public virtual DbSet<EnvReversation> EnvReversations { get; set; }

    public virtual DbSet<SubmissionRecord> SubmissionRecords { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .UseCollation("utf8mb4_0900_ai_ci")
            .HasCharSet("utf8mb4");

        modelBuilder.Entity<Account>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.ToTable("account");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreateDatetime)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime")
                .HasColumnName("create_datetime");
            entity.Property(e => e.EnvUrl)
                .HasColumnType("text")
                .HasColumnName("env_url");
            entity.Property(e => e.Role)
                .HasMaxLength(45)
                .HasDefaultValueSql("'User'")
                .HasColumnName("role");
            entity.Property(e => e.Sha256)
                .HasMaxLength(256)
                .HasColumnName("sha256");
            entity.Property(e => e.UpdateDatetime)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime")
                .HasColumnName("update_datetime");
            entity.Property(e => e.Username)
                .HasMaxLength(45)
                .HasColumnName("username");
        });

        modelBuilder.Entity<Efmigrationshistory>(entity =>
        {
            entity.HasKey(e => e.MigrationId);

            entity.ToTable("__efmigrationshistory");

            entity.Property(e => e.MigrationId).HasMaxLength(150);
            entity.Property(e => e.ProductVersion).HasMaxLength(32);
        });

        modelBuilder.Entity<EnvReversation>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.ToTable("env_reversation");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AccountId).HasColumnName("account_id");
            entity.Property(e => e.BookEndDatetime)
                .HasColumnType("datetime")
                .HasColumnName("book_end_datetime");
            entity.Property(e => e.BookStartDatetime)
                .HasColumnType("datetime")
                .HasColumnName("book_start_datetime");
            entity.Property(e => e.CreateDatetime)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime")
                .HasColumnName("create_datetime");
            entity.Property(e => e.UpdateDatetime)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime")
                .HasColumnName("update_datetime");
        });

        modelBuilder.Entity<SubmissionRecord>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("submission_record");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AccountId).HasColumnName("account_id");
            entity.Property(e => e.CreateDatetime)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime")
                .HasColumnName("create_datetime");
            entity.Property(e => e.FileName)
                .HasMaxLength(45)
                .HasColumnName("file_name");
            entity.Property(e => e.IsSuccess).HasColumnName("is_success");
            entity.Property(e => e.Score)
                .HasPrecision(5, 2)
                .HasColumnName("score");
            entity.Property(e => e.UpdateDatetime)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime")
                .HasColumnName("update_datetime");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
