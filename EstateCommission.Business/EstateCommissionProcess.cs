using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EstateCommission.Business
{
    public class EstateCommissionProcess
    {
        static string _congifurationName = ConfigurationManager.AppSettings["ConfigurationName"].ToString();
        static string i_customers = ConfigurationManager.AppSettings["i_customers"].ToString();


        private static List<CommissionProduct> GetCommissionProducts(string commProducts)
        {
            string[] comProdArray = commProducts.Split(';');
            List<CommissionProduct> commissionProductObjects = new List<CommissionProduct>();

            foreach (var item in comProdArray)
            {
                try
                {
                    commissionProductObjects.Add(new CommissionProduct
                    {
                        commission = Convert.ToDouble(item.Split('|')[1]),
                        i_product = Convert.ToInt32(item.Split('|')[0])
                    });
                }
                catch
                {
                    continue;
                }
            }
            return commissionProductObjects;
        }
        public static void ProcessEstateCommision()
        {
            var commProducts = GetConfigurationValue();
            if (string.IsNullOrEmpty(commProducts)) return;
            List<CommissionProduct> commissionProductObjects = GetCommissionProducts(commProducts);
            var subs = GetIncomingSubscriptions();//where i_customerin ()

            if (subs == null)
            {
                Console.WriteLine("No pending subscriptions for " + i_customers);
                return;
            }

            if (subs.Rows.Count == 0)
            {
                Console.WriteLine("No pending subscriptions for " + i_customers);
                return;
            }

            int counter = 0;

            foreach (DataRow row in subs.Rows)
            {
                var i_prod = row["i_product"].ToString();
                int i_product = Convert.ToInt32(i_prod);

                var commission = commissionProductObjects.FirstOrDefault(x => x.i_product == i_product);

                if (commission == null) continue;


                //if (!commProducts.Contains(i_prod)) continue;

                var cld = row["Cld"].ToString();

                if ((cld.Trim() == "" || cld.Trim().Length < 9 || cld.Split('-') == null || !GetConfigurationValue("Multiple_Payment_Promo_Allowed_Channels").Contains(cld.Substring(0, 9))) && !DealerTransaction(cld))
                {
                    continue;
                }



                if (AlreadyLogged(cld)) continue;
                var i_cust = row["i_customer"].ToString();
                var mac = row["id"].ToString();
                var amountPaid = Math.Abs(double.Parse(row["Charged_amount"].ToString()));
                var paymenttype = row["paymenttype"].ToString();
                if (paymenttype == "Debit")
                {
                    amountPaid += GetRestAccountPaymentAmount(cld, i_cust);
                }
                var i_account = row["i_account"].ToString();
                if (i_account == "0") continue;
                //format: iproduct|commissionn  4332|20;2234|20


                //var commision = commProducts.Split(';').ToList().Where(x => x.Contains(i_prod)).FirstOrDefault().ToString().Split('|')[1];// to be either calculated or gotten from system parameter 

                var commissionAmount = commission.commission; //Convert.ToDouble(commision); //double.TryParse(commision, out CommissionValue) ? CommissionValue : 0.0f;
                commissionAmount = (commissionAmount / 100) * amountPaid;
                //var sucess = LogEstateCommission(i_cust, i_account, mac, i_prod, amountPaid.ToString(), cld, commissionAmount);



                //EstateTransaction.LogTransaction()


                double paystackCharge = 0;
                if (commissionAmount <= 5000)
                {
                    paystackCharge = 10;
                }
                else if (commissionAmount > 5000 && commissionAmount <= 50000)
                {
                    paystackCharge = 25;
                }
                else
                {
                    paystackCharge = 50;
                }

                double netCommissionAmount = commissionAmount - paystackCharge;
                try
                {
                    EstateCommisionPaymentLog log = new EstateCommisionPaymentLog
                    {
                        i_account = Convert.ToInt32(i_account),
                        Amount = decimal.Parse(netCommissionAmount.ToString()),
                        DateCreated = DateTime.Now,
                        i_customer = Convert.ToInt32(i_cust),
                        i_product = i_product,
                        MacAddress = mac
                    };
                    EstateTransaction.LogTransaction(log);
                }
                catch (Exception ex)
                {

                }
                counter++;

                // Perform paystack transaction;


                double payStackEquivalentNetCommissionAmount = 100 * netCommissionAmount;//Convert amount from Naira to Kobo

                var transferResult = EstateTransaction.InitiateTransfer(payStackEquivalentNetCommissionAmount.ToString());

                // update Estate Commison if paystack transaction issuccesful
                //UpdateEstateCommissionLog(sucess);
                if (row["paymenttype"].ToString() == "Credit") break;

            }

            if (counter == 0) Console.WriteLine("No record was found for processing");
            else if (counter == 1) Console.WriteLine("Only 1 record was processed");
            else Console.WriteLine($"{counter} records were processed");

        }
        private static string GetConfigurationValue(string parameterName = "")
        {
            if (string.IsNullOrEmpty(parameterName)) parameterName = _congifurationName;
            SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["SwiftUtility"].ConnectionString);
            string retStr = "";
            try
            {
                string ipaddress = ConfigurationManager.AppSettings["ServerIP"].ToString();
                using (conn)
                {
                    conn.Open();
                    SqlCommand command = conn.CreateCommand();
                    command.CommandText = @"SELECT isnull(Value, '') as Value, Server from [SystemParameters] where [Server] = '" + ipaddress.Trim() + "' and Name = '" + parameterName.Trim() + "' union SELECT isnull(Value, '') as Value, Server from [SystemParameters] where [SERVER] = 'ALL' and Name = '" + parameterName.Trim() + "'";
                    SqlDataAdapter adapter = new SqlDataAdapter(command);
                    DataTable result = new DataTable();
                    adapter.Fill(result);

                    if (result.Rows.Count == 1)
                    {
                        retStr = result.Rows[0]["Value"] != DBNull.Value ? result.Rows[0]["Value"].ToString() : "";
                    }
                    else if (result.Rows.Count > 1)
                    {
                        foreach (DataRow row in result.Rows)
                        {
                            if (row["Server"].ToString().Trim().Equals(ipaddress))
                            {
                                retStr = row["Value"] != DBNull.Value ? row["Value"].ToString() : "";
                            }
                        }
                    }

                    command.Dispose();
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
            return retStr;
        }
        private static bool DealerTransaction(string paymentRef)
        {
            int[] churnedId = { };
            bool exist = false;

            try
            {
                string connStr = System.Configuration.ConfigurationManager.ConnectionStrings["SwiftUtility"].ToString();
                //
                SqlConnection conn = new SqlConnection(connStr);
                using (conn)
                {
                    using (SqlCommand cmd = new SqlCommand("SELECT  * FROM CRMPAYMENTS WHERE username in (select crmid from dealercrm) and TRANSTYPE ='CREDITUNUSED' and cPaymentUID =  '" + paymentRef + "'", conn))
                    {

                        conn.Open();
                        SqlDataReader reader = cmd.ExecuteReader();

                        if (reader.HasRows)
                        {

                            exist = true;
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                //EmailEngine.sendError(ex, typeof(WinbackPromoMonitor).FullName + "." + System.Reflection.MethodBase.GetCurrentMethod().Name);
            }



            return exist;
        }
        private static DataTable GetIncomingSubscriptions()
        {
            try
            {
                DataTable dt = new DataTable();

                //                string commString = @"SELECT * FROM OPENQUERY(PORTAONEDBMR75, 'SELECT COALESCE(CDR.cld, ''pass'') as cld,(select i_product from PB_USER.ACCOUNTS where i_account=g.i_account) i_product, CDR.charged_amount, CDR.i_customer,g.i_account, g.id, ''Credit'' as PaymentType
                //                    FROM PB_USER.CDR_CUSTOMERS CDR  join
                //					(select  i_customer, i_account, id from PB_USER.Accounts a where
                //bill_status <> ''C''  ) g on CDR.i_customer = g.i_customer
                //                    WHERE CDR.I_DEST = 14                                 
                //                             AND CDR.BILL_TIME>=SYSDATE - (0.5/24) 
                //                                AND CDR.BILL_TIME<=SYSDATE 
                //								AND CDR.I_CUSTOMER in (190210)
                //                      AND CDR.I_CUSTOMER 
                //                    IN (SELECT I_CUSTOMER
                //                    FROM PB_USER.Custom_Field_Values cfv WHERE i_custom_field = 2 AND ( (cfv.VALUE = ''Consumer''))) 

                //					union SELECT CDR.cld, (select i_product from PB_USER.ACCOUNTS where i_account=CDR.i_account) i_product, CDR.charged_amount, CDR.i_customer,CDR.i_account, CDR.account_id, ''Debit'' as PaymentType
                //                                FROM PB_USER.CDR_ACCOUNTS CDR 
                //                                WHERE I_DEST = 14                                 
                //                                     AND BILL_TIME>=SYSDATE - (0.5/24) 
                //                                AND BILL_TIME<=SYSDATE 
                //                                AND I_CUSTOMER in (190210)
                //                                AND I_CUSTOMER
                //                                IN (SELECT I_CUSTOMER
                //                                FROM PB_USER.Custom_Field_Values cfv WHERE i_custom_field = 2 AND ( (cfv.VALUE = ''Consumer''))) ') order by cld";

                //           string conString = @"SELECT * FROM OPENQUERY(PORTAONEDBMR75, 'SELECT COALESCE(CDR.cld, ''pass'') as cld,0 i_product, CDR.charged_amount, CDR.i_customer,0 i_account, '''' id, ''Credit'' as PaymentType
                //               FROM PB_USER.CDR_CUSTOMERS CDR					
                //WHERE EXISTS ( SELECT 1 FROM
                //(SELECT I_CUSTOMER, I_PRODUCT FROM PB_USER.ACCOUNTS WHERE BILL_STATUS <> ''C'' 
                //   AND I_PRODUCT IN (2537)
                //) A WHERE A.I_CUSTOMER = CDR.I_CUSTOMER)
                //               AND CDR.I_DEST = 14                                 
                //                        AND CDR.BILL_TIME>=SYSDATE - (2/24) 
                //                           AND CDR.BILL_TIME<=SYSDATE 
                //			AND CDR.I_CUSTOMER in (190210)
                //                 AND CDR.I_CUSTOMER 
                //               IN (SELECT I_CUSTOMER
                //               FROM PB_USER.Custom_Field_Values cfv WHERE i_custom_field = 2 AND ( (cfv.VALUE = ''Consumer''))) 

                //union SELECT CDR.cld, (select i_product from PB_USER.ACCOUNTS where i_account=CDR.i_account) i_product, CDR.charged_amount, CDR.i_customer,CDR.i_account, CDR.account_id, ''Debit'' as PaymentType
                //                           FROM PB_USER.CDR_ACCOUNTS CDR 
                //                           WHERE I_DEST = 14                                 
                //                                AND BILL_TIME>=SYSDATE - (2/24) 
                //                           AND BILL_TIME<=SYSDATE 
                //                           AND I_CUSTOMER in (190210)
                //                           AND I_CUSTOMER
                //                           IN (SELECT I_CUSTOMER
                //                           FROM PB_USER.Custom_Field_Values cfv WHERE i_custom_field = 2 AND ( (cfv.VALUE = ''Consumer''))) ') order by cld";

                string conString = @"SELECT * FROM OPENQUERY(PORTAONEDBMR75, 'SELECT COALESCE(CDR.cld, ''pass'') as cld, B.i_product, CDR.charged_amount, CDR.i_customer, 0 i_account, '''' id, ''Credit'' as PaymentType
                    FROM PB_USER.CDR_CUSTOMERS CDR

					LEFT OUTER JOIN PB_USER.ACCOUNTS B on CDR.I_CUSTOMER=B.I_CUSTOMER

					WHERE EXISTS ( SELECT 1 FROM
					(SELECT I_CUSTOMER, I_PRODUCT FROM PB_USER.ACCOUNTS WHERE BILL_STATUS <> ''C'' 
					   
					) A WHERE A.I_CUSTOMER = CDR.I_CUSTOMER)
                    AND CDR.I_DEST = 14                                 
                             AND CDR.BILL_TIME>=SYSDATE - (4/24) 
                                AND CDR.BILL_TIME<=SYSDATE 
								AND CDR.I_CUSTOMER in (" + i_customers + ")";
                conString += @"AND CDR.I_CUSTOMER 
                    IN (SELECT I_CUSTOMER
                    FROM PB_USER.Custom_Field_Values cfv WHERE i_custom_field = 2 AND ( (cfv.VALUE = ''Consumer''))) 
					
					union SELECT CDR.cld, (select i_product from PB_USER.ACCOUNTS where i_account=CDR.i_account) i_product, CDR.charged_amount, CDR.i_customer,CDR.i_account, CDR.account_id, ''Debit'' as PaymentType
                                FROM PB_USER.CDR_ACCOUNTS CDR 
                                WHERE I_DEST = 14                                 
                                     AND BILL_TIME>=SYSDATE - (4/24) 
                                AND BILL_TIME<=SYSDATE 
                                AND I_CUSTOMER in (" + i_customers + ")";
                conString += @"AND I_CUSTOMER
                                IN (SELECT I_CUSTOMER
                                FROM PB_USER.Custom_Field_Values cfv WHERE i_custom_field = 2 AND ( (cfv.VALUE = ''Consumer''))) ') order by cld";
                SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["SwiftUtility"].ConnectionString);

                // SqlConnection conn = new SqlConnection(connString);
                SqlCommand cmd = new SqlCommand(conString, conn);
                using (conn)
                {
                    conn.Open();
                    // create data adapter
                    SqlDataAdapter da = new SqlDataAdapter(cmd);
                    // this will query the database and return the result to a datatable
                    da.Fill(dt);
                    da.Dispose();
                    if (dt.Rows.Count < 1)
                    {

                        return dt = null;

                    }
                    return dt;
                }
            }
            catch (Exception ex)
            {
                return null;
            }
        }





        public static bool AlreadyLogged(string paymentRef)
        {
            bool ret = false;
            try
            {
                string connStr = System.Configuration.ConfigurationManager.ConnectionStrings["SwiftUtility"].ToString();
                //
                SqlConnection conn = new SqlConnection(connStr);
                using (conn)
                {
                    using (SqlCommand cmd = new SqlCommand("SELECT * from MultiplePaymentPromoLog where PAYMENTREF=@PAYMENTREF", conn))
                    {
                        cmd.Parameters.AddWithValue("PAYMENTREF", paymentRef);
                        conn.Open();
                        SqlDataReader reader = cmd.ExecuteReader();
                        if (reader.HasRows)
                        {
                            ret = true;
                            // 
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                //EmailEngine.sendError(ex, typeof(WinbackPromoMonitor).FullName + "." + System.Reflection.MethodBase.GetCurrentMethod().Name);
            }

            return ret;
        }
        private static int LogEstateCommission(string i_customer, string i_account, string macaddress, string i_product, string totalamountpaid, string paymentref, double commissionamount)
        {
            int MPid = 0;
            string sql = "INSERT INTO EstateCommissionLog " +
                            "(I_CUSTOMER, I_ACCOUNT, MACADDRESS, I_PRODUCT, TOTALAMOUNTPAID, " +
                            "PAYMENTREF, PAYMENTDATE, CommissionAmount) " +
                            "VALUES " +
                            "(@I_CUSTOMER, @I_ACCOUNT, @MACADDRESS, @I_PRODUCT, @TOTALAMOUNTPAID, " +
                            "@PAYMENTREF, @PAYMENTDATE, @PROMOAMOUNT); select @@identity";
            try
            {
                SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["SwiftUtility"].ConnectionString);
                using (conn)
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("I_CUSTOMER", i_customer);// 
                        cmd.Parameters.AddWithValue("I_ACCOUNT", i_account); cmd.Parameters.AddWithValue("MACADDRESS", macaddress);
                        cmd.Parameters.AddWithValue("I_PRODUCT", i_product); cmd.Parameters.AddWithValue("TOTALAMOUNTPAID", totalamountpaid);
                        cmd.Parameters.AddWithValue("PAYMENTREF", paymentref); cmd.Parameters.AddWithValue("PAYMENTDATE", DateTime.Now);
                        cmd.Parameters.AddWithValue("PROMOAMOUNT", commissionamount);

                        int.TryParse(cmd.ExecuteScalar().ToString(), out MPid);
                    }
                }


            }
            catch (Exception ex)
            {

            }

            return MPid;
        }
        private static void UpdateEstateCommissionLog(int MPid)
        {

            string desc = "AD-BN-M-S-" + MPid.ToString();
            SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["SwiftUtility"].ConnectionString);
            try
            {
                using (conn)
                {
                    conn.Open();
                    SqlCommand command1 = conn.CreateCommand();
                    command1.CommandText = "Update [EstateCommissionLog] set Credited = 1, dateCredited = getdate(), CommissionRef = '" + desc + "' where ID = " + MPid;
                    command1.ExecuteNonQuery();

                    command1.Dispose();
                }
                if (conn.State == System.Data.ConnectionState.Open)
                {
                    conn.Close();
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
        private static double GetRestAccountPaymentAmount(string paymentRef, string i_customer)
        {
            try
            {
                string connStr = System.Configuration.ConfigurationManager.ConnectionStrings["SwiftUtility"].ToString();
                //
                SqlConnection conn = new SqlConnection(connStr);
                using (conn)
                {
                    string query = @"SELECT * FROM OPENQUERY(PORTAONEDBMR75, 'select  charged_amount FROM PB_USER.CDR_CUSTOMERS CDR WHERE I_DEST = 14 AND I_CUSTOMER = " + i_customer + " AND CLD LIKE ''" + paymentRef.Substring(0, 14) + "% '' AND BILL_TIME >= SYSDATE - (0.6 / 24) AND BILL_TIME <= SYSDATE')";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("PAYMENTREF", paymentRef);
                        conn.Open();
                        SqlDataReader reader = cmd.ExecuteReader();
                        if (reader.HasRows)
                        {
                            return reader.GetDouble(0);
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                //EmailEngine.sendError(ex, typeof(WinbackPromoMonitor).FullName + "." + System.Reflection.MethodBase.GetCurrentMethod().Name);
            }

            return 0;
        }
    }
}
