using System.Collections.Generic;
using Avalonia;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Lisovanie.Models;

public partial class CDeviceEpos4Data : ObservableObject
{
    [ObservableProperty]
    private string _motorName = string.Empty;

    [ObservableProperty]
    private int _nodeId;

    [ObservableProperty]
    private double _actualGearPosition;

    [ObservableProperty]
    private double _actualPositionSensor2Float;

    [ObservableProperty]
    private int _actualVelocity;

    [ObservableProperty]
    private int _actualAnalog1;

    [ObservableProperty]
    private double _actualCurrent;

    partial void OnActualCurrentChanged(double value)
    {
        _currentHistory.Enqueue(value);
        if (_currentHistory.Count > MaxHistory)
        {
            _currentHistory.Dequeue();
        }

        UpdateGraphPoints();
    }

    private readonly Queue<double> _currentHistory = new Queue<double>();
    private const int MaxHistory = 30;

    [ObservableProperty]
    private IList<Point> _currentGraphPoints = new List<Point> { new Point(0, 50) };

    private void UpdateGraphPoints()
    {
        if (_currentHistory.Count == 0) return;

        double fixedMin = -200.0;
        double fixedMax = 200.0;
        double range = fixedMax - fixedMin;

        double width = 180.0;
        double height = 50.0;
        
        var newPoints = new List<Point>();
        int i = 0;
        int count = _currentHistory.Count;
        
        foreach (var val in _currentHistory)
        {
            // Clamp value to -200 .. 200
            double clampedVal = val;
            if (clampedVal > fixedMax) clampedVal = fixedMax;
            if (clampedVal < fixedMin) clampedVal = fixedMin;

            // Align points to the right: start X based on how many points we have relative to MaxHistory
            double xOffset = (MaxHistory - count) * (width / (MaxHistory - 1));
            double x = xOffset + (i / (double)(MaxHistory - 1)) * width;
            
            // Calculate Y coordinate (0 is top, 50 is bottom)
            double y = height - ((clampedVal - fixedMin) / range * height);
            
            newPoints.Add(new Point(x, y));
            i++;
        }
        
        CurrentGraphPoints = newPoints;
    }
}
