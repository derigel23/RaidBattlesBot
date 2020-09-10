using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace RaidBattlesBot.Model
{
  public partial class RaidBattlesContext : DbContext
  {
    public RaidBattlesContext(DbContextOptions<RaidBattlesContext> options)
      : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
      modelBuilder.Entity<Raid>(raidEntity =>
      {
        raidEntity.ToTable("Raids");
        raidEntity.HasKey(raid => raid.Id);
        raidEntity.HasOne(raid => raid.EggRaid).WithOne(raid => raid.PostEggRaid).HasForeignKey<Raid>(raid => raid.EggRaidId);
        raidEntity.Property(p => p.Lat).HasColumnType("decimal(18,15)");
        raidEntity.Property(p => p.Lon).HasColumnType("decimal(18,15)");
      });
      
      modelBuilder.Entity<Poll>(pollEntity =>
      {
        var pollIdSequence = modelBuilder.HasSequence<int>("PollId").StartsAt(10_000_000).IncrementsBy(1);

        pollEntity.ToTable("Polls");
        pollEntity.Property(poll => poll.Id).HasDefaultValueSql($"NEXT VALUE FOR {pollIdSequence.Metadata.Name}");
        pollEntity.HasIndex(poll => poll.Id).IsUnique();
        pollEntity.HasMany(poll => poll.Messages).WithOne(message => message.Poll).HasForeignKey(message => message.PollId);
        pollEntity.HasMany(poll => poll.Votes).WithOne().HasForeignKey(vote => vote.PollId);
        pollEntity.HasOne(poll => poll.Raid).WithMany(raid => raid.Polls).HasForeignKey(poll => poll.RaidId);
        pollEntity.HasOne(poll => poll.Portal).WithMany().HasForeignKey(poll => poll.PortalId);
      });

      modelBuilder.Entity<PollMessage>(messageEntity =>
      {
        messageEntity.ToTable("Messages");
        messageEntity.HasKey(message => message.Id);
        messageEntity.Ignore(message => message.Chat);
        messageEntity.HasIndex(message => new { message.ChatId, message.MesssageId });
      });

      modelBuilder.Entity<Vote>(voteEntity =>
      {
        voteEntity.ToTable("Votes");
        voteEntity.HasKey(vote => new {vote.PollId, User = vote.UserId});
        voteEntity.Ignore(vote => vote.User);
        voteEntity.HasIndex(vote => vote.PollId);
        voteEntity.HasIndex(vote => vote.UserId);
      });

      modelBuilder.Entity<Settings>(settingsEntity =>
      {
        settingsEntity.ToTable("Settings");
        settingsEntity.HasKey(settings => settings.Id);
        settingsEntity.HasIndex(settings => settings.Chat).IncludeProperties(nameof(Settings.Format));
        settingsEntity.Property(settings => settings.Format).HasDefaultValue(VoteEnum.Standard);
      });
      
      modelBuilder.Entity<Portal>(builder =>
      {
        builder.ToTable("Portals");
        builder.HasKey(portal => portal.Guid);
        builder.Property(portal => portal.Latitude).HasColumnType("decimal(18,15)");
        builder.Property(portal => portal.Longitude).HasColumnType("decimal(18,15)");
      });

      modelBuilder.Entity<Player>(builder =>
      {
        builder.ToTable("Players");
        builder.HasKey(player => player.UserId);
        builder.Property(player => player.UserId).ValueGeneratedNever();
      });
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
      SetLastModifiedDate();
      return base.SaveChanges(acceptAllChangesOnSuccess);
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
          
          case EntityState.Unchanged:
            if (entry.Entity.Modified == null)
            {
              entry.State = EntityState.Added;
              goto case EntityState.Added;
            }
            break;
        }
      }
    }
  }
}