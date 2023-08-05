using System.Runtime.CompilerServices;

namespace RinhaBackEnd
{
    public class ModuleInitialize
    {
        [ModuleInitializer]
        public static void Initialize()
        {
          //  AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
        }
    }
}
