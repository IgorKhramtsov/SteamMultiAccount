using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SteamKit2;
using SteamKit2.Internal;
using System.IO;
using System.Net;

namespace SteamMultiAccount
{
    internal sealed class CustomHandler : ClientMsgHandler
    {
        /*
        //
        // Callbacks
        //
        */
        internal sealed class PurchaseResponseCallback : CallbackMsg
        {
            internal enum EPurchaseResult
            {
                Unknown = -1,
                OK = 0,
                AlreadyOwned = 9,
                RegionLocked = 13,
                InvalidKey = 14,
                DuplicatedKey = 15,
                BaseGameRequired = 24,
                OnCooldown = 53
            }

            internal readonly EPurchaseResult PurchaseResult;
            internal readonly KeyValue ReceiptInfo;
            internal readonly Dictionary<uint, string> Items;

            internal PurchaseResponseCallback(JobID jobID, CMsgClientPurchaseResponse body)
            {
                JobID = jobID;

                PurchaseResult = (EPurchaseResult) body.purchase_result_details;

                if (body.purchase_receipt_info == null)
                    return;

                ReceiptInfo = new KeyValue();
                using (var stream = new MemoryStream(body.purchase_receipt_info))
                {
                    if (!ReceiptInfo.TryReadAsBinary(stream))
                        return;

                    List<KeyValue> lineitems = ReceiptInfo["lineitems"].Children;
                    Items = new Dictionary<uint, string>(lineitems.Count);

                    foreach (var lineitem in lineitems)
                    {
                        uint appID = (uint) lineitem["PackageID"].AsUnsignedLong();
                        string gameName = (string) lineitem["ItemDescription"].AsString();
                        gameName = WebUtility.UrlDecode(gameName); // Apparently steam expects client to decode sent HTML
                        Items.Add(appID, gameName);
                    }
                }
            }
        }

        /*
        //
        // Methods
        //
        */

        internal async Task<PurchaseResponseCallback> KeyActivate(string key)
        {
            if (string.IsNullOrEmpty(key) || !Client.IsConnected)
                return null;

            var request = new ClientMsgProtobuf<CMsgClientRegisterKey>(EMsg.ClientRegisterKey) {SourceJobID = Client.GetNextJobID()};
            request.Body.key = key;
            Client.Send(request);
            try {
                return await new AsyncJob<PurchaseResponseCallback>(Client, request.SourceJobID);
            } catch(Exception e) { Logging.LogToFile("Cant get callback from custom handler: "+e.Message); return null; }
        }
        public override void HandleMsg(IPacketMsg packetMsg)
        {
            if (packetMsg == null)
                return;
            if (packetMsg.MsgType == EMsg.Multi || packetMsg.MsgType == EMsg.ClientClanState)
                return;
            switch (packetMsg.MsgType)
            {
                case EMsg.ClientPurchaseResponse:
                    HandlePurchaseResponse(packetMsg);
                    break;
            }
        }

        private void HandlePurchaseResponse(IPacketMsg packetMsg)
        {
            var response = new ClientMsgProtobuf<CMsgClientPurchaseResponse>(packetMsg);
            Client.PostCallback(new PurchaseResponseCallback(packetMsg.TargetJobID, response.Body));
        }
    }
}
