using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EstateCommission.Business
{
    public static class LogManager
    {
        public static void Log(string message)
        {
            string fileName = DateTime.Now.Ticks.ToString();
            string filePath = ConfigurationManager.AppSettings["MessageLogPath"].ToString() + DateTime.Now.Ticks.ToString() + ".txt";
            FileStream fs = File.Create(filePath);
            fs.Close();
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(DateTime.Now.ToString());
            sb.AppendLine(message);
            string content = sb.ToString();
            File.WriteAllText(filePath, content);
        }
        public static void Log(Exception ex) 
        {
            string fileName = DateTime.Now.Ticks.ToString();
            string filePath = ConfigurationManager.AppSettings["ErrorLogPath"].ToString() + DateTime.Now.Ticks.ToString() + ".txt";
            FileStream fs = File.Create(filePath);
            fs.Close();
            string message = ex.Message;
            string stackTrace = ex.StackTrace;
            StringBuilder sb=new StringBuilder();
            sb.AppendLine(DateTime.Now.ToString());
            sb.AppendLine(message);
            sb.AppendLine(stackTrace);
            string content = sb.ToString();

            File.WriteAllText(filePath, content);
        }
    }
}
