using System.ComponentModel;

namespace Lisovanie.Models;

public class DeviceParameters
{
    // ========================================================
    // --- 1. SYSTÉM (0x6000) ---
    // ========================================================
    [Category("1. SYSTÉM (0x6000)"), DisplayName("0x01: can_ID"), Description("CANopen Node ID zariadenia. (Predvolené: 4)"), DefaultValue(0)]
    public int Sys_CanID { get; set; } = 0;

    [Category("1. SYSTÉM (0x6000)"), DisplayName("0x02: mode"), Description("Pracovný režim zariadenia. (Predvolené: 0)"), DefaultValue(0)]
    public int Sys_Mode { get; set; } = 0;

    [Category("1. SYSTÉM (0x6000)"), DisplayName("0x03: repeat_actual"), Description("Aktuálny počet opakovaní cyklu. (Predvolené: 0)"), DefaultValue(0)]
    public int Sys_RepeatActual { get; set; } = 0;

    [Category("1. SYSTÉM (0x6000)"), DisplayName("0x04: repeat_max"), Description("Maximálny počet opakovaní cyklu. (Predvolené: 0)"), DefaultValue(0)]
    public int Sys_RepeatMax { get; set; } = 0;

    [Category("1. SYSTÉM (0x6000)"), DisplayName("0x05: actual_position_double"), Description("Aktuálna pozícia (Double). (Predvolené: 0)"), DefaultValue(0)]
    public int Sys_ActualPositionDouble { get; set; } = 0;

    // ========================================================
    // --- 2. ZÁMOK (0x6001) ---
    // ========================================================
    [Category("2. ZÁMOK (0x6001)"), DisplayName("0x01: za_offset_M4"), Description("Korekcia nulovej polohy motora hlavy. (Predvolené: 0)"), DefaultValue(0)]
    public int Za_Offset_M4 { get; set; } = 0;
    [Category("2. ZÁMOK (0x6001)"), DisplayName("0x08: za_offset_M5"), Description("Korekcia nulovej polohy motora celusti. (Predvolené: 0)"), DefaultValue(0)]
    public int Za_Offset_M5 { get; set; } = 0;

    [Category("2. ZÁMOK (0x6001)"), DisplayName("0x02: za_naklopenie_vlavo"), Description("Uhol naklopenia pre výsyp vľavo. (Predvolené: 1000)"), DefaultValue(0)]
    public int Za_NaklopenieVlavo { get; set; } = 0;

    [Category("2. ZÁMOK (0x6001)"), DisplayName("0x03: za_pocet_zatraseni_vlavo"), Description("Počet kmitov pri výsype vľavo. (Predvolené: 3)"), DefaultValue(0)]
    public int Za_PocetZatraseniVlavo { get; set; } = 0;

    [Category("2. ZÁMOK (0x6001)"), DisplayName("0x04: za_amplituda_zatraseni_vlavo"), Description("Amplitúda kmitov pri výsype vľavo. (Predvolené: 200)"), DefaultValue(0)]
    public int Za_AmplitudaZatraseniVlavo { get; set; } = 0;

    [Category("2. ZÁMOK (0x6001)"), DisplayName("0x05: za_naklopenie_vpravo"), Description("Uhol naklopenia pre výsyp vpravo. (Predvolené: 1000)"), DefaultValue(0)]
    public int Za_NaklopenieVpravo { get; set; } = 0;

    [Category("2. ZÁMOK (0x6001)"), DisplayName("0x06: za_pocet_zatraseni_vpravo"), Description("Počet kmitov pri výsype vpravo. (Predvolené: 3)"), DefaultValue(0)]
    public int Za_PocetZatraseniVpravo { get; set; } = 0;

    [Category("2. ZÁMOK (0x6001)"), DisplayName("0x07: za_amplituda_zatraseni_vpravo"), Description("Amplitúda kmitov pri výsype vpravo. (Predvolené: 200)"), DefaultValue(0)]
    public int Za_AmplitudaZatraseniVpravo { get; set; } = 0;

