using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Paga.Domain.Entities;

namespace Paga.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for the <see cref="ExpenseType"/> entity.
/// </summary>
public class ExpenseTypeConfiguration : IEntityTypeConfiguration<ExpenseType>
{
    public void Configure(EntityTypeBuilder<ExpenseType> builder)
    {
        builder.HasKey(et => et.Id);

        builder.Property(et => et.Id)
            .UseIdentityColumn();

        builder.Property(et => et.UserId)
            .IsRequired();

        builder.Property(et => et.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(et => new { et.UserId, et.Name })
            .IsUnique();

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(et => et.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
