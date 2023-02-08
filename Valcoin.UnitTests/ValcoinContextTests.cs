using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Valcoin.Services;

namespace Valcoin.UnitTests
{
    [CollectionDefinition("RequiresDB", DisableParallelization = true)]
    public class ValcoinContextTests
    {
        //[Fact]
        //public void ValcoinContextTestsBuildDB()
        //{
        //    var Db = new ValcoinContext();
        //    Assert.NotNull(Db);
        //    Db.Dispose();
        //}
    }
}
