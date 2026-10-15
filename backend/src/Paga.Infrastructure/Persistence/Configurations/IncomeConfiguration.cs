using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Paga.Domain.Entities;
using Paga.Infrastructure.Persistence.Converters;

namespace Paga.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for the <see cref="Income"/> entity.
/// </summary>
public class IncomeConfiguration : IEntityTypeConfiguration<Income>
{
    public void Configure(EntityTypeBuilder<Income> builder)
    {
        builder.HasKey(i => i.Id);

        builder.Property(i => i.Id)
            .UseIdentityColumn();

        builder.Property(i => i.UserId)
            .IsRequired();

        builder.Property(i => i.Date)
            .IsRequired();

        builder.Property(i => i.Description)
            .IsRequired()
            .HasMaxLength(300);

        builder.Property(i => i.Value)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(i => i.IsRecurring)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(i => i.Frequency)
            .HasConversion(new RecurrenceFrequencyConverter())
            .HasMaxLength(10);

        builder.Property(i => i.IsGenerated)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(i => i.LastGeneratedDate);

        builder.Property(i => i.SourceEntryId);

        builder.HasIndex(i => new { i.UserId, i.Date });

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(i => i.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Self-referential FK for recurring generation chain
        builder.HasOne(i => i.SourceEntry)
            .WithMany()
            .HasForeignKey(i => i.SourceEntryId)
            .OnDelete(DeleteBehavior.Restrict);

        // Unique index for idempotency: same source + same date = duplicate
        builder.HasIndex(i => new { i.SourceEntryId, i.Date })
            .HasFilter("source_entry_id IS NOT NULL")
            .IsUnique();
    }
}
