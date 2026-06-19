using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace Temasek.Tests;

internal static class RoomsApi
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static string NewRoomId() => $"room-{Guid.NewGuid():N}";

    public static async Task<RoomSummaryResponse> CreateRoomAsync(
        this HttpClient api,
        CreateRoomRequest request,
        CancellationToken ct = default
    )
    {
        using var response = await api.PostAsJsonAsync("/rooms", request, ct);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<RoomSummaryResponse>(JsonOptions, ct);
        await Assert.That(payload).IsNotNull();
        return payload!;
    }

    public static async Task<RoomScheduleResponse> GetRoomScheduleAsync(
        this HttpClient api,
        string roomId,
        CancellationToken ct = default
    )
    {
        var payload = await api.GetFromJsonAsync<RoomScheduleResponse>(
            $"/rooms/{roomId}/schedule",
            JsonOptions,
            ct
        );

        await Assert.That(payload).IsNotNull();
        return payload!;
    }

    public static async Task<RoomScheduleResponse> UpdateRoomScheduleAsync(
        this HttpClient api,
        string roomId,
        UpdateRoomScheduleRequest request,
        CancellationToken ct = default
    )
    {
        using var response = await api.PutAsJsonAsync($"/rooms/{roomId}/schedule", request, ct);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<RoomScheduleResponse>(JsonOptions, ct);
        await Assert.That(payload).IsNotNull();
        return payload!;
    }

    public static T Deserialize<T>(string json) where T : class
    {
        return JsonSerializer.Deserialize<T>(json, JsonOptions)
            ?? throw new InvalidOperationException("Expected a JSON payload.");
    }

    public static async Task<ServerSentEvent> ReadServerSentEventAsync(
        StreamReader reader,
        CancellationToken ct
    )
    {
        string eventName = string.Empty;
        var data = new StringBuilder();

        while (true)
        {
            var line = await reader.ReadLineAsync(ct);
            if (line is null)
            {
                throw new InvalidOperationException("The server-sent event stream ended unexpectedly.");
            }

            if (line.Length == 0)
            {
                if (eventName.Length > 0 || data.Length > 0)
                {
                    return new ServerSentEvent(eventName, data.ToString());
                }

                continue;
            }

            if (line.StartsWith("event:", StringComparison.Ordinal))
            {
                eventName = line["event:".Length..].Trim();
                continue;
            }

            if (line.StartsWith("data:", StringComparison.Ordinal))
            {
                if (data.Length > 0)
                {
                    data.AppendLine();
                }

                data.Append(line["data:".Length..].TrimStart());
            }
        }
    }

    public sealed record ServerSentEvent(string EventName, string Data);
}
