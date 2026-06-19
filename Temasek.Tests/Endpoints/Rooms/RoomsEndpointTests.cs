using System.Net.Http.Json;

namespace Temasek.Tests;

[ClassDataSource<AppFixture>(Shared = SharedType.PerTestSession)]
public class RoomsEndpointTests(AppFixture fixture)
{
    private readonly HttpClient api = fixture.CreateHttpClient("temasek-webapi");

    [Test]
    public async Task PostRoomsCreatesRoomWithFallbackNameWhenMissing()
    {
        var roomId = RoomsApi.NewRoomId();

        var response = await api.CreateRoomAsync(new CreateRoomRequest { RoomId = roomId });

        await Assert.That(response.RoomId).IsEqualTo(roomId);
        await Assert.That(response.Name).IsEqualTo(roomId);
        await Assert.That(response.ScheduleCount).IsEqualTo(0);
        await Assert.That(response.UpdatedAtUtc > DateTime.UnixEpoch).IsTrue();
    }

    [Test]
    public async Task PostRoomsWithDuplicateRoomIdReturnsConflict()
    {
        var roomId = RoomsApi.NewRoomId();

        _ = await api.CreateRoomAsync(
            new CreateRoomRequest
            {
                RoomId = roomId,
                Name = "Duplicate check room",
            }
        );

        using var duplicateResponse = await api.PostAsJsonAsync(
            "/rooms",
            new CreateRoomRequest
            {
                RoomId = roomId,
                Name = "Duplicate check room",
            }
        );

        await Assert.That(duplicateResponse.StatusCode).IsEqualTo(HttpStatusCode.Conflict);
    }

    [Test]
    public async Task GetRoomsIncludesCreatedRoomAndCurrentScheduleCount()
    {
        var roomId = RoomsApi.NewRoomId();

        _ = await api.UpdateRoomScheduleAsync(
            roomId,
            new UpdateRoomScheduleRequest
            {
                Name = "Operations Room",
                Schedule =
                [
                    new RoomScheduleActivityDto
                    {
                        Id = "activity-1",
                        Title = "Morning Brief",
                        Start = "2026-04-10T09:00:00Z",
                        End = "2026-04-10T10:00:00Z",
                        Personnel = "S1",
                        Location = "HQ",
                    },
                    new RoomScheduleActivityDto
                    {
                        Id = "activity-2",
                        Title = "Planning Session",
                        Start = "2026-04-10T11:00:00Z",
                        End = "2026-04-10T12:00:00Z",
                        Personnel = "S3",
                        Location = "HQ",
                    },
                ],
            }
        );

        var rooms = await api.GetFromJsonAsync<IReadOnlyList<RoomSummaryResponse>>("/rooms");

        await Assert.That(rooms).IsNotNull();

        var room = rooms!.SingleOrDefault(item => item.RoomId == roomId);

        await Assert.That(room).IsNotNull();
        await Assert.That(room!.Name).IsEqualTo("Operations Room");
        await Assert.That(room.ScheduleCount).IsEqualTo(2);
        await Assert.That(room.UpdatedAtUtc > DateTime.UnixEpoch).IsTrue();
    }
}
