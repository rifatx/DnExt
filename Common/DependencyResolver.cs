using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace DnExt.Common
{
    internal static class DependencyResolver
    {
        private static bool _isAssemblyResolveRegistered = false;
        private static readonly List<string> _assemblies = new List<string>
        {
            "Microsoft.Diagnostics.Runtime",
            "RGiesecke.DllExport.Metadata",
            "CommandLine"
        };

        private static Assembly ResolveAssembly(object sender, ResolveEventArgs args)
        {
            var rm = new System.Resources.ResourceManager("DnExt.ExternalAssemblies", Assembly.GetExecutingAssembly());

            if (_assemblies.FirstOrDefault(a => args.Name.Contains(a)) is var assemblyName && string.IsNullOrEmpty(assemblyName))
            {
                return null;
            }

            return Assembly.Load((byte[])rm.GetObject(assemblyName.Replace('.', '_')));
        }

        internal static void RegisterAssemblyResolve()
        {
            if (!_isAssemblyResolveRegistered)
            {
                AppDomain.CurrentDomain.AssemblyResolve += ResolveAssembly;
                _isAssemblyResolveRegistered = true;
            }
        }
    }
}
