using System;
using System.Collections.Generic;

namespace LastFmStatsServer
{
    public abstract class IdNameEntity
    {
        public long Id { get; set; }
        public string Name { get; set; }
    }

    public partial class User : IdNameEntity
    {
        public User()
        {
            Scrobbles = new HashSet<Scrobble>();
        }

        public string DisplayName { get; set; }
        public virtual ICollection<Scrobble> Scrobbles { get; set; }
    }

    public partial class Artist : IdNameEntity
    {
        public Artist()
        {
            Tracks = new HashSet<Track>();
        }

        public virtual ICollection<Track> Tracks { get; set; }
    }

    public partial class Album : IdNameEntity
    {
        public Album()
        {
            Tracks = new HashSet<Track>();
        }

        public virtual ICollection<Track> Tracks { get; set; }
    }


    public partial class Track : IdNameEntity
    {
        public Track()
        {
            Scrobbles = new HashSet<Scrobble>();
        }

        public long ArtistId { get; set; }
        public long AlbumId { get; set; }

        public virtual Album Album { get; set; }
        public virtual Artist Artist { get; set; }
        public virtual ICollection<Scrobble> Scrobbles { get; set; }
    }


    public partial class Scrobble
    {
        public long Id { get; set; }
        public long Timestamp { get; set; }
        public long UserId { get; set; }
        public long TrackId { get; set; }

        public virtual Track Track { get; set; }
        public virtual User User { get; set; }
    }
}
