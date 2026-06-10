using Lisovanie.Models;

namespace Lisovanie.Net
{
    public static class IL
    {
        // Tvoj existujúci zámok pre Sync
        public static readonly object LockSync = new object(); 
        
        // Naša nová spoločná zóna pre sypanie, lisovanie aj odoberanie
        public static CMutexZone ZonePress { get; } = new CMutexZone("Zóna Lisu");
    }
}