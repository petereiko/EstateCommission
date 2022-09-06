namespace EstateCommission.WindowService
{
    partial class ProjectInstaller
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.EstateCommissionServiceProcessInstaller = new System.ServiceProcess.ServiceProcessInstaller();
            this.EstateCommissionServiceInstaller = new System.ServiceProcess.ServiceInstaller();
            // 
            // EstateCommissionServiceProcessInstaller
            // 
            this.EstateCommissionServiceProcessInstaller.Account = System.ServiceProcess.ServiceAccount.LocalSystem;
            this.EstateCommissionServiceProcessInstaller.Password = null;
            this.EstateCommissionServiceProcessInstaller.Username = null;
            // 
            // EstateCommissionServiceInstaller
            // 
            this.EstateCommissionServiceInstaller.Description = "This service returns commission from subscribers to Estate Management";
            this.EstateCommissionServiceInstaller.DisplayName = "Estate Commission Service";
            this.EstateCommissionServiceInstaller.ServiceName = "EstateCommissionService";
            this.EstateCommissionServiceInstaller.StartType = System.ServiceProcess.ServiceStartMode.Automatic;
            // 
            // ProjectInstaller
            // 
            this.Installers.AddRange(new System.Configuration.Install.Installer[] {
            this.EstateCommissionServiceProcessInstaller,
            this.EstateCommissionServiceInstaller});

        }

        #endregion

        private System.ServiceProcess.ServiceProcessInstaller EstateCommissionServiceProcessInstaller;
        private System.ServiceProcess.ServiceInstaller EstateCommissionServiceInstaller;
    }
}