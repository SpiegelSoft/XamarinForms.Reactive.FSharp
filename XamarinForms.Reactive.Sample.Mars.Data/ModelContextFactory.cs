using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.FSharp.Control;
using Microsoft.FSharp.Core;
using XamarinForms.Reactive.FSharp;
using XamarinForms.Reactive.Sample.Mars.Common;

namespace XamarinForms.Reactive.Sample.Mars.Data
{
    public class ModelContextFactory : ICreateModelContext<IMarsContext>
    {
        private readonly IMarsPlatform _platform;
        public ModelContextFactory(IMarsPlatform platform)
        {
            _platform = platform;
        }

        private async Task<IMarsContext> CreateDataContext()
        {
            var context = new MarsContext(DatabaseStorage.MarsDatabase, _platform);
            await context.Database.MigrateAsync();
            return context;
        }

        FSharpAsync<IMarsContext> ICreateModelContext<IMarsContext>.CreateModelContextAsync() => FSharpAsync.AwaitTask(CreateDataContext());
    }
}
