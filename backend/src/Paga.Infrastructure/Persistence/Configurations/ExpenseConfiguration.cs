using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Paga.Domain.Entities;
using Paga.Infrastructure.Persistence.Converters;

namespace Paga.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for the <see cref="Expense"/> entity.
/// </summary>
public class ExpenseConfiguration : IEntityTypeConfiguration<Expense>
{
    public void Configure(EntityTypeBuilder<Expense> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .UseIdentityColumn();

        builder.Property(e => e.UserId)
            .IsRequired();

        builder.Property(e => e.DueDate)
            .IsRequired();

        builder.Property(e => e.Description)
            .IsRequired()
            .HasMaxLength(300);

        builder.Property(e => e.ExpenseTypeId)
            .IsRequired();

        builder.Property(e => e.Value)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(e => e.IsRecurring)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(e => e.Frequency)
            .HasConversion(new RecurrenceFrequencyConverter())
            .HasMaxLength(10);

        builder.HasIndex(e => new { e.UserId, e.DueDate });

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<ExpenseType>()
            .WithMany()
            .HasForeignKey(e => e.ExpenseTypeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
