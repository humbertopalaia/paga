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

        builder.HasIndex(i => new { i.UserId, i.Date });

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(i => i.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
