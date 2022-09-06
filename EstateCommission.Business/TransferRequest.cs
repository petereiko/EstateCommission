using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EstateCommission.Business
{
    public class TransferRequest
    {
        public string amount { get; set; }
        public string reason { get; set; }
        public string recipient { get; set; }
        public string source { get; set; }
    }
}
