using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace ArkIOInterfaceData
{
    [DataContract]
    public class ArkIOInterfaceRequestData
    {
        //Used to send data between the server and the interface.
        [DataMember]
        public RequestType type;

        [DataMember]
        public string strArgOne;
        [DataMember]
        public string strArgTwo;

        [DataMember]
        public int intArgOne;
        [DataMember]
        public int intArgTwo;

        [DataMember]
        public ArkIOInterfaceResponse response;

        [DataMember]
        public int id;
    }

    public enum RequestType
    {
        Copy,
        Move,
        Delete,
        DirList,
        Compress,
        StartProcess,
        EndProcess,
        GetProcessByName
    }
}
