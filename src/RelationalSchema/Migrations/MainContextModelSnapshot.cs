// <auto-generated />
using LastFmStatsServer;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace RelationalSchema.Migrations
{
    [DbContext(typeof(MainContext))]
    partial class MainContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "5.0.4");

            modelBuilder.Entity("LastFmStatsServer.Album", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("Name")
                        .IsUnique();

                    b.ToTable("Albums");
                });

            modelBuilder.Entity("LastFmStatsServer.Artist", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("Name")
                        .IsUnique();

                    b.ToTable("Artists");
                });

            modelBuilder.Entity("LastFmStatsServer.Scrobble", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<long>("Timestamp")
                        .HasColumnType("INTEGER");

                    b.Property<long>("TrackId")
                        .HasColumnType("INTEGER");

                    b.Property<long>("UserId")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.HasIndex("Timestamp");

                    b.HasIndex("TrackId");

                    b.HasIndex("UserId", "TrackId", "Timestamp")
                        .IsUnique();

                    b.ToTable("Scrobbles");
                });

            modelBuilder.Entity("LastFmStatsServer.Track", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<long>("AlbumId")
                        .HasColumnType("INTEGER");

                    b.Property<long>("ArtistId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("AlbumId");

                    b.HasIndex("ArtistId");

                    b.HasIndex("Name");

                    b.HasIndex("ArtistId", "AlbumId", "Name")
                        .IsUnique();

                    b.ToTable("Tracks");
                });

            modelBuilder.Entity("LastFmStatsServer.User", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("DisplayName")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("Name")
                        .IsUnique();

                    b.ToTable("Users");
                });

            modelBuilder.Entity("LastFmStatsServer.Scrobble", b =>
                {
                    b.HasOne("LastFmStatsServer.Track", "Track")
                        .WithMany("Scrobbles")
                        .HasForeignKey("TrackId")
                        .IsRequired();

                    b.HasOne("LastFmStatsServer.User", "User")
                        .WithMany("Scrobbles")
                        .HasForeignKey("UserId")
                        .IsRequired();

                    b.Navigation("Track");

                    b.Navigation("User");
                });

            modelBuilder.Entity("LastFmStatsServer.Track", b =>
                {
                    b.HasOne("LastFmStatsServer.Album", "Album")
                        .WithMany("Tracks")
                        .HasForeignKey("AlbumId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("LastFmStatsServer.Artist", "Artist")
                        .WithMany("Tracks")
                        .HasForeignKey("ArtistId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Album");

                    b.Navigation("Artist");
                });

            modelBuilder.Entity("LastFmStatsServer.Album", b =>
                {
                    b.Navigation("Tracks");
                });

            modelBuilder.Entity("LastFmStatsServer.Artist", b =>
                {
                    b.Navigation("Tracks");
                });

            modelBuilder.Entity("LastFmStatsServer.Track", b =>
                {
                    b.Navigation("Scrobbles");
                });

            modelBuilder.Entity("LastFmStatsServer.User", b =>
                {
                    b.Navigation("Scrobbles");
                });
#pragma warning restore 612, 618
        }
    }
}
