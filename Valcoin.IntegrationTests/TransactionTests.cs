using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Valcoin.Models;

namespace Valcoin.IntegrationTests
{
    public class TransactionTests
    {
        readonly Wallet wallet;

        public TransactionTests()
        {
            // Create the test wallet with static data
            // this is a random public/private keyset that was generated. Not in use.
            var D = "A2ED2801588AD588FF87E18117C94CA53371A3BEE1F574F08553502CDC846A11DAF1173D9337D0C23C84C59F2C5FF9726C54EEBF1EDCF5EACE6E99FFE4F9A96B20DD205B0BE86A302DE1E113C6D616555198C3447B0419CD1DC691D3034267CB61FC710FE4104F180EDAF433E3C71F9D05A1DFCF06EEFAC7E940B90C74F6692F59B2B1C14ACFA89F2EE864BB1F71B58F43B463B05A2C21F0457D3F0BA6ECA1BC9B9250D3E03694C7E9ACF05FABBECA9E1509307DECF531B7D36C85DED5059F0BD7DF88BFA82E26BC143F9ECAF6CC43972DC74109478008FB7269E403FAFEA35E99226C7460613BBAAE54022994F14C839BDE2F0009DFCB17927863A4E09632D9";
            var DP = "2D5D4762DD8DF863B507A3C55BDD22E30D77D5BF406D8ED49A468390280C251CCBEE46AA921AB8A0088583C443FA1F4305A357C6EC367A0D39CE6298C5D920395CEC4A0F78DE309DEA9B15E25FC8102B615AC337A2392F6933C876C511D1E774044ED87CD7C0467483F228C2599A3C67A351F221A30FE75BB23DE96CCF1D8597";
            var DQ = "2A41C80D840192AFC655078DD293E8E476416CBFD98BB21CF91F3DAAE161FCF010292C667BA2E194E98ED11CBF46CE8135BE0043C4AD3832672BF18E70205566D9619D66B5CDDE8E9E0252B814E519BD66117BADF14E7B27FE4EE0501B0306CB8B86CC6A8204133FD916319EE4CFBCF99479241870CCEE6468CB2AAEEB9AF23D";
            var Exponent = "010001";
            var InverseQ = "040EAC7AD364F655CE0B1B68850311C9AAC2A4AAD13EECC03A11F93BA732B0A43965961F1B3ECC573CA8363A58FAEB283F47C25456FB69BF2DB660F1D274E8A69ACD54AA1045B58D3590D418E9DA48B97FD12E8D16311F25E6A44AF670454D595827799C313A423E9DD3A0910487DEA64F2DF88BEB409E8E26C7C1FF346D5D98";
            var Modulus = "B6A18C735A42F307A1D89D51AF2E680BFAF36511C69FC78F407A8E216BACF9ADAFBE81689033DC359E098E04050162033AFE9EFF1668FC2C0CE594D28DE4329153D108156BDC431E34383782B38387A7D4F9E1EA65AC879A2B4B5AABA5795841206467FC196596C1DBC699DC01F98D50E55D4C0AFE5A8A2AAA54B144D96FDD903A4A3AED7DDCF447C28868763AD3988F712816A6B8D39919D5BC6CC433B0EB19992DCDAD4A0E1638A55F38B3EAFA02DECE49E60C13A0C8A3930EEC5D4DD3AFED166BC86A63AAF88E0F80A74EEEC1905A028D140B2000ED62F40D8F70BCE0DC903348999292BCD0A840C57A9FDF70133D42F7DA4752CAA912399F614A5A139A05";
            var P = "E2DD074008748ABA591A90252FC5873DC16DBFCAAC9576B6E76386782136EF28FBFA30BC53F2110C81454B6B24527FBD35A120FFBEF033A4BB5E1B0EF3DDF8E6239FCAC885420942EDFD3C54E3F9FB7F0613F9B7B618EB3E0BA2D4C2CF51C6E1267AF743111352DC837B05CFCBBCAF24E535C4915C36AA7352F49AD841FE7D77";
            var Q = "CE1637FEB4A83A8C75983FC251301C81ADE3F053A319F0B238B2BF140FA34E7D24F17E86CD3CCB4D715854A6E3AEBA024A6F23F67E2200ADBA657040A6B4C7A2189F3D14FA7C85494761839BB607D2A19ED2462BC235599F53EA2E152BD66DA09037EE5897C45BAB589F9E40F86450CC8DA282DE5FBC5141B563EDC056C0D363";

            RSAParameters rsaParams = new()
            {
                D = Convert.FromHexString(D),
                DP = Convert.FromHexString(DP),
                DQ = Convert.FromHexString(DQ),
                Exponent = Convert.FromHexString(Exponent),
                InverseQ = Convert.FromHexString(InverseQ),
                Modulus = Convert.FromHexString(Modulus),
                P = Convert.FromHexString(P),
                Q = Convert.FromHexString(Q)
            };

            var rsa = RSA.Create(rsaParams);
            wallet = new Wallet(rsa.ExportRSAPublicKey(), rsa.ExportRSAPrivateKey());
        }

