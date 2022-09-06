using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EstateCommission.Business
{
    public class RecipientCodeRequest
    {
        public string account_number { get; set; }
        public string name { get; set; }
        public string bank_code { get; set; }
        public string currency { get; set; }
        public string type { get; set; }
    }
}
