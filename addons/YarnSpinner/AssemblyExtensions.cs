using System.Collections.Generic;
using System.Reflection;
using System.Linq;

public static class AssemblyExtensions {
    public static IEnumerable<System.Type> GetLoadableTypes(this Assembly @this) {
        try {
            return @this.GetTypes();
        }
        catch (ReflectionTypeLoadException e) {
            return e.Types.Where(t => t != null);
        }
    }
}
