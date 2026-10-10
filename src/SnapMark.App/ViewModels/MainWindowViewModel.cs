using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SnapMark.App.ViewModels;
public sealed class MainWindowViewModel : INotifyPropertyChanged
{
    private string status = "Ready · Closing this window keeps SnapMark in the system tray.";
    public string Status
    {
        get => status;
        set { if (status == value) return; status = value; OnPropertyChanged(); }
    }
    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new(name));
}
