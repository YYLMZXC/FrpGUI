using FrpGUI.Configs;
using FrpGUI.Enums;
using FrpGUI.Services;

namespace FrpGUI.Models;

public class FrpProcessCollection(AppConfig config, LoggerBase logger) : Dictionary<string, FrpProcess>
{
    public IFrpProcess GetOrCreateProcess(string id)
    {
        if (TryGetValue(id, out FrpProcess process))
        {
            return process;
        }
        var frp = GetFrpConfig(id);
        process = new FrpProcess(frp, logger);
        Add(id, process);
        return process;
    }

    protected FrpConfigBase GetFrpConfig(string id)
    {
        var client = config.Clients.FirstOrDefault(p => p.ID == id);
        if (client != null)
        {
            return client;
        }
        var server = config.Servers.FirstOrDefault(p => p.ID == id);
        if (server != null)
        {
            return server;
        }
        throw new ArgumentException($"找不到ID为{id}的配置");
    }

    public IList<IFrpProcess> GetAll()
    {
        List<IFrpProcess> list = new List<IFrpProcess>();
        foreach (var item in config.Servers)
        {
            list.Add(GetOrCreateProcess(item.ID));
        }
        foreach (var item in config.Clients)
        {
            list.Add(GetOrCreateProcess(item.ID));
        }
        return list;
    }

    public async Task<FrpConfigBase> RemoveFrpAsync(string id)
    {
        var frp = GetOrCreateProcess(id);
        if (frp.ProcessStatus == ProcessStatus.Running)
        {
            await frp.StopAsync();
        }
        switch(frp.Config)
        {
            case ServerConfig s:
                config.Servers.Remove(s);
                break;
            case ClientConfig c:
                config.Clients.Remove(c);
                break;
                throw new ArgumentOutOfRangeException();
        }
        Remove(frp.Config.ID);
        config.Save();
        return frp.Config;
    }
}