using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Lisovanie.Models;

public partial class Matrix : ObservableObject
{
    [ObservableProperty] private int _xfirst;
    [ObservableProperty] private int _yfirst;
    [ObservableProperty] private int _xdelta;
    [ObservableProperty] private int _ydelta;
    [ObservableProperty] private int _xnum;
    [ObservableProperty] private int _ynum;
    [ObservableProperty] private int _actualItem;
    [ObservableProperty] private int _countItem;
    [ObservableProperty] private bool _lastItem;

    public int Xactual
    {
        get => Items[ActualItem].X;
    }

    public int Yactual
    {
        get => Items[ActualItem].Y;
    }

    [ObservableProperty] private string _lastToggledItem = "-";

    public ObservableCollection<Item> Items { get; } = new();

    public Matrix()
    {
        Xfirst = 10;
        Yfirst = 10;
        Xdelta = 30;
        Ydelta = 30;
        Xnum = 5;
        Ynum = 5;
    }

    public Matrix(int xfirst, int yfirst, int xdelta, int ydelta, int xnum, int ynum)
    {
        Xfirst = xfirst;
        Yfirst = yfirst;
        Xdelta = xdelta;
        Ydelta = ydelta;
        Xnum = xnum;
        Ynum = ynum;
        ActualItem = 0;
        _lastItem = false;
    }

    public bool SetNextItem()
    {
        if (ActualItem >= Items.Count - 2)
        {
            ActualItem += 1;
           LastItem = true;
            return true;
        }
        ActualItem += 1;
        LastItem = false;
        return false;
    }

    private void OnItemToggled(Item item)
    {
        LastToggledItem = $"#{item.Id} [X:{item.X}, Y:{item.Y}]";
    }


    [RelayCommand]
    public void RecalculDIA()
    {
        Items.Clear();
        LastToggledItem = "-";
        int count = 0;

        for (int riadok = 0; riadok < Ynum; riadok++)
        {
            int rowOffset = (riadok % 2 == 1) ? Xdelta / 2 : 0;

            for (int stlpec = 0; stlpec < Xnum; stlpec++)
            {
                if (count >= 100) return;

                int posX = Xfirst + (stlpec * Xdelta) + rowOffset;
                int posY = Yfirst + (riadok * Ydelta);

                // Id prvku bude od 1 vyššie
                Items.Add(new Item(count + 1, posX, posY, OnItemToggled));
                count++;
            }
        }

        CountItem = Items.Count;
    }


    [RelayCommand]
    public void RecalculSQR()
    {
        Items.Clear();
        LastToggledItem = "-";
        int count = 0;

        for (int riadok = 0; riadok < Ynum; riadok++)
        {
            for (int stlpec = 0; stlpec < Xnum; stlpec++)
            {
                if (count >= 100) return;

                int posX = Xfirst + (stlpec * Xdelta);
                int posY = Yfirst + (riadok * Ydelta);

                // Id prvku bude od 1 vyššie
                Items.Add(new Item(count + 1, posX, posY, OnItemToggled));
                count++;
            }
        }

        ;
    }

    [RelayCommand]
    private void Start()
    {
        RecalculSQR();
    }
}

public partial class Item : ObservableObject
{
    public int Id { get; set; }
    public int X { get; set; }
    public int Y { get; set; }

    private readonly Action<Item> _onToggled;

    [ObservableProperty] private bool _aktiv;

    public Item(int id, int x, int y, Action<Item> onToggled, bool aktiv = false)
    {
        Id = id;
        X = x;
        Y = y;
        _onToggled = onToggled;
        Aktiv = aktiv;
    }

    [RelayCommand]
    private void Toggle()
    {
        Aktiv = !Aktiv;
        _onToggled?.Invoke(this);
    }
}