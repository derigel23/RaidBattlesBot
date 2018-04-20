using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace RaidBattlesBot.Model
{
  public class RaidBattlesContext : DbContext
  {
    public DbSet<Raid> Raids { get; set; }
    public DbSet<Poll> Polls { get; set; }
    public DbSet<PollMessage> Messages { get; set; }
    public DbSet<Vote> Votes { get; set; }
    public DbSet<Settings> Settings { get; set; }

    public RaidBattlesContext(DbContextOptions<RaidBattlesContext> options)
      : base(options) { }

    public const int PollIdSeed = 10100;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
      var raidEntity = modelBuilder.Entity<Raid>();
      raidEntity.HasKey(raid => raid.Id);
      raidEntity.HasMany(raid => raid.Polls).WithOne(poll => poll.Raid).HasForeignKey(poll => poll.RaidId);
      raidEntity.HasOne(raid => raid.EggRaid).WithOne(raid => raid.PostEggRaid).HasForeignKey<Raid>(raid => raid.EggRaidId);
      raidEntity.Property(p => p.Lat).HasColumnType("decimal(18,15)");
      raidEntity.Property(p => p.Lon).HasColumnType("decimal(18,15)");

      modelBuilder.HasSequence<int>("PollId").StartsAt(PollIdSeed).IncrementsBy(VoteEnumEx.AllowedVoteFormats.Length);
      
      var pollEntity = modelBuilder.Entity<Poll>();
      pollEntity.Property(poll => poll.Id).HasDefaultValueSql("NEXT VALUE FOR PollId");
      pollEntity.HasIndex(poll => poll.Id).IsUnique();
      pollEntity.HasMany(poll => poll.Messages).WithOne(message => message.Poll).HasForeignKey(message => message.PollId);
      pollEntity.HasMany(poll => poll.Votes).WithOne().HasForeignKey(vote => vote.PollId);

      var messageEntity = modelBuilder.Entity<PollMessage>();
      messageEntity.HasKey(message => message.Id);
      messageEntity.Ignore(message => message.Chat);

      var voteEntity = modelBuilder.Entity<Vote>();
      voteEntity.HasKey(vote => new { vote.PollId, User = vote.UserId });
      voteEntity.Ignore(vote => vote.User);

      var settingsEntity = modelBuilder.Entity<Settings>();
      settingsEntity.ToTable("Settings");
      settingsEntity.HasKey(vote => vote.Chat);
      settingsEntity.Property(vote => vote.Chat).ValueGeneratedNever();
    }

    public override int SaveChanges()
    {
      SetLastModifiedDate();
      return base.SaveChanges();
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
      SetLastModifiedDate();
      return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = new CancellationToken())
    {
      SetLastModifiedDate();
      return base.SaveChangesAsync(cancellationToken);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = new CancellationToken())
    {
      SetLastModifiedDate();
      return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void SetLastModifiedDate()
    {
      var utcNow = DateTimeOffset.UtcNow;
      foreach (var entry in ChangeTracker.Entries<ITrackable>())
      {
        switch (entry.State)
        {
          case EntityState.Added:
          case EntityState.Modified:
            entry.Entity.Modified = utcNow;
            break;
        }
      }
    }
  }
}