    // ========================================================
    // --- 3. VÁHA (0x6002) ---
    // ========================================================
    [Category("3. VÁHA (0x6002)"), DisplayName("0x01: va_senzor"), Description("Typ pripojeného senzora. (Predvolené: 0)"), DefaultValue(0)]
    public int Va_Senzor { get; set; } = 0;

    [Category("3. VÁHA (0x6002)"), DisplayName("0x02: va_scale"), Description("Mierka váhy (Scale). (Predvolené: 1000)"), DefaultValue(0)]
    public int Va_Scale { get; set; } = 0;

    [Category("3. VÁHA (0x6002)"), DisplayName("0x03: va_offset"), Description("Offset váhy (Nula). (Predvolené: 0)"), DefaultValue(0)]
    public int Va_Offset { get; set; } = 0;

    [Category("3. VÁHA (0x6002)"), DisplayName("0x04: va_vaha"), Description("Aktuálna hodnota váhy. (Predvolené: 0)"), DefaultValue(0)]
    public int Va_Vaha { get; set; } = 0;

    [Category("3. VÁHA (0x6002)"), DisplayName("0x05: va_parameter"), Description("Rezerva pre parametre váhy. (Predvolené: 0)"), DefaultValue(0)]
    public int Va_Parameter { get; set; } = 0;

    [Category("3. VÁHA (0x6002)"), DisplayName("0x06: va_weight1_g"), Description("Kalibračný bod 1 (mg) - Nula. (Predvolené: 0)"), DefaultValue(0)]
    public int Va_Weight1_g { get; set; } = 0;

    [Category("3. VÁHA (0x6002)"), DisplayName("0x07: va_weight2_g"), Description("Kalibračný bod 2 (mg) - Závažie. (Predvolené: 50000)"), DefaultValue(0)]
    public int Va_Weight2_g { get; set; } = 0;

    [Category("3. VÁHA (0x6002)"), DisplayName("0x08: va_raw1"), Description("RAW hodnota pre kalibračný bod 1. (Predvolené: 0)"), DefaultValue(0)]
    public int Va_Raw1 { get; set; } = 0;

    [Category("3. VÁHA (0x6002)"), DisplayName("0x09: va_raw2"), Description("RAW hodnota pre kalibračný bod 2. (Predvolené: 1000000)"), DefaultValue(0)]
    public int Va_Raw2 { get; set; } = 0;

    [Category("3. VÁHA (0x6002)"), DisplayName("0x0A: va_valid"), Description("Príznak platnej kalibrácie (1=OK). (Predvolené: 0)"), DefaultValue(0)]
    public int Va_Valid { get; set; } = 0;

    [Category("3. VÁHA (0x6002)"), DisplayName("0x0B: va_rate"), Description("Rýchlosť prevodníka (SPS, 8=1000Hz). (Predvolené: 8)"), DefaultValue(0)]
    public int Va_Rate { get; set; } = 0;

    [Category("3. VÁHA (0x6002)"), DisplayName("0x0C: va_enableLog"), Description("Povolenie logovania váhy (1=ZAP). (Predvolené: 0)"), DefaultValue(0)]
    public int Va_EnableLog { get; set; } = 0;

    [Category("3. VÁHA (0x6002)"), DisplayName("0x0D: va_logValue"), Description("Typ logu (0=RAW, 1=Inter, 2=Netto). (Predvolené: 2)"), DefaultValue(0)]
    public int Va_LogValue { get; set; } = 0;

    // ========================================================
    // --- 4. VYKLADAČ (0x6003) ---
    // ========================================================
    [Category("4. VYKLADAČ (0x6003)"), DisplayName("0x01: vy_velocity"), Description("Rýchlosť presunu výložníka. (Predvolené: 50000)"), DefaultValue(0)]
    public int Vy_Velocity { get; set; } = 0;

    [Category("4. VYKLADAČ (0x6003)"), DisplayName("0x02: vy_acc"), Description("Zrýchlenie presunu výložníka. (Predvolené: 50000)"), DefaultValue(0)]
    public int Vy_Acc { get; set; } = 0;

