using System;

namespace Lisovanie.Models;

/// <summary>
/// Jeden výrobný záznam (jeden výlisok) ukladaný do databázy.
/// Mapuje sa cez Dapper na tabuľku ProductionRecord.
/// </summary>
public class CProductionRecord
{
    public long Id { get; set; }

    /// <summary>Čas dokončenia kusu v UTC (zobrazuje sa lokálne cez <see cref="TimestampLocal"/>).</summary>
    public DateTime TimestampUtc { get; set; }

    /// <summary>Hmotnosť dávky [g] prevzatá z váhy cez handoff zóny.</summary>
    public double Hmotnost { get; set; }

    /// <summary>Dosiahnutá lisovacia sila.</summary>
    public double Sila { get; set; }

    /// <summary>Výsledná vzdialenosť / hrúbka výlisku.</summary>
    public double Vzdialenost { get; set; }

    /// <summary>Čas zhutňovania [ms] – od začiatku lisovania po dosiahnutie požadovanej sily.</summary>
    public long CasZhutnovaniaMs { get; set; }

    /// <summary>Čas zotrvania pod silou [ms].</summary>
    public long CasZotrvaniaMs { get; set; }

    /// <summary>Stav výlisku (Ok / Nok / Unknow) – ukladaný ako int.</summary>
    public EnProduktLis Status { get; set; }

    /// <summary>Lokálny čas pre zobrazenie v UI a exporte.</summary>
    public DateTime TimestampLocal => TimestampUtc.ToLocalTime();
}
