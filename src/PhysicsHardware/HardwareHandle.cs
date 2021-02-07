using PhysicsHardware.Common;
using PhysicsHardware.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;

namespace PhysicsHardware
{
    public class HardwareHandle
    {


        public static OSInfo OSInfo {
            get => Set();
            
        }

        private static OSInfo Set()
        {
            OSInfo osInfo = new OSInfo();
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                osInfo.OS = OSPlatform.Windows.ToString();
                osInfo.TotalPhysicalMemory = PlatformForWindows.TotalPhysicalMemory();
                osInfo.FreePhysicalMemory = PlatformForWindows.FreePhysicalMemory();
                osInfo.LogicalDisk = PlatformForWindows.LogicalDisk();
                osInfo.ProcessorName = PlatformForWindows.ProcessorName();
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                osInfo.OS = OSPlatform.Linux.ToString();
                osInfo.TotalPhysicalMemory = PlatformForLinux.MemInfo("MemTotal:");
                osInfo.FreePhysicalMemory = PlatformForLinux.MemInfo("MemAvailable:");
                osInfo.SwapFree = PlatformForLinux.MemInfo("SwapFree:");
                osInfo.SwapTotal = PlatformForLinux.MemInfo("SwapTotal:");
                osInfo.LogicalDisk = PlatformForLinux.LogicalDisk();
                osInfo.ProcessorName = PlatformForLinux.CpuInfo("model name");
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                osInfo.OS = OSPlatform.OSX.ToString();
                osInfo.TotalPhysicalMemory = PlatformForLinux.MemInfo("MemTotal:");
                osInfo.FreePhysicalMemory = PlatformForLinux.MemInfo("MemAvailable:");
                osInfo.LogicalDisk = PlatformForLinux.LogicalDisk();
                osInfo.ProcessorName = PlatformForLinux.CpuInfo("model name");
            }

            osInfo.Is64BitOperatingSystem  = Environment.Is64BitOperatingSystem;
            osInfo.MachineName = Environment.MachineName;
            osInfo.OSVersion = Environment.OSVersion;
            osInfo.ProcessorCount = Environment.ProcessorCount;

            osInfo.SystemDirectory  = Environment.SystemDirectory;
            osInfo.SystemPageSize = Environment.SystemPageSize;
            osInfo.TickCount = Environment.TickCount;
            osInfo.UserDomainName = Environment.UserDomainName;
            osInfo.UserName  = Environment.UserName;
            osInfo.Version = Environment.Version;
            osInfo.FrameworkDescription = RuntimeInformation.FrameworkDescription;
            osInfo.OSDescription = RuntimeInformation.OSDescription;
            osInfo.UnicastIPAddresses = NetworkInterface.GetAllNetworkInterfaces()
                                                      .Where(network => network.OperationalStatus == OperationalStatus.Up)
                                                      .Select(network => network.GetIPProperties())
                                                      .OrderByDescending(properties => properties.GatewayAddresses.Count)
                                                      .SelectMany(properties => properties.UnicastAddresses)
                                                      .Where(address => !IPAddress.IsLoopback(address.Address) && address.Address.AddressFamily == AddressFamily.InterNetwork)
                                                      .ToList();


            return osInfo;
        }



        /// <summary>
        /// WINDOWS
        /// </summary>
        public class PlatformForWindows
        {
            /// <summary>
            /// 获取物理内存 B
            /// </summary>
            /// <returns></returns>
            public static long TotalPhysicalMemory()
            {
                long TotalPhysicalMemory = 0;

                using (var mc = new ManagementClass("Win32_ComputerSystem"))
                {
                    var moc = mc.GetInstances();
                    foreach (ManagementObject mo in moc)
                    {
                        if (mo["TotalPhysicalMemory"] != null)
                        {
                            TotalPhysicalMemory = long.Parse(mo["TotalPhysicalMemory"].ToString());

                            break;
                        }
                    }
                }


                return TotalPhysicalMemory;
            }

