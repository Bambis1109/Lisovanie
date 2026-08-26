using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EposCmd.Net;
using Lisovanie.Models;
using Serilog;

namespace Lisovanie.ViewModels;

public partial class ParametersViewModel : ViewModelBase
{
    private const string DavkaCategory = CDavkaParametersIo.DavkaCategory;

    private readonly DeviceParameters _parameters;
    private readonly CDeviceScale? _device;
    private readonly CControlScales? _scales;
    private readonly bool _davkaOnly;

    /// <summary>Index dávkovača (1..3) v multi-mix režime; 0 = spoločný profil pre všetky váhy.</summary>
    private readonly int _doserIndex;

    [ObservableProperty] private ObservableCollection<CategoryViewModel> _categories = new();
    [ObservableProperty] private ParameterItemViewModel? _selectedParameter;
    [ObservableProperty] private bool _isBusy;

    /// <summary>Režim jednej váhy - parametre sa čítajú aj zapisujú do konkrétneho zariadenia.</summary>
    public ParametersViewModel(DeviceParameters parameters, CDeviceScale device, bool davkaOnly = false)
    {
        _parameters = parameters;
        _device = device;
        _davkaOnly = davkaOnly;
        BuildUI();
    }

    /// <summary>
    /// Režim všetkých váh - jedna spoločná sada parametrov dávky sa odosiela do všetkých aktívnych váh.
    /// </summary>
    public ParametersViewModel(DeviceParameters parameters, CControlScales scales)
    {
        _parameters = parameters;
        _scales = scales;
        _davkaOnly = true;
        BuildUI();
    }

    /// <summary>
    /// Režim jedného dávkovača (multi-mix) - profil patrí konkrétnej váhe a len do nej sa zapisuje.
    /// Uloženie ide stále cez recept, aby sa všetky tri profily zapísali naraz.
    /// </summary>
    public ParametersViewModel(DeviceParameters parameters, CControlScales scales, int doserIndex)
    {
        _parameters = parameters;
        _scales = scales;
        _doserIndex = doserIndex;
        _davkaOnly = true;
        BuildUI();
    }

    /// <summary>V režime všetkých váh niet jedného zdroja, z ktorého by sa dalo pri otvorení čítať.</summary>
    public bool IsScalesMode => _scales != null;

    /// <summary>Režim jedného dávkovača - zdroj aj cieľ je práve jedna váha.</summary>
    public bool IsDoserMode => _scales != null && _doserIndex > 0;

    /// <summary>
    /// Automaticky čítať zo zariadenia sa smie len tam, kde profil nepochádza z receptu.
    /// V režime dávkovača by to prepísalo hodnoty receptu hodnotami z váhy.
    /// </summary>
    public bool AutoLoadOnOpen => !IsScalesMode;

    public bool ShowLoadFromDevice => !IsScalesMode || IsDoserMode;

    public string SendButtonText => IsDoserMode
        ? $"Odošli do váhy {_doserIndex}"
        : IsScalesMode ? "Odošli do váh" : "Save to Device";

    /// <summary>Zápis profilu do receptu má zmysel len v režime všetkých váh.</summary>
    public bool ShowSaveToRecipe => IsScalesMode;

    public string SaveToRecipeButtonText => "Ulož do receptu";

    public string LoadFromDeviceButtonText => "Load from Device";

    public string LoadFileButtonText => IsScalesMode ? "Nahraj zo súboru" : "Load from File";

    public string SaveFileButtonText => IsScalesMode ? "Ulož do súboru" : "Save to File";

    private bool IsInScope(PropertyInfo prop)
    {
        var catAttr = prop.GetCustomAttribute<CategoryAttribute>();
        return catAttr != null && (catAttr.Category == DavkaCategory) == _davkaOnly;
    }

    /// <summary>Property, ktoré toto okno spravuje - určuje aj obsah súboru.</summary>
    private IEnumerable<PropertyInfo> InScopeProperties =>
        typeof(DeviceParameters)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(IsInScope);

    /// <summary>Váha, ktorej profil toto okno spravuje v režime dávkovača.</summary>
    private CDeviceScale? DoserDevice => IsDoserMode ? _scales!.GetScale(_doserIndex) : null;

