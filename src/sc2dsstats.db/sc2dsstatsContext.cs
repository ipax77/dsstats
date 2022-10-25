using Microsoft.EntityFrameworkCore;

namespace sc2dsstats.db
{
    public partial class sc2dsstatsContext : DbContext
    {
        public sc2dsstatsContext()
        {
            Database.SetCommandTimeout(TimeSpan.FromMinutes(10));
        }

        public sc2dsstatsContext(DbContextOptions<sc2dsstatsContext> options)
            : base(options)
        {
        }

        protected sc2dsstatsContext(DbContextOptions options)
            : base(options)
        {
        }

        public virtual DbSet<Breakpoint> Breakpoints { get; set; }
        public virtual DbSet<Dsplayer> Dsplayers { get; set; }
        public virtual DbSet<Dsreplay> Dsreplays { get; set; }
        public virtual DbSet<Dsunit> Dsunits { get; set; }
        public virtual DbSet<Middle> Middles { get; set; }
        public virtual DbSet<DSRestPlayer> DSRestPlayers { get; set; }
        public virtual DbSet<DsTimeResult> DsTimeResults { get; set; }
        public virtual DbSet<DsParticipant> Participants { get; set; }
        public virtual DbSet<DsTimeResultValue> DsTimeResultValues { get; set; }
        public virtual DbSet<DsParticipantsValue> DsParticipantsValues { get; set; }
        public virtual DbSet<CommanderName> CommanderNames { get; set; }
        public virtual DbSet<UnitName> UnitNames { get; set; }
        public virtual DbSet<UpgradeName> UpgradeNames { get; set; }
        public virtual DbSet<DsInfo> DsInfo { get; set; }
        public virtual DbSet<DsPlayerName> DsPlayerNames { get; set; }

        // public virtual DbSet<CmdrStats> CmdrStats { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {



            // modelBuilder.Entity<CmdrStats>(entity =>
            // {
            //     entity.HasNoKey();
            // });

            modelBuilder.Entity<DsTimeResult>(entity =>
            {
                entity.HasKey(k => k.Id);
                entity.HasIndex(i => i.Timespan);
                entity.HasIndex(i => i.Cmdr);
                entity.HasIndex(i => i.Opp);
                entity.HasIndex(i => i.Player);
                entity.HasIndex(i => new { i.Timespan, i.Cmdr });
                entity.HasIndex(i => new { i.Timespan, i.Cmdr, i.Player });
                entity.HasIndex(i => new { i.Timespan, i.Cmdr, i.Opp });
                entity.HasIndex(i => new { i.Timespan, i.Cmdr, i.Opp, i.Player });
            });

            modelBuilder.Entity<DsParticipantsValue>(entity =>
            {
                entity.ToTable("dsparticipantsvalues");

                entity.HasKey(k => k.Id);

                entity.HasIndex(e => e.DsTimeResultValuesId, "IX_DsParticipantsValues_DsTimeResultValuesId");

                entity.HasIndex(e => e.DsTimeResultValuesId1, "IX_DsParticipantsValues_DsTimeResultValuesId1");

                entity.HasOne(d => d.DsTimeResultValues)
                    .WithMany(p => p.Teammates)
                    .HasForeignKey(d => d.DsTimeResultValuesId)
                    .HasConstraintName("FK_DsParticipantsValues_DsTimeResultValues_DsTimeResultValuesId");

                entity.HasOne(d => d.DsTimeResultValuesId1Navigation)
                    .WithMany(p => p.Opponents)
                    .HasForeignKey(d => d.DsTimeResultValuesId1)
                    .HasConstraintName("FK_DsParticipantsValues_DsTimeResultValues_DsTimeResultValuesId1");
            });

            modelBuilder.Entity<DsParticipant>(entity =>
            {
                entity.ToTable("participants");

                entity.HasKey(k => k.Id);

                entity.HasIndex(e => e.DsTimeResultId, "IX_Participants_DsTimeResultId");

                entity.HasIndex(e => e.DsTimeResultId1, "IX_Participants_DsTimeResultId1");

                entity.HasOne(d => d.DsTimeResult)
                    .WithMany(p => p.Teammates)
                    .HasForeignKey(d => d.DsTimeResultId)
                    .HasConstraintName("FK_Participants_DsTimeResults_DsTimeResultId");

                entity.HasOne(d => d.DsTimeResultId1Navigation)
                    .WithMany(p => p.Opponents)
                    .HasForeignKey(d => d.DsTimeResultId1)
                    .HasConstraintName("FK_Participants_DsTimeResults_DsTimeResultId1");
            });

