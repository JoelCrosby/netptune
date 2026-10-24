using System.Globalization;
using System.Runtime.CompilerServices;

namespace Netptune.UnitTests;

internal static class TestCulture
{
    [ModuleInitializer]
    internal static void PinEnglish()
    {
        var english = CultureInfo.GetCultureInfo("en-GB");

        CultureInfo.DefaultThreadCurrentUICulture = english;
        CultureInfo.CurrentUICulture = english;
    }
}
