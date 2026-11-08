using Microsoft.Maui.Controls;

namespace VectronTablet.Pages;

public partial class SettingsPage : ContentPage
{
    private string currentTheme;
    private string currentMotion;

    public SettingsPage()
    {
        InitializeComponent();
        currentTheme = Preferences.Default.Get("appearance.theme", "dark");
        currentMotion = Preferences.Default.Get("appearance.motion", "full");

        Theme.PressBind(backBtn, async () => await Navigation.PopAsync());

        Theme.PressBind(darkChip, () => SelectTheme("dark"));
        Theme.PressBind(lightChip, () => SelectTheme("light"));

        Theme.PressBind(motionOffChip, () => SelectMotion("off"));
        Theme.PressBind(motionSubtleChip, () => SelectMotion("subtle"));
        Theme.PressBind(motionFullChip, () => SelectMotion("full"));

        UpdateChips();
    }

    private void SelectTheme(string theme)
    {
        currentTheme = theme;
        Theme.ApplyTheme(currentTheme, currentMotion);
        UpdateChips();
    }

    private void SelectMotion(string motion)
    {
        currentMotion = motion;
        Theme.ApplyTheme(currentTheme, currentMotion);
        UpdateChips();
    }

    private void UpdateChips()
    {
        var mint = Theme.C("Mint");
        var panel2 = Theme.C("Panel2");
        var txtDim = Theme.C("TxtDim");

        bool isDark = currentTheme == "dark";
        darkChip.BackgroundColor = isDark ? mint : panel2;
        darkChip.TextColor = isDark ? (currentTheme == "light" ? Colors.White : Color.FromArgb("#0F141A")) : txtDim;

        bool isLight = currentTheme == "light";
        lightChip.BackgroundColor = isLight ? mint : panel2;
        lightChip.TextColor = isLight ? (currentTheme == "light" ? Colors.White : Color.FromArgb("#0F141A")) : txtDim;

        motionOffChip.BackgroundColor = currentMotion == "off" ? mint : panel2;
        motionOffChip.TextColor = currentMotion == "off" ? (currentTheme == "light" ? Colors.White : Color.FromArgb("#0F141A")) : txtDim;

        motionSubtleChip.BackgroundColor = currentMotion == "subtle" ? mint : panel2;
        motionSubtleChip.TextColor = currentMotion == "subtle" ? (currentTheme == "light" ? Colors.White : Color.FromArgb("#0F141A")) : txtDim;

        motionFullChip.BackgroundColor = currentMotion == "full" ? mint : panel2;
        motionFullChip.TextColor = currentMotion == "full" ? (currentTheme == "light" ? Colors.White : Color.FromArgb("#0F141A")) : txtDim;
    }
}
