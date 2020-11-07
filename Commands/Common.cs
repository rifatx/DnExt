using DnExt.Common;
using System;

namespace DnExt.Commands
{
    public static partial class ClrCommands
    {
        static ClrCommands()
        {
            DependencyResolver.RegisterAssemblyResolve();
        }
    }
}
