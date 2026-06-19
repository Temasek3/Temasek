using FreeSql.DataAnnotations;
using FreeSql.Internal;
using FreeSql.Internal.Model;

namespace Temasek.WebApi.Entities;

internal static class UserTypeHandlers
{
    public static IServiceCollection AddUserTypeHandlers(this IServiceCollection services)
    {
        Utils.TypeHandlers.TryAdd(typeof(UserNric), new UserNricTypeHandler());
        Utils.TypeHandlers.TryAdd(typeof(UserName), new UserNameTypeHandler());
        Utils.TypeHandlers.TryAdd(typeof(UserUnit), new UserUnitTypeHandler());
        return services;
    }

    private sealed class UserNricTypeHandler : BaseUserStringTypeHandler<UserNric>
    {
        public override UserNric Deserialize(object value) => UserNric.From(string.Concat(value));

        public override object Serialize(UserNric value) => value.Value;
    }

    private sealed class UserNameTypeHandler : BaseUserStringTypeHandler<UserName>
    {
        public override UserName Deserialize(object value) => UserName.From(string.Concat(value));

        public override object Serialize(UserName value) => value.Value;
    }

    private sealed class UserUnitTypeHandler : BaseUserStringTypeHandler<UserUnit>
    {
        public override UserUnit Deserialize(object value) => UserUnit.From(string.Concat(value));

        public override object Serialize(UserUnit value) => value.Value;
    }

    private abstract class BaseUserStringTypeHandler<T> : TypeHandler<T>
    {
        public override void FluentApi(ColumnFluent col)
        {
            col.MapType(typeof(string));
        }
    }
}
