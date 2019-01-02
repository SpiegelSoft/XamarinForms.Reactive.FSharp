using Microsoft.EntityFrameworkCore;
using XamarinForms.Reactive.Sample.Mars.Common;

namespace XamarinForms.Reactive.Sample.Mars.Data
{
    public class MarsContext : ModelContext, IMarsContext
    {
        public MarsContext(string connectionString, IMarsPlatform platform) : base(connectionString, platform) { }
        public DbSet<RoverSolPhotoSetDto> PhotoSets { get; set; }
        public DbSet<PhotoManifestDto> PhotoManifests { get; set; }
    }
}