    [Category("4. VYKLADAČ (0x6003)"), DisplayName("0x03: vy_dcc"), Description("Spomalenie presunu výložníka. (Predvolené: 50000)"), DefaultValue(0)]
    public int Vy_Dcc { get; set; } = 0;

    [Category("4. VYKLADAČ (0x6003)"), DisplayName("0x04: vy_vzdialenost_vysun_1"), Description("Pracovná poloha 1 (Výsun). (Predvolené: 10000)"), DefaultValue(0)]
    public int Vy_VzdialenostVysun1 { get; set; } = 0;

    [Category("4. VYKLADAČ (0x6003)"), DisplayName("0x05: vy_vzdialenost_vyklop"), Description("Vzdialenosť pre vyklopenie misky. (Predvolené: 2000)"), DefaultValue(0)]
    public int Vy_VzdialenostVyklop { get; set; } = 0;

    [Category("4. VYKLADAČ (0x6003)"), DisplayName("0x06: vy_pocet_zatraseni"), Description("Počet kmitov pri výsype misky. (Predvolené: 3)"), DefaultValue(0)]
    public int Vy_PocetZatraseni { get; set; } = 0;

    [Category("4. VYKLADAČ (0x6003)"), DisplayName("0x07: vy_amplituda_zatrasenia"), Description("Amplitúda kmitov pri výsype misky. (Predvolené: 500)"), DefaultValue(0)]
    public int Vy_AmplitudaZatrasenia { get; set; } = 0;

    [Category("4. VYKLADAČ (0x6003)"), DisplayName("0x08: vy_acc_vyklop"), Description("Zrýchlenie pri vyklápaní. (Predvolené: 100000)"), DefaultValue(0)]
    public int Vy_AccVyklop { get; set; } = 0;

    [Category("4. VYKLADAČ (0x6003)"), DisplayName("0x09: vy_dcc_vyklop"), Description("Spomalenie pri vyklápaní. (Predvolené: 100000)"), DefaultValue(0)]
    public int Vy_DccVyklop { get; set; } = 0;

    [Category("4. VYKLADAČ (0x6003)"), DisplayName("0x0A: vy_vzdialenost_vysun_2"), Description("Pracovná poloha 2 (Výsun). (Predvolené: 15000)"), DefaultValue(0)]
    public int Vy_VzdialenostVysun2 { get; set; } = 0;

    // ========================================================
    // --- 5. PODÁVAČ (0x6004) ---
    // ========================================================
    [Category("5. PODÁVAČ (0x6004)"), DisplayName("0x01: po_velocity_horna"), Description("Rezerva. (Predvolené: 0)"), DefaultValue(0)]
    public int Po_VelocityHorna { get; set; } = 0;

    [Category("5. PODÁVAČ (0x6004)"), DisplayName("0x02: po_velocity_spodna"), Description("Rezerva. (Predvolené: 0)"), DefaultValue(0)]
    public int Po_VelocitySpodna { get; set; } = 0;

    [Category("5. PODÁVAČ (0x6004)"), DisplayName("0x03: po_velocity_max"), Description("Rezerva. (Predvolené: 0)"), DefaultValue(0)]
    public int Po_VelocityMax { get; set; } = 0;

    [Category("5. PODÁVAČ (0x6004)"), DisplayName("0x04: po_cas_pulzu"), Description("Rezerva. (Predvolené: 0)"), DefaultValue(0)]
    public int Po_CasPulzu { get; set; } = 0;

    [Category("5. PODÁVAČ (0x6004)"), DisplayName("0x05: po_pocet_pulzov"), Description("Rezerva. (Predvolené: 0)"), DefaultValue(0)]
    public int Po_PocetPulzov { get; set; } = 0;

    [Category("5. PODÁVAČ (0x6004)"), DisplayName("0x06: po_pocet_zatraseni"), Description("Rezerva. (Predvolené: 0)"), DefaultValue(0)]
    public int Po_PocetZatraseni { get; set; } = 0;

    [Category("5. PODÁVAČ (0x6004)"), DisplayName("0x07: po_zrychlenie_zatrasenia"), Description("Rezerva. (Predvolené: 0)"), DefaultValue(0)]
    public int Po_ZrychlenieZatrasenia { get; set; } = 0;

