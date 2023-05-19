using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using System;

public static class UptimeCalculation
{
    [FunctionName("UptimeCalculation")]
    public static IActionResult Run(
        [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req)
    {
        var uptimeStr = req.Query["uptime"];
        var uptime = int.TryParse(uptimeStr, out var parsed) ? parsed : 3;

        float GetUptimePercent(int uptime)
        {
            return uptime switch
            {
                3 => 99.9f,
                4 => 99.99f,
                5 => 99.999f,
                _ => -1
            };
        }

        var uptimePercent = GetUptimePercent(uptime);

        if (uptimePercent < 0)
            return new BadRequestObjectResult("Invalid number of nines. Accepted values are 3, 4 or 5.");

        var result = new
        {
            uptime = $"{uptimePercent}%",
            year = CalculateAvailability(uptimePercent, 8760f),
            month = CalculateAvailability(uptimePercent, 30f * 24f)
        };

        object CalculateAvailability(float uptimePercent, float totalHours)
        {
            var uptimeHours = totalHours * (uptimePercent / 100);
            var downtimeHours = totalHours - uptimeHours;

            string FormatToReadableDuration(float hours)
            {
                var weeks = (int)(hours / (24 * 7));
                hours %= (24 * 7);
                var days = (int)(hours / 24);
                hours %= 24;
                var hour = (int)hours;
                var minutes = (int)Math.Round((hours - hour) * 60);

                return $"{weeks} weeks, {days} days, {hour} hours, {minutes} minutes";
            }

            return new
            {
                UptimeDuration = FormatToReadableDuration(uptimeHours),
                DowntimeDuration = FormatToReadableDuration(downtimeHours)
            };
        }

        return new OkObjectResult(result);
    }
}
