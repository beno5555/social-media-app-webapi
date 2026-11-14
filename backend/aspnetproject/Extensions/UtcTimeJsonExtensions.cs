using aspnetproject.Common.Json;
using Microsoft.AspNetCore.SignalR;

namespace aspnetproject.Extensions;

// Marks every time the API writes as UTC (ADR 0005).
public static class UtcTimeJsonExtensions
{
    public static IMvcBuilder AddUtcTimeJson(this IMvcBuilder builder)
    {
        builder.AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new UtcDateTimeJsonConverter()));

        return builder;
    }

    public static ISignalRServerBuilder AddUtcTimeJson(this ISignalRServerBuilder builder)
    {
        builder.AddJsonProtocol(options => options.PayloadSerializerOptions.Converters.Add(new UtcDateTimeJsonConverter()));

        return builder;
    }
}
