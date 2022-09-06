using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EstateCommission.Business
{
    public class Details
    {
        public object authorization_code { get; set; }
        public string account_number { get; set; }
        public string account_name { get; set; }
        public string bank_code { get; set; }
        public string bank_name { get; set; }
    }
}
