using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyBooksKanban.Domain.Entities;

namespace MyBooksKanban.Infrastructure.Configurations;

public class BookConfiguration : IEntityTypeConfiguration<Book>
{
    public void Configure(EntityTypeBuilder<Book> builder)
    {
        builder.ToTable("Books");

        builder.HasKey(b => b.Id);
        builder.Property(b => b.Id).ValueGeneratedOnAdd();

        builder.Property(b => b.Title).IsRequired().HasMaxLength(150);
        builder.Property(b => b.Author).IsRequired().HasMaxLength(120);
        builder.Property(b => b.Description).HasMaxLength(2000);
        builder.Property(b => b.CoverPath).HasMaxLength(300);
        builder.Property(b => b.Isbn).HasMaxLength(20);

        builder.Property(b => b.ReadingStatus)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(b => b.Position).IsRequired();
        builder.Property(b => b.CreatedAt).IsRequired();
        builder.Property(b => b.UpdatedAt).IsRequired();

        builder.HasOne(b => b.Category)
            .WithMany(c => c.Books)
            .HasForeignKey(b => b.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(b => b.Title);
        builder.HasIndex(b => b.ReadingStatus);
        builder.HasIndex(b => new { b.ReadingStatus, b.Position });
        builder.HasIndex(b => b.CategoryId);
    }
}
