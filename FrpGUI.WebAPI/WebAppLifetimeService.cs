using FrpGUI.Configs;
using FrpGUI.Models;
using FrpGUI.Services;
using FrpGUI.WebAPI.Services;

namespace FrpGUI.WebAPI;

public class WebAppLifetimeService(AppConfig config, LoggerBase logger, FrpProcessCollection processes, WebConfigService serverConfigService) :
    AppLifetimeService(config, logger, processes)
{
    private readonly WebConfigService serverConfigService = serverConfigService;

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        if (serverConfigService.ServerOnly())
        {
            Config.FrpConfigs.RemoveAll(p => p is ClientConfig);
        }
        return base.StartAsync(cancellationToken);
    }
}
