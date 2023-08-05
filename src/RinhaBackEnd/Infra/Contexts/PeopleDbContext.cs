using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.Extensions.Configuration;
using RinhaBackEnd.Domain;

namespace RinhaBackEnd.Infra.Contexts;

public class PeopleDbContext : DbContext
{
    public DbSet<Person> People { get; set; }
    public DbSet<Stack> Stacks { get; set; }
    public DbSet<PersonStack> PersonStacks { get; set; }

    public PeopleDbContext([NotNull] DbContextOptions<PeopleDbContext> options) : base(options)
    {

    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);
        if(Database.IsNpgsql())
        {
            configurationBuilder.Properties<DateTime>().HaveConversion(typeof(DateTimeToDateTimeUtc));
        }

    }
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Person>(entity =>
        {
            entity.ToTable("People");
            entity.HasKey(p => p.Id);
            entity.HasIndex(p => p.Apelido).IsUnique();
        });

        builder.Entity<Stack>(entity =>
        {
            entity.ToTable("Stacks");
            entity.HasKey(p => p.Id);
        });

        builder.Entity<PersonStack>(entity =>
        {
            entity.ToTable("PersonStacks");

            entity.HasKey(p => new { p.StackId, p.PersonId });

            entity.HasOne(p => p.Person)
                  .WithMany(p => p.PersonStacks)
                  .HasForeignKey(p => p.PersonId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(p => p.Stack)
                  .WithMany(p => p.PersonStacks)
                  .HasForeignKey(p => p.StackId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Ignore<Notification>()
               .Ignore<Notifiable<Notification>>();
    }
}


public class DateTimeToDateTimeUtc : ValueConverter<DateTime, DateTime>
{
    public DateTimeToDateTimeUtc() : base(c => DateTime.SpecifyKind(c, DateTimeKind.Local).ToUniversalTime(), c => c.ToLocalTime())
    {

    }
}