﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using RaidBattlesBot.Model;

#nullable disable

namespace RaidBattlesBot.Migrations
{
    [DbContext(typeof(RaidBattlesContext))]
    partial class RaidBattlesContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "6.0.0-rc.2.21480.5")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder, 1L, 1);

            modelBuilder.HasSequence<int>("PollId")
                .StartsAt(10000000L);

            modelBuilder.Entity("RaidBattlesBot.Model.Friendship", b =>
                {
                    b.Property<long>("Id")
                        .HasColumnType("bigint");

                    b.Property<long>("FriendId")
                        .HasColumnType("bigint");

                    b.Property<DateTimeOffset?>("Modified")
                        .HasColumnType("datetimeoffset");

                    b.Property<int?>("PollId")
                        .HasColumnType("int");

                    b.Property<int>("Type")
                        .HasColumnType("int");

                    b.HasKey("Id", "FriendId");

                    b.HasIndex("PollId");

                    b.ToTable("Friendship", (string)null);
                });

            modelBuilder.Entity("RaidBattlesBot.Model.Notification", b =>
                {
                    b.Property<int>("PollId")
                        .HasColumnType("int");

                    b.Property<long>("ChatId")
                        .HasColumnType("bigint");

                    b.Property<byte>("Type")
                        .HasColumnType("tinyint");

                    b.Property<long?>("BotId")
                        .HasColumnType("bigint");

                    b.Property<DateTimeOffset?>("DateTime")
                        .HasColumnType("datetimeoffset");

                    b.Property<long?>("MessageId")
                        .HasColumnType("bigint");

                    b.HasKey("PollId", "ChatId", "Type");

                    b.ToTable("Notifications", (string)null);
                });

            modelBuilder.Entity("RaidBattlesBot.Model.Player", b =>
                {
                    b.Property<long>("UserId")
                        .HasColumnType("bigint");

                    b.Property<bool?>("AutoApproveFriendship")
                        .HasColumnType("bit");

                    b.Property<long?>("FriendCode")
                        .HasColumnType("bigint");

                    b.Property<DateTimeOffset?>("Modified")
                        .HasColumnType("datetimeoffset");

                    b.Property<string>("Nickname")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("UserId");

                    b.ToTable("Players", (string)null);
                });

            modelBuilder.Entity("RaidBattlesBot.Model.Poll", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasDefaultValueSql("NEXT VALUE FOR PollId");

                    b.Property<int?>("AllowedVotes")
                        .HasColumnType("int");

                    b.Property<bool>("Cancelled")
                        .HasColumnType("bit");

                    b.Property<bool>("ExRaidGym")
                        .HasColumnType("bit");

                    b.Property<DateTimeOffset?>("Modified")
                        .HasColumnType("datetimeoffset");

                    b.Property<long?>("Owner")
                        .HasColumnType("bigint");

                    b.Property<string>("PortalId")
                        .HasColumnType("nvarchar(450)");

                    b.Property<int?>("RaidId")
                        .HasColumnType("int");

                    b.Property<DateTimeOffset?>("Time")
                        .HasColumnType("datetimeoffset");

                    b.Property<string>("TimeZoneId")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Title")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.HasIndex("Id")
                        .IsUnique();

                    b.HasIndex("PortalId");

                    b.HasIndex("RaidId");

                    b.HasIndex("Time");

                    b.ToTable("Polls", (string)null);
                });

            modelBuilder.Entity("RaidBattlesBot.Model.PollMessage", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"), 1L, 1);

                    b.Property<long?>("BotId")
                        .HasColumnType("bigint");

                    b.Property<long?>("ChatId")
                        .HasColumnType("bigint");

                    b.Property<int?>("ChatType")
                        .HasColumnType("int");

                    b.Property<string>("InlineMessageId")
                        .HasColumnType("nvarchar(max)");

                    b.Property<int?>("MessageId")
                        .HasColumnType("int");

                    b.Property<DateTimeOffset?>("Modified")
                        .HasColumnType("datetimeoffset");

                    b.Property<int>("PollId")
                        .HasColumnType("int");

                    b.Property<byte?>("PollMode")
                        .HasColumnType("tinyint");

                    b.Property<long?>("UserId")
                        .HasColumnType("bigint");

                    b.HasKey("Id");

                    b.HasIndex("PollId");

                    b.HasIndex("ChatId", "MessageId");

                    b.ToTable("Messages", (string)null);
                });

            modelBuilder.Entity("RaidBattlesBot.Model.Portal", b =>
                {
                    b.Property<string>("Guid")
                        .HasColumnType("nvarchar(450)");

                    b.Property<string>("Address")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Image")
                        .HasColumnType("nvarchar(max)");

                    b.Property<decimal>("Latitude")
                        .HasColumnType("decimal(18,15)");

                    b.Property<decimal>("Longitude")
                        .HasColumnType("decimal(18,15)");

                    b.Property<DateTimeOffset?>("Modified")
                        .HasColumnType("datetimeoffset");

                    b.Property<string>("Name")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Guid");

                    b.ToTable("Portals", (string)null);
                });

            modelBuilder.Entity("RaidBattlesBot.Model.Raid", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"), 1L, 1);

                    b.Property<string>("Description")
                        .HasColumnType("nvarchar(max)");

                    b.Property<int?>("EggRaidId")
                        .HasColumnType("int");

                    b.Property<DateTimeOffset?>("EndTime")
                        .HasColumnType("datetimeoffset");

                    b.Property<string>("Gym")
                        .HasColumnType("nvarchar(max)");

                    b.Property<decimal?>("Lat")
                        .HasColumnType("decimal(18,15)");

                    b.Property<decimal?>("Lon")
                        .HasColumnType("decimal(18,15)");

                    b.Property<DateTimeOffset?>("Modified")
                        .HasColumnType("datetimeoffset");

                    b.Property<string>("Move1")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Move2")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Name")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("NearByAddress")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("NearByPlaceId")
                        .HasColumnType("nvarchar(max)");

                    b.Property<int?>("Pokemon")
                        .HasColumnType("int");

                    b.Property<string>("PossibleGym")
                        .HasColumnType("nvarchar(max)");

                    b.Property<int?>("RaidBossLevel")
                        .HasColumnType("int");

                    b.Property<DateTimeOffset?>("StartTime")
                        .HasColumnType("datetimeoffset");

                    b.Property<string>("Title")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.HasIndex("EggRaidId")
                        .IsUnique()
                        .HasFilter("[EggRaidId] IS NOT NULL");

                    b.ToTable("Raids", (string)null);
                });

            modelBuilder.Entity("RaidBattlesBot.Model.ReplyNotification", b =>
                {
                    b.Property<long>("ChatId")
                        .HasColumnType("bigint");

                    b.Property<int>("MessageId")
                        .HasColumnType("int");

                    b.Property<long?>("BotId")
                        .HasColumnType("bigint");

                    b.Property<long>("FromChatId")
                        .HasColumnType("bigint");

                    b.Property<int>("FromMessageId")
                        .HasColumnType("int");

                    b.Property<long?>("FromUserId")
                        .HasColumnType("bigint");

                    b.Property<DateTimeOffset?>("Modified")
                        .HasColumnType("datetimeoffset");

                    b.Property<int>("PollId")
                        .HasColumnType("int");

                    b.HasKey("ChatId", "MessageId");

                    b.HasIndex("PollId");

                    b.ToTable("ReplyNotifications", (string)null);
                });

            modelBuilder.Entity("RaidBattlesBot.Model.Settings", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"), 1L, 1);

                    b.Property<long>("Chat")
                        .HasColumnType("bigint");

                    b.Property<int>("Format")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasDefaultValue(71108369);

                    b.Property<DateTimeOffset?>("Modified")
                        .HasColumnType("datetimeoffset");

                    b.Property<int>("Order")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("Chat");

                    SqlServerIndexBuilderExtensions.IncludeProperties(b.HasIndex("Chat"), new[] { "Format" });

                    b.ToTable("Settings", (string)null);
                });

            modelBuilder.Entity("RaidBattlesBot.Model.TimeZoneSettings", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"), 1L, 1);

                    b.Property<long>("ChatId")
                        .HasColumnType("bigint");

                    b.Property<string>("TimeZone")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.HasIndex("ChatId");

                    b.ToTable("TimeZoneSettings", (string)null);
                });

            modelBuilder.Entity("RaidBattlesBot.Model.UserSettings", b =>
                {
                    b.Property<long>("UserId")
                        .HasColumnType("bigint");

                    b.Property<decimal?>("Lat")
                        .HasColumnType("decimal(18,15)");

                    b.Property<decimal?>("Lon")
                        .HasColumnType("decimal(18,15)");

                    b.Property<string>("TimeZoneId")
                        .HasMaxLength(32)
                        .HasColumnType("nvarchar(32)");

                    b.HasKey("UserId");

                    b.ToTable("UserSettings", (string)null);
                });

            modelBuilder.Entity("RaidBattlesBot.Model.Vote", b =>
                {
                    b.Property<int>("PollId")
                        .HasColumnType("int");

                    b.Property<long>("UserId")
                        .HasColumnType("bigint");

                    b.Property<long?>("BotId")
                        .HasColumnType("bigint");

                    b.Property<string>("FirstName")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("LastName")
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTimeOffset?>("Modified")
                        .HasColumnType("datetimeoffset");

                    b.Property<int?>("Team")
                        .HasColumnType("int");

                    b.Property<string>("Username")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("PollId", "UserId");

                    b.HasIndex("PollId");

                    b.HasIndex("UserId");

                    b.ToTable("Votes", (string)null);
                });

            modelBuilder.Entity("RaidBattlesBot.Model.Notification", b =>
                {
                    b.HasOne("RaidBattlesBot.Model.Poll", "Poll")
                        .WithMany("Notifications")
                        .HasForeignKey("PollId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Poll");
                });

            modelBuilder.Entity("RaidBattlesBot.Model.Poll", b =>
                {
                    b.HasOne("RaidBattlesBot.Model.Portal", "Portal")
                        .WithMany()
                        .HasForeignKey("PortalId");

                    b.HasOne("RaidBattlesBot.Model.Raid", "Raid")
                        .WithMany("Polls")
                        .HasForeignKey("RaidId");

                    b.Navigation("Portal");

                    b.Navigation("Raid");
                });

            modelBuilder.Entity("RaidBattlesBot.Model.PollMessage", b =>
                {
                    b.HasOne("RaidBattlesBot.Model.Poll", "Poll")
                        .WithMany("Messages")
                        .HasForeignKey("PollId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Poll");
                });

            modelBuilder.Entity("RaidBattlesBot.Model.Raid", b =>
                {
                    b.HasOne("RaidBattlesBot.Model.Raid", "EggRaid")
                        .WithOne("PostEggRaid")
                        .HasForeignKey("RaidBattlesBot.Model.Raid", "EggRaidId");

                    b.Navigation("EggRaid");
                });

            modelBuilder.Entity("RaidBattlesBot.Model.ReplyNotification", b =>
                {
                    b.HasOne("RaidBattlesBot.Model.Poll", "Poll")
                        .WithMany()
                        .HasForeignKey("PollId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Poll");
                });

            modelBuilder.Entity("RaidBattlesBot.Model.Vote", b =>
                {
                    b.HasOne("RaidBattlesBot.Model.Poll", null)
                        .WithMany("Votes")
                        .HasForeignKey("PollId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("RaidBattlesBot.Model.Poll", b =>
                {
                    b.Navigation("Messages");

                    b.Navigation("Notifications");

                    b.Navigation("Votes");
                });

            modelBuilder.Entity("RaidBattlesBot.Model.Raid", b =>
                {
                    b.Navigation("Polls");

                    b.Navigation("PostEggRaid");
                });
#pragma warning restore 612, 618
        }
    }
}
