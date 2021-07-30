using CommandLine;
using DnExt.Commands.Utils;
using DnExt.Helpers;
using Microsoft.Diagnostics.Runtime;
using Microsoft.Diagnostics.Runtime.Interop;
using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace DnExt.Commands
{
    public static partial class ClrCommands
    {
        class GetModulesOptions
        {
            [Option('s', "saveto", HelpText = "Path to save module to disk")]
            public string SaveTo { get; set; }
            [Option('p', "pattern", Required = false, HelpText = "Filter pattern for module names")]
            public string FilterPattern { get; set; }
            [Option('r', "regex", Required = false, Default = false, HelpText = "Enable/disable regex filtering")]
            public bool IsRegex { get; set; }
        }

        class SaveModuleOptions
        {
            [Option('a', "address", Required = true, HelpText = "Module address")]
            public string Address { get; set; }
            [Option('s', "saveto", Required = true, HelpText = "Path to save module to disk")]
            public string SaveTo { get; set; }
        }

        public static string GetModules(this DataTarget dataTarget, IntPtr clientPtr, string args)
        {
            if (args.ParseAsCommandLine<GetModulesOptions>() is var clo && !clo.IsValid)
            {
                return clo.Message;
            }

            var rt = dataTarget.GetRuntime();
            var options = clo.Options;
            var matcher = new Matcher(options.IsRegex, options.FilterPattern);
            var modules = rt.Modules
                .Where(m => !string.IsNullOrEmpty(m.AssemblyName))
                .Select(m => new
                {
                    Name = new FileInfo(m.AssemblyName).Name,
                    Module = m
                });

            var sbr = new StringBuilder();

            foreach (var m in modules.Where(m => matcher.IsMatch(m.Name)))
            {
                var a = dataTarget.FormatAddress(m.Module.Address);
                sbr.AppendLine($"{OutputHelper.MakeDml($"!dumpmodule -mt {a}", $"{a}", $": {m.Name}")} ({OutputHelper.MakeDml($"!savemanagedmodule --address {a} --saveto {Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory)}", $"save")})");

                if (!string.IsNullOrEmpty(options.SaveTo))
                {
                    using var fs = new FileStream(Path.Combine(options.SaveTo, m.Name), FileMode.Create);
                    SaveToStream(dataTarget, m.Module, fs, sbr);
                }
            }

            return sbr.ToString();
        }

        public static string SaveModule(this DataTarget dataTarget, string args)
        {
            if (args.ParseAsCommandLine<SaveModuleOptions>() is var clo && !clo.IsValid)
            {
                return clo.Message;
            }

            var rt = dataTarget.GetRuntime();
            var options = clo.Options;
            var module = rt.Modules
                .FirstOrDefault(m => !string.IsNullOrEmpty(m.AssemblyName)
                    && m.Address == AddressHelper.ConvertHexAddressToUlong(options.Address));
            var moduleName = new FileInfo(module.AssemblyName).Name;

            using var fs = new FileStream(Path.Combine(options.SaveTo, moduleName), FileMode.Create);
            var sbr = new StringBuilder();
            SaveToStream(dataTarget, module, fs, sbr);

            return sbr.ToString();
        }

        private static bool SaveToStream(DataTarget dataTarget, ClrModule module, Stream moduleStream, StringBuilder sbr)
        {
            var idh = dataTarget.ReadMemory<IMAGE_DOS_HEADER>(module.ImageBase);

            if (idh.e_magic != 23117) // MZ
            {
                sbr.AppendLine($"Bad IMAGE_DOS_HEADER: {module.Name}, unable to save");
                return false;
            }

            // NT PE Headers
            var dwAddr = module.ImageBase;
            uint nRead;
            ulong dwEnd;
            ulong sectionAddr;
            int nSection;

            switch (dataTarget.Architecture)
            {
                case Microsoft.Diagnostics.Runtime.Architecture.X86:
                case Microsoft.Diagnostics.Runtime.Architecture.Arm:
                    if (dataTarget.ReadMemory<IMAGE_NT_HEADERS32>(dwAddr + idh.e_lfanew) is var ntHeader32 && ntHeader32.FileHeader.Machine <= 0)
                    {
                        sbr.AppendLine($"coud not read IMAGE_NT_HEADERS32, unable to save");
                        return false;
                    }

                    sectionAddr = dwAddr + (ulong)idh.e_lfanew + (ulong)Marshal.OffsetOf(typeof(IMAGE_NT_HEADERS32), "OptionalHeader") + ntHeader32.FileHeader.SizeOfOptionalHeader;
                    nSection = ntHeader32.FileHeader.NumberOfSections;
                    dwEnd = dwAddr + ntHeader32.OptionalHeader.SizeOfHeaders;
                    break;
                case Microsoft.Diagnostics.Runtime.Architecture.Amd64:
                case Microsoft.Diagnostics.Runtime.Architecture.Arm64:
                    if (dataTarget.ReadMemory<IMAGE_NT_HEADERS64>(dwAddr + idh.e_lfanew) is var ntHeader && ntHeader.FileHeader.Machine <= 0)
                    {
                        sbr.AppendLine($"coud not read IMAGE_NT_HEADERS64, unable to save");
                        return false;
                    }

                    sectionAddr = dwAddr + (ulong)idh.e_lfanew + (ulong)Marshal.OffsetOf(typeof(IMAGE_NT_HEADERS64), "OptionalHeader") + ntHeader.FileHeader.SizeOfOptionalHeader;
                    nSection = ntHeader.FileHeader.NumberOfSections;
                    dwEnd = dwAddr + ntHeader.OptionalHeader.SizeOfHeaders;
                    break;
                default:
                    sbr.AppendLine($"invalid architecture: {dataTarget.Architecture}, unable to save");
                    return false;
            }

            var memLoc = new MemLocation[nSection];

            var indxSec = -1;
            int slot;

            for (int n = 0; n < nSection; n++)
            {
                if (dataTarget.ReadMemory<IMAGE_SECTION_HEADER>(sectionAddr) is var section && section.SizeOfRawData <= 0)
                {
                    sbr.AppendLine("could not read PE section info");
                    return false;
                }


                for (slot = 0; slot <= indxSec; slot++)
                {
                    if (section.PointerToRawData < (ulong)memLoc[slot].FileAddr)
                    {
                        break;
                    }
                }

                for (int k = indxSec; k >= slot; k--)
                {
                    memLoc[k + 1] = memLoc[k];
                }

                memLoc[slot].VAAddr = (IntPtr)section.VirtualAddress;
                memLoc[slot].VASize = (IntPtr)section.VirtualSize;
                memLoc[slot].FileAddr = (IntPtr)section.PointerToRawData;
                memLoc[slot].FileSize = (IntPtr)section.SizeOfRawData;

                ++indxSec;
                sectionAddr += (ulong)Marshal.SizeOf(section);
            }

            ((IDebugControl)dataTarget.DebuggerInterface).GetPageSize(out var pageSize);

            var buffer = new byte[pageSize];

            var dds = (IDebugDataSpaces3)dataTarget.DebuggerInterface;

            while (dwAddr < dwEnd)
            {
                nRead = pageSize;

                if (dwEnd - dwAddr < nRead)
                {
                    nRead = (uint)(dwEnd - dwAddr);
                }

                if (dds.ReadVirtual(dwAddr, buffer, nRead, out nRead) == 0)
                {
                    moduleStream.Write(buffer, 0, (int)nRead);
                }
                else
                {
                    sbr.AppendLine("Error reading memory");
                    return false;
                }

                dwAddr += nRead;
            }

            var hr = dds.QueryVirtual(module.ImageBase, out var mbi);

            if (hr != 0)
            {
                sbr.AppendLine("Error QueryVirtual");
                return false;
            }

            var dc = (IDebugControl)dataTarget.DebuggerInterface;
            dc.GetDebuggeeType(out var cls, out var qlf);

            var bIsImage = qlf == DEBUG_CLASS_QUALIFIER.KERNEL_IDNA
                || qlf == DEBUG_CLASS_QUALIFIER.USER_WINDOWS_IDNA
                || mbi.Type == MEM.IMAGE
                || mbi.Type == MEM.PRIVATE;

            for (slot = 0; slot <= indxSec; slot++)
            {
                //if (!DebugApi.IsTaget64Bits)
                //{
                if (bIsImage)
                    dwAddr = module.ImageBase + (ulong)memLoc[slot].VAAddr;
                else
                    dwAddr = module.ImageBase + (ulong)memLoc[slot].FileAddr;
                //}
                //else
                //    dwAddr = BaseAddress + (ulong)memLoc[slot].FileAddr;
                dwEnd = (ulong)memLoc[slot].FileSize + dwAddr - 1;

                while (dwAddr <= dwEnd)
                {
                    nRead = pageSize;
                    if (dwEnd - dwAddr + 1 < pageSize)
                        nRead = (uint)(dwEnd - dwAddr + 1);

                    if (dds.ReadVirtual(dwAddr, buffer, nRead, out nRead) == 0)
                        moduleStream.Write(buffer, 0, (int)nRead);
                    else
                    {
                        sbr.AppendLine("Error reading memory");
                        return false;
                    }

                    dwAddr += pageSize;
                }
            }

            return true;
        }
    }
}
