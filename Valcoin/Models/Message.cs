using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Valcoin.Services;

namespace Valcoin.Models
{
    /// <summary>
    /// The type of message.
    /// </summary>
    public enum MessageType
    {
        Sync,
        SyncResponse,
        BlockRequest,
        ClientRequest,
        ClientShare,
        BlockShare,
        TransactionShare
    }

    /// <summary>
    /// A control message sent over the Valcoin network to provide context to the information being sent. Not all properties need to be filled.
    /// </summary>
    public class Message
    {
        public int ListenPort { get; private set; } = NetworkService.ListenPort;
        public MessageType MessageType { get; set; }
        public long HighestBlockNumber { get; set; } = 0;
        public string BlockId { get; set; }
        public List<Client> Clients { get; set; } = new();
        public ValcoinBlock Block { get; set; }
        public Transaction Transaction { get; set; }

        public static implicit operator byte[](Message m) => JsonSerializer.SerializeToUtf8Bytes(m);

        /// <summary>
        /// Constructor for JSON. Not intended for normal use.
        /// </summary>
        [JsonConstructor]
        public Message() { }

        /// <summary>
        /// Constructor to create a new Message while only specifying the message type. Be sure to fill any needed properties yourself, as it does
        /// nothing on its own.
        /// </summary>
        /// <param name="type"></param>
        public Message(MessageType type)
        {
            MessageType = type;
        }

        /// <summary>
        /// Sync request control message.
        /// </summary>
        /// <param name="highestBlockNumber">The highest block number the client has.</param>
        /// <param name="blockId">The blockId of the client's highest main-chain block.</param>
        public Message(long highestBlockNumber, string blockId)
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
        public Message(List<Client> clients, int listenPort)
        {
            MessageType = MessageType.ClientShare;
            clients.ForEach(c => Clients.Add(c));
        }

        /// <summary>
        /// Block Share control message. Send a requested block to a client, or a newly found block to all clients.
        /// </summary>
        /// <param name="block"></param>
        public Message(ValcoinBlock block)
        {
            MessageType = MessageType.BlockShare;
            Block = block;
        }

        /// <summary>
        /// Transaction Share control message. Send a newly transacted transaction to the network.
        /// </summary>
        /// <param name="tx"></param>
        public Message(Transaction tx)
        {
            MessageType = MessageType.BlockShare;
            Transaction = tx;
        }
    }
}
