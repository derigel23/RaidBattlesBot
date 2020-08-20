﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using RaidBattlesBot.Model;

namespace RaidBattlesBot.Migrations
{
    [DbContext(typeof(RaidBattlesContext))]
    [Migration("20200820173914_Notifications")]
    partial class Notifications
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "3.1.6")
                .HasAnnotation("Relational:MaxIdentifierLength", 128)
                .HasAnnotation("Relational:Sequence:.PollId", "'PollId', '', '10000000', '1', '', '', 'Int32', 'False'")
                .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

            modelBuilder.Entity("RaidBattlesBot.Model.Notification", b =>
                {
                    b.Property<int>("UserId")
                        .HasColumnType("int");

                    b.Property<DateTimeOffset?>("Modified")
                        .HasColumnType("datetimeoffset");

                    b.Property<byte>("Type")
                        .HasColumnType("tinyint");

                    b.HasKey("UserId");

                    b.ToTable("Notifications");
                });

            modelBuilder.Entity("RaidBattlesBot.Model.Player", b =>
                {
                    b.Property<int>("UserId")
                        .HasColumnType("int");

                    b.Property<DateTimeOffset?>("Modified")
                        .HasColumnType("datetimeoffset");

                    b.Property<string>("Nickname")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("UserId");

                    b.ToTable("Players");
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

                    b.Property<string>("Title")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.HasIndex("Id")
                        .IsUnique();

                    b.HasIndex("PortalId");

                    b.HasIndex("RaidId");

                    b.ToTable("Polls");
                });

            modelBuilder.Entity("RaidBattlesBot.Model.PollMessage", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<long?>("ChatId")
                        .HasColumnType("bigint");

                    b.Property<int?>("ChatType")
                        .HasColumnType("int");

                    b.Property<string>("InlineMesssageId")
                        .HasColumnType("nvarchar(max)");

                    b.Property<int?>("MesssageId")
                        .HasColumnType("int");

                    b.Property<DateTimeOffset?>("Modified")
                        .HasColumnType("datetimeoffset");

                    b.Property<int>("PollId")
                        .HasColumnType("int");

                    b.Property<byte?>("PollMode")
                        .HasColumnType("tinyint");

                    b.Property<int?>("UserId")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("PollId");

                    b.HasIndex("ChatId", "MesssageId");

                    b.ToTable("Messages");
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

                    b.ToTable("Portals");
                });

            modelBuilder.Entity("RaidBattlesBot.Model.Raid", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

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

                    b.ToTable("Raids");
                });

            modelBuilder.Entity("RaidBattlesBot.Model.Settings", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<long>("Chat")
                        .HasColumnType("bigint");

                    b.Property<int>("Format")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasDefaultValue(329489);

                    b.Property<DateTimeOffset?>("Modified")
                        .HasColumnType("datetimeoffset");

                    b.Property<int>("Order")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("Chat")
                        .HasAnnotation("SqlServer:Include", new[] { "Format" });

                    b.ToTable("Settings");
                });

            modelBuilder.Entity("RaidBattlesBot.Model.Vote", b =>
                {
                    b.Property<int>("PollId")
                        .HasColumnType("int");

                    b.Property<int>("UserId")
                        .HasColumnType("int");

                    b.Property<string>("FirstName")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("LasttName")
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTimeOffset?>("Modified")
                        .HasColumnType("datetimeoffset");

                    b.Property<int?>("Team")
                        .HasColumnType("int");

                    b.Property<string>("Username")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("PollId", "UserId");

                    b.ToTable("Votes");
                });

            modelBuilder.Entity("RaidBattlesBot.Model.Poll", b =>
                {
                    b.HasOne("RaidBattlesBot.Model.Portal", "Portal")
                        .WithMany()
                        .HasForeignKey("PortalId");

                    b.HasOne("RaidBattlesBot.Model.Raid", "Raid")
                        .WithMany("Polls")
                        .HasForeignKey("RaidId");
                });

            modelBuilder.Entity("RaidBattlesBot.Model.PollMessage", b =>
                {
                    b.HasOne("RaidBattlesBot.Model.Poll", "Poll")
                        .WithMany("Messages")
                        .HasForeignKey("PollId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("RaidBattlesBot.Model.Raid", b =>
                {
                    b.HasOne("RaidBattlesBot.Model.Raid", "EggRaid")
                        .WithOne("PostEggRaid")
                        .HasForeignKey("RaidBattlesBot.Model.Raid", "EggRaidId");
                });

            modelBuilder.Entity("RaidBattlesBot.Model.Vote", b =>
                {
                    b.HasOne("RaidBattlesBot.Model.Poll", null)
                        .WithMany("Votes")
                        .HasForeignKey("PollId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });
#pragma warning restore 612, 618
        }
    }
}
