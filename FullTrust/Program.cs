using System;
using System.Diagnostics;
using Windows.ApplicationModel.AppService;

namespace FullTrust
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.Title = "Hello World";
            Console.WriteLine("This process has access to the entire public desktop API surface");
            Console.WriteLine("Press any key to exit ...");
            Process.Start("C:\\Setup.exe");
            Console.ReadLine();

            /*using (Process myProcess = new Process())
            {
                myProcess.StartInfo.UseShellExecute = false;
                // You can start any process, HelloWorld is a do-nothing example.
                myProcess.StartInfo.FileName = "C:\\HelloWorld.exe";
                myProcess.StartInfo.CreateNoWindow = true;
                myProcess.Start();
                // This code assumes the process you are starting will terminate itself. 
                // Given that is is started without a window so you cannot terminate it 
                // on the desktop, it must terminate itself or you can do it programmatically
                // from this application using the Kill method.
            }*/
        }
    }
}
