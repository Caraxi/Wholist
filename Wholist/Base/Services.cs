using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Objects;
using Dalamud.IoC;
using Dalamud.Plugin;
using Sirensong;
using Wholist.Managers;
using XivCommon;

namespace Wholist.Base
{
    /// <summary>
    ///     Provides access to necessary instances and services.
    /// </summary>
    internal sealed class Services
    {
        [PluginService] internal static DalamudPluginInterface PluginInterface { get; private set; } = null!;
        [PluginService] internal static ClientState ClientState { get; private set; } = null!;
        [PluginService] internal static ObjectTable ObjectTable { get; private set; } = null!;
        [PluginService] internal static Dalamud.Game.Command.CommandManager Commands { get; private set; } = null!;

        internal static CommandManager CommandManager { get; private set; } = null!;
        internal static WindowManager WindowManager { get; private set; } = null!;
        internal static ResourceManager ResourceManager { get; private set; } = null!;
        internal static XivCommonBase XivCommon { get; private set; } = null!;
        internal static Configuration Configuration { get; private set; } = null!;

        /// <summary>
        ///     Initializes the service class.
        /// </summary>
        internal static void Initialize(DalamudPluginInterface pluginInterface)
        {
            BetterLog.Debug("Initializing services.");

            SirenCore.InjectServices<Services>();
            pluginInterface.Create<Services>();

            ResourceManager = new ResourceManager();
            Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            WindowManager = new WindowManager();
            CommandManager = new CommandManager();
            XivCommon = new XivCommonBase();

        }

        /// <summary>
        ///     Disposes of the service class.
        /// </summary>
        internal static void Dispose()
        {
            BetterLog.Debug("Disposing of services.");

            ResourceManager?.Dispose();
            WindowManager?.Dispose();
            CommandManager?.Dispose();
            XivCommon?.Dispose();
        }
    }
}
