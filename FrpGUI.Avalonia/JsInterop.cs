using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;

namespace FrpGUI.Avalonia;

[SupportedOSPlatform("browser")]
public partial class JsInterop
{
    [JSImport("showAlert", "utils.js")]
    public static partial string Alert(string message);

    [JSImport("getCurrentUrl", "utils.js")]
    public static partial string GetCurrentUrl();

    [JSImport("getLocalStorage", "utils.js")]
    public static partial string GetLocalStorage(string key);

    [JSImport("openUrl", "utils.js")]
    public static partial void OpenUrl(string url);

    [JSImport("reload", "utils.js")]
    public static partial void Reload();

    [JSImport("setLocalStorage", "utils.js")]
    public static partial void SetLocalStorage(string key, string value);
}