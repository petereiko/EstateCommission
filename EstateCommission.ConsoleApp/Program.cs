using EstateCommission.Business;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EstateCommission.ConsoleApp
{
    internal class Program
    {
        static void Main(string[] args)
        {
            try
            {
                int y = 0;
                var x = 1 / y;
            }
            catch (Exception ex)
            {

                LogManager.Log(ex);
            }
        }
    }
}
