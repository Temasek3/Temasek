namespace Temasek.Tests;

[ClassDataSource<AppFixture>(Shared = SharedType.PerTestSession)]
public class ProtectedEndpointsTests(AppFixture fixture)
{
    private readonly HttpClient api = fixture.CreateHttpClient("temasek-webapi");

    [Test]
    public async Task GetPokedexWithoutAuthenticationReturnsUnauthorized()
    {
        using var response = await api.GetAsync("/pokedex");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task GetFormSgValidateWithoutAuthenticationReturnsUnauthorized()
    {
        using var response = await api.GetAsync("/formsg/validate");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }
}
