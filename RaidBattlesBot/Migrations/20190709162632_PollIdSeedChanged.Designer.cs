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
    [Migration("20190709162632_PollIdSeedChanged")]
    partial class PollIdSeedChanged
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "2.2.4-servicing-10062")
                .HasAnnotation("Relational:MaxIdentifierLength", 128)
                .HasAnnotation("Relational:Sequence:.PollId", "'PollId', '', '10000000', '1', '', '', 'Int32', 'False'")
                .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

            modelBuilder.Entity("RaidBattlesBot.Model.Poll", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasDefaultValueSql("NEXT VALUE FOR PollId");

                    b.Property<int?>("AllowedVotes");

                    b.Property<bool>("Cancelled");

                    b.Property<bool>("ExRaidGym");

                    b.Property<DateTimeOffset?>("Modified");

                    b.Property<long?>("Owner");

                    b.Property<string>("PortalId");

                    b.Property<int?>("RaidId");

                    b.Property<DateTimeOffset?>("Time");

                    b.Property<string>("Title");

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
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<long?>("ChatId");

                    b.Property<int?>("ChatType");

                    b.Property<string>("InlineMesssageId");

                    b.Property<int?>("MesssageId");

                    b.Property<DateTimeOffset?>("Modified");

                    b.Property<int>("PollId");

                    b.Property<int?>("UserId");

                    b.HasKey("Id");

                    b.HasIndex("PollId");

                    b.ToTable("Messages");
                });

            modelBuilder.Entity("RaidBattlesBot.Model.Portal", b =>
                {
                    b.Property<string>("Guid")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("Address");

                    b.Property<string>("Image");

                    b.Property<decimal>("Latitude")
                        .HasColumnType("decimal(18,15)");

                    b.Property<decimal>("Longitude")
                        .HasColumnType("decimal(18,15)");

                    b.Property<DateTimeOffset?>("Modified");

                    b.Property<string>("Name");

                    b.HasKey("Guid");

                    b.ToTable("Portals");
                });

            modelBuilder.Entity("RaidBattlesBot.Model.Raid", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<string>("Description");

                    b.Property<int?>("EggRaidId");

                    b.Property<DateTimeOffset?>("EndTime");

                    b.Property<string>("Gym");

                    b.Property<decimal?>("Lat")
                        .HasColumnType("decimal(18,15)");

                    b.Property<decimal?>("Lon")
                        .HasColumnType("decimal(18,15)");

                    b.Property<DateTimeOffset?>("Modified");

                    b.Property<string>("Move1");

                    b.Property<string>("Move2");

                    b.Property<string>("Name");

                    b.Property<string>("NearByAddress");

                    b.Property<string>("NearByPlaceId");

                    b.Property<int?>("Pokemon");

                    b.Property<string>("PossibleGym");

                    b.Property<int?>("RaidBossLevel");

                    b.Property<DateTimeOffset?>("StartTime");

                    b.Property<string>("Title");

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
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<long>("Chat");

                    b.Property<int>("Format")
                        .ValueGeneratedOnAdd()
                        .HasDefaultValue(1822);

                    b.Property<DateTimeOffset?>("Modified");

                    b.Property<int>("Order");

                    b.HasKey("Id");

                    b.HasIndex("Chat")
                        .HasAnnotation("SqlServer:Include", new[] { "Format" });

                    b.ToTable("Settings");
                });

            modelBuilder.Entity("RaidBattlesBot.Model.Vote", b =>
                {
                    b.Property<int>("PollId");

                    b.Property<int>("UserId");

                    b.Property<string>("FirstName");

                    b.Property<string>("LasttName");

                    b.Property<DateTimeOffset?>("Modified");

                    b.Property<int?>("Team");

                    b.Property<string>("Username");

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
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("RaidBattlesBot.Model.Raid", b =>
                {
                    b.HasOne("RaidBattlesBot.Model.Raid", "EggRaid")
                        .WithOne("PostEggRaid")
                        .HasForeignKey("RaidBattlesBot.Model.Raid", "EggRaidId");
                });

            modelBuilder.Entity("RaidBattlesBot.Model.Vote", b =>
                {
                    b.HasOne("RaidBattlesBot.Model.Poll")
                        .WithMany("Votes")
                        .HasForeignKey("PollId")
                        .OnDelete(DeleteBehavior.Cascade);
                });
#pragma warning restore 612, 618
        }
    }
}
