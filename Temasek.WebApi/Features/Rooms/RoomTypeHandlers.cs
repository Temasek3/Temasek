using System.Text.Json;
using System.Text.Json.Serialization;
using FreeSql.DataAnnotations;
using FreeSql.Internal;
using FreeSql.Internal.Model;

namespace Temasek.WebApi.Features.Rooms.Schedule;

internal static class RoomTypeHandlers
{
    private const int RoomIdLength = 512;
    private const string UnlimitedTextDbType = "longtext";

    public static IServiceCollection AddRoomTypeHandlers(this IServiceCollection services)
    {
        Utils.TypeHandlers.TryAdd(typeof(RoomId), new RoomIdTypeHandler());
        Utils.TypeHandlers.TryAdd(typeof(RoomName), new RoomNameTypeHandler());
        Utils.TypeHandlers.TryAdd(typeof(RoomSchedule), new RoomScheduleTypeHandler());
        return services;
    }

    private sealed class RoomIdTypeHandler : TypeHandler<RoomId>
    {
        public override RoomId Deserialize(object value) => RoomId.From(string.Concat(value));

        public override object Serialize(RoomId value) => value.Value;

        public override void FluentApi(ColumnFluent col)
        {
            col.MapType(typeof(string));
            col.StringLength(RoomIdLength);
        }
    }

    private sealed class RoomNameTypeHandler : TypeHandler<RoomName>
    {
        public override RoomName Deserialize(object value) => RoomName.From(string.Concat(value));

        public override object Serialize(RoomName value) => value.Value;

        public override void FluentApi(ColumnFluent col)
        {
            col.MapType(typeof(string));
            col.DbType(UnlimitedTextDbType);
        }
    }

    private sealed class RoomScheduleTypeHandler : TypeHandler<RoomSchedule>
    {
        public override RoomSchedule Deserialize(object value)
        {
            try
            {
                var activities = JsonSerializer.Deserialize(
                    string.Concat(value),
                    RoomJsonContext.Default.RoomScheduleActivityArray
                );
                return activities is null or { Length: 0 }
                    ? RoomSchedule.Empty
                    : new RoomSchedule(activities);
            }
            catch
            {
                return RoomSchedule.Empty;
            }
        }

        public override object Serialize(RoomSchedule value)
        {
            return JsonSerializer.Serialize(
                value.Activities,
                RoomJsonContext.Default.RoomScheduleActivityArray
            );
        }

        public override void FluentApi(ColumnFluent col)
        {
            col.MapType(typeof(string));
            col.DbType(UnlimitedTextDbType);
        }
    }
}

[JsonSerializable(typeof(RoomScheduleActivity[]))]
internal sealed partial class RoomJsonContext : JsonSerializerContext { }
