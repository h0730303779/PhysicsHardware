using System;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace ConsolePhysicsHardwareTest
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            var osinfo = PhysicsHardware.HardwareHandle.OSInfo;
            //Console.WriteLine("获取运行应用程序的.NET安装的名称" + info.FrameworkDescription);
            Console.WriteLine("OS version:" + osinfo.OSVersion.ToString()); //get OS information
            Console.WriteLine(".NET version: " + osinfo.Version.ToString()); //get net framework version
            Console.WriteLine("CurrentDirectory: " + Environment.CurrentDirectory.ToString()); //get current directory
            String[] drives = Environment.GetLogicalDrives();                             //get all drivers into a string array
            Console.WriteLine("GetLogicalDrives: {0}", String.Join(", ", drives));      //print all logical drivers

            Console.WriteLine("Login User:" + osinfo.UserName.ToString());           //get login name
            Console.WriteLine("Memory: " + Environment.WorkingSet.ToString());            //used memory
            Console.WriteLine("ProcesserCount: " + osinfo.ProcessorCount.ToString()); //get processor number
            Console.WriteLine("Domainname: " + osinfo.UserDomainName.ToString());            //get domaim name

            //Environment.SetEnvironmentVariable("Path", "Test");           //set path



            foreach (var item in osinfo.UnicastIPAddresses)
            {
                Console.WriteLine(item.Address.ToString());
            }
            Console.ReadLine();

        }
    }
}
