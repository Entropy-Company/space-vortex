using Robust.Shared.Configuration;

namespace Content.Shared.CCVar;

public sealed partial class CCVars
{
    /// <summary>
    /// Интервал между выплатами зарплаты (секунды).
    /// </summary>
    public static readonly CVarDef<int> SalaryTime =
        CVarDef.Create("economy.salary_time", 1200, CVar.SERVER | CVar.ARCHIVE);
}