    [Category("5. PODÁVAČ (0x6004)"), DisplayName("0x08: po_velocity"), Description("Rezerva. (Predvolené: 0)"), DefaultValue(0)]
    public int Po_Velocity { get; set; } = 0;

    [Category("5. PODÁVAČ (0x6004)"), DisplayName("0x09: po_cas"), Description("Rezerva. (Predvolené: 0)"), DefaultValue(0)]
    public int Po_Cas { get; set; } = 0;

    [Category("5. PODÁVAČ (0x6004)"), DisplayName("0x0A: po_start_rpm"), Description("Štartovacie otáčky vibropodávača (RPM). (Predvolené: 600)"), DefaultValue(0)]
    public int Po_StartRpm { get; set; } = 0;

    [Category("5. PODÁVAČ (0x6004)"), DisplayName("0x0B: po_target_rpm"), Description("Cieľové stredné otáčky modulácie (RPM). (Predvolené: 1400)"), DefaultValue(0)]
    public int Po_TargetRpm { get; set; } = 0;

    [Category("5. PODÁVAČ (0x6004)"), DisplayName("0x0C: po_rozbeh_accel"), Description("Zrýchlenie pre nábeh (RPM/s). (Predvolené: 2000)"), DefaultValue(0)]
    public int Po_RozbehAccel { get; set; } = 0;

    [Category("5. PODÁVAČ (0x6004)"), DisplayName("0x0D: po_mod_depth"), Description("Hĺbka modulácie (± RPM). (Predvolené: 500)"), DefaultValue(0)]
    public int Po_ModDepth { get; set; } = 0;

    [Category("5. PODÁVAČ (0x6004)"), DisplayName("0x0E: po_mod_accel"), Description("Strmosť modulácie (RPM/s). (Predvolené: 10000)"), DefaultValue(0)]
    public int Po_ModAccel { get; set; } = 0;

    [Category("5. PODÁVAČ (0x6004)"), DisplayName("0x0F: po_stop_decel"), Description("Spomalenie pre zastavenie (RPM/s). (Predvolené: 4000)"), DefaultValue(0)]
    public int Po_StopDecel { get; set; } = 0;

    // ========================================================
    // --- 6. RIADENIE DÁVKY (0x6006) ---
    // ========================================================
    [Category("6. RIADENIE DÁVKY (0x6006)"), DisplayName("0x20: rs_target_weight_mg"), Description("Cieľová váha dávky (mg). (Predvolené: 5500)"), DefaultValue(0)]
    public int Rs_TargetWeightMg { get; set; } = 0;

    [Category("6. RIADENIE DÁVKY (0x6006)"), DisplayName("0x21: rs_tol_plus_mg"), Description("Kladná tolerancia (+OK) (mg). (Predvolené: 50)"), DefaultValue(0)]
    public int Rs_TolPlusMg { get; set; } = 0;

    [Category("6. RIADENIE DÁVKY (0x6006)"), DisplayName("0x22: rs_tol_minus_mg"), Description("Záporná tolerancia (-OK) (mg). (Predvolené: 30)"), DefaultValue(0)]
    public int Rs_TolMinusMg { get; set; } = 0;

    [Category("6. RIADENIE DÁVKY (0x6006)"), DisplayName("0x23: rs_bulk_limit_mg"), Description("Hranica prechodu Hrubá -> Útlm (mg). (Predvolené: 3000)"), DefaultValue(0)]
    public int Rs_BulkLimitMg { get; set; } = 0;

    [Category("6. RIADENIE DÁVKY (0x6006)"), DisplayName("0x24: rs_flow_target_mgps"), Description("Cieľový prietok v hrubej fáze (mg/s). (Predvolené: 600)"), DefaultValue(0)]
    public int Rs_FlowTargetMgps { get; set; } = 0;

    [Category("6. RIADENIE DÁVKY (0x6006)"), DisplayName("0x25: rs_rpm_max_limit"), Description("Maximálne povolené otáčky (RPM). (Predvolené: 2000)"), DefaultValue(0)]
    public int Rs_RpmMaxLimit { get; set; } = 0;

