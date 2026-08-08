using System;
using System.Runtime.InteropServices;
using System.Threading;
using Avalonia.Controls;
using Avalonia.Threading;

namespace Lisovanie;

/// <summary>
/// Zabezpečuje beh iba jednej inštancie aplikácie.
/// Detekcia cez pomenovaný <see cref="Mutex"/>, signalizácia (zobraz/maximalizuj
/// existujúce okno) cez pomenovaný <see cref="EventWaitHandle"/>.
/// </summary>
public static class SingleInstance
{
    private const string MutexName = "Lisovanie_SingleInstance_Mutex";
    private const string EventName = "Lisovanie_SingleInstance_Activate";

    private static Mutex? _mutex;
    private static EventWaitHandle? _event;
    private static RegisteredWaitHandle? _registeredWait;

    /// <summary>
    /// Pokúsi sa získať vlastníctvo inštancie. Vráti <c>true</c>, ak sme prvá
    /// (jediná) inštancia; <c>false</c>, ak už beží iná.
    /// </summary>
    public static bool TryAcquire()
    {
        // Pomenovaný Mutex funguje aj na Unixe; pomenovaný EventWaitHandle nie
        // (na Linuxe hádže PlatformNotSupportedException), preto ho vytvárame len na Windows.
        _mutex = new Mutex(true, MutexName, out bool createdNew);

        if (createdNew && OperatingSystem.IsWindows())
        {
            // Sme prvá inštancia – pripravíme signál pre prípadné ďalšie spustenia.
            _event = new EventWaitHandle(false, EventResetMode.AutoReset, EventName);
        }

        return createdNew;
    }

    /// <summary>
    /// Zavolá druhá inštancia: zobudí prvú (existujúcu) inštanciu, aby sa
    /// maximalizovala a dostala do popredia.
    /// </summary>
    public static void SignalExistingInstance()
    {
        // Pomenovaný event existuje len na Windows.
        if (!OperatingSystem.IsWindows()) return;

        try
        {
            if (EventWaitHandle.TryOpenExisting(EventName, out var handle))
            {
                handle.Set();
                handle.Dispose();
            }
        }
        catch
        {
            // Ak sa nepodarí, druhá inštancia aj tak skončí – nič kritické.
        }
    }

    /// <summary>
    /// Zaregistruje v prvej inštancii reakciu na signál z ďalšej inštancie:
    /// dané okno sa maximalizuje a aktivuje.
    /// </summary>
    public static void RegisterActivator(Window window)
    {
        if (_event == null) return;

        _registeredWait = ThreadPool.RegisterWaitForSingleObject(
            _event,
            (_, _) => Dispatcher.UIThread.Post(() => BringToFront(window)),
            null,
            Timeout.Infinite,
            executeOnlyOnce: false);
    }

    private static void BringToFront(Window window)
    {
        if (window.WindowState == WindowState.Minimized)
            window.WindowState = WindowState.Normal;

        window.WindowState = WindowState.Maximized;
        window.Show();
        window.Activate();

        // Krátky topmost prepín – spoľahlivejšie vytiahne okno do popredia.
        window.Topmost = true;
        window.Topmost = false;
    }

    /// <summary>Zobrazí natívne hlásenie, že aplikácia už beží.</summary>
    public static void ShowAlreadyRunningMessage()
    {
        // MessageBoxW je Windows-only (user32.dll); na iných platformách nič nerobíme.
        if (!OperatingSystem.IsWindows()) return;

        const uint MB_OK = 0x0;
        const uint MB_ICONINFORMATION = 0x40;
        const uint MB_SETFOREGROUND = 0x10000;

        MessageBoxW(
            IntPtr.Zero,
            "Aplikácia Lisovanie už beží.\n\nPo potvrdení sa zobrazí už spustená inštancia.",
            "Lisovanie",
            MB_OK | MB_ICONINFORMATION | MB_SETFOREGROUND);
    }

    public static void Release()
    {
        _registeredWait?.Unregister(null);
        try { _mutex?.ReleaseMutex(); } catch { /* nebola vlastnená */ }
        _mutex?.Dispose();
        _event?.Dispose();
    }

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int MessageBoxW(IntPtr hWnd, string text, string caption, uint type);
}
