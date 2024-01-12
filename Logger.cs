using System;
using System.IO;
using System.Diagnostics;
using System.Security.Principal;
using System.Runtime.InteropServices;

class SystemLogger
{
    [StructLayout(LayoutKind.Sequential)]
    public struct MEMORYSTATUSEX
    {
        public uint dwLength;
        public ulong ullTotalPhys;
        public ulong ullAvailPhys;
        public ulong ullTotalPageFile;
        public ulong ullAvailPageFile;
        public ulong ullTotalVirtual;
        public ulong ullAvailVirtual;
        public ulong ullAvailExtendedVirtual;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct OSVERSIONINFOEX
    {
        public int dwOSVersionInfoSize;
        public int dwMajorVersion;
        public int dwMinorVersion;
        public int dwBuildNumber;
        public int dwPlatformId;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string szCSDVersion;
        public ushort wServicePackMajor;
        public ushort wServicePackMinor;
        public ushort wSuiteMask;
        public byte wProductType;
        public byte wReserved;
    }

    [DllImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);

    [DllImport("kernel32.dll")]
    public static extern bool GetVersionEx(ref OSVERSIONINFOEX lpVersionInfo);

    [DllImport("psapi.dll", SetLastError = true)]
    public static extern bool EnumDeviceDrivers([MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.U4)] out IntPtr[] lphModule, uint cb, out uint lpcbNeeded);

    public static void GetRAMInformation(StreamWriter writer)
    {
        MEMORYSTATUSEX status = new MEMORYSTATUSEX();
        status.dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));

        if (GlobalMemoryStatusEx(ref status))
        {
            writer.WriteLine($"Whole RAM: {status.ullTotalPhys / (1024 * 1024)} MB");
        }
        else
        {
            writer.WriteLine("Error while catching RAM informations");
        }
    }

    public static void GetSystemInformation(StreamWriter writer)
    {
        OSVERSIONINFOEX os_info = new OSVERSIONINFOEX();
        os_info.dwOSVersionInfoSize = Marshal.SizeOf(typeof(OSVERSIONINFOEX));

        if (GetVersionEx(ref os_info))
        {
            writer.WriteLine($"Operating System: Windows {os_info.dwMajorVersion}.{os_info.dwMinorVersion}");
        }
        else
        {
            writer.WriteLine("Error while catching OS informations");
        }
    }

    public static void GetKernelInformation(StreamWriter writer)
    {
        IntPtr[] drivers;
        uint needed;

        if (EnumDeviceDrivers(out drivers, 0, out needed))
        {
            uint drivers_count = needed / (uint)IntPtr.Size;
            writer.WriteLine($"Count of kernel drivers: {drivers_count}");
        }
        else
        {
            writer.WriteLine("Error while catching kernel driver informations");
        }
    }

    public static bool IsUserAnAdmin()
    {
        WindowsIdentity identity = WindowsIdentity.GetCurrent();
        WindowsPrincipal principal = new WindowsPrincipal(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }
}

class Program
{
    static void Main()
    {
        string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        string filePath = Path.Combine(desktopPath, "system_info.txt");

        if (!SystemLogger.IsUserAnAdmin())
        {
            Console.WriteLine("Administrator permissions needed! Please run as admin otherwise it won't work!");
            return;
        }

        using (StreamWriter fileWriter = new StreamWriter(filePath))
        {
            SystemLogger.GetSystemInformation(fileWriter);
            SystemLogger.GetKernelInformation(fileWriter);
            SystemLogger.GetRAMInformation(fileWriter);
        }

        Console.WriteLine($"The Systeminformations were caught to the '{filePath}' file on your desktop!");
    }
}
