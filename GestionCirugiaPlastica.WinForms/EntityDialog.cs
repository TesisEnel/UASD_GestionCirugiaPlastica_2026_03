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
        var group = Group(label); var control = new TextBox { Text = value ?? "", Width = 470, Font = new Font("Segoe UI", 10), Multiline = multiline, Height = multiline ? 72 : 34 };
        group.Controls.Add(control); _fields.Controls.Add(group); return control;
    }
    public DateTimePicker AddDate(string label, DateTime? value = null)
    {
        var group = Group(label); var control = new DateTimePicker { Width = 470, Format = DateTimePickerFormat.Short, ShowCheckBox = true, Checked = value.HasValue, Value = value ?? DateTime.Today };
        group.Controls.Add(control); _fields.Controls.Add(group); return control;
    }
    public DateTimePicker AddDateTime(string label, DateTime value)
    {
        var group = Group(label); var control = new DateTimePicker { Width = 470, Format = DateTimePickerFormat.Custom, CustomFormat = "dd/MM/yyyy  HH:mm", ShowUpDown = true, Value = value };
        group.Controls.Add(control); _fields.Controls.Add(group); return control;
    }
    public CheckBox AddCheck(string label, bool value)
    {
        var group = Group(label); var control = new CheckBox { Text = "Sí", Checked = value, AutoSize = true, Font = new Font("Segoe UI", 10) };
        group.Controls.Add(control); _fields.Controls.Add(group); return control;
    }
    public ComboBox AddChoice(string label, List<ChoiceItem> choices, int? selectedId)
    {
        var group = Group(label); var control = new ComboBox { Width = 470, DropDownStyle = ComboBoxStyle.DropDownList, DataSource = choices, DisplayMember = nameof(ChoiceItem.Label), ValueMember = nameof(ChoiceItem.Id) };
        if (selectedId.HasValue) control.SelectedValue = selectedId.Value;
        group.Controls.Add(control); _fields.Controls.Add(group); return control;
    }
    public ComboBox AddChoiceText(string label, string[] choices, string selected)
    {
        var group = Group(label); var control = new ComboBox { Width = 470, DropDownStyle = ComboBoxStyle.DropDownList, DataSource = choices, SelectedItem = selected };
        group.Controls.Add(control); _fields.Controls.Add(group); return control;
    }
    private static Panel Group(string label)
    {
        var group = new Panel { Width = 490, Height = 74, Margin = new Padding(0, 0, 0, 5) };
        group.Controls.Add(new Label { Text = label, Dock = DockStyle.Top, Height = 26, ForeColor = ColorTranslator.FromHtml("#36383A"), Font = new Font("Segoe UI", 9, FontStyle.Bold) }); return group;
    }
}