        [Fact]
        public void BuildTransaction()
        {
            ulong blockId = 10; // this tx is part of block 10

            var input = new TxInput
            {
                PreviousTransactionId = "0000000000000000000000000000000000000000000000000000000000000000", // coinbase
                PreviousOutputIndex = -1, // 0xffffffff
                UnlockerPublicKey = wallet.PublicKey, // this doesn't matter for the coinbase transaction
                UnlockSignature = wallet.SignData(new UnlockSignatureStruct { BlockId = blockId, PublicKey = wallet.PublicKey }) // neither does this
            };

            var output = new TxOutput
            {
                Amount = 50,
                LockSignature = wallet.AddressBytes // this does though, as no one should spend these coins other than the owner
                                                    // of this hashed public key
            };

            var tx = new Transaction(new TxInput[] { input }, new TxOutput[] { output });

            // assert on field that are generated and not statically assigned in the test
            Assert.NotNull(tx.TxId);
            Assert.NotNull(tx.Inputs);
            Assert.NotNull(tx.Outputs);
            Assert.NotNull(tx.JsonInputs);
            Assert.NotNull(tx.JsonOutputs);
            Assert.NotNull(tx.Inputs[0].UnlockerPublicKey);
            Assert.NotNull(tx.Inputs[0].UnlockSignature);
            Assert.NotNull(tx.Outputs[0].LockSignature);
        }

        [Fact]
        public void VerifyCorrectDataSigning()
        {
            // first, assert this configuration even worked
            var publicKey = "3082010A0282010100B6A18C735A42F307A1D89D51AF2E680BFAF36511C69FC78F407A8E216BACF9ADAFBE81689033DC359E098E04050162033AFE9EFF1668FC2C0CE594D28DE4329153D108156BDC431E34383782B38387A7D4F9E1EA65AC879A2B4B5AABA5795841206467FC196596C1DBC699DC01F98D50E55D4C0AFE5A8A2AAA54B144D96FDD903A4A3AED7DDCF447C28868763AD3988F712816A6B8D39919D5BC6CC433B0EB19992DCDAD4A0E1638A55F38B3EAFA02DECE49E60C13A0C8A3930EEC5D4DD3AFED166BC86A63AAF88E0F80A74EEEC1905A028D140B2000ED62F40D8F70BCE0DC903348999292BCD0A840C57A9FDF70133D42F7DA4752CAA912399F614A5A139A050203010001";
            Assert.Equal(Convert.ToHexString(wallet.PublicKey), publicKey);

            ulong blockId = 10; // this tx is part of block 10
            var input = new TxInput
            {
                PreviousTransactionId = new string('0', 64), // coinbase
                PreviousOutputIndex = -1, // 0xffffffff
                UnlockerPublicKey = wallet.PublicKey, // this doesn't matter for the coinbase transaction
                UnlockSignature = wallet.SignData(new UnlockSignatureStruct { BlockId = blockId, PublicKey = wallet.PublicKey }) // neither does this
            };

            var output = new TxOutput
            {
                Amount = 50,
                LockSignature = wallet.AddressBytes // this does though, as no one should spend these coins other than the owner
                                                    // of this hashed public key
            };

            Assert.Equal(output.LockSignature, wallet.AddressBytes);
            Assert.Equal(input.UnlockerPublicKey, wallet.PublicKey);
            Assert.True(wallet.VerifyData(
                new UnlockSignatureStruct { BlockId = blockId, PublicKey = wallet.PublicKey },
                input.UnlockSignature)
            );
        }
    }
}
