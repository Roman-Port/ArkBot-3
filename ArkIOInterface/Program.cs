using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RomansSimpleNetworking;
using ArkIOInterfaceData;

namespace ArkIOInterface
{
    class Program
    {
        private static RSN_Server server;
        private static Random rand;
        private static int requestNum = 0;

        static Dictionary<int, RSN_ServerResponse_Data> requests =
            new Dictionary<int, RSN_ServerResponse_Data>();

        static void Main(string[] args)
        {
            rand = new Random();
            
            //This program sits on the same computer as the ARK server and is used to communicate with it.
            //It connects via TCP to communicate data.
            List<RSN_Server_CallbackConfig> callbacks = new List<RSN_Server_CallbackConfig>();

            //Add the callback with ID 1
            callbacks.Add(new RSN_Server_CallbackConfig(1, new RSN_ServerResponse(OnRequest), typeof(ArkIOInterfaceRequestData)));

            //Start server
            server = RSN_Server.CreateServer(callbacks.ToArray(), "password", 13000, false);

            Console.WriteLine("Ark IO Interface ready!");

            Console.ReadLine();
        }

        static void OnRequest(object raw, RSN_ServerResponse_Data data)
        {
            ArkIOInterfaceRequestData req = (ArkIOInterfaceRequestData)raw;
            req.id = data.packet.id;
            ArkIOInterface_Updates callback = new ArkIOInterface_Updates(OnRequestUpdate);
            Console.WriteLine("Command " + req.type.ToString().ToUpper()+" issued.");
            int id = requestNum;
            requestNum++;

            requests.Add(id, data);

            switch (req.type)
            {
                case RequestType.Compress:
                    ArkIOInterfaceRequests.Cmd_Compress(id, req, callback);
                    break;
                case RequestType.Copy:
                    ArkIOInterfaceRequests.Cmd_Copy(id, req, callback);
                    break;
                case RequestType.Delete:
                    ArkIOInterfaceRequests.Cmd_Delete(id, req, callback);
                    break;
                case RequestType.DirList:
                    ArkIOInterfaceRequests.Cmd_DirList(id, req, callback);
                    break;
                case RequestType.EndProcess:
                    ArkIOInterfaceRequests.Cmd_StopProcess(id, req, callback);
                    break;
                case RequestType.Move:
                    ArkIOInterfaceRequests.Cmd_Move(id, req, callback);
                    break;
                case RequestType.StartProcess:
                    ArkIOInterfaceRequests.Cmd_StartProcess(id, req, callback);
                    break;
                case RequestType.GetProcessByName:
                    ArkIOInterfaceRequests.Cmd_GetProcessByName(id, req, callback);
                    break;
            }
        }

        static void OnRequestUpdate(ArkIOInterfaceResponse data, int id)
        {
            if(!data.hasErrored && data.currentStep!=data.maxStep)
            {
                //Not important. Ignore
                return;
            }
            ArkIOInterfaceRequestData d = new ArkIOInterfaceRequestData();
            d.response = data;
            d.id = data.clientId;
            requests[id].Respond(d);
        }
    }
}
