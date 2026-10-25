namespace PaperFlow.Data
{
    using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore;
    using PaperFlow.Data.Models;
    public class PowerFlowDbContext : IdentityDbContext
    {
        public PowerFlowDbContext(
            DbContextOptions<PowerFlowDbContext> options)
            : base(options)
        {
        }


        public DbSet<Paper> Papers { get; set; }

        public DbSet<ResearchSource> ResearchSources { get; set; }

        public DbSet<Author> Authors { get; set; }

        public DbSet<PaperAuthor> PaperAuthors { get; set; }

        public DbSet<Category> Categories { get; set; }

        public DbSet<PaperCategory> PaperCategories { get; set; }
     

        protected override void OnModelCreating(
            ModelBuilder builder)
        {
            // Must be called for ASP.NET Core Identity
            base.OnModelCreating(builder);


            builder.Entity<Paper>(entity =>
            {
                entity.HasOne(p => p.User)
                    .WithMany()
                    .HasForeignKey(p => p.UserId)
                    .IsRequired()
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(p => p.ResearchSource)
                    .WithMany(s => s.Papers)
                    .HasForeignKey(p => p.ResearchSourceId)
                    .OnDelete(DeleteBehavior.SetNull);

                // Prevent duplicate external papers in the same user's library.

                entity.HasIndex(p => new
                {
                    p.UserId,
                    p.ResearchSourceId,
                    p.ExternalId
                })
                .IsUnique()
                .HasFilter(
                    "[ResearchSourceId] IS NOT NULL " +
                    "AND [ExternalId] IS NOT NULL");

                // A DOI can be saved once per user.

                entity.HasIndex(p => new
                {
                    p.UserId,
                    p.Doi
                })
                .IsUnique()
                .HasFilter("[Doi] IS NOT NULL");

                // CurrentPage cannot exceed TotalPages.

                entity.ToTable(t =>
                    t.HasCheckConstraint(
                        "CK_Papers_ReadingProgress",
                        "[CurrentPage] >= 0 AND " +
                        "([TotalPages] IS NULL OR " +
                        "[CurrentPage] <= [TotalPages])"));
            });

           
            builder.Entity<PaperAuthor>(entity =>
            {
                entity.HasKey(pa => new
                {
                    pa.PaperId,
                    pa.AuthorId
                });

                entity.HasOne(pa => pa.Paper)
                    .WithMany(p => p.PaperAuthors)
                    .HasForeignKey(pa => pa.PaperId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(pa => pa.Author)
                    .WithMany(a => a.PaperAuthors)
                    .HasForeignKey(pa => pa.AuthorId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<PaperCategory>(entity =>
            {
                entity.HasKey(pc => new
                {
                    pc.PaperId,
                    pc.CategoryId
                });

                entity.HasOne(pc => pc.Paper)
                    .WithMany(p => p.PaperCategories)
                    .HasForeignKey(pc => pc.PaperId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(pc => pc.Category)
                    .WithMany(c => c.PaperCategories)
                    .HasForeignKey(pc => pc.CategoryId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<Category>(entity =>
            {
                entity.HasIndex(c => c.Name)
                    .IsUnique();
            });


            builder.Entity<ResearchSource>(entity =>
            {
                entity.HasIndex(s => s.Name)
                    .IsUnique();
            });

            builder.Entity<Author>(entity =>
            {
                entity.HasIndex(a => a.Orcid)
                    .IsUnique()
                    .HasFilter("[Orcid] IS NOT NULL");
            });
        }
    }
}
