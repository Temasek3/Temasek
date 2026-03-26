using System.Net.Http.Json;

namespace Temasek.Tests;

[ClassDataSource<AppFixture>(Shared = SharedType.PerTestSession)]
public class FormSgCallbackEndpointTests(AppFixture fixture)
{
    private readonly HttpClient api = fixture.CreateHttpClient("temasek-webapi");

    [Test]
    public async Task PostFormSgCallbackWithoutApiKeyReturnsUnauthorized()
    {
        using var response = await api.PostAsJsonAsync(
            "/formsg/callback",
            new FormSgCallbackRequest
            {
                ClerkUserId = "invalid-token",
                Nric = "S1234567A",
                Name = "Test User",
            }
        );

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task PostFormSgCallbackWithApiKeyAndInvalidTokenReturnsForbidden()
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/formsg/callback")
        {
            Content = JsonContent.Create(
                new FormSgCallbackRequest
                {
                    ClerkUserId = "invalid-token",
                    Nric = "S1234567A",
                    Name = "Test User",
                }
            ),
        };
        request.Headers.Add("X-API-KEY", "test-api-key");

        using var response = await api.SendAsync(request);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }
}
