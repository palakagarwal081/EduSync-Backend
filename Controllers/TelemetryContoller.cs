using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Abstractions;

namespace AppInsightsLoggerDemo.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TelemetryController : ControllerBase
{
    private readonly TelemetryClient _telemetry;

    public TelemetryController(TelemetryClient telemetryClient)
    {
        _telemetry = telemetryClient;
    }

    [HttpGet("trace")]
    public IActionResult LogTrace(string message = "Default trace log")
    {
        _telemetry.TrackTrace(message);
        return Ok("Trace log recorded.");
    }

    [HttpGet("event")]
    public IActionResult LogEvent(string eventName = "CustomEvent")
    {
        _telemetry.TrackEvent(eventName, new Dictionary<string, string>
        {
            { "Category", "Telemetry" },
            { "User", "TestUser" }
        });
        return Ok("Event log recorded.");
    }

    [HttpGet("exception")]
    public IActionResult LogException()
    {
        try
        {
            throw new InvalidOperationException("Simulated failure.");
        }
        catch (Exception ex)
        {
            _telemetry.TrackException(ex);
            return Ok("Exception logged.");
        }
    }

    [HttpGet("metric")]
    public IActionResult LogMetric(string metricName = "ServerLoad", double value = 0.75)
    {
        _telemetry.GetMetric(metricName).TrackValue(value);
        return Ok("Metric log recorded.");
    }

    [HttpGet("dependency")]
    public IActionResult LogDependency()
    {
        _telemetry.TrackDependency(
            dependencyTypeName: "HTTP",
            dependencyName: "https://myapi.example.com",
            data: "GET /resource",
            startTime: DateTime.UtcNow,
            duration: TimeSpan.FromMilliseconds(200),
            success: true
        );
        return Ok("Dependency log recorded.");
    }
}


