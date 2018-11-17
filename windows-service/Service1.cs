using System.ServiceProcess;

namespace windows_service
{
    public partial class Service1 : ServiceBase
    {
        public Service1()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            windows_service_logic.Worker.Run();
        }

        protected override void OnStop()
        {
        }
    }
}
