﻿using System;
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
        pollEntity.HasIndex(poll => poll.Time);
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
        messageEntity.HasIndex(message => new { message.ChatId, MesssageId = message.MessageId });
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
        SqlServerIndexBuilderExtensions.IncludeProperties(settingsEntity.HasIndex(settings => settings.Chat), nameof(Settings.Format));
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
      
      modelBuilder.Entity<Notification>(builder =>
      {
        builder.ToTable("Notifications");
        builder.HasKey(notification => new { notification.PollId, notification.ChatId, notification.Type });
        builder.HasOne(notification => notification.Poll)
          .WithMany(poll => poll.Notifications)
          .HasForeignKey(notification => notification.PollId);
      });
      
      modelBuilder.Entity<ReplyNotification>(builder =>
      {
        builder.ToTable("ReplyNotifications");
        builder.HasKey(notification => new { notification.ChatId, notification.FromChatId, notification.FromMessageId });
        builder.HasIndex(notification => notification.MessageId);
        builder.HasOne(notification => notification.Poll)
          .WithMany()
          .HasForeignKey(notification => notification.PollId);
      });
     
      modelBuilder.Entity<UserSettings>(builder =>
      {
        builder.ToTable("UserSettings");
        builder.HasKey(settings => settings.UserId);
        builder.Property(settings => settings.UserId).ValueGeneratedNever();
        builder.Property(settings => settings.TimeZoneId).HasMaxLength(32);
        builder.Property(p => p.Lat).HasColumnType("decimal(18,15)");
        builder.Property(p => p.Lon).HasColumnType("decimal(18,15)");
      });
      
      modelBuilder.Entity<Friendship>(builder =>
      {
        builder.ToTable("Friendship");
        builder.HasKey(friendship => new { friendship.Id, friendship.FriendId });
        builder.HasIndex(friendship => friendship.PollId);
      });

      modelBuilder.Entity<TimeZoneSettings>(builder =>
      {
        builder.ToTable(nameof(TimeZoneSettings));
        builder.HasKey(settings => settings.Id);
        builder.HasIndex(settings => settings.ChatId);
      });

      modelBuilder.Entity<VoteLimit>(builder =>
      {
        builder.ToTable("VoteLimits");
        builder.HasKey(vl => new { vl.PollId, vl.Vote });
        builder
          .HasOne<Poll>()
          .WithMany(poll => poll.Limits)
          .HasForeignKey(limit => limit.PollId);
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