            /// <summary>
            /// 获取可用内存 B
            /// </summary>
            public static long FreePhysicalMemory()
            {
                long FreePhysicalMemory = 0;

                using (var mos = new ManagementClass("Win32_OperatingSystem"))
                {
                    foreach (ManagementObject mo in mos.GetInstances())
                    {
                        if (mo["FreePhysicalMemory"] != null)
                        {
                            FreePhysicalMemory = 1024 * long.Parse(mo["FreePhysicalMemory"].ToString());

                            break;
                        }
                    }
                }
                return FreePhysicalMemory;
            }

            /// <summary>
            /// 获取磁盘信息
            /// </summary>
            /// <returns></returns>
            public static List<object> LogicalDisk()
            {
                var listld = new List<object>();

                using (var diskClass = new ManagementClass("Win32_LogicalDisk"))
                {
                    var disks = diskClass.GetInstances();
                    foreach (ManagementObject disk in disks)
                    {
                        // DriveType.Fixed 为固定磁盘(硬盘) 
                        if (int.Parse(disk["DriveType"].ToString()) == (int)DriveType.Fixed)
                        {
                            listld.Add(new
                            {
                                Name = disk["Name"],
                                Size = disk["Size"],
                                FreeSpace = disk["FreeSpace"]
                            });
                        }
                    }
                }
                return listld;
            }

            /// <summary>
            /// 获取处理器名称
            /// </summary>
            /// <returns></returns>
            public static string ProcessorName()
            {
                var cmd = "wmic cpu get name";
                var cr = CmdSend.Run(cmd).TrimEnd(Environment.NewLine.ToCharArray());
                var pvalue = cr.Split(Environment.NewLine.ToCharArray()).LastOrDefault();
                return pvalue;
            }

        }

        /// <summary>
        /// Linux系统
        /// </summary>
        public class PlatformForLinux
        {
            /// <summary>
            /// 获取 /proc/meminfo
            /// </summary>
            /// <param name="pkey"></param>
            /// <returns></returns>
            public static long MemInfo(string pkey)
            {
                var meminfo = ReadText("/proc/", "meminfo");
                var pitem = meminfo.Split(Environment.NewLine.ToCharArray()).FirstOrDefault(x => x.StartsWith(pkey));

                var pvalue = 1024 * long.Parse(pitem.Replace(pkey, "").ToLower().Replace("kb", "").Trim());

                return pvalue;
            }

            /// <summary>
            /// 获取 /proc/cpuinfo
            /// </summary>
            /// <param name="pkey"></param>
            /// <returns></returns>
            public static string CpuInfo(string pkey)
            {
                var meminfo = ReadText("/proc/", "cpuinfo");
                var pitem = meminfo.Split(Environment.NewLine.ToCharArray()).FirstOrDefault(x => x.StartsWith(pkey));

                var pvalue = pitem.Split(':')[1].Trim();

                return pvalue;
            }

            private static string ReadText(string path, string fileName, Encoding e = null)
            {
                string result = string.Empty;
                try
                {
                    if (e == null) e = Encoding.UTF8;
                    using (var sr = new StreamReader(path + fileName, Encoding.Default))
                    {
                        result = sr.ReadToEnd();
                    }
                }
                catch (System.Exception)
                {
                }
                return result;
            }

            /// <summary>
            /// 获取磁盘信息
            /// </summary>
            /// <returns></returns>
            public static List<object> LogicalDisk()
            {
                var listld = new List<object>();

                var dfresult = CmdSend.Shell("df");
                var listdev = dfresult.Output.Split(Environment.NewLine.ToCharArray()).Where(x => x.StartsWith("/dev/"));
                foreach (var devitem in listdev)
                {
                    var dis = devitem.Split(' ').Where(x => !string.IsNullOrWhiteSpace(x)).ToList();

                    listld.Add(new
                    {
                        Name = dis[0],
                        Size = long.Parse(dis[1]) * 1024,
                        FreeSpace = long.Parse(dis[3]) * 1024
                    });
                }

                return listld;
            }

            /// <summary>
            /// 获取CPU使用率 %
            /// </summary>
            /// <returns></returns>
            public static float CPULoad()
            {
                var br = CmdSend.Shell("vmstat 1 2");
                var cpuitems = br.Output.Split(Environment.NewLine.ToCharArray()).LastOrDefault().Split(' ').Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
                var us = cpuitems[cpuitems.Count - 5];

                return float.Parse(us);
            }
        }

    }
}
