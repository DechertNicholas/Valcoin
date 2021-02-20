using System;
using Valcoin_Core;

namespace ValUI_CLI
{
    class Program
    {
        static void Main(string[] args)
        {
            var node = Node.GetInstance;
            node.StartNode();
            PrintMenu();
            GetAndInvokeUserChoice();
        }

        private static void PrintMenu()
        {
            Console.WriteLine("Welcome to the Valcoin Wallet/Miner!");
            Console.WriteLine("Version: 0.0.1a");
            Console.WriteLine("Address: <addr>");
            Console.WriteLine("");
            Console.WriteLine("");
            Console.WriteLine("");
            Console.WriteLine("[M] : Begin mining");
            Console.WriteLine("[E] : Exit");
        }

        private static void GetAndInvokeUserChoice()
        {
            var choice = "";
            while (choice != "E")
            {
                choice = Console.ReadLine().ToUpper();
                if (choice == "M")
                {
                    var miner = new Miner();
                    miner.BeginMining();
                }
            }
            
        }
    }
}
