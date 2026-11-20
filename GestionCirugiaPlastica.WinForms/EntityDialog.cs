using System.Drawing;

namespace GestionCirugiaPlastica.WinForms;

internal sealed record ChoiceItem(int Id, string Label)
{
    public override string ToString() => Label;
}

internal sealed class EntityDialog : Form
{
    private readonly FlowLayoutPanel _fields;
    public EntityDialog(string title)
    {
        Text = title; Width = 560; Height = 690; MinimumSize = new Size(520, 480); StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.Sizable; MaximizeBox = false; MinimizeBox = false; Font = new Font("Segoe UI", 10); BackColor = Color.White;
        _fields = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true, Padding = new Padding(24), BackColor = Color.White };
        var buttons = new Panel { Dock = DockStyle.Bottom, Height = 64, Padding = new Padding(20, 10, 20, 10), BackColor = Color.FromArgb(246, 244, 243) };
        var save = new Button { Text = "Guardar", DialogResult = DialogResult.OK, Dock = DockStyle.Right, Width = 120, BackColor = ColorTranslator.FromHtml("#9E6E79"), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
        var cancel = new Button { Text = "Cancelar", DialogResult = DialogResult.Cancel, Dock = DockStyle.Right, Width = 120, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 10) };
        buttons.Controls.Add(save); buttons.Controls.Add(cancel); Controls.Add(_fields); Controls.Add(buttons); AcceptButton = save; CancelButton = cancel;
    }

    public TextBox AddText(string label, string? value = null, bool multiline = false)
    {
        var group = Group(label, multiline ? 110 : 74); var control = new TextBox { Text = value ?? "", Location = new Point(0, 30), Width = 470, Font = new Font("Segoe UI", 10), Multiline = multiline, Height = multiline ? 72 : 34, ForeColor = ColorTranslator.FromHtml("#36383A"), BackColor = Color.White };
        group.Controls.Add(control); _fields.Controls.Add(group); return control;
    }
    public DateTimePicker AddDate(string label, DateTime? value = null)
    {
        var group = Group(label); var control = new DateTimePicker { Location = new Point(0, 32), Width = 470, Format = DateTimePickerFormat.Short, ShowCheckBox = true, Checked = value.HasValue, Value = value ?? DateTime.Today, ForeColor = ColorTranslator.FromHtml("#36383A"), BackColor = Color.White };
        group.Controls.Add(control); _fields.Controls.Add(group); return control;
    }
    public DateTimePicker AddDateTime(string label, DateTime value)
    {
        var group = Group(label); var control = new DateTimePicker { Location = new Point(0, 32), Width = 470, Format = DateTimePickerFormat.Custom, CustomFormat = "dd/MM/yyyy  hh:mm tt", ShowUpDown = true, Value = value, ForeColor = ColorTranslator.FromHtml("#36383A"), BackColor = Color.White };
        group.Controls.Add(control); _fields.Controls.Add(group); return control;
    }
    public CheckBox AddCheck(string label, bool value)
    {
        var group = Group(label); var control = new CheckBox { Text = "Sí", Checked = value, AutoSize = true, Location = new Point(0, 32), Font = new Font("Segoe UI", 10), ForeColor = ColorTranslator.FromHtml("#36383A") };
        group.Controls.Add(control); _fields.Controls.Add(group); return control;
    }
    public ComboBox AddChoice(string label, List<ChoiceItem> choices, int? selectedId)
    {
        var group = Group(label); var control = new ComboBox { Location = new Point(0, 32), Width = 470, DropDownStyle = ComboBoxStyle.DropDownList, DataSource = choices, DisplayMember = nameof(ChoiceItem.Label), ValueMember = nameof(ChoiceItem.Id), ForeColor = ColorTranslator.FromHtml("#36383A"), BackColor = Color.White };
        if (selectedId.HasValue) control.SelectedValue = selectedId.Value;
        group.Controls.Add(control); _fields.Controls.Add(group); return control;
    }
    public ComboBox AddChoiceText(string label, string[] choices, string selected)
    {
        var group = Group(label); var control = new ComboBox { Location = new Point(0, 32), Width = 470, DropDownStyle = ComboBoxStyle.DropDownList, DataSource = choices, SelectedItem = selected, ForeColor = ColorTranslator.FromHtml("#36383A"), BackColor = Color.White };
        group.Controls.Add(control); _fields.Controls.Add(group); return control;
    }
    public Button AddAction(string text, Action action)
    {
        var group = new Panel { Width = 490, Height = 48, Margin = new Padding(0, 0, 0, 5) };
        var button = new Button { Text = text, Width = 180, Height = 36, Location = Point.Empty, BackColor = Color.White, ForeColor = ColorTranslator.FromHtml("#9E6E79"), FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9, FontStyle.Bold) };
        button.Click += (_, _) => action(); group.Controls.Add(button); _fields.Controls.Add(group); return button;
    }
    private static Panel Group(string label, int height = 74)
    {
        var group = new Panel { Width = 490, Height = height, Margin = new Padding(0, 0, 0, 5) };
        group.Controls.Add(new Label { Text = label, Location = Point.Empty, Width = 480, Height = 24, ForeColor = ColorTranslator.FromHtml("#36383A"), Font = new Font("Segoe UI", 9, FontStyle.Bold) }); return group;
    }
}
