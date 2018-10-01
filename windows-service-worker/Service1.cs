using System.Security.Permissions;
using System.ServiceProcess;

namespace windows_service_worker
{
    public partial class Service1 : ServiceBase
    {
        public Service1()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            
        }

        [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
        

        protected override void OnStop()
        {
        }
    }
}