    /// <summary>
    /// Zariadenia, do ktorých sa zapisuje. V režime všetkých váh sú to aktívne váhy,
    /// v režime dávkovača práve tá jedna, ktorej profil sa edituje.
    /// </summary>
    private List<CDeviceScale> TargetDevices
    {
        get
        {
            if (_device != null) return new List<CDeviceScale> { _device };

            if (IsDoserMode)
            {
                var doser = DoserDevice;
                return doser != null ? new List<CDeviceScale> { doser } : new List<CDeviceScale>();
            }

            return _scales?.ActiveScales.ToList() ?? new List<CDeviceScale>();
        }
    }

    private void BuildUI()
    {
        var props = _parameters.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var tempCategories = new Dictionary<string, CategoryViewModel>();

        foreach (var prop in props)
        {
            var catAttr = prop.GetCustomAttribute<CategoryAttribute>();
            var dispAttr = prop.GetCustomAttribute<DisplayNameAttribute>();
            var descAttr = prop.GetCustomAttribute<DescriptionAttribute>();

            if (catAttr == null || !IsInScope(prop)) continue;

            var categoryName = catAttr.Category;
            if (!tempCategories.TryGetValue(categoryName, out var categoryVm))
            {
                categoryVm = new CategoryViewModel(categoryName);
                tempCategories.Add(categoryName, categoryVm);
                Categories.Add(categoryVm);
            }

            categoryVm.Parameters.Add(new ParameterItemViewModel(
                _parameters, 
                prop, 
                dispAttr?.DisplayName ?? prop.Name, 
                descAttr?.Description ?? "", 
                categoryName));
        }
    }

    private (ushort index, byte subindex) GetIndices(ParameterItemViewModel vm)
    {
        // Extract index from "1. SYSTÉM (0x6000)"
        var catMatch = Regex.Match(vm.Category, @"\(0x([0-9A-Fa-f]{4})\)");
        ushort index = catMatch.Success ? Convert.ToUInt16(catMatch.Groups[1].Value, 16) : (ushort)0;

        // Extract subindex from "0x01: can_ID"
        var dispMatch = Regex.Match(vm.DisplayName, @"0x([0-9A-Fa-f]{2})");
        byte subindex = dispMatch.Success ? Convert.ToByte(dispMatch.Groups[1].Value, 16) : (byte)0;

        return (index, subindex);
    }

