using System;
using System.IO;
using Microsoft.FSharp.Control;
using Splat;
using Xamarin.Forms;
using XamarinForms.Reactive.Sample.Mars.Common;
using XamarinForms.Reactive.Sample.Mars.Data;

namespace XamarinForms.Reactive.Sample.Mars.Migrations.Migrations
{
    public class MigrationPlatform : IMarsPlatform
    {
        private static readonly string AppFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
        public void RegisterDependencies(IMutableDependencyResolver dependencyResolver) { }
        public string GetMetadataEntry(string key) => null;
        public string GetLocalFilePath(string fileName) => Path.Combine(AppFolderPath, fileName);
        public void HandleAppLinkRequest(Uri appLinkRequestUri) { }
        public ImageSource GetHeadlineImage(string name) => null;
        public FSharpAsync<PhotoSet> GetCameraDataAsync(RoverSolPhotoSet photoSet, string camera) => throw new NotImplementedException();
        public FSharpAsync<Rover[]> PullRoversAsync() => throw new NotImplementedException();
    }

    public class MigrationContext : MarsContext
    {
        public MigrationContext() : base(DatabaseStorage.MarsDatabase, new MigrationPlatform()) { }
    }
}