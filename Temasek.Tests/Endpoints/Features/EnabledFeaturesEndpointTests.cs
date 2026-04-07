using System.Net.Http.Json;

namespace Temasek.Tests;

[ClassDataSource<AppFixture>(Shared = SharedType.PerTestSession)]
public class EnabledFeaturesEndpointTests(AppFixture fixture)
{
    private readonly HttpClient api = fixture.CreateHttpClient("temasek-webapi");

    [Test]
    public async Task GetEnabledFeaturesReturnsFeatureFlags()
    {
        var response = await api.GetFromJsonAsync<EnabledFeaturesResponse>("/enabled-features");

        await Assert.That(response).IsNotNull();
        await Assert.That(response!.IsFacilitiesEnabled).IsTypeOf<bool>();
    }
}