    [RelayCommand]
    private async Task LoadFromDevice()
    {
        if (IsBusy) return;

        // V režime dávkovača je zdrojom jeho vlastná váha, v režime všetkých váh prvá aktívna.
        var source = _device ?? DoserDevice ?? _scales?.ActiveScales.FirstOrDefault();
        if (source == null)
        {
            Log.Error("Nie je dostupná žiadna aktívna váha, z ktorej by sa dali načítať parametre.");
            return;
        }

        IsBusy = true;

        try
        {
            await Task.Run(() =>
            {
                foreach (var cat in Categories)
                {
                    foreach (var param in cat.Parameters)
                    {
                        var (index, subindex) = GetIndices(param);
                        if (index == 0) continue;

                        try
                        {
                            uint val = source.LowLayer.Can.GetRegister(index, subindex);
                            param.Value = (int)val; // setter zároveň označí hodnotu za známu
                        }
                        catch (Exception ex)
                        {
                            // Hodnota ostáva zástupná nula - nesmie sa zapísať späť do zariadenia.
                            param.IsValueKnown = false;
                            Log.Error($"Error loading parameter {param.DisplayName}: {ex.Message}");
                        }
                    }
                }
            });
            Log.Information($"Parametre úspešne načítané z váhy {source.Name}.");
        }
        catch (Exception ex)
        {
            Log.Error($"Chyba pri hromadnom načítaní parametrov: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task SaveToDevice()
    {
        if (IsBusy) return;

        var targets = TargetDevices;
        if (targets.Count == 0)
        {
            Log.Error("Parametre sa nemajú kam odoslať - žiadna aktívna váha.");
            return;
        }

        IsBusy = true;

        try
        {
            int errors = 0;

            // Parametre, ktorých čítanie pri otvorení okna zlyhalo, držia len zástupnú nulu.
            // Zapísať ich späť by znamenalo vynulovať register v zariadení (napr. kalibráciu).
            var neznameHodnoty = Categories
                .SelectMany(c => c.Parameters)
                .Where(p => !p.IsValueKnown)
                .ToList();

            if (neznameHodnoty.Count > 0)
            {
                Log.Warning(
                    $"Nezapisujem {neznameHodnoty.Count} parametrov s neznámou hodnotou (zlyhalo čítanie zo zariadenia): " +
                    string.Join(", ", neznameHodnoty.Select(p => p.DisplayName)));
            }

            await Task.Run(() =>
            {
                foreach (var device in targets)
                {
                    foreach (var cat in Categories)
                    {
                        foreach (var param in cat.Parameters)
                        {
                            if (!param.IsValueKnown) continue;

                            var (index, subindex) = GetIndices(param);
                            if (index == 0) continue;

                            try
                            {
                                device.LowLayer.Can.SetRegister(index, subindex, param.RegisterValue);
                            }
                            catch (Exception ex)
                            {
                                errors++;
                                Log.Error($"Error saving parameter {param.DisplayName} -> {device.Name}: {ex.Message}");
                            }
                        }
                    }
                }
            });

            if (errors > 0)
                Log.Error($"Odoslanie parametrov skončilo s chybami ({errors} hodnôt).");
            else if (neznameHodnoty.Count > 0)
                Log.Warning(
                    $"Parametre odoslané do váh [{string.Join(",", targets.Select(d => d.NodeId))}], " +
                    $"{neznameHodnoty.Count} preskočených. Otvorte okno znova alebo hodnoty zadajte ručne.");
            else
                Log.Information($"Parametre úspešne odoslané do váh [{string.Join(",", targets.Select(d => d.NodeId))}].");
        }
        catch (Exception ex)
        {
            Log.Error($"Chyba pri hromadnom ukladaní parametrov: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Zapíše profil dávky do receptu (sekcia Vaha). Init ho odtiaľ vždy pošle do váh.
    /// </summary>
    [RelayCommand]
    private void SaveToRecipe()
    {
        if (IsBusy) return;

        if (_scales == null)
        {
            Log.Error("Profil dávky sa dá uložiť do receptu len v režime všetkých váh.");
            return;
        }

        if (_scales.SaveDavkaParametersToRecipe())
        {
            Log.Information("Profil dávky uložený do receptu.");
        }
    }

    [RelayCommand]
    private async Task LoadFromFile(Window window)
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            var topLevel = TopLevel.GetTopLevel(window);
            if (topLevel == null) return;

            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Načítať parametre zo súboru",
                AllowMultiple = false,
                SuggestedStartLocation = await GetDefaultFolderAsync(topLevel),
                FileTypeFilter = new[] { new FilePickerFileType("JSON File") { Patterns = new[] { "*.json" } } }
            });

            if (files.Count > 0)
            {
                CDavkaParametersIo.Load(files[0].Path.LocalPath, _parameters, InScopeProperties);

                foreach (var cat in Categories)
                {
                    foreach (var param in cat.Parameters)
                    {
                        param.Refresh();
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error($"Chyba pri načítaní zo súboru: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task SaveToFile(Window window)
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            var topLevel = TopLevel.GetTopLevel(window);
            if (topLevel == null) return;

            var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Uložiť parametre do súboru",
                SuggestedFileName = IsScalesMode
                    ? IsDoserMode
                        ? $"Davka_{Program.MainProgram?.RecipeManager.ActiveRecipeName}_D{_doserIndex}.json"
                        : $"Davka_{Program.MainProgram?.RecipeManager.ActiveRecipeName}.json"
                    : $"ScaleNode{_device!.NodeId}{(_davkaOnly ? "_Davka" : "")}.json",
                SuggestedStartLocation = await GetDefaultFolderAsync(topLevel),
                DefaultExtension = "json",
                FileTypeChoices = new[] { new FilePickerFileType("JSON File") { Patterns = new[] { "*.json" } } }
            });

            if (file != null)
            {
                CDavkaParametersIo.Save(file.Path.LocalPath, _parameters, InScopeProperties);
            }
        }
        catch (Exception ex)
        {
            Log.Error($"Chyba pri ukladaní do súboru: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// V režime všetkých váh otvára dialóg rovno v priečinku Parameters.
    /// V režime jednej váhy ponechá výber na operačnom systéme (pôvodné správanie).
    /// </summary>
    private async Task<IStorageFolder?> GetDefaultFolderAsync(TopLevel topLevel)
    {
        if (!IsScalesMode) return null;

        try
        {
            var directory = CRecipeManager.ParametersDir;
            Directory.CreateDirectory(directory);
            return await topLevel.StorageProvider.TryGetFolderFromPathAsync(directory);
        }
        catch (Exception ex)
        {
            Log.Debug($"Predvolený priečinok pre dialóg sa nepodarilo určiť: {ex.Message}");
            return null;
        }
    }
}
