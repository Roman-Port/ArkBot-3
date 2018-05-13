using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace ArkIOInterfaceData
{
    [DataContract]
    public class ArkIOInterfaceResponse
    {
        [DataMember]
        public int minStep; //Inclusive
        [DataMember]
        public int currentStep;
        [DataMember]
        public int maxStep; //Inclusive

        [DataMember]
        public bool hasFinished;
        [DataMember]
        public bool hasErrored;

        [DataMember]
        public string[] output;

        [DataMember]
        public int clientId;

        public ArkIOInterfaceResponse(int _minStep, int _currentStep, int _maxStep, bool _finished, bool _error, int _clientId)
        {
            hasErrored = _error;
            hasFinished = _finished;
            minStep = _minStep;
            maxStep = _maxStep;
            currentStep = _currentStep;
            output = new string[0];
            clientId = _clientId;
        }
    }
}
