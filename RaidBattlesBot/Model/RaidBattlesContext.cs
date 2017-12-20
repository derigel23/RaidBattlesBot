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

    public RaidBattlesContext(DbContextOptions<RaidBattlesContext> options)
      : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
      var raidEntity = modelBuilder.Entity<Raid>();
      raidEntity.HasKey(raid => raid.Id);
      raidEntity.HasMany(raid => raid.Polls).WithOne(poll => poll.Raid).HasForeignKey(poll => poll.RaidId);

      var pollEntity = modelBuilder.Entity<Poll>();
      pollEntity.HasKey(poll => poll.Id);
      pollEntity.HasMany(poll => poll.Messages).WithOne(message => message.Poll).HasForeignKey(message => message.PollId);
      pollEntity.HasMany(poll => poll.Votes).WithOne().HasForeignKey(vote => vote.PollId);

      var messageEntity = modelBuilder.Entity<PollMessage>();
      messageEntity.HasKey(message => message.Id);
      //messageEntity.HasIndex(message => new { Chat = message.ChatId, message.MesssageId, message.InlineMesssageId });
      messageEntity.Ignore(message => message.Chat);

      var voteEntity = modelBuilder.Entity<Vote>();
      voteEntity.HasKey(vote => new { vote.PollId, User = vote.UserId });
      voteEntity.Ignore(vote => vote.User);
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