    [Category("6. RIADENIE DÁVKY (0x6006)"), DisplayName("0x26: rs_rpm_min_limit"), Description("Minimálne povolené otáčky (RPM). (Predvolené: 600)"), DefaultValue(0)]
    public int Rs_RpmMinLimit { get; set; } = 0;

    [Category("6. RIADENIE DÁVKY (0x6006)"), DisplayName("0x27: rs_adapt_bulk_rpm"), Description("Štartovacie otáčky hrubej fázy (RPM). (Predvolené: 1400)"), DefaultValue(0)]
    public int Rs_AdaptBulkRpm { get; set; } = 0;

    [Category("6. RIADENIE DÁVKY (0x6006)"), DisplayName("0x28: rs_preact_learned_mg"), Description("Predstih vypnutia (naučený) (mg). (Predvolené: 50)"), DefaultValue(0)]
    public int Rs_PreactLearnedMg { get; set; } = 0;

    [Category("6. RIADENIE DÁVKY (0x6006)"), DisplayName("0x29: rs_preact_flow_nominal_mgps"), Description("Nominálny prietok pre predstih (mg/s). (Predvolené: 500)"), DefaultValue(0)]
    public int Rs_PreactFlowNominalMgps { get; set; } = 0;

    [Category("6. RIADENIE DÁVKY (0x6006)"), DisplayName("0x2A: rs_preact_learn_coeff"), Description("Koeficient učenia predstihu (x1000). (Predvolené: 100)"), DefaultValue(0)]
    public int Rs_PreactLearnCoeff { get; set; } = 0;

    [Category("6. RIADENIE DÁVKY (0x6006)"), DisplayName("0x2B: rs_Ki_bulk"), Description("Integračná konštanta (Hrubá) (x1000). (Predvolené: 450000)"), DefaultValue(0)]
    public int Rs_KiBulk { get; set; } = 0;

    [Category("6. RIADENIE DÁVKY (0x6006)"), DisplayName("0x2C: rs_Kp_fine"), Description("Proporcionálna konštanta (Jemná) (x1000). (Predvolené: 400000)"), DefaultValue(0)]
    public int Rs_KpFine { get; set; } = 0;

    [Category("6. RIADENIE DÁVKY (0x6006)"), DisplayName("0x2D: rs_Ki_fine"), Description("Integračná konštanta (Jemná) (x1000). (Predvolené: 100000)"), DefaultValue(0)]
    public int Rs_KiFine { get; set; } = 0;

    [Category("6. RIADENIE DÁVKY (0x6006)"), DisplayName("0x2E: rs_damping_drop_rpm"), Description("Skokové zníženie RPM v útlme (RPM). (Predvolené: 400)"), DefaultValue(0)]
    public int Rs_DampingDropRpm { get; set; } = 0;

    [Category("6. RIADENIE DÁVKY (0x6006)"), DisplayName("0x2F: rs_damping_time_ms"), Description("Trvanie fázy útlmu (ms). (Predvolené: 1000)"), DefaultValue(0)]
    public int Rs_DampingTimeMs { get; set; } = 0;

    [Category("6. RIADENIE DÁVKY (0x6006)"), DisplayName("0x30: rs_timeout_bulk_ms"), Description("Timeout hrubej fázy (ms). (Predvolené: 15000)"), DefaultValue(0)]
    public int Rs_TimeoutBulkMs { get; set; } = 0;

    [Category("6. RIADENIE DÁVKY (0x6006)"), DisplayName("0x31: rs_timeout_fine_ms"), Description("Timeout jemnej fázy (ms). (Predvolené: 20000)"), DefaultValue(0)]
    public int Rs_TimeoutFineMs { get; set; } = 0;

    [Category("6. RIADENIE DÁVKY (0x6006)"), DisplayName("0x32: rs_mod_nominal"), Description("Nominálna hĺbka modulácie. (Predvolené: 0)"), DefaultValue(0)]
    public int Rs_ModNominal { get; set; } = 0;
}
