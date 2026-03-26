using Projects;
using TUnit.Aspire;

namespace Temasek.Tests;

public class AppFixture : AspireFixture<Temasek_AppHost>
{
    protected override ResourceWaitBehavior WaitBehavior => ResourceWaitBehavior.Named;

    protected override TimeSpan ResourceTimeout => TimeSpan.FromMinutes(2);

    protected override IEnumerable<string> ResourcesToWaitFor() =>
        ["db", "temasek-webapi", "temasek-webapp"];

    protected override void ConfigureBuilder(IDistributedApplicationTestingBuilder builder)
    {
        builder.Services.ConfigureHttpClientDefaults(clientBuilder =>
        {
            clientBuilder.AddStandardResilienceHandler();
        });
    }
}
