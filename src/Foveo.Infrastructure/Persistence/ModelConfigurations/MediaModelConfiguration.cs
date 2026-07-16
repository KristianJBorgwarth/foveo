using Foveo.Infrastructure.Persistence.ModelConfigurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class MediaModelConfiguration : BaseEntityTypeConfiguration<Media>
{
    public override void Configure(EntityTypeBuilder<Media> builder)
    {
        builder.ToTable("media");

        base.Configure(builder);
    }
}
