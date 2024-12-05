using System.Net;
using Undersoft.SDK.Utilities;

namespace Undersoft.SDK.Ethernet
{
    public class EthernetContext
    {        
        public string Method;

        public IPEndPoint LocalEndPoint;

        public IPEndPoint RemoteEndPoint;

        private Type type;

        public TransitComplexity Complexity { get; set; } = TransitComplexity.Standard;

        public Type Type
        {
            get
            {
                if (type == null && TypeName != null)
                    Type = AssemblyUtilities.FindType(TypeName);
                return type;
            }
            set
            {
                if (value != null)
                {
                    TypeName = value.FullName;
                    type = value;
                }
            }
        }

        public string TypeName { get; set; }

        public string Notice { get; set; }

        public int Errors { get; set; }

        public EthernetSite Site { get; set; } = EthernetSite.Client;

        public int ItemsCount { get; set; } = 0;

        public bool ReceiveMessage { get; set; } = true;

        public bool SendMessage { get; set; } = true;

        public bool Synchronic { get; set; } = false;
    }
}
