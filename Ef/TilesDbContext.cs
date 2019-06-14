using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace GeoMapDownloader
{
    public partial class TilesDbContext : DbContext
    {
        public TilesDbContext()
        {
        }

        public TilesDbContext(DbContextOptions<TilesDbContext> options)
            : base(options)
        {
        }

        public virtual DbSet<Tiles> Tiles { get; set; }
        public virtual DbSet<TilesData> TilesData { get; set; }
        public virtual DbSet<CacheUrl> CacheUrl { get; set; }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlite("Datasource=wwwroot/data.sqlite");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasAnnotation("ProductVersion", "2.2.4-servicing-10062");

            modelBuilder.Entity<Tiles>(entity =>
            {
                entity.HasIndex(e => new { e.X, e.Y, e.Zoom, e.Type })
                    .HasName("IndexOfTiles");
                entity.HasIndex(e => new { e.Hash })
                    .HasName("IndexOfHash");

                // entity.Property(e => e.Id)
                //     .HasColumnName("id")
                //     .UseSqlServerIdentityColumn();


            });

            modelBuilder.Entity<TilesData>((Action<Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<TilesData>>)((Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<TilesData> entity) =>
            {
                // entity.Property(e => e.Id)
                //     .HasColumnName("id")
                //     .ValueGeneratedNever();

                entity.HasMany(d => d.Tiles)
                    .WithOne(p => p.TilesData)
                    .HasForeignKey(d => d.DataId);
            }));
        }
    }
}
