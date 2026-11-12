using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using GorevYolluklari.Core.Records;

namespace GorevYolluklari.App;

public partial class MainWindow
{
    private void DateField(Panel parent, string label, DutyRecord duty, string property, bool withTime = true, Action? changed = null)
    {
        var member = typeof(DutyRecord).GetProperty(property)!;
        var stored = member.GetValue(duty) as DateTime?;
        var initial = stored is { } date && date != default ? date : (DateTime?)null;
        var picker = new DatePicker
        {
            SelectedDate = initial?.Date,
            DisplayDate = initial?.Date ?? (DateTime.Today.Year == 2026 ? DateTime.Today : new DateTime(2026, 1, 1)),
            SelectedDateFormat = DatePickerFormat.Short,
            Language = System.Windows.Markup.XmlLanguage.GetLanguage("tr-TR"),
            Margin = new(0, 0, 8, 5),
            MinHeight = 37,
            Style = (Style)FindResource("HakedisDatePicker")
        };
        AutomationProperties.SetName(picker, label);
        picker.Loaded += (_, _) =>
        {
            if (picker.Template.FindName("PART_TextBox", picker) is DatePickerTextBox box) box.IsReadOnly = true;
        };
        parent.Children.Add(new Label { Content = Heading(label), Target = picker,
            Style = (Style)FindResource("HakedisFieldLabel") });
        parent.Children.Add(picker);
        RegisterIssue(property, picker, parent);

        ComboBox? hour = null;
        ComboBox? minute = null;
        if (withTime)
        {
            var time = new UniformGrid { Columns = 2 };
            hour = TimePart(time, label + " saati", "Saat", 24, initial?.Hour);
            minute = TimePart(time, label + " dakikası", "Dakika", 60, initial?.Minute);
            parent.Children.Add(time);
        }

        void Store()
        {
            DateTime? value = picker.SelectedDate?.Date;
            if (withTime)
            {
                if (hour!.SelectedIndex < 0 || minute!.SelectedIndex < 0) value = null;
                else value = value?.AddHours(hour.SelectedIndex).AddMinutes(minute.SelectedIndex);
            }
            object? next = member.PropertyType == typeof(DateTime?) ? value : value ?? default(DateTime);
            if (Equals(member.GetValue(duty), next)) return;
            member.SetValue(duty, next);
            Invalidate(duty, property);
            changed?.Invoke();
        }

        picker.SelectedDateChanged += (_, _) => Store();
        if (withTime)
        {
            hour!.SelectionChanged += (_, _) => Store();
            minute!.SelectionChanged += (_, _) => Store();
        }
        if (member.PropertyType == typeof(DateTime?))
        {
            var clear = new Button { Content = "Tarihi temizle", HorizontalAlignment = HorizontalAlignment.Left };
            AutomationProperties.SetName(clear, label + " tarihini temizle");
            clear.Click += (_, _) =>
            {
                picker.SelectedDate = null;
                if (hour is not null) hour.SelectedIndex = -1;
                if (minute is not null) minute.SelectedIndex = -1;
            };
            parent.Children.Add(clear);
        }
    }

    private static ComboBox TimePart(Panel panel, string automationName, string caption, int count, int? selected)
    {
        var field = new StackPanel();
        var combo = new ComboBox
        {
            ItemsSource = Enumerable.Range(0, count).Select(value => value.ToString("00", Tr)).ToArray(),
            SelectedIndex = selected ?? -1
        };
        AutomationProperties.SetName(combo, automationName);
        field.Children.Add(new Label { Content = caption, Target = combo });
        field.Children.Add(combo);
        panel.Children.Add(field);
        return combo;
    }

    private static void AddCalendarColumn(DataGrid grid)
    {
        var display = new FrameworkElementFactory(typeof(TextBlock));
        display.SetBinding(TextBlock.TextProperty, new Binding(nameof(ReviewedDayEntry.Date)) { StringFormat = "dd.MM.yyyy" });
        var editor = new FrameworkElementFactory(typeof(DatePicker));
        editor.SetValue(DatePicker.SelectedDateFormatProperty, DatePickerFormat.Short);
        editor.SetBinding(DatePicker.SelectedDateProperty, new Binding(nameof(ReviewedDayEntry.Date))
        {
            Mode = BindingMode.TwoWay,
            UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
            ValidatesOnExceptions = true
        });
        grid.Columns.Add(new DataGridTemplateColumn
        {
            Header = "Tarih", Width = 135,
            CellTemplate = new DataTemplate { VisualTree = display },
            CellEditingTemplate = new DataTemplate { VisualTree = editor }
        });
    }
}
