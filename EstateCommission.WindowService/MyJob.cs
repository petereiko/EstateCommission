using EstateCommission.Business;
using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EstateCommission.WindowService
{
    public class MyJob : IJob
    {
        public void Execute(IJobExecutionContext context)
        {
            try
            {
                //EstateCommissionProcess.ProcessEstateCommision();
                LogManager.Log(DateTime.Now.ToString());
            }
            catch(Exception ex)
            {
                LogManager.Log(ex);
            }
        }
    }
}
