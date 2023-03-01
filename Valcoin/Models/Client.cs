using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Valcoin.Models
{
    public class Client
    {
        /// <summary>
        /// Unique ID for the client, mainly for DB operations.
        /// </summary>
        public int ClientId { get; set; }

        /// <summary>
        /// The internet address the client is reachable at.
        /// </summary>
        public string Address { get; set; }

        /// <summary>
        /// The port the client is listening on.
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// The last time the client communicated with us. Useful for finding which nodes are active and which are dead.
        /// </summary>
        public DateTime LastCommunicationUTC { get; set; }

        public Client(string address, int port)
        {
            Address = address;
            Port = port;
        }
    }
}
