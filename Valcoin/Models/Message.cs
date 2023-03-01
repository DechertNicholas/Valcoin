using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Valcoin.Models
{
    public enum MessageType
    {
        Sync,
        BlockRequest,
        ClientRequest, // potentially unused
        ClientShare
    }

    /// <summary>
    /// A control message sent over the Valcoin network to do things like request blocks or
    /// exchange other non-block information.
    /// </summary>
    public class Message
    {
        public MessageType MessageType { get; set; }
        public ulong HighestBlockNumber { get; set; } = 0;
        public string BlockId { get; set; }
        public List<Client> Clients { get; set; } = new();

        public static implicit operator byte[](Message m) => JsonSerializer.SerializeToUtf8Bytes(m);

        /// <summary>
        /// Constructor for JSON. Not intended for normal use.
        /// </summary>
        [JsonConstructor]
        public Message() { }

        /// <summary>
        /// Sync control message
        /// </summary>
        /// <param name="highestBlockNumber">The highest block number the client has.</param>
        /// <param name="blockId">The blockId of the client's highest main-chain block.</param>
        public Message(ulong highestBlockNumber, string blockId)
        {
            MessageType = MessageType.Sync;
            HighestBlockNumber = highestBlockNumber;
            BlockId = blockId;
        }

        /// <summary>
        /// Block Request message. Gets a specified block from the network.
        /// </summary>
        public Message(string blockId)
        {
            MessageType = MessageType.BlockRequest;
            BlockId = blockId;
        }

        /// <summary>
        /// Client share message. Contains a list of clients to share to the recipient. 
        /// </summary>
        /// <param name="clients"></param>
        public Message(List<Client> clients)
        {
            MessageType = MessageType.ClientShare;
            clients.ForEach(c => Clients.Add(c));
        }
    }
}
