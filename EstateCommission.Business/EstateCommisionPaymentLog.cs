using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EstateCommission.Business
{
    public class EstateCommisionPaymentLog
    {
        public int i_account { get; set; }
        public decimal Amount { get; set; }
        public DateTime DateCreated { get; set; }
        public int i_customer { get; set; }
        public int i_product { get; set; }
        public string MacAddress { get; set; }
        public string AccountName { get; set; }
        public string AccountNumber { get; set; }
        public string BankCode { get; set; }
        public string BankName { get; set; }
        public string Description { get; set; }
        public string TransactionId { get; set; }
        public string Reference { get; set; }
        public string TransferCode { get; set; }
        public int Status { get; set; }
    }

}
