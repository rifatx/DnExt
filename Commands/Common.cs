using DnExt.Common;

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