            modelBuilder.Entity<Breakpoint>(entity =>
            {
                entity.ToTable("breakpoints");

                entity.HasIndex(e => e.PlayerId, "IX_Breakpoints_PlayerID");

                entity.Property(e => e.Id)
                    .HasColumnName("ID");

                entity.Property(e => e.Army).HasColumnType("int(11)");

                entity.Property(e => e.Breakpoint1)
                    .HasColumnName("Breakpoint");

                entity.Property(e => e.DbUnitsString)
                    .HasColumnName("dbUnitsString");

                entity.Property(e => e.DbUpgradesString)
                    .HasColumnName("dbUpgradesString");

                entity.Property(e => e.DsUnitsString)
                    .HasColumnName("dsUnitsString");

                entity.Property(e => e.Gas).HasColumnType("int(11)");

                entity.Property(e => e.Income).HasColumnType("int(11)");

                entity.Property(e => e.Kills).HasColumnType("int(11)");

                entity.Property(e => e.Mid).HasColumnType("int(11)");

                entity.Property(e => e.PlayerId)
                    .HasColumnName("PlayerID");

                entity.Property(e => e.Tier).HasColumnType("int(11)");

                entity.Property(e => e.Upgrades).HasColumnType("int(11)");

                entity.HasOne(d => d.Player)
                    .WithMany(p => p.Breakpoints)
                    .HasForeignKey(d => d.PlayerId)
                    .HasConstraintName("FK_Breakpoints_DSPlayers_PlayerID");
            });

            modelBuilder.Entity<Dsplayer>(entity =>
            {
                entity.ToTable("dsplayers");

                entity.HasIndex(e => e.DsreplayId, "IX_DSPlayers_DSReplayID");

                entity.HasIndex(e => e.Race, "IX_DSPlayers_RACE");
                entity.HasIndex(e => e.Name, "IX_DSPlayers_NAME");

                entity.HasIndex(e => new { e.Race, e.Opprace }, "IX_DSPlayers_RACE_OPPRACE");
                entity.HasIndex(e => new { e.Race, e.isPlayer }, "IX_DSPlayers_RACE_PLAYER");
                entity.HasIndex(e => new { e.Race, e.Opprace, e.isPlayer }, "IX_DSPlayers_RACE_OPPRACE_PLAYER");

                entity.Property(e => e.Id)
                    .HasColumnName("ID");

                entity.Property(e => e.Army)
                    .HasColumnType("int(11)")
                    .HasColumnName("ARMY");

                entity.Property(e => e.DsreplayId)
                    .HasColumnType("int(11)")
                    .HasColumnName("DSReplayID");

                entity.Property(e => e.Gas)
                    .HasColumnType("tinyint(3)")
                    .HasColumnName("GAS");

                entity.Property(e => e.Income)
                    .HasColumnType("int(11)")
                    .HasColumnName("INCOME");

                entity.Property(e => e.Killsum)
                    .HasColumnType("int(11)")
                    .HasColumnName("KILLSUM");

                entity.Property(e => e.Name)
                    .HasMaxLength(64)
                    .HasColumnName("NAME");

                entity.Property(e => e.Pduration)
                    .HasColumnType("int(11)")
                    .HasColumnName("PDURATION");

                entity.Property(e => e.Pos)
                    .HasColumnType("tinyint(3)")
                    .HasColumnName("POS");

                entity.Property(e => e.Realpos)
                    .HasColumnType("tinyint(3)")
                    .HasColumnName("REALPOS");

                entity.Property(e => e.Team)
                    .HasColumnType("tinyint(3)")
                    .HasColumnName("TEAM");

                entity.Property(e => e.Win).HasColumnName("WIN");

                entity.HasOne(d => d.Dsreplay)
                    .WithMany(p => p.Dsplayers)
                    .HasForeignKey(d => d.DsreplayId)
                    .HasConstraintName("FK_DSPlayers_DSReplays_DSReplayID");
            });

