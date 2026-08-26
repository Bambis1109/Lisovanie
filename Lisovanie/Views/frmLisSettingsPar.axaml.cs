using Avalonia.Controls;
using Avalonia.Interactivity;
using Lisovanie.Models;
using System;
using System.Threading.Tasks;

namespace Lisovanie.Views;

public partial class FrmLisSettingsPar : Window
{
    private CControlLis? _controlLis;

    public FrmLisSettingsPar()
    {
        InitializeComponent();
    }

    public FrmLisSettingsPar(CControlLis controlLis)
    {
        InitializeComponent();
        _controlLis = controlLis;
        DataContext = controlLis.ParametersLis;
    }

    private void BtnClose_OnClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    private void BtnSave_OnClick(object? sender, RoutedEventArgs e)
    {
        _controlLis?.SaveParameters();
    }

    private void BtnLoad_OnClick(object? sender, RoutedEventArgs e)
    {
        _controlLis?.LoadParameters();
    }

    private void BtnRecalculate_OnClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is CParametersLis p)
        {
            p.ParLis.RecalculateCalibration();
        }
    }

    private async Task EditValueAsync(string title, double initialValue, Action<double> setter)
    {
        var numpad = new NumpadWindow(title, initialValue);
        var result = await numpad.ShowDialog<bool>(this);
        if (result)
        {
            setter(numpad.ResultValue);
        }
    }

    private async Task EditValueIntAsync(string title, int initialValue, Action<int> setter)
    {
        var numpad = new NumpadWindow(title, initialValue);
        var result = await numpad.ShowDialog<bool>(this);
        if (result)
        {
            setter((int)numpad.ResultValue);
        }
    }

    // --- LIS ---
    private async void BtnLisVyskaNasypacia_OnClick(object? sender, RoutedEventArgs e) { if (DataContext is CParametersLis p) await EditValueAsync("Výška násypacia", p.ParLis.VyskaNasypacia, v => p.ParLis.VyskaNasypacia = v); }
    private async void BtnLisVyskaPriblizenie_OnClick(object? sender, RoutedEventArgs e) { if (DataContext is CParametersLis p) await EditValueAsync("Výška priblíženie", p.ParLis.VyskaPriblizenie, v => p.ParLis.VyskaPriblizenie = v); }
    private async void BtnLisVyskaKalibra_OnClick(object? sender, RoutedEventArgs e) { if (DataContext is CParametersLis p) await EditValueAsync("Výška kalibra", p.ParLis.VyskaKalibra, v => p.ParLis.VyskaKalibra = v); }
    private async void BtnLisVyskaSenAbsolut1_OnClick(object? sender, RoutedEventArgs e) { if (DataContext is CParametersLis p) await EditValueAsync("VyskaSenAbsolut 1", p.ParLis.VyskaSenAbsolut1, v => p.ParLis.VyskaSenAbsolut1 = v); }
    private async void BtnLisSilaKalib1_OnClick(object? sender, RoutedEventArgs e) { if (DataContext is CParametersLis p) await EditValueAsync("SilaKalib 1", p.ParLis.SilaKalib1, v => p.ParLis.SilaKalib1 = v); }
    private async void BtnLisVyskaSenAbsolut2_OnClick(object? sender, RoutedEventArgs e) { if (DataContext is CParametersLis p) await EditValueAsync("VyskaSenAbsolut 2", p.ParLis.VyskaSenAbsolut2, v => p.ParLis.VyskaSenAbsolut2 = v); }
    private async void BtnLisSilaKalib2_OnClick(object? sender, RoutedEventArgs e) { if (DataContext is CParametersLis p) await EditValueAsync("SilaKalib 2", p.ParLis.SilaKalib2, v => p.ParLis.SilaKalib2 = v); }
    private async void BtnLisVyskaSenPulz_OnClick(object? sender, RoutedEventArgs e) { if (DataContext is CParametersLis p) await EditValueAsync("VyskaSenPulz", p.ParLis.VyskaSenPulz, v => p.ParLis.VyskaSenPulz = v); }
    private async void BtnLisVyskaCistenia_OnClick(object? sender, RoutedEventArgs e) { if (DataContext is CParametersLis p) await EditValueAsync("Výška čistenia", p.ParLis.VyskaCistenia, v => p.ParLis.VyskaCistenia = v); }

    // --- KONZOLA ---
    private async void BtnKonzolaVyskaOdoberacia_OnClick(object? sender, RoutedEventArgs e) { if (DataContext is CParametersLis p) await EditValueAsync("Výška odoberacia", p.ParKonzola.VyskaOdoberacia, v => p.ParKonzola.VyskaOdoberacia = v); }
    private async void BtnKonzolaVyskaNasypacia_OnClick(object? sender, RoutedEventArgs e) { if (DataContext is CParametersLis p) await EditValueAsync("Výška násypacia", p.ParKonzola.VyskaNasypacia, v => p.ParKonzola.VyskaNasypacia = v); }
    private async void BtnKonzolaVyskaLisovacia_OnClick(object? sender, RoutedEventArgs e) { if (DataContext is CParametersLis p) await EditValueAsync("Výška lisovacia", p.ParKonzola.VyskaLisovacia, v => p.ParKonzola.VyskaLisovacia = v); }
    private async void BtnKonzolaVyskaCistenia_OnClick(object? sender, RoutedEventArgs e) { if (DataContext is CParametersLis p) await EditValueAsync("Výška čistenia", p.ParKonzola.VyskaCistenia, v => p.ParKonzola.VyskaCistenia = v); }
    private async void BtnKonzolaCyklovCistenia_OnClick(object? sender, RoutedEventArgs e) { if (DataContext is CParametersLis p) await EditValueIntAsync("Cyklov čistenia", p.ParKonzola.CyklovCistenia, v => p.ParKonzola.CyklovCistenia = v); }

    // --- LISOVANIE: SILOVA METODA ---
    private async void BtnLisovanieStredVychodzia_OnClick(object? sender, RoutedEventArgs e) { if (DataContext is CParametersLis p) await EditValueAsync("Stred východzia", p.ParLisovanie.StredVychodzia, v => p.ParLisovanie.StredVychodzia = v); }
    private async void BtnLisovanieDobaDrzania_OnClick(object? sender, RoutedEventArgs e) { if (DataContext is CParametersLis p) await EditValueIntAsync("Doba držania [ms]", (int)p.ParLisovanie.DobaDrzaniaMs, v => p.ParLisovanie.DobaDrzaniaMs = v); }
    private async void BtnLisovanieKrokHruby_OnClick(object? sender, RoutedEventArgs e) { if (DataContext is CParametersLis p) await EditValueAsync("Krok hrubý", p.ParLisovanie.KrokPritlakuHruby, v => p.ParLisovanie.KrokPritlakuHruby = v); }
    private async void BtnLisovanieKrokStredny_OnClick(object? sender, RoutedEventArgs e) { if (DataContext is CParametersLis p) await EditValueAsync("Krok stredný", p.ParLisovanie.KrokPritlakuStredny, v => p.ParLisovanie.KrokPritlakuStredny = v); }
    private async void BtnLisovanieKrokJemny_OnClick(object? sender, RoutedEventArgs e) { if (DataContext is CParametersLis p) await EditValueAsync("Krok jemný", p.ParLisovanie.KrokPritlakuJemny, v => p.ParLisovanie.KrokPritlakuJemny = v); }
    private async void BtnLisovaniePrahStredny_OnClick(object? sender, RoutedEventArgs e) { if (DataContext is CParametersLis p) await EditValueAsync("Prah stredný [N]", p.ParLisovanie.PrahStredny, v => p.ParLisovanie.PrahStredny = v); }
    private async void BtnLisovaniePrahJemny_OnClick(object? sender, RoutedEventArgs e) { if (DataContext is CParametersLis p) await EditValueAsync("Prah jemný [N]", p.ParLisovanie.PrahJemny, v => p.ParLisovanie.PrahJemny = v); }
    private async void BtnLisovanieKrokUdrziavania_OnClick(object? sender, RoutedEventArgs e) { if (DataContext is CParametersLis p) await EditValueAsync("Krok udržiavania", p.ParLisovanie.KrokUdrziavania, v => p.ParLisovanie.KrokUdrziavania = v); }
    private async void BtnLisovanieRychlyVelocity_OnClick(object? sender, RoutedEventArgs e) { if (DataContext is CParametersLis p) await EditValueIntAsync("Rýchly profil - rýchlosť", (int)p.ParLisovanie.ProfilRychlyVelocity, v => p.ParLisovanie.ProfilRychlyVelocity = (uint)v); }
    private async void BtnLisovanieRychlyAcc_OnClick(object? sender, RoutedEventArgs e) { if (DataContext is CParametersLis p) await EditValueIntAsync("Rýchly profil - zrýchlenie", (int)p.ParLisovanie.ProfilRychlyAcc, v => p.ParLisovanie.ProfilRychlyAcc = (uint)v); }
    private async void BtnLisovanieRychlyDcc_OnClick(object? sender, RoutedEventArgs e) { if (DataContext is CParametersLis p) await EditValueIntAsync("Rýchly profil - spomalenie", (int)p.ParLisovanie.ProfilRychlyDcc, v => p.ParLisovanie.ProfilRychlyDcc = (uint)v); }
    private async void BtnLisovaniePomalyVelocity_OnClick(object? sender, RoutedEventArgs e) { if (DataContext is CParametersLis p) await EditValueIntAsync("Pomalý profil - rýchlosť", (int)p.ParLisovanie.ProfilPomalyVelocity, v => p.ParLisovanie.ProfilPomalyVelocity = (uint)v); }
    private async void BtnLisovaniePomalyAcc_OnClick(object? sender, RoutedEventArgs e) { if (DataContext is CParametersLis p) await EditValueIntAsync("Pomalý profil - zrýchlenie", (int)p.ParLisovanie.ProfilPomalyAcc, v => p.ParLisovanie.ProfilPomalyAcc = (uint)v); }
    private async void BtnLisovaniePomalyDcc_OnClick(object? sender, RoutedEventArgs e) { if (DataContext is CParametersLis p) await EditValueIntAsync("Pomalý profil - spomalenie", (int)p.ParLisovanie.ProfilPomalyDcc, v => p.ParLisovanie.ProfilPomalyDcc = (uint)v); }

    // --- LISOVANIE: METODA NA VZDIALENOST ---
    private async void BtnVzdialenostDobaDrzania_OnClick(object? sender, RoutedEventArgs e) { if (DataContext is CParametersLis p) await EditValueIntAsync("Doba držania [ms]", (int)p.ParLisovanieVzdialenost.DobaDrzaniaMs, v => p.ParLisovanieVzdialenost.DobaDrzaniaMs = v); }
    private async void BtnVzdialenostKrokHruby_OnClick(object? sender, RoutedEventArgs e) { if (DataContext is CParametersLis p) await EditValueAsync("Krok hrubý", p.ParLisovanieVzdialenost.KrokPritlakuHruby, v => p.ParLisovanieVzdialenost.KrokPritlakuHruby = v); }
    private async void BtnVzdialenostKrokStredny_OnClick(object? sender, RoutedEventArgs e) { if (DataContext is CParametersLis p) await EditValueAsync("Krok stredný", p.ParLisovanieVzdialenost.KrokPritlakuStredny, v => p.ParLisovanieVzdialenost.KrokPritlakuStredny = v); }
    private async void BtnVzdialenostKrokJemny_OnClick(object? sender, RoutedEventArgs e) { if (DataContext is CParametersLis p) await EditValueAsync("Krok jemný", p.ParLisovanieVzdialenost.KrokPritlakuJemny, v => p.ParLisovanieVzdialenost.KrokPritlakuJemny = v); }
    private async void BtnVzdialenostPrahStredny_OnClick(object? sender, RoutedEventArgs e) { if (DataContext is CParametersLis p) await EditValueAsync("Prah stredný [mm]", p.ParLisovanieVzdialenost.PrahStredny, v => p.ParLisovanieVzdialenost.PrahStredny = v); }
    private async void BtnVzdialenostPrahJemny_OnClick(object? sender, RoutedEventArgs e) { if (DataContext is CParametersLis p) await EditValueAsync("Prah jemný [mm]", p.ParLisovanieVzdialenost.PrahJemny, v => p.ParLisovanieVzdialenost.PrahJemny = v); }
    private async void BtnVzdialenostPauzaKroku_OnClick(object? sender, RoutedEventArgs e) { if (DataContext is CParametersLis p) await EditValueIntAsync("Pauza kroku [ms]", p.ParLisovanieVzdialenost.PauzaKrokuMs, v => p.ParLisovanieVzdialenost.PauzaKrokuMs = v); }

    // --- MULTI-MIX ---
    private async void BtnMultiMixVyskaPritlacenia_OnClick(object? sender, RoutedEventArgs e) { if (DataContext is CParametersLis p) await EditValueAsync("Výška pritlačenia", p.ParMultiMix.VyskaPritlacenia, v => p.ParMultiMix.VyskaPritlacenia = v); }
    private async void BtnMultiMixSilaMax_OnClick(object? sender, RoutedEventArgs e) { if (DataContext is CParametersLis p) await EditValueAsync("Sila max pri zrovnávaní [N]", p.ParMultiMix.SilaMaxPritlacenia, v => p.ParMultiMix.SilaMaxPritlacenia = v); }
    private async void BtnMultiMixProfilVelocity_OnClick(object? sender, RoutedEventArgs e) { if (DataContext is CParametersLis p) await EditValueIntAsync("Zrovnávanie - rýchlosť", (int)p.ParMultiMix.ProfilVelocity, v => p.ParMultiMix.ProfilVelocity = (uint)v); }
    private async void BtnMultiMixProfilAcc_OnClick(object? sender, RoutedEventArgs e) { if (DataContext is CParametersLis p) await EditValueIntAsync("Zrovnávanie - zrýchlenie", (int)p.ParMultiMix.ProfilAcc, v => p.ParMultiMix.ProfilAcc = (uint)v); }
    private async void BtnMultiMixProfilDcc_OnClick(object? sender, RoutedEventArgs e) { if (DataContext is CParametersLis p) await EditValueIntAsync("Zrovnávanie - spomalenie", (int)p.ParMultiMix.ProfilDcc, v => p.ParMultiMix.ProfilDcc = (uint)v); }

    // --- VYROBOK ---
    private async void BtnVyrobokVyskaMax_OnClick(object? sender, RoutedEventArgs e) { if (DataContext is CParametersLis p) await EditValueAsync("Výška Max", p.ParVyrobok.VyskaMax, v => p.ParVyrobok.VyskaMax = v); }
    private async void BtnVyrobokVyskaMin_OnClick(object? sender, RoutedEventArgs e) { if (DataContext is CParametersLis p) await EditValueAsync("Výška Min", p.ParVyrobok.VyskaMin, v => p.ParVyrobok.VyskaMin = v); }
    private async void BtnVyrobokVyskaPozadovana_OnClick(object? sender, RoutedEventArgs e) { if (DataContext is CParametersLis p) await EditValueAsync("Výška Požadovaná", p.ParVyrobok.VyskaPozadovana, v => p.ParVyrobok.VyskaPozadovana = v); }
    private async void BtnVyrobokSilaMax_OnClick(object? sender, RoutedEventArgs e) { if (DataContext is CParametersLis p) await EditValueAsync("Sila Max", p.ParVyrobok.SilaMax, v => p.ParVyrobok.SilaMax = v); }
    private async void BtnVyrobokSilaMin_OnClick(object? sender, RoutedEventArgs e) { if (DataContext is CParametersLis p) await EditValueAsync("Sila Min", p.ParVyrobok.SilaMin, v => p.ParVyrobok.SilaMin = v); }
    private async void BtnVyrobokSilaPozadovana_OnClick(object? sender, RoutedEventArgs e) { if (DataContext is CParametersLis p) await EditValueAsync("Sila Požadovaná", p.ParVyrobok.SilaPozadovana, v => p.ParVyrobok.SilaPozadovana = v); }

    // --- VAHA ---
    private async void BtnVahaVahaPozadovana_OnClick(object? sender, RoutedEventArgs e) { if (DataContext is CParametersLis p) await EditValueAsync("Váha Požadovaná", p.ParVaha.VahaPozadovana, v => p.ParVaha.VahaPozadovana = v); }
    private async void BtnVahaVahaMax_OnClick(object? sender, RoutedEventArgs e) { if (DataContext is CParametersLis p) await EditValueAsync("Váha Max", p.ParVaha.VahaMax, v => p.ParVaha.VahaMax = v); }
    private async void BtnVahaVahaMin_OnClick(object? sender, RoutedEventArgs e) { if (DataContext is CParametersLis p) await EditValueAsync("Váha Min", p.ParVaha.VahaMin, v => p.ParVaha.VahaMin = v); }

    // --- SYSTEM ---
    private async void BtnSysCanLine_OnClick(object? sender, RoutedEventArgs e) { if (DataContext is CParametersLis p) await EditValueIntAsync("CAN Line", p.CanLine, v => p.CanLine = v); }
    private async void BtnSysBoardLine_OnClick(object? sender, RoutedEventArgs e) { if (DataContext is CParametersLis p) await EditValueIntAsync("Board Line", p.BoardLine, v => p.BoardLine = v); }
    private async void BtnSysIDVaha1_OnClick(object? sender, RoutedEventArgs e) { if (DataContext is CParametersLis p) await EditValueIntAsync("ID Váha 1", p.IDVaha1, v => p.IDVaha1 = v); }
    private async void BtnSysIDVaha2_OnClick(object? sender, RoutedEventArgs e) { if (DataContext is CParametersLis p) await EditValueIntAsync("ID Váha 2", p.IDVaha2, v => p.IDVaha2 = v); }
    private async void BtnSysIDVaha3_OnClick(object? sender, RoutedEventArgs e) { if (DataContext is CParametersLis p) await EditValueIntAsync("ID Váha 3", p.IDVaha3, v => p.IDVaha3 = v); }
    private async void BtnSysIDBox_OnClick(object? sender, RoutedEventArgs e) { if (DataContext is CParametersLis p) await EditValueIntAsync("ID Box", p.IDBox, v => p.IDBox = v); }
}