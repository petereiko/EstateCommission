using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace EstateCommission.Business
{
    public static class EstateTransaction
    {
        static string _bankCode = ConfigurationManager.AppSettings["BankCode"].ToString();
        static string _accountName = ConfigurationManager.AppSettings["AccountName"].ToString();
        static string _accountNumber = ConfigurationManager.AppSettings["AccountNumber"].ToString();
        static string _paystackTranferRecipientUri = ConfigurationManager.AppSettings["PayStackTransferRecipientUri"].ToString();

        static string _username = ConfigurationManager.AppSettings["BankOpsUsername"].ToString();
        static string _password = ConfigurationManager.AppSettings["BankOpsPassword"].ToString();
        static string _bankOpsBaseUri = ConfigurationManager.AppSettings["BankOpsBaseUri"].ToString();
        static string GetRecipientCode()
        {
            string result = string.Empty;
            RecipientCodeRequest request = new RecipientCodeRequest
            {
                account_number = _accountNumber,
                name = _accountName,
                bank_code = _bankCode,
                currency = "NGN",
                type = "nuban"
            };

            string payload = JsonConvert.SerializeObject(request);

            using (HttpClient client = new HttpClient())
            {
                client.BaseAddress = new Uri(_paystackTranferRecipientUri);
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "sk_live_42417e5acd745423c79d8f8e60f8251f68fd7680");
                var response = client.PostAsync(_paystackTranferRecipientUri, new StringContent(payload, Encoding.UTF8, "application/json")).Result;
                if (response.IsSuccessStatusCode)
                {
                    var json = response.Content.ReadAsStringAsync().Result;
                    RecipientCodeResponse transferRecipientResponse = JsonConvert.DeserializeObject<RecipientCodeResponse>(json);
                    if (transferRecipientResponse != null)
                    {
                        if (transferRecipientResponse.status)
                        {
                            result = transferRecipientResponse.data.recipient_code;
                        }
                    }
                }
            }

            return result;
        }

        static string GetBankOpsToken()
        {
            ServicePointManager.ServerCertificateValidationCallback =
        delegate (
            object s,
            X509Certificate certificate,
            X509Chain chain,
            SslPolicyErrors sslPolicyErrors
        ) {
            return true;
        };

            string token = string.Empty;
            BankOpsTokenRequest request = new BankOpsTokenRequest
            {
                Username = _username,
                Password = _password
            };

            string payload = JsonConvert.SerializeObject(request);
            using (HttpClient client = new HttpClient())
            {
                client.BaseAddress = new Uri(_bankOpsBaseUri);

                var response = client.PostAsync(_bankOpsBaseUri + "api/auth/login", new StringContent(payload, Encoding.UTF8, "application/json")).Result;
                if (response.IsSuccessStatusCode)
                {
                    var json = response.Content.ReadAsStringAsync().Result;
                    BankOpsTokenResponse model = JsonConvert.DeserializeObject<BankOpsTokenResponse>(json);
                    if (model != null)
                    {
                        token = model.Token;
                    }


                }
            }
            return token;
        }

        public static void LogTransaction(EstateCommisionPaymentLog log)
        {
            SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["SwiftUtility"].ConnectionString);
            try
            {
                string ipaddress = ConfigurationManager.AppSettings["ServerIP"].ToString();
                using (conn)
                {
                    using (SqlCommand cmd = new SqlCommand("InsertToEstateCommissionPaymentLogs", conn))
                    {
                        cmd.Parameters.AddWithValue("@i_customer", log.i_customer);
                        cmd.Parameters.AddWithValue("@i_account", log.i_account);
                        cmd.Parameters.AddWithValue("@i_product", log.i_product);
                        cmd.Parameters.AddWithValue("@AccountName", log.AccountName);
                        cmd.Parameters.AddWithValue("@AccountNumber", log.AccountNumber);
                        cmd.Parameters.AddWithValue("@BankCode", log.BankCode);
                        cmd.Parameters.AddWithValue("@BankName", log.BankName);
                        cmd.Parameters.AddWithValue("@Amount", log.Amount);
                        cmd.Parameters.AddWithValue("@Status", log.Status);
                        cmd.Parameters.AddWithValue("@TransactionId", log.TransactionId);
                        cmd.Parameters.AddWithValue("@Reference", log.Reference);
                        cmd.Parameters.AddWithValue("@TransferCode", log.TransferCode);
                        cmd.Parameters.AddWithValue("@Description", log.Description);
                        cmd.Parameters.AddWithValue("@MacAddress", log.MacAddress);

                        cmd.CommandType = CommandType.StoredProcedure;
                        conn.Open();
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {

            }
            finally
            {
                if (conn.State.Equals(ConnectionState.Open))
                {
                    conn.Close();
                }
            }
        }

        public static TransferResponse InitiateTransfer(string amount)
        {
            TransferResponse result = null;

            string recipientCode = GetRecipientCode();

            TransferRequest request = new TransferRequest
            {
                amount = amount,
                reason = "Commission for Reference Number " + recipientCode,
                recipient = recipientCode,
                source = "balance"
            };

            string token = GetBankOpsToken();

            string payload = JsonConvert.SerializeObject(request);

            using (HttpClient client = new HttpClient())
            {
                client.BaseAddress = new Uri(_bankOpsBaseUri);
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                var response = client.PostAsync(_bankOpsBaseUri + "api/account/BankTransfer", new StringContent(payload, Encoding.UTF8, "application/json")).Result;
                if (response.IsSuccessStatusCode)
                {
                    var json = response.Content.ReadAsStringAsync().Result;
                    result = JsonConvert.DeserializeObject<TransferResponse>(json);
                }
            }



            return result;
        }
    }
}
