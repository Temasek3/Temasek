using FastEndpoints;
using Microsoft.Extensions.Options;
using Temasek.WebApi.Features.Calendarr;
using Temasek.WebApi.Features.Facilities;

namespace Temasek.WebApi.Features.EnabledFeatures.Get;

public class Endpoint(IOptionsMonitor<FacilitiesOptions> facilitiesOptions, IOptionsMonitor<CalendarrOptions> calendarrOptions) : EndpointWithoutRequest<Response>
{
    public override void Configure()
    {
        Get("/enabled-features");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        await Send.OkAsync(new Response
        {
            IsFacilitiesEnabled = facilitiesOptions.CurrentValue.Enabled ?? false,
            IsCalendarrEnabled = calendarrOptions.CurrentValue.Enabled ?? false
        }, ct);
    }
}
