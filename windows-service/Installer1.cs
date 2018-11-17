using System.ComponentModel;
using System.ServiceProcess;

namespace windows_service
{
    [RunInstaller(true)]
    public partial class Installer1 : System.Configuration.Install.Installer
    {
        ServiceInstaller serviceInstaller;
        ServiceProcessInstaller processInstaller;
        public Installer1()
        {
            InitializeComponent();
            serviceInstaller = new ServiceInstaller();
            processInstaller = new ServiceProcessInstaller();

            processInstaller.Account = ServiceAccount.LocalService;
            serviceInstaller.StartType = ServiceStartMode.Automatic;
            serviceInstaller.ServiceName = "FTP files watcher";
            serviceInstaller.DisplayName = "FTP files watcher";
            Installers.Add(processInstaller);
            Installers.Add(serviceInstaller);
        }
    }
}
