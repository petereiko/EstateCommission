using EstateCommission.Business;
using Quartz;
using Quartz.Impl;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace EstateCommission.WindowService
{
    public partial class Service1 : ServiceBase
    {
        public Service1()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            try
            {
                IScheduler scheduler = StdSchedulerFactory.GetDefaultScheduler();
                scheduler.Start();


                IJobDetail job = JobBuilder.Create<MyJob>().Build();

                ITrigger trigger = TriggerBuilder.Create()
                       .WithSimpleSchedule(a => a.WithIntervalInMinutes(Convert.ToInt32(ConfigurationManager.AppSettings["TimeIntervalInMinutes"])).RepeatForever())
                       .Build();

                scheduler.ScheduleJob(job, trigger);
            }
            catch(Exception ex)
            {
                LogManager.Log(ex);
            }
        }

        protected override void OnStop()
        {
            try
            {
                IScheduler scheduler = StdSchedulerFactory.GetDefaultScheduler();
                scheduler.Shutdown();
            }
            catch (Exception ex)
            {

                LogManager.Log(ex);
            }
            
        }
    }
}
