using windows_service_logic;

namespace windows_service_console_app
{
    class Program
    {
        static void Main(string[] args)
        {
            Worker.Run();
        }
    }
}
