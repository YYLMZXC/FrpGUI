using FrpGUI.Models;
using FrpGUI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace FrpGUI.WebAPI.Controllers;

[NeedToken]
[ApiController]
[Route("[controller]")]
public class LogController : ControllerBase
{
    private readonly Logger logger;

    public LogController(LoggerBase logger)
    {
        this.logger = (Logger)logger;
    }

    [HttpGet("List")]
    public IList<LogEntity> Get(DateTime timeAfter)
    {
        Debug.WriteLine(logger.CacheLogs.Count);
        var logs = logger.CacheLogs
              .Where(p => p.ProcessStartTime == LogEntity.CurrentProcessStartTime)
              .Where(p => p.Time > timeAfter)
              .OrderByDescending(p => p.Time)
              .Take(100)
              .Reverse()
              .ToList();
        return logs;
    }
}