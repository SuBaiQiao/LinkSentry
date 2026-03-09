using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using LinkSentry.Models;
using Microsoft.Extensions.Logging;

namespace LinkSentry.Services;

/// <summary>
/// Scans active TCP/UDP connections and resolves owning process information.
/// Uses native P/Invoke to GetExtendedTcpTable / GetExtendedUdpTable for PID mapping,
/// which is far more reliable than parsing netstat output across locales.
/// </summary>
public class PortService : IPortService
{
    private readonly ILogger<PortService> _logger;
    private readonly IDiagnosticLogger _diag;

    public PortService(ILogger<PortService> logger, IDiagnosticLogger diag)
    {
        _logger = logger;
        _diag = diag;
    }

    public async Task<List<PortConnectionInfo>> GetActiveConnectionsAsync()
    {
        _diag.Log("PortService: Starting active connections scan.");
        return await Task.Run(() =>
        {
            var results = new List<PortConnectionInfo>();
            
            _diag.Log("PortService: Building process map.");
            var processMap = new Dictionary<uint, (string, string)>();
            try
            {
                var allProcesses = Process.GetProcesses();
                _diag.Log($"PortService: Found {allProcesses.Length} system processes.");
                foreach (var p in allProcesses)
                {
                    try
                    {
                        var pid = (uint)p.Id;
                        var name = p.ProcessName;
                        var path = "";
                        if (pid > 4) 
                        {
                            try { path = p.MainModule?.FileName ?? ""; } catch { }
                        }
                        processMap[pid] = (name, path);
                    }
                    catch { }
                    finally { p.Dispose(); }
                }
                _diag.Log($"PortService: Process map built with {processMap.Count} entries.");
            }
            catch (Exception ex)
            {
                _diag.Log($"PortService Process Map Error: {ex.Message}");
            }

            try
            {
                (string name, string path) GetInfo(uint pid)
                {
                    if (pid == 0) return ("System Idle Process", "");
                    if (pid == 4) return ("System", "");
                    return processMap.TryGetValue(pid, out var info) ? info : ("未知进程", "");
                }

                // --- TCP IPv4 ---
                var tcp4 = GetExtendedTcpTable(false);
                _diag.Log($"PortService: Retrieved {tcp4.Count} TCPv4 rows.");
                foreach (var row in tcp4)
                {
                    var info = GetInfo(row.OwningPid);
                    results.Add(new PortConnectionInfo { Protocol = "TCP", LocalAddress = $"{SafeIp(row.LocalAddr)}:{NtoHs(row.LocalPort)}", LocalPort = NtoHs(row.LocalPort), RemoteAddress = row.RemoteAddr == 0 ? "*:*" : $"{SafeIp(row.RemoteAddr)}:{NtoHs(row.RemotePort)}", State = MapTcpState(row.State), ProcessId = (int)row.OwningPid, ProcessName = info.name, ProcessPath = info.path });
                }

                // --- TCP IPv6 ---
                var tcp6 = GetExtendedTcp6Table();
                _diag.Log($"PortService: Retrieved {tcp6.Count} TCPv6 rows.");
                foreach (var row in tcp6)
                {
                    var info = GetInfo(row.OwningPid);
                    results.Add(new PortConnectionInfo { Protocol = "TCPv6", LocalAddress = $"[{SafeIp6(row.LocalAddr)}]:{NtoHs(row.LocalPort)}", LocalPort = NtoHs(row.LocalPort), RemoteAddress = IsEmptyIpv6(row.RemoteAddr) ? "*:*" : $"[{SafeIp6(row.RemoteAddr)}]:{NtoHs(row.RemotePort)}", State = MapTcpState(row.State), ProcessId = (int)row.OwningPid, ProcessName = info.name, ProcessPath = info.path });
                }

                // --- UDP IPv4 ---
                var udp4 = GetExtendedUdpTable(false);
                _diag.Log($"PortService: Retrieved {udp4.Count} UDPv4 rows.");
                foreach (var row in udp4)
                {
                    var info = GetInfo(row.OwningPid);
                    results.Add(new PortConnectionInfo { Protocol = "UDP", LocalAddress = $"{SafeIp(row.LocalAddr)}:{NtoHs(row.LocalPort)}", LocalPort = NtoHs(row.LocalPort), RemoteAddress = "*:*", State = "Listen", ProcessId = (int)row.OwningPid, ProcessName = info.name, ProcessPath = info.path });
                }

                // --- UDP IPv6 ---
                var udp6 = GetExtendedUdp6Table();
                _diag.Log($"PortService: Retrieved {udp6.Count} UDPv6 rows.");
                foreach (var row in udp6)
                {
                    var info = GetInfo(row.OwningPid);
                    results.Add(new PortConnectionInfo { Protocol = "UDPv6", LocalAddress = $"[{SafeIp6(row.LocalAddr)}]:{NtoHs(row.LocalPort)}", LocalPort = NtoHs(row.LocalPort), RemoteAddress = "*:*", State = "Listen", ProcessId = (int)row.OwningPid, ProcessName = info.name, ProcessPath = info.path });
                }
            }
            catch (Exception ex)
            {
                _diag.Log($"PortService Table Retrieval Error: {ex.Message}");
            }

            _diag.Log($"PortService: Finished scan. Total connections: {results.Count}");
            return results;
        });
    }

    private static string SafeIp(uint addr) { try { return new IPAddress(addr).ToString(); } catch { return "0.0.0.0"; } }
    private static string SafeIp6(byte[] addr) { try { return new IPAddress(addr).ToString(); } catch { return "::"; } }
    private static bool IsEmptyIpv6(byte[] addr) => addr == null || addr.All(b => b == 0);


    private static string MapTcpState(uint state) => state switch
    {
        1 => "Closed", 2 => "Listen", 3 => "SynSent", 4 => "SynReceived",
        5 => "Established", 6 => "FinWait1", 7 => "FinWait2", 8 => "CloseWait",
        9 => "Closing", 10 => "LastAck", 11 => "TimeWait", 12 => "DeleteTCB",
        _ => $"Unknown({state})"
    };

    private static int NtoHs(uint networkPort) => IPAddress.NetworkToHostOrder((short)(networkPort & 0xFFFF)) & 0xFFFF;

    // --- P/Invoke Definitions ---

    [DllImport("iphlpapi.dll", SetLastError = true)]
    private static extern uint GetExtendedTcpTable(IntPtr pTcpTable, ref int dwOutBufLen, bool sort, int ipVersion, int tableClass, uint reserved);

    [DllImport("iphlpapi.dll", SetLastError = true)]
    private static extern uint GetExtendedUdpTable(IntPtr pUdpTable, ref int dwOutBufLen, bool sort, int ipVersion, int tableClass, uint reserved);

    [StructLayout(LayoutKind.Sequential)]
    private struct MIB_TCPROW_OWNER_PID { public uint State; public uint LocalAddr; public uint LocalPort; public uint RemoteAddr; public uint RemotePort; public uint OwningPid; }

