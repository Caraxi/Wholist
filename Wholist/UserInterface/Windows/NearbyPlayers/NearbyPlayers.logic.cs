using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using Dalamud.Hooking;
using Dalamud.Memory;
using Dalamud.Utility;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using ImGuiNET;
using Sirensong.Game.Enums;
using Sirensong.Game.Helpers;
using Wholist.Common;
using Wholist.Configuration;
using Wholist.DataStructures;
using Wholist.Game;
using Wholist.Resources.Localization;

namespace Wholist.UserInterface.Windows.NearbyPlayers
{
    internal sealed class NearbyPlayersLogic : IDisposable
    {
        #region PlaceholderDelegate

        private readonly nint placeholderNamePtr = Marshal.AllocHGlobal(128);
        private readonly string placeholder = $"<{Guid.NewGuid():N}>";
        private string? replacementName;

        private unsafe delegate nint ResolveTextCommandPlaceholderDelegate(nint a1, byte* placeholderText, byte a3, byte a4);

        [Signature("E8 ?? ?? ?? ?? 49 8D 4F 18 4C 8B E0", DetourName = nameof(ResolveTextCommandPlaceholderDetour))]
        private Hook<ResolveTextCommandPlaceholderDelegate>? ResolveTextCommandPlaceholderHook { get; init; }

        private unsafe nint ResolveTextCommandPlaceholderDetour(nint a1, byte* placeholderText, byte a3, byte a4)
        {
            var placeholder = MemoryHelper.ReadStringNullTerminated((nint)placeholderText);
            if (this.replacementName == null || placeholder != this.placeholder)
            {
                return this.ResolveTextCommandPlaceholderHook!.Original(a1, placeholderText, a3, a4);
            }
            MemoryHelper.WriteString(this.placeholderNamePtr, this.replacementName);
            this.replacementName = null;
            return this.placeholderNamePtr;
        }

        #endregion

        /// <summary>
        ///     The search text to use when filtering the object table.
        /// </summary>
        internal string SearchText = string.Empty;

        public NearbyPlayersLogic()
        {
            Services.GameInteropProvider.InitializeFromAttributes(this);
            this.ResolveTextCommandPlaceholderHook?.Enable();
        }

        public void Dispose()
        {
            Marshal.FreeHGlobal(this.placeholderNamePtr);
            this.ResolveTextCommandPlaceholderHook?.Dispose();
        }

        /// <inheritdoc cref="MapHelper.FlagAndOpenCurrentMap(Vector3, string?, MapType)" />
        internal static void FlagAndOpen(Vector3 position, string? title = null, MapType mapType = MapType.FlagMarker) => MapHelper.FlagAndOpenCurrentMap(position, title, mapType);

        /// <inheritdoc cref="PlayerManager.GetNearbyPlayersSlim" />
        internal List<PlayerInfoSlim> GetNearbyPlayers()
        {
            var players = new List<PlayerInfoSlim>();
            var nearbyPlayers = PlayerManager.GetNearbyPlayersSlim(
                Services.Configuration.NearbyPlayers.MaxPlayersToShow,
                Services.Configuration.NearbyPlayers.FilterAfk,
                Services.Configuration.NearbyPlayers.PrioritizeKnown);

            foreach (var player in nearbyPlayers)
            {
                if (!this.SearchText.IsNullOrWhitespace() && !player.Name.Contains(this.SearchText, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                players.Add(player);
            }
            return players;
        }

        /// <summary>
        ///     Searches for the player on the lodestone.
        /// </summary>
        /// <param name="name">The name of the player.</param>
        /// <param name="homeworld">The name of the player's homeworld.</param>
        internal static void SearchPlayerOnLodestone(string name, string homeworld)
        {
            switch (Services.Configuration.NearbyPlayers.LodestonePlayerSearchRegion)
            {
                case PluginConfiguration.NearbyPlayersConfiguration.LodestoneSearchRegion.Europe:
                    Util.OpenLink($"https://eu.finalfantasyxiv.com/lodestone/character/?q={name}&worldname={homeworld}");
                    break;
                case PluginConfiguration.NearbyPlayersConfiguration.LodestoneSearchRegion.Germany:
                    Util.OpenLink($"https://de.finalfantasyxiv.com/lodestone/character/?q={name}&worldname={homeworld}");
                    break;
                case PluginConfiguration.NearbyPlayersConfiguration.LodestoneSearchRegion.France:
                    Util.OpenLink($"https://fr.finalfantasyxiv.com/lodestone/character/?q={name}&worldname={homeworld}");
                    break;
                case PluginConfiguration.NearbyPlayersConfiguration.LodestoneSearchRegion.NorthAmerica:
                    Util.OpenLink($"https://na.finalfantasyxiv.com/lodestone/character/?q={name}&worldname={homeworld}");
                    break;
                case PluginConfiguration.NearbyPlayersConfiguration.LodestoneSearchRegion.Japan:
                    Util.OpenLink($"https://jp.finalfantasyxiv.com/lodestone/character/?q={name}&worldname={homeworld}");
                    break;
                default:
                    throw new NotImplementedException("No link handler for specified region");
            }

        }

        /// <summary>
        ///     Sets the chat target to the given player.
        /// </summary>
        /// <param name="name">The name of the player.</param>
        /// <param name="homeworldName">The homeworld name of the player.</param>
        internal static unsafe void SetChatTellTarget(string name, string homeworld)
        {
            UIModule.Instance()->ProcessChatBoxEntry(Utf8String.FromString($"/tell {name}@{homeworld}"));
            ChatHelper.Print(Strings.UserInterface_NearbyPlayers_SetChatTarget.Format($"{name}@{homeworld}"));
            UIGlobals.PlaySoundEffect((uint)SoundEffect.Se16, 0, 0, 0);
        }

        /// <summary>
        ///     Shows the 'add this user to blacklist' UI.
        /// </summary>
        /// <param name="name">The name of the player.</param>
        /// <param name="homeworldName">The homeworld name of the player.</param>
        internal unsafe void PromptUserBlacklist(string name, string homeworld)
        {
            this.replacementName = $"{name}@{homeworld}";
            UIModule.Instance()->ProcessChatBoxEntry(Utf8String.FromString($"/blacklist add {this.placeholder}"));
        }

        /// <summary>
        ///     Gets the job name of the given player based on the current configuration.
        /// </summary>
        /// <param name="job">The job of the player.</param>
        /// <returns>The job name.</returns>
        internal static string GetJobName(JobInfoSlim job) => Services.Configuration.NearbyPlayers.UseJobAbbreviations ? job.Abbreviation : job.Name;

        /// <summary>
        ///     Gets the job colour of the given player based on the current configuration.
        /// </summary>
        /// <param name="job">The job of the player.</param>
        /// <returns>The job colour.</returns>
        /// <exception cref="NotImplementedException">When the colour mode is not implemented or unknown.</exception>
        internal static Vector4 GetJobColour(JobInfoSlim job) => Services.Configuration.Colours.JobColMode switch
        {
            PluginConfiguration.ColourConfiguration.JobColourMode.Job => job.JobColour,
            PluginConfiguration.ColourConfiguration.JobColourMode.Role => job.RoleColour,
            _ => throw new NotImplementedException()
        };

        internal static bool IsPvP => Services.ClientState.IsPvP;
    }
}