            modelBuilder.Entity<Dsreplay>(entity =>
            {
                entity.ToTable("dsreplays");

                entity.HasIndex(e => e.Hash, "IX_DSReplays_HASH");

                entity.HasIndex(e => e.Replay, "IX_DSReplays_REPLAY");

                entity.HasIndex(e => new { e.Gametime, e.DefaultFilter });

                entity.HasIndex(e => e.Replaypath, "IX_DSReplays_REPLAYPATH");

                entity.HasIndex(e => e.Gametime);

                entity.Property(e => e.Id)
                    .HasColumnName("ID");

                entity.Property(e => e.Duration)
                    .HasColumnType("int(11)")
                    .HasColumnName("DURATION");

                entity.Property(e => e.Gametime)
                    .HasMaxLength(6)
                    .HasColumnName("GAMETIME");

                entity.Property(e => e.Hash)
                    .HasMaxLength(32)
                    .HasColumnName("HASH")
                    .IsFixedLength();

                entity.Property(e => e.Isbrawl).HasColumnName("ISBRAWL");

                entity.Property(e => e.Maxkillsum)
                    .HasColumnType("int(11)")
                    .HasColumnName("MAXKILLSUM");

                entity.Property(e => e.Maxleaver)
                    .HasColumnType("int(11)")
                    .HasColumnName("MAXLEAVER");

                entity.Property(e => e.Minarmy)
                    .HasColumnType("int(11)")
                    .HasColumnName("MINARMY");

                entity.Property(e => e.Minincome)
                    .HasColumnType("int(11)")
                    .HasColumnName("MININCOME");

                entity.Property(e => e.Minkillsum)
                    .HasColumnType("int(11)")
                    .HasColumnName("MINKILLSUM");

                entity.Property(e => e.Objective)
                    .HasColumnType("int(11)")
                    .HasColumnName("OBJECTIVE");

                entity.Property(e => e.Playercount)
                    .HasColumnType("tinyint(3)")
                    .HasColumnName("PLAYERCOUNT");

                entity.Property(e => e.Replay)
                    .HasColumnName("REPLAY");

                entity.Property(e => e.Replaypath)
                    .HasColumnName("REPLAYPATH");

                entity.Property(e => e.Reported)
                    .HasColumnType("tinyint(3)")
                    .HasColumnName("REPORTED");

                entity.Property(e => e.Upload).HasMaxLength(6);

                entity.Property(e => e.Version)
                    .HasColumnName("VERSION");

                entity.Property(e => e.Winner)
                    .HasColumnType("tinyint(4)")
                    .HasColumnName("WINNER");

                entity.Property(e => e.Mid1).HasPrecision(5, 2);
                entity.Property(e => e.Mid2).HasPrecision(5, 2);

            });

            modelBuilder.Entity<Dsunit>(entity =>
            {
                entity.ToTable("dsunits");

                entity.HasIndex(e => e.BreakpointId, "IX_DSUnits_BreakpointID");

                entity.HasIndex(e => e.DsplayerId, "IX_DSUnits_DSPlayerID");

                entity.Property(e => e.Id)
                    .HasColumnName("ID");

                entity.Property(e => e.Bp)
                    .HasColumnName("BP");

                entity.Property(e => e.BreakpointId)
                    .HasColumnName("BreakpointID");

                entity.Property(e => e.Count).HasColumnType("int(11)");

                entity.Property(e => e.DsplayerId)
                    .HasColumnName("DSPlayerID");

                entity.HasOne(d => d.Breakpoint)
                    .WithMany(p => p.Dsunits)
                    .HasForeignKey(d => d.BreakpointId)
                    .HasConstraintName("FK_DSUnits_Breakpoints_BreakpointID");

                entity.HasOne(d => d.Dsplayer)
                    .WithMany(p => p.Dsunits)
                    .HasForeignKey(d => d.DsplayerId)
                    .HasConstraintName("FK_DSUnits_DSPlayers_DSPlayerID");
            });

            //modelBuilder.Entity<Efmigrationshistory>(entity =>
            //{
            //    entity.HasKey(e => e.MigrationId)
            //        .HasName("PRIMARY");

            //    entity.ToTable("__efmigrationshistory");

            //    entity.Property(e => e.MigrationId).HasMaxLength(95);

            //    entity.Property(e => e.ProductVersion)
            //        .IsRequired()
            //        .HasMaxLength(32);
            //});

            modelBuilder.Entity<Middle>(entity =>
            {
                entity.ToTable("middle");

                entity.HasIndex(e => e.ReplayId, "IX_Middle_ReplayID");

                entity.Property(e => e.Id)
                    .HasColumnName("ID");

                entity.Property(e => e.Gameloop).HasColumnType("int(11)");

                entity.Property(e => e.ReplayId)
                    .HasColumnName("ReplayID");

                entity.Property(e => e.Team).HasColumnType("tinyint(3)");

                entity.HasOne(d => d.Replay)
                    .WithMany(p => p.Middles)
                    .HasForeignKey(d => d.ReplayId)
                    .HasConstraintName("FK_Middle_DSReplays_ReplayID");
            });

            modelBuilder.Entity<DsPlayerName>(entity =>
            {
                entity.HasIndex(i => i.AppId);
                entity.HasIndex(i => i.DbId);
                entity.HasIndex(i => i.Hash);
                entity.HasIndex(i => i.Name);

                entity.Property(p => p.Hash)
                .HasMaxLength(64)
                .IsFixedLength();
                entity.Property(p => p.Name)
                .HasMaxLength(64);
                entity.Property(p => p.AppVersion)
                .HasMaxLength(32);
                entity.HasIndex(p => p.Name)
                .IsUnique();
            });

            modelBuilder.Entity<DsInfo>().HasData(new db.DsInfo()
            {
                Id = 1,
                UnitNamesUpdate = new DateTime(2021, 10, 31),
                UpgradeNamesUpdate = new DateTime(2021, 10, 31)
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