    [StructLayout(LayoutKind.Sequential)]
    private struct MIB_TCP6ROW_OWNER_PID { [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)] public byte[] LocalAddr; public uint LocalScopeId; public uint LocalPort; [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)] public byte[] RemoteAddr; public uint RemoteScopeId; public uint RemotePort; public uint State; public uint OwningPid; }

    [StructLayout(LayoutKind.Sequential)]
    private struct MIB_UDPROW_OWNER_PID { public uint LocalAddr; public uint LocalPort; public uint OwningPid; }

    [StructLayout(LayoutKind.Sequential)]
    private struct MIB_UDP6ROW_OWNER_PID { [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)] public byte[] LocalAddr; public uint LocalScopeId; public uint LocalPort; public uint OwningPid; }

    private static List<MIB_TCPROW_OWNER_PID> GetExtendedTcpTable(bool sort)
    {
        int size = 0;
        GetExtendedTcpTable(IntPtr.Zero, ref size, sort, 2, 5, 0);
        var buffer = Marshal.AllocHGlobal(size);
        try
        {
            if (GetExtendedTcpTable(buffer, ref size, sort, 2, 5, 0) != 0) return new List<MIB_TCPROW_OWNER_PID>();
            int count = Marshal.ReadInt32(buffer);
            var ptr = buffer + 4;
            var list = new List<MIB_TCPROW_OWNER_PID>();
            for (int i = 0; i < count; i++) { list.Add(Marshal.PtrToStructure<MIB_TCPROW_OWNER_PID>(ptr)); ptr += Marshal.SizeOf<MIB_TCPROW_OWNER_PID>(); }
            return list;
        }
        finally { Marshal.FreeHGlobal(buffer); }
    }

    private static List<MIB_TCP6ROW_OWNER_PID> GetExtendedTcp6Table()
    {
        int size = 0;
        GetExtendedTcpTable(IntPtr.Zero, ref size, true, 23, 5, 0);
        var buffer = Marshal.AllocHGlobal(size);
        try
        {
            if (GetExtendedTcpTable(buffer, ref size, true, 23, 5, 0) != 0) return new List<MIB_TCP6ROW_OWNER_PID>();
            int count = Marshal.ReadInt32(buffer);
            var ptr = buffer + 4;
            var list = new List<MIB_TCP6ROW_OWNER_PID>();
            for (int i = 0; i < count; i++) { list.Add(Marshal.PtrToStructure<MIB_TCP6ROW_OWNER_PID>(ptr)); ptr += Marshal.SizeOf<MIB_TCP6ROW_OWNER_PID>(); }
            return list;
        }
        finally { Marshal.FreeHGlobal(buffer); }
    }

    private static List<MIB_UDPROW_OWNER_PID> GetExtendedUdpTable(bool sort)
    {
        int size = 0;
        GetExtendedUdpTable(IntPtr.Zero, ref size, sort, 2, 1, 0);
        var buffer = Marshal.AllocHGlobal(size);
        try
        {
            if (GetExtendedUdpTable(buffer, ref size, sort, 2, 1, 0) != 0) return new List<MIB_UDPROW_OWNER_PID>();
            int count = Marshal.ReadInt32(buffer);
            var ptr = buffer + 4;
            var list = new List<MIB_UDPROW_OWNER_PID>();
            for (int i = 0; i < count; i++) { list.Add(Marshal.PtrToStructure<MIB_UDPROW_OWNER_PID>(ptr)); ptr += Marshal.SizeOf<MIB_UDPROW_OWNER_PID>(); }
            return list;
        }
        finally { Marshal.FreeHGlobal(buffer); }
    }

    private static List<MIB_UDP6ROW_OWNER_PID> GetExtendedUdp6Table()
    {
        int size = 0;
        GetExtendedUdpTable(IntPtr.Zero, ref size, true, 23, 1, 0);
        var buffer = Marshal.AllocHGlobal(size);
        try
        {
            if (GetExtendedUdpTable(buffer, ref size, true, 23, 1, 0) != 0) return new List<MIB_UDP6ROW_OWNER_PID>();
            int count = Marshal.ReadInt32(buffer);
            var ptr = buffer + 4;
            var list = new List<MIB_UDP6ROW_OWNER_PID>();
            for (int i = 0; i < count; i++) { list.Add(Marshal.PtrToStructure<MIB_UDP6ROW_OWNER_PID>(ptr)); ptr += Marshal.SizeOf<MIB_UDP6ROW_OWNER_PID>(); }
            return list;
        }
        finally { Marshal.FreeHGlobal(buffer); }
    }
}

