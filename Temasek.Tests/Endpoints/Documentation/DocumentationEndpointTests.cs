namespace Temasek.Tests;

[ClassDataSource<AppFixture>(Shared = SharedType.PerTestSession)]
public class DocumentationEndpointTests(AppFixture fixture)
{
    private readonly HttpClient api = fixture.CreateHttpClient("temasek-webapi");

    [Test]
    public async Task GetRootRedirectsToScalarDocumentation()
    {
        using var response = await api.GetAsync("/");

        await Assert.That(response.RequestMessage?.RequestUri?.AbsolutePath).Contains("/scalar");
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }
}
