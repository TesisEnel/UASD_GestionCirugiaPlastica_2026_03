using System.Drawing;
using System.Globalization;
using System.Net.Http;
using System.Text.Json;
using GestionCirugiaPlastica.WinForms;

namespace GestionCirugiaPlastica.WinForms.Forms;

internal sealed class PublicAppointmentForm : Form
{
    private static readonly Color Charcoal = ClinicTheme.Charcoal;
    private static readonly Color Taupe = ClinicTheme.Taupe;
    private static readonly Color Mauve = ClinicTheme.Mauve;
    private readonly ClinicApiClient _api;
    private readonly TextBox _name = Field("Nombre *");
    private readonly TextBox _surname = Field("Apellido *");
    private readonly TextBox _id = Field("Cédula (opcional)");
    private readonly TextBox _phone = Field("Teléfono (opcional)");
    private readonly TextBox _email = Field("Correo electrónico (opcional)");
    private readonly DateTimePicker _date = new() { Format = DateTimePickerFormat.Custom, CustomFormat = "dd/MM/yyyy hh:mm tt", Value = DateTime.Today.AddDays(1).AddHours(9), MinDate = DateTime.Now.AddMinutes(1), Width = 230 };
    private readonly ComboBox _reason = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 230, DataSource = new[] { "Consulta", "Evaluación", "Seguimiento", "Postoperatorio" } };
    private readonly TextBox _notes = Field("Información adicional");
    private readonly TextBox _code = Field("Código de gestión (preferible)");
    private readonly TextBox _cancelId = Field("Cédula");
    private readonly TextBox _cancelPhone = Field("Teléfono");
    private readonly Label _bookingMessage = MessageLabel();
    private readonly Label _cancelMessage = MessageLabel();
    private readonly Button _bookButton;
    private readonly Button _cancelButton;

    public PublicAppointmentForm(ClinicApiClient api)
    {
        _api = api;
        Text = "Agendar una cita | Tanger Clinic";
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(720, 680);
        Size = new Size(820, 760);
        Font = new Font("Segoe UI", 10);
        BackColor = Color.FromArgb(246, 244, 243);

        var header = new Panel { Dock = DockStyle.Top, Height = 100, BackColor = Color.White, Padding = new Padding(26, 16, 26, 10) };
        header.Controls.Add(new Label { Text = "Solicita o cancela una cita", Dock = DockStyle.Top, Height = 39, Font = new Font("Segoe UI", 20, FontStyle.Bold), ForeColor = Charcoal });
        header.Controls.Add(new Label { Text = "Tanger Clinic · Plastic Surgery", Dock = DockStyle.Top, Height = 27, ForeColor = Taupe });
        var tabs = new TabControl { Dock = DockStyle.Fill, Padding = new Point(16, 8) };
        var bookingTab = new TabPage("Solicitar cita") { BackColor = Color.White, Padding = new Padding(18) };
        var cancelTab = new TabPage("Cancelar cita") { BackColor = Color.White, Padding = new Padding(18) };
        var bookingContent = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = Color.White };
        var bookingFields = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 2, RowCount = 0, BackColor = Color.White };
        bookingFields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50)); bookingFields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        AddField(bookingFields, "Nombre", _name); AddField(bookingFields, "Apellido", _surname);
        AddField(bookingFields, "Cédula (opcional)", _id); AddField(bookingFields, "Teléfono (opcional)", _phone);
        AddField(bookingFields, "Correo electrónico (opcional)", _email); AddField(bookingFields, "Fecha y hora", _date);
        AddField(bookingFields, "Motivo", _reason); AddField(bookingFields, "Información adicional", _notes, true);
        _notes.Multiline = true; _notes.Height = 75;
        _bookButton = ActionButton("Solicitar cita", primary: true);
        _bookButton.Click += async (_, _) => await BookAsync();
        var bookActions = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 50, Padding = new Padding(0, 8, 0, 0) };
        bookActions.Controls.Add(_bookButton);
        bookingContent.Controls.Add(_bookingMessage); bookingContent.Controls.Add(bookActions); bookingContent.Controls.Add(bookingFields);
        bookingTab.Controls.Add(bookingContent);

        var cancelContent = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = Color.White };
        var cancelFields = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 1, RowCount = 0, BackColor = Color.White };
        cancelFields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        AddField(cancelFields, "Código de gestión (preferible)", _code);
        cancelFields.Controls.Add(new Label { Text = "o bien, verifica tu identidad con ambos datos:", AutoSize = true, ForeColor = Taupe, Margin = new Padding(3, 10, 3, 14) });
        AddField(cancelFields, "Cédula", _cancelId); AddField(cancelFields, "Teléfono", _cancelPhone);
        _cancelButton = ActionButton("Cancelar cita", primary: false);
        _cancelButton.Click += async (_, _) => await CancelAsync();
        var cancelActions = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 50, Padding = new Padding(0, 8, 0, 0) };
        cancelActions.Controls.Add(_cancelButton);
        cancelContent.Controls.Add(_cancelMessage); cancelContent.Controls.Add(cancelActions); cancelContent.Controls.Add(cancelFields);
        cancelTab.Controls.Add(cancelContent);
        tabs.TabPages.Add(bookingTab); tabs.TabPages.Add(cancelTab);
        Controls.Add(tabs); Controls.Add(header);
    }

    private async Task BookAsync()
    {
        if (string.IsNullOrWhiteSpace(_name.Text) || string.IsNullOrWhiteSpace(_surname.Text)) { ShowMessage(_bookingMessage, "Indica tu nombre y apellido.", false); return; }
        if (_date.Value <= DateTime.Now) { ShowMessage(_bookingMessage, "Selecciona una fecha y hora futuras.", false); return; }
        var email = _email.Text.Trim();
        if (_name.Text.Length > 250 || _surname.Text.Length > 250 || _id.Text.Length > 20 || _phone.Text.Length > 250 || email.Length > 250 || _notes.Text.Length > 1000)
        { ShowMessage(_bookingMessage, "Revisa la longitud de los datos ingresados.", false); return; }
        if (email.Length > 0 && !System.Net.Mail.MailAddress.TryCreate(email, out _)) { ShowMessage(_bookingMessage, "El correo electrónico no es válido.", false); return; }
        _bookButton.Enabled = false;
        try
        {
            var response = await _api.SendForJsonAsync<JsonElement>(HttpMethod.Post, "api/Citas/publica", new
            {
                Nombre = _name.Text.Trim(), Apellido = _surname.Text.Trim(), Cedula = EmptyNull(_id.Text), Telefono = EmptyNull(_phone.Text),
                Email = EmptyNull(email), Fecha = _date.Value, TipoCita = _reason.Text, Observaciones = EmptyNull(_notes.Text)
            });
            var code = ReadText(response, "CodigoGestion") ?? "";
            ShowMessage(_bookingMessage, $"Solicitud recibida. La cita queda pendiente de confirmación. Código de gestión: {code}", true);
            if (!string.IsNullOrWhiteSpace(code) && MessageBox.Show(this, $"Código de gestión:\r\n{code}\r\n\r\n¿Copiarlo al portapapeles?", "Solicitud recibida", MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
                Clipboard.SetText(code);
            _name.Clear(); _surname.Clear(); _id.Clear(); _phone.Clear(); _email.Clear(); _notes.Clear();
            _date.Value = DateTime.Today.AddDays(1).AddHours(9);
        }
        catch (Exception ex) { ShowMessage(_bookingMessage, ex.Message, false); }
        finally { _bookButton.Enabled = true; }
    }

    private async Task CancelAsync()
    {
        if (string.IsNullOrWhiteSpace(_code.Text) && (string.IsNullOrWhiteSpace(_cancelId.Text) || string.IsNullOrWhiteSpace(_cancelPhone.Text)))
        { ShowMessage(_cancelMessage, "Indica el código o completa tanto la cédula como el teléfono.", false); return; }
        _cancelButton.Enabled = false;
        try
        {
            await _api.SendAsync(HttpMethod.Post, "api/Citas/publica/cancelar", new { CodigoGestion = EmptyNull(_code.Text), Cedula = EmptyNull(_cancelId.Text), Telefono = EmptyNull(_cancelPhone.Text) });
            ShowMessage(_cancelMessage, "La cita fue cancelada.", true);
            _code.Clear(); _cancelId.Clear(); _cancelPhone.Clear();
        }
        catch (Exception ex) { ShowMessage(_cancelMessage, ex.Message, false); }
        finally { _cancelButton.Enabled = true; }
    }

    private static void AddField(TableLayoutPanel table, string label, Control control, bool fullWidth = false)
    {
        var column = fullWidth || table.ColumnCount == 1 ? 0 : table.Controls.Count % table.ColumnCount;
        if (column == 0) table.RowCount++;
        var group = new Panel { Dock = DockStyle.Fill, Height = control is TextBox { Multiline: true } ? 115 : 76, Padding = new Padding(5, 4, 12, 4), Margin = new Padding(2) };
        group.Controls.Add(new Label { Text = label, Dock = DockStyle.Top, Height = 24, ForeColor = Charcoal, Font = new Font("Segoe UI", 9, FontStyle.Bold) });
        control.Dock = DockStyle.Top; control.Height = Math.Max(control.Height, 32); control.Margin = new Padding(0, 3, 0, 0); control.Font = new Font("Segoe UI", 10); control.ForeColor = Charcoal;
        group.Controls.Add(control);
        table.Controls.Add(group, column, table.RowCount - 1);
        if (fullWidth && table.ColumnCount > 1) table.SetColumnSpan(group, table.ColumnCount);
    }

    private static TextBox Field(string label) => new() { PlaceholderText = label, Width = 230, BackColor = Color.White };
    private static Label MessageLabel() => new() { Dock = DockStyle.Top, Height = 72, ForeColor = Mauve, Padding = new Padding(4), AutoSize = false };
    private static Button ActionButton(string text, bool primary) => new() { Text = text, AutoSize = true, Height = 40, Padding = new Padding(12, 4, 12, 4), BackColor = primary ? Mauve : Color.White, ForeColor = primary ? Color.White : Mauve, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 10, FontStyle.Bold), Cursor = Cursors.Hand };
    private static void ShowMessage(Label label, string text, bool success) { label.Text = text; label.ForeColor = success ? ColorTranslator.FromHtml("#507A60") : Color.Firebrick; }
    private static string? EmptyNull(string value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static string? ReadText(JsonElement element, string property)
    {
        if (element.ValueKind != JsonValueKind.Object) return null;
        foreach (var item in element.EnumerateObject())
            if (string.Equals(item.Name, property, StringComparison.OrdinalIgnoreCase) && item.Value.ValueKind == JsonValueKind.String)
                return item.Value.GetString();
        return null;
    }
}
