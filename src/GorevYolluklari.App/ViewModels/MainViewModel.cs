using System.Collections.ObjectModel;
using System.ComponentModel;
using GorevYolluklari.Core.Records;
namespace GorevYolluklari.App.ViewModels;
public sealed class MainViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    public void Refresh(string? name = null) => PropertyChanged?.Invoke(this, new(name));
    public WorkspaceRecords Workspace { get; private set; } = new();
    public ObservableCollection<PersonnelRecord> Personnel { get; } = [];
    public ObservableCollection<DutyRecord> Duties { get; } = [];
    public PersonnelRecord? SelectedPerson { get; set; }
    public DutyRecord? SelectedDuty { get; set; }
    public void Load(WorkspaceRecords workspace)
    {
        Workspace = workspace; Personnel.Clear(); Duties.Clear();
        foreach (var p in workspace.Personnel) Personnel.Add(p);
        foreach (var d in workspace.Duties) Duties.Add(d);
        SelectedDuty = Duties.FirstOrDefault(); SelectedPerson = Personnel.FirstOrDefault(); Refresh();
    }
    public void Synchronize() { Workspace.Personnel = Personnel.ToList(); Workspace.Duties = Duties.ToList(); }
}
