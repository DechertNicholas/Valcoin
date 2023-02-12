// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Valcoin.Models;
using Valcoin.Services;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Valcoin.Views
{
    /// <summary>
    /// The page dedicated to controlling the <see cref="Miner"/> and viewing related mining status.
    /// </summary>
    public sealed partial class MiningPage : Page
    {
        public MiningPage()
        {
            this.InitializeComponent();

            // testing
            //var wallet = new Wallet();
            //ulong blockId = 10; // this tx is part of block 10

            //var input = new TxInput
            //{
            //    PreviousTransactionId = new string('0', 64), // coinbase
            //    PreviousOutputIndex = -1, // 0xffffffff
            //    UnlockerPublicKey = wallet.PublicKey, // this doesn't matter for the coinbase transaction
            //    UnlockSignature = wallet.SignData(new UnlockSignatureStruct { BlockNumber = blockId, PublicKey = wallet.PublicKey }) // neither does this
            //};

            //var output = new TxOutput
            //{
            //    Amount = 50,
            //    LockSignature = wallet.AddressBytes // this does though, as no one should spend these coins other than the owner
            //                                        // of this hashed public key
            //};

            //var tx = new Transaction(blockId, new TxInput[] { input }, new TxOutput[] { output });

            ////await NetworkService.SendData(new byte[] {0,1,2,3,4,5,6,7,8,9});
            //NetworkService.SendData(tx);
        }
    }
}
