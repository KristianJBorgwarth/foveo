using Foveo.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Foveo.Infrastructure.Persistence.ModelConfigurations;

public sealed class MediaModelConfiguration : BaseEntityTypeConfiguration<Media>
{
    public override void Configure(EntityTypeBuilder<Media> builder)
    {
        builder.ToTable("media");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(x => x.OriginalFileName).HasColumnName("original_file_name").IsRequired();
        builder.Property(x => x.ContentType).HasColumnName("content_type").IsRequired();
        builder.Property(x => x.Type).HasColumnName("type").HasConversion<string>().IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").HasConversion<string>().IsRequired();
        builder.Property(x => x.StorageKey).HasColumnName("storage_key").IsRequired();
        builder.Property(x => x.ThumbnailKey).HasColumnName("thumbnail_key");
        builder.Property(x => x.DisplayKey).HasColumnName("display_key");
        builder.Property(x => x.SizeBytes).HasColumnName("size_bytes").IsRequired();
        builder.Property(x => x.UploaderName).HasColumnName("uploader_name");

        // Serves the gallery query: ready items, newest first.
        builder.HasIndex(x => new { x.Status, x.Created });

        // Soft-deleted rows never surface in queries.
        builder.HasQueryFilter(x => x.Deleted == null);

        base.Configure(builder);
    }
}
