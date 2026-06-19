namespace Temasek.Tests;

[ClassDataSource<AppFixture>(Shared = SharedType.PerTestSession)]
public class ScheduleEndpointTests(AppFixture fixture)
{
    private readonly HttpClient api = fixture.CreateHttpClient("temasek-webapi");

    [Test]
    public async Task GetScheduleForUnknownRoomReturnsFallbackSnapshot()
    {
        var roomId = RoomsApi.NewRoomId();

        var response = await api.GetRoomScheduleAsync(roomId);

        await Assert.That(response.RoomId).IsEqualTo(roomId);
        await Assert.That(response.Name).IsEqualTo(roomId);
        await Assert.That(response.Schedule.Count).IsEqualTo(0);
        await Assert.That(response.UpdatedAtUtc > DateTime.UnixEpoch).IsTrue();
    }

    [Test]
    public async Task PutScheduleCreatesOrUpdatesRoomAndPersistsActivities()
    {
        var roomId = RoomsApi.NewRoomId();

        var updated = await api.UpdateRoomScheduleAsync(
            roomId,
            new UpdateRoomScheduleRequest
            {
                Name = "Signal Room",
                Schedule =
                [
                    new RoomScheduleActivityDto
                    {
                        Id = "signal-1",
                        Title = "Shift Change",
                        Start = "2026-04-10T01:00:00Z",
                        End = "2026-04-10T02:00:00Z",
                        Personnel = "Duty Clerk",
                        Location = "Signal Desk",
                    },
                    new RoomScheduleActivityDto
                    {
                        Id = "signal-2",
                        Title = "Comms Check",
                        Start = "2026-04-10T03:00:00Z",
                        End = "2026-04-10T03:30:00Z",
                        Personnel = "Signals",
                        Location = "Radio Bay",
                    },
                ],
            }
        );

        var fetched = await api.GetRoomScheduleAsync(roomId);

        await Assert.That(updated.RoomId).IsEqualTo(roomId);
        await Assert.That(updated.Name).IsEqualTo("Signal Room");
        await Assert.That(updated.Schedule.Count).IsEqualTo(2);
        await Assert.That(updated.Schedule[0].Id).IsEqualTo("signal-1");
        await Assert.That(updated.Schedule[1].Title).IsEqualTo("Comms Check");

        await Assert.That(fetched.RoomId).IsEqualTo(roomId);
        await Assert.That(fetched.Name).IsEqualTo("Signal Room");
        await Assert.That(fetched.Schedule.Count).IsEqualTo(2);
        await Assert.That(fetched.Schedule[0].Personnel).IsEqualTo("Duty Clerk");
        await Assert.That(fetched.Schedule[1].Location).IsEqualTo("Radio Bay");
    }

    [Test]
    public async Task GetScheduleStreamEmitsInitialSnapshotAndSubsequentUpdate()
    {
        var roomId = RoomsApi.NewRoomId();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        using var streamResponse = await api.GetAsync(
            $"/rooms/{roomId}/schedule/stream",
            HttpCompletionOption.ResponseHeadersRead,
            cts.Token
        );

        await Assert.That(streamResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(streamResponse.Content.Headers.ContentType?.MediaType).IsEqualTo(
            "text/event-stream"
        );

        using var stream = await streamResponse.Content.ReadAsStreamAsync(cts.Token);
        using var reader = new StreamReader(stream);

        var initialEvent = await RoomsApi.ReadServerSentEventAsync(reader, cts.Token);
        var initialPayload = RoomsApi.Deserialize<RoomScheduleResponse>(initialEvent.Data);

        await Assert.That(initialEvent.EventName).IsEqualTo("schedule");
        await Assert.That(initialPayload.RoomId).IsEqualTo(roomId);
        await Assert.That(initialPayload.Name).IsEqualTo(roomId);
        await Assert.That(initialPayload.Schedule.Count).IsEqualTo(0);

        _ = await api.UpdateRoomScheduleAsync(
            roomId,
            new UpdateRoomScheduleRequest
            {
                Name = "Updated Stream Room",
                Schedule =
                [
                    new RoomScheduleActivityDto
                    {
                        Id = "stream-1",
                        Title = "Live Update",
                        Start = "2026-04-10T04:00:00Z",
                        End = "2026-04-10T05:00:00Z",
                        Personnel = "Watchkeeper",
                        Location = "Ops Floor",
                    },
                ],
            },
            cts.Token
        );

        var updatedEvent = await RoomsApi.ReadServerSentEventAsync(reader, cts.Token);
        var updatedPayload = RoomsApi.Deserialize<RoomScheduleResponse>(updatedEvent.Data);

        await Assert.That(updatedEvent.EventName).IsEqualTo("schedule");
        await Assert.That(updatedPayload.RoomId).IsEqualTo(roomId);
        await Assert.That(updatedPayload.Name).IsEqualTo("Updated Stream Room");
        await Assert.That(updatedPayload.Schedule.Count).IsEqualTo(1);
        await Assert.That(updatedPayload.Schedule[0].Id).IsEqualTo("stream-1");
        await Assert.That(updatedPayload.Schedule[0].Title).IsEqualTo("Live Update");
    }
}
