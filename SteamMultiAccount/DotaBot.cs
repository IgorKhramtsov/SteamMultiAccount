using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SteamKit2.GC.Dota.Internal;
using SteamKit2;
using SteamKit2.GC;
using SteamKit2.GC.Internal;
using SteamKit2.GC.Dota;
using Dota2.GC.Internal;
using Dota2.GC;
using Dota2.GC.Dota.Internal;
using Dota2.GC.Dota;

namespace SteamMultiAccount
{
    class DotaBot
    {
        internal bool initialized = false;
        Logging logging;
        SteamClient sClient;
        DotaGCHandler dotaCoordinator;
        delegate Task dCallbcak(IPacketGCMsg msg);
        internal const uint appID = 570;
        internal DotaBot(Logging _logging, SteamClient client, CallbackManager callbackManager)
        {
            if (_logging == null || client == null)
                return;
            logging = _logging;
            sClient = client;

            callbackManager.Subscribe<DotaGCHandler.PartyInviteSnapshot>(OnPartyInviteSnapshot);
            callbackManager.Subscribe<DotaGCHandler.GCWelcomeCallback>(OnClientWelcome);
            callbackManager.Subscribe<DotaGCHandler.LobbyInviteSnapshot>(OnLobbyInviteSnapshot);
            callbackManager.Subscribe<DotaGCHandler.PracticeLobbySnapshot>(OnLobbySnapshot);


            initialized = true;
        }
        
        internal async Task<bool> Initialize()
        {
            bool result = true;
            if (!initialized)
                return false;

            DotaGCHandler.Bootstrap(sClient);
            dotaCoordinator = sClient.GetHandler<DotaGCHandler>();
            dotaCoordinator.Start();

            return result;
        }
        internal void OnClientWelcome(DotaGCHandler.GCWelcomeCallback msg)
        {
            logging.Log($"Game coordinator is welcoming us, version is {msg.Version}");
        }
        internal void OnPartyInviteSnapshot(DotaGCHandler.PartyInviteSnapshot msg)
        {
            logging.Log($"Accept invite from {msg.invite.sender_name}");
            dotaCoordinator.RespondPartyInvite(msg.invite.group_id);
        }
        internal void OnLobbyInviteSnapshot(DotaGCHandler.LobbyInviteSnapshot msg)
        {
            logging.Log($"Accept lobby invite from {msg.invite.sender_name}");
            dotaCoordinator.RespondLobbyInvite(msg.invite.group_id);
            
        }
        internal void OnLobbySnapshot(DotaGCHandler.PracticeLobbySnapshot msg)
        {
            dotaCoordinator.JoinTeam(msg.lobby.members.First().team,2);
        }
        internal void OnPartyInviteLeave(DotaGCHandler.PartyInviteLeave msg)
        {
            logging.Log("Party invite leave.");
        }
    }
}
