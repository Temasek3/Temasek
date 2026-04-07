using System.Text.Json;
using Temasek.WebApi.Entities;
using Temasek.WebApi.Features.Rooms.Signboard.Contracts;

namespace Temasek.WebApi.Features.Rooms.Signboard;

public class RoomSignboardStore(IFreeSql sql)
{
    public sealed class RoomSummary
    {
        public string RoomId { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public int ScheduleCount { get; set; }

        public DateTime UpdatedAtUtc { get; set; }
    }

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<IReadOnlyList<RoomSummary>> ListAsync(CancellationToken ct)
    {
        var rooms = await sql.Select<RoomSignboard>().OrderBy(item => item.Name).ToListAsync(ct);

        return rooms
            .Select(item =>
            {
                var roomId = NormalizeRoomId(item.RoomId);
                var roomName = NormalizeName(item.Name, null, roomId);

                return new RoomSummary
                {
                    RoomId = roomId,
                    Name = roomName,
                    ScheduleCount = DeserializeSchedule(item.ScheduleJson).Count,
                    UpdatedAtUtc = item.UpdatedAtUtc,
                };
            })
            .ToArray();
    }

    public async Task<(RoomSignboardResponse Room, bool Created)> CreateAsync(
        string roomId,
        string? name,
        CancellationToken ct
    )
    {
        var normalizedRoomId = NormalizeRoomId(roomId);
        var existing = await sql.Select<RoomSignboard>()
            .Where(item => item.RoomId == normalizedRoomId)
            .FirstAsync(ct);

        if (existing is not null)
        {
            return (ToResponse(existing, normalizedRoomId), false);
        }

        var roomSignboard = new RoomSignboard
        {
            RoomId = normalizedRoomId,
            Name = NormalizeName(name, null, normalizedRoomId),
            ScheduleJson = "[]",
            UpdatedAtUtc = DateTime.UtcNow,
        };

        await sql.Insert(roomSignboard).ExecuteAffrowsAsync(ct);

        return (ToResponse(roomSignboard, normalizedRoomId), true);
    }

    public async Task<RoomSignboardResponse> GetAsync(string roomId, CancellationToken ct)
    {
        var normalizedRoomId = NormalizeRoomId(roomId);
        var roomSignboard = await sql.Select<RoomSignboard>()
            .Where(item => item.RoomId == normalizedRoomId)
            .FirstAsync(ct);

        return ToResponse(roomSignboard, normalizedRoomId);
    }

    public async Task<RoomSignboardResponse> SaveAsync(
        string roomId,
        string? name,
        IEnumerable<SignboardActivityDto>? schedule,
        CancellationToken ct
    )
    {
        var normalizedRoomId = NormalizeRoomId(roomId);
        var roomSignboard = await sql.Select<RoomSignboard>()
            .Where(item => item.RoomId == normalizedRoomId)
            .FirstAsync(ct);
        var exists = roomSignboard is not null;
        var normalizedSchedule = NormalizeSchedule(schedule);

        roomSignboard ??= new RoomSignboard { RoomId = normalizedRoomId };

        roomSignboard.Name = NormalizeName(name, roomSignboard.Name, normalizedRoomId);
        roomSignboard.ScheduleJson = JsonSerializer.Serialize(normalizedSchedule, JsonOptions);
        roomSignboard.UpdatedAtUtc = DateTime.UtcNow;

        if (exists)
        {
            await sql.Update<RoomSignboard>().SetSource(roomSignboard).ExecuteAffrowsAsync(ct);
        }
        else
        {
            await sql.Insert(roomSignboard).ExecuteAffrowsAsync(ct);
        }

        return ToResponse(roomSignboard, normalizedRoomId, normalizedSchedule);
    }

    private static RoomSignboardResponse ToResponse(
        RoomSignboard? roomSignboard,
        string roomId,
        IReadOnlyList<SignboardActivityDto>? normalizedSchedule = null
    )
    {
        var safeRoomId = NormalizeRoomId(roomId);
        var schedule = normalizedSchedule ?? DeserializeSchedule(roomSignboard?.ScheduleJson);

        return new RoomSignboardResponse
        {
            RoomId = safeRoomId,
            Name = NormalizeName(roomSignboard?.Name, null, safeRoomId),
            Schedule = schedule,
            UpdatedAtUtc = roomSignboard?.UpdatedAtUtc ?? DateTime.UtcNow,
        };
    }

    private static IReadOnlyList<SignboardActivityDto> DeserializeSchedule(string? scheduleJson)
    {
        if (string.IsNullOrWhiteSpace(scheduleJson))
        {
            return Array.Empty<SignboardActivityDto>();
        }

        try
        {
            var schedule = JsonSerializer.Deserialize<List<SignboardActivityDto>>(
                scheduleJson,
                JsonOptions
            );
            return NormalizeSchedule(schedule);
        }
        catch
        {
            return Array.Empty<SignboardActivityDto>();
        }
    }

    private static IReadOnlyList<SignboardActivityDto> NormalizeSchedule(
        IEnumerable<SignboardActivityDto>? schedule
    )
    {
        return schedule
                ?.Select(
                    (activity, index) =>
                        new SignboardActivityDto
                        {
                            Id = string.IsNullOrWhiteSpace(activity.Id)
                                ? $"activity-{index + 1}"
                                : activity.Id.Trim(),
                            Title = activity.Title?.Trim() ?? string.Empty,
                            Start = activity.Start?.Trim() ?? string.Empty,
                            End = activity.End?.Trim() ?? string.Empty,
                            Personnel = activity.Personnel?.Trim() ?? string.Empty,
                            Location = activity.Location?.Trim() ?? string.Empty,
                        }
                )
                .ToArray()
            ?? Array.Empty<SignboardActivityDto>();
    }

    private static string NormalizeName(string? preferredName, string? fallbackName, string roomId)
    {
        if (!string.IsNullOrWhiteSpace(preferredName))
        {
            return preferredName.Trim();
        }

        if (!string.IsNullOrWhiteSpace(fallbackName))
        {
            return fallbackName.Trim();
        }

        return roomId;
    }

    private static string NormalizeRoomId(string roomId)
    {
        return string.IsNullOrWhiteSpace(roomId) ? string.Empty : roomId.Trim();
    }
}
