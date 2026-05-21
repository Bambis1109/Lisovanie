using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EposCmd.Net;
using MojaPrvaAvalonia.Models;
using Serilog;

namespace MojaPrvaAvalonia.ViewModels;

public partial class ParametersViewModel : ViewModelBase
{
    private readonly DeviceParameters _parameters;
    private readonly CDeviceScale _device;
    
    [ObservableProperty] private ObservableCollection<CategoryViewModel> _categories = new();
    [ObservableProperty] private ParameterItemViewModel? _selectedParameter;
    [ObservableProperty] private bool _isBusy;

    public ParametersViewModel(DeviceParameters parameters, CDeviceScale device)
    {
        _parameters = parameters;
        _device = device;
        BuildUI();
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

            if (catAttr == null) continue;

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
                            uint val = _device.LowLayer.Can.GetRegister(index, subindex);
                            param.Value = (int)val;
                        }
                        catch (Exception ex)
                        {
                            Log.Error($"Error loading parameter {param.DisplayName}: {ex.Message}");
                        }
                    }
                }
            });
            Log.Information("Parametre úspešne načítané.");
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
                            _device.LowLayer.Can.SetRegister(index, subindex, (uint)param.Value);
                        }
                        catch (Exception ex)
                        {
                            Log.Error($"Error saving parameter {param.DisplayName}: {ex.Message}");
                        }
                    }
                }
            });
            Log.Information("Parametre úspešne uložené.");
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
}
