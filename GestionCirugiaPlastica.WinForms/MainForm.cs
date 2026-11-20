using System.Drawing;
using System.Text.Json;

namespace GestionCirugiaPlastica.WinForms;

internal sealed class MainForm : Form
{
    private static readonly Color Charcoal = ColorTranslator.FromHtml("#36383A");
    private static readonly Color Taupe = ColorTranslator.FromHtml("#9E9A9B");
    private static readonly Color Mauve = ColorTranslator.FromHtml("#9E6E79");
    private static readonly Color Rose = ColorTranslator.FromHtml("#C67C8E");
    private readonly ClinicApiClient _api;
    private readonly Panel _root = new() { Dock = DockStyle.Fill, BackColor = Color.FromArgb(244, 245, 247) };
    private readonly Panel _sidebar = new() { Dock = DockStyle.Left, Width = 245, BackColor = Color.White, Padding = new Padding(18, 24, 18, 18) };
    private readonly Panel _content = new() { Dock = DockStyle.Fill, Padding = new Padding(30), BackColor = Color.FromArgb(244, 245, 247) };
    private readonly Label _status = new() { AutoSize = true, ForeColor = Mauve, Font = new Font("Segoe UI", 9) };
    private string _section = "Inicio";
    private DataGridView? _grid;
    private List<ApiPaciente> _patients = [];

    public MainForm(ClinicApiClient api)
    {
        _api = api;
        Text = "Tanger Clinic | Gestión clínica";
        MinimumSize = new Size(1024, 680);
        Size = new Size(1360, 850);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 10);
        Controls.Add(_root);
        ShowLogin();
    }

    private void ShowLogin()
    {
        _root.Controls.Clear();
        var card = new Panel { Size = new Size(460, 510), BackColor = Color.White, Padding = new Padding(40) };
        card.Left = (ClientSize.Width - card.Width) / 2; card.Top = (ClientSize.Height - card.Height) / 2;
        _root.Resize += (_, _) => { card.Left = (_root.ClientSize.Width - card.Width) / 2; card.Top = (_root.ClientSize.Height - card.Height) / 2; };
        var brand = new Label { Text = "TANGER CLINIC", Font = new Font("Segoe UI", 13, FontStyle.Bold), ForeColor = Mauve, AutoSize = true, Top = 38 };
        var title = new Label { Text = "Bienvenido", Font = new Font("Segoe UI", 26, FontStyle.Bold), ForeColor = Charcoal, AutoSize = true, Top = 82 };
        var subtitle = new Label { Text = "Accede al sistema de gestión clínica", ForeColor = Taupe, AutoSize = true, Top = 128 };
        var user = new TextBox { PlaceholderText = "Usuario, correo, cédula o teléfono", Left = 0, Top = 190, Width = 375, Height = 40, Font = new Font("Segoe UI", 11) };
        var password = new TextBox { PlaceholderText = "Contraseña", UseSystemPasswordChar = true, Left = 0, Top = 250, Width = 375, Height = 40, Font = new Font("Segoe UI", 11) };
        var message = new Label { AutoSize = false, Left = 0, Top = 305, Width = 375, Height = 42, ForeColor = Color.Firebrick };
        var login = Button("Iniciar sesión", Mauve, Color.White); login.Left = 0; login.Top = 365; login.Width = 375; login.Height = 48;
        login.Click += async (_, _) =>
        {
            login.Enabled = false; message.Text = "Conectando con la API…"; message.ForeColor = Taupe;
            try { await _api.LoginAsync(user.Text.Trim(), password.Text); await BuildWorkspace(); }
            catch (Exception ex) { message.ForeColor = Color.Firebrick; message.Text = UserMessage(ex); }
            finally { login.Enabled = true; }
        };
        password.KeyDown += (_, e) => { if (e.KeyCode == Keys.Enter) login.PerformClick(); };
        card.Controls.AddRange([brand, title, subtitle, user, password, message, login]);
        _root.Controls.Add(card);
    }

    private async Task BuildWorkspace()
    {
        _root.Controls.Clear();
        _sidebar.Controls.Clear(); _content.Controls.Clear();
        _sidebar.Paint += (_, e) => { using var pen = new Pen(Color.FromArgb(240, 236, 238)); e.Graphics.DrawLine(pen, _sidebar.Width - 1, 0, _sidebar.Width - 1, _sidebar.Height); };
        _root.Controls.Add(_content); _root.Controls.Add(_sidebar);
        var brand = new Panel { Dock = DockStyle.Top, Height = 74, BackColor = Color.White };
        var brandIcon = new Label { Text = "", Width = 44, Height = 44, Left = 0, Top = 4, BackColor = Mauve, ForeColor = Color.White, Font = new Font("Segoe UI Symbol", 19, FontStyle.Bold), TextAlign = ContentAlignment.MiddleCenter };
        brandIcon.Paint += (_, e) => { using var brush = new System.Drawing.Drawing2D.LinearGradientBrush(brandIcon.ClientRectangle, Mauve, Rose, 35f); e.Graphics.FillRectangle(brush, brandIcon.ClientRectangle); TextRenderer.DrawText(e.Graphics, "✚", brandIcon.Font, brandIcon.ClientRectangle, Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter); };
        brand.Controls.Add(brandIcon);
        brand.Controls.Add(new Label { Text = "TANGER CLINIC", Left = 56, Top = 7, Width = 155, Height = 24, ForeColor = Charcoal, Font = new Font("Segoe UI", 11, FontStyle.Bold) });
        brand.Controls.Add(new Label { Text = "P L A S T I C   S U R G E R Y", Left = 56, Top = 31, Width = 165, Height = 20, ForeColor = Taupe, Font = new Font("Segoe UI", 7, FontStyle.Regular) });
        _sidebar.Controls.Add(brand);
        var menuLabel = new Label { Text = "MENÚ PRINCIPAL", Dock = DockStyle.Top, Height = 38, ForeColor = Color.FromArgb(180, 175, 176), Font = new Font("Segoe UI", 8, FontStyle.Bold), TextAlign = ContentAlignment.BottomLeft, Padding = new Padding(7, 0, 0, 8) };
        _sidebar.Controls.Add(menuLabel);
        var menu = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, Padding = new Padding(0, 10, 0, 0), BackColor = Color.White };
        _sidebar.Controls.Add(menu);
        AddNav(menu, "Inicio");
        if (string.Equals(_api.Role, "Paciente", StringComparison.OrdinalIgnoreCase)) AddNav(menu, "Mi portal");
        else
        {
            AddNav(menu, "Pacientes"); AddNav(menu, "Citas");
            if (_api.IsDoctor) { AddNav(menu, "Cirugías"); AddNav(menu, "Expediente"); }
            AddNav(menu, "Pagos");
        }
        var footer = new Panel { Dock = DockStyle.Bottom, Height = 112, BackColor = Color.White, Padding = new Padding(0, 8, 0, 0) };
        footer.Paint += (_, e) => { using var pen = new Pen(Color.FromArgb(240, 236, 238)); e.Graphics.DrawLine(pen, 0, 0, footer.Width, 0); };
        footer.Controls.Add(new Label { Text = _api.UserName ?? "Usuario", Dock = DockStyle.Top, Height = 26, ForeColor = Charcoal, Font = new Font("Segoe UI", 10, FontStyle.Bold), TextAlign = ContentAlignment.MiddleLeft });
        footer.Controls.Add(new Label { Text = _api.Role ?? "", Dock = DockStyle.Top, Height = 24, ForeColor = Taupe, Font = new Font("Segoe UI", 9), TextAlign = ContentAlignment.MiddleLeft });
        var logout = Button("↪   Cerrar sesión", Color.White, Mauve); logout.Dock = DockStyle.Bottom; logout.Height = 40;
        logout.FlatAppearance.BorderSize = 0; logout.FlatAppearance.MouseOverBackColor = Color.FromArgb(248, 233, 237); logout.TextAlign = ContentAlignment.MiddleLeft;
        logout.Click += (_, _) => { _api.Logout(); ShowLogin(); };
        footer.Controls.Add(logout); _sidebar.Controls.Add(footer);
        await ShowSection("Inicio");
    }

    private void AddNav(FlowLayoutPanel menu, string name)
    {
        var icon = name switch { "Inicio" => "◈", "Pacientes" => "♙", "Citas" => "▦", "Cirugías" => "✚", "Expediente" => "▤", "Pagos" => "$", "Mi portal" => "○", _ => "•" };
        var initialSelection = name == "Inicio";
        var button = Button($"{icon}   {name}", initialSelection ? Color.FromArgb(248, 233, 237) : Color.White, initialSelection ? Mauve : Color.FromArgb(111, 105, 107)); button.TextAlign = ContentAlignment.MiddleLeft; button.Width = 207; button.Height = 44;
        if (initialSelection) button.Font = new Font("Segoe UI", 10, FontStyle.Bold);
        button.Font = new Font("Segoe UI", 10); button.FlatAppearance.BorderSize = 0; button.FlatAppearance.MouseOverBackColor = Color.FromArgb(248, 233, 237);
        button.Click += async (_, _) =>
        {
            foreach (Button item in menu.Controls)
            {
                var selected = ReferenceEquals(item, button);
                item.BackColor = selected ? Color.FromArgb(248, 233, 237) : Color.White;
                item.ForeColor = selected ? Mauve : Color.FromArgb(111, 105, 107);
                item.Font = new Font("Segoe UI", 10, selected ? FontStyle.Bold : FontStyle.Regular);
            }
            await ShowSection(name);
        };
        menu.Controls.Add(button);
    }

    private async Task ShowSection(string name)
    {
        _section = name; _content.Controls.Clear();
        var header = new Panel { Dock = DockStyle.Top, Height = 86 };
        header.Controls.Add(new Label { Text = name == "Inicio" ? "Resumen" : name, Font = new Font("Segoe UI", 24, FontStyle.Bold), ForeColor = Charcoal, Dock = DockStyle.Top, Height = 47 });
        header.Controls.Add(new Label { Text = "Gestión segura y centralizada de Tanger Clinic", ForeColor = Taupe, Dock = DockStyle.Bottom, Height = 25 });
        _content.Controls.Add(header);
        if (name == "Inicio") { ShowDashboard(); return; }
        if (name == "Mi portal") { await ShowPatientPortal(); return; }
        if (name == "Expediente") { ShowRecordSearch(); return; }
        var toolbar = new Panel { Dock = DockStyle.Top, Height = 64, Padding = new Padding(0, 8, 0, 10) };
        var search = new TextBox { PlaceholderText = "Buscar en la lista…", Width = 310, Height = 36, Font = new Font("Segoe UI", 10), Dock = DockStyle.Left };
        toolbar.Controls.Add(search);
        if (name is "Pacientes" or "Citas")
        {
            var add = Button(name == "Pacientes" ? "+  Nuevo paciente" : "+  Nueva cita", Mauve, Color.White); add.Dock = DockStyle.Right; add.Width = 175;
            add.Click += async (_, _) => { if (name == "Pacientes") await EditPatient(null); else await EditAppointment(null); };
            toolbar.Controls.Add(add);
        }
        _content.Controls.Add(toolbar);
        _grid = CreateGrid(); _grid.Dock = DockStyle.Fill; _content.Controls.Add(_grid); _content.Controls.SetChildIndex(_grid, 1);
        var statusBar = new Panel { Dock = DockStyle.Bottom, Height = 36 }; _status.Text = ""; statusBar.Controls.Add(_status); _content.Controls.Add(statusBar);
        search.TextChanged += (_, _) => ApplySearch(search.Text);
        await LoadSection(name);
    }

    private void ShowDashboard()
    {
        var flow = new FlowLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(0, 22, 0, 0), BackColor = Color.Transparent };
        _content.Controls.Add(flow);
        AddCard(flow, "Atención", "Pacientes y citas", "Consulta y actualiza registros clínicos de manera ágil.", Mauve);
        AddCard(flow, "Seguridad", _api.Role ?? "", "Cada operación se valida con tu sesión y permisos en la API.", Rose);
        AddCard(flow, "Conexión", "API REST", "Cliente conectado mediante HTTPS y autenticación JWT.", Taupe);
    }

    private static void AddCard(FlowLayoutPanel flow, string tag, string title, string text, Color accent)
    {
        var card = new Panel { Width = 290, Height = 180, BackColor = Color.White, Padding = new Padding(22), Margin = new Padding(0, 0, 18, 18) };
        card.Controls.Add(new Label { Text = tag.ToUpperInvariant(), Dock = DockStyle.Top, Height = 27, ForeColor = accent, Font = new Font("Segoe UI", 9, FontStyle.Bold) });
        card.Controls.Add(new Label { Text = title, Dock = DockStyle.Top, Height = 42, ForeColor = Charcoal, Font = new Font("Segoe UI", 17, FontStyle.Bold) });
        card.Controls.Add(new Label { Text = text, Dock = DockStyle.Fill, ForeColor = Taupe }); flow.Controls.Add(card);
    }

    private async Task LoadSection(string name)
    {
        try
        {
            _status.Text = "Cargando…";
            switch (name)
            {
                case "Pacientes":
                    _patients = await _api.GetAsync<List<ApiPaciente>>("api/Pacientes");
                    _grid!.DataSource = _patients.Select(p => new { p.PacienteId, Nombre = $"{p.Nombre} {p.Apellido}", p.Cedula, p.Telefono, p.Email, Seguro = p.TieneSeguro ? "Sí" : "No", Registro = p.FechaRegistro.ToString("dd/MM/yyyy") }).ToList();
                    AddRowAction("Editar", async row => { var p = _patients.First(x => x.PacienteId == Convert.ToInt32(row.Cells["PacienteId"].Value)); await EditPatient(p); }); break;
                case "Citas":
                    var citas = await _api.GetAsync<List<ApiCita>>("api/Citas");
                    _grid!.DataSource = citas.Select(c => new { c.CitaId, Paciente = c.Paciente is null ? $"#{c.PacienteId}" : $"{c.Paciente.Nombre} {c.Paciente.Apellido}", c.TipoCita, Fecha = c.Fecha.ToString("dd/MM/yyyy HH:mm"), c.Estado, c.EstadoPago, c.Costo }).ToList();
                    AddRowAction("Editar", async row => { var c = citas.First(x => x.CitaId == Convert.ToInt32(row.Cells["CitaId"].Value)); await EditAppointment(c); });
                    AddRowAction("Estado", async row => await ChangeAppointmentStatus(Convert.ToInt32(row.Cells["CitaId"].Value))); break;
                case "Cirugías":
                    var surgeries = await _api.GetAsync<List<ApiCirugia>>("api/Cirugias");
                    _grid!.DataSource = surgeries.Select(c => new { c.CirugiaId, Paciente = c.Paciente is null ? $"#{c.PacienteId}" : $"{c.Paciente.Nombre} {c.Paciente.Apellido}", Procedimiento = c.Procedimiento?.Nombre, Fecha = c.FechaCirugia.ToString("dd/MM/yyyy HH:mm"), c.Estado, c.DiagnosticoPreoperatorio }).ToList(); break;
                case "Pagos":
                    var payments = await _api.GetAsync<List<ApiPago>>("api/Pagos");
                    _grid!.DataSource = payments.Select(p => new { p.PagoId, Fecha = p.Fecha.ToString("dd/MM/yyyy HH:mm"), Destino = p.CirugiaId is not null ? $"Cirugía #{p.CirugiaId}" : $"Cita #{p.CitaId}", p.Concepto, p.MetodoPago, p.Monto }).ToList(); break;
            }
            _status.Text = $"{_grid?.Rows.Count ?? 0} registros";
        }
        catch (Exception ex) { _status.Text = UserMessage(ex); }
    }

    private DataGridView CreateGrid() => new()
    {
        BackgroundColor = Color.White, BorderStyle = BorderStyle.None, GridColor = Color.FromArgb(235, 231, 230),
        AllowUserToAddRows = false, AllowUserToDeleteRows = false, ReadOnly = true, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
        RowHeadersVisible = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false,
        ColumnHeadersHeight = 44, RowTemplate = { Height = 42 }, EnableHeadersVisualStyles = false
    };

    private void AddRowAction(string title, Func<DataGridViewRow, Task> action)
    {
        var column = new DataGridViewButtonColumn { Name = "Accion" + title, HeaderText = "", Text = title, UseColumnTextForButtonValue = true, FillWeight = 55, FlatStyle = FlatStyle.Flat };
        _grid!.Columns.Add(column);
        _grid.CellContentClick += async (_, e) => { if (e.RowIndex >= 0 && _grid.Columns[e.ColumnIndex].Name == column.Name) await Run(() => action(_grid.Rows[e.RowIndex])); };
    }

    private void ApplySearch(string text)
    {
        if (_grid is null) return;
        foreach (DataGridViewRow row in _grid.Rows)
            row.Visible = string.IsNullOrWhiteSpace(text) || row.Cells.Cast<DataGridViewCell>().Any(cell => cell.Value?.ToString()?.Contains(text, StringComparison.OrdinalIgnoreCase) == true);
    }

    private async Task EditPatient(ApiPaciente? current)
    {
        using var form = new EntityDialog(current is null ? "Registrar paciente" : "Editar paciente");
        var first = form.AddText("Nombre", current?.Nombre); var last = form.AddText("Apellido", current?.Apellido);
        var cedula = form.AddText("Cédula", current?.Cedula); var phone = form.AddText("Teléfono", current?.Telefono); var email = form.AddText("Correo", current?.Email);
        var address = form.AddText("Dirección", current?.Direccion); var sex = form.AddText("Sexo", current?.Sexo);
        var birth = form.AddDate("Fecha de nacimiento", current?.FechaNacimiento?.ToDateTime(TimeOnly.MinValue)); var insurance = form.AddCheck("Tiene seguro", current?.TieneSeguro ?? false);
        if (form.ShowDialog(this) != DialogResult.OK) return;
        if (string.IsNullOrWhiteSpace(first.Text) || string.IsNullOrWhiteSpace(last.Text)) { MessageBox.Show(this, "Nombre y apellido son obligatorios.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
        var payload = new { PacienteId = current?.PacienteId ?? 0, FechaRegistro = current?.FechaRegistro ?? DateTime.Now, Nombre = first.Text.Trim(), Apellido = last.Text.Trim(), Cedula = EmptyNull(cedula.Text), FechaNacimiento = birth.Checked ? birth.Value.Date.ToString("yyyy-MM-dd") : null, TieneSeguro = insurance.Checked, Sexo = EmptyNull(sex.Text), Telefono = EmptyNull(phone.Text), Email = EmptyNull(email.Text), Direccion = EmptyNull(address.Text) };
        await Run(async () => { await _api.SendAsync(current is null ? HttpMethod.Post : HttpMethod.Put, current is null ? "api/Pacientes" : $"api/Pacientes/{current.PacienteId}", payload); MessageBox.Show(this, "Paciente guardado correctamente.", "Tanger Clinic", MessageBoxButtons.OK, MessageBoxIcon.Information); await LoadSection("Pacientes"); });
    }

    private async Task EditAppointment(ApiCita? current)
    {
        try { _patients = await _api.GetAsync<List<ApiPaciente>>("api/Pacientes"); }
        catch (Exception ex) { MessageBox.Show(this, UserMessage(ex), "No se pudo cargar pacientes", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
        using var form = new EntityDialog(current is null ? "Registrar cita" : "Editar cita");
        var patient = form.AddChoice("Paciente", _patients.Select(p => new ChoiceItem(p.PacienteId, $"{p.Nombre} {p.Apellido} · #{p.PacienteId}")).ToList(), current?.PacienteId);
        var date = form.AddDateTime("Fecha y hora", current?.Fecha ?? DateTime.Now.AddDays(1)); var type = form.AddText("Tipo de cita", current?.TipoCita);
        var cost = form.AddText("Costo", current?.Costo.ToString("0.00") ?? "0.00"); var state = form.AddChoiceText("Estado", ["Pendiente", "Confirmada", "Completada", "Cancelada"], current?.Estado ?? "Pendiente");
        var payment = form.AddChoiceText("Estado de pago", ["Pendiente", "Pagado", "Parcial"], current?.EstadoPago ?? "Pendiente"); var notes = form.AddText("Observaciones", current?.Observaciones, multiline: true);
        if (form.ShowDialog(this) != DialogResult.OK) return;
        if (patient.SelectedItem is not ChoiceItem selected || !decimal.TryParse(cost.Text, out var amount)) { MessageBox.Show(this, "Selecciona un paciente e indica un costo válido.", "Validación"); return; }
        var payload = new { CitaId = current?.CitaId ?? 0, Fecha = date.Value, PacienteId = selected.Id, TipoCita = EmptyNull(type.Text), Costo = amount, Estado = state.Text, EstadoPago = payment.Text, Observaciones = EmptyNull(notes.Text), CitaAnteriorId = current?.CitaAnteriorId };
        await Run(async () => { await _api.SendAsync(HttpMethod.Post, "api/Citas", payload); MessageBox.Show(this, "Cita guardada correctamente.", "Tanger Clinic", MessageBoxButtons.OK, MessageBoxIcon.Information); await LoadSection("Citas"); });
    }

    private async Task ChangeAppointmentStatus(int id)
    {
        var states = new[] { "Pendiente", "Confirmada", "Completada", "Cancelada" };
        using var form = new EntityDialog($"Estado de la cita #{id}"); var state = form.AddChoiceText("Nuevo estado", states, states[0]);
        if (form.ShowDialog(this) != DialogResult.OK) return;
        await Run(async () => { await _api.SendAsync(HttpMethod.Patch, $"api/Citas/{id}/estado", state.Text); await LoadSection("Citas"); });
    }

    private void ShowRecordSearch()
    {
        var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 24, 0, 0), BackColor = Color.White };
        var search = new TextBox { PlaceholderText = "Nombre, cédula o ID del paciente", Width = 450, Height = 38, Left = 22, Top = 22 };
        var button = Button("Consultar expediente", Mauve, Color.White); button.Left = 490; button.Top = 20; button.Width = 190; button.Height = 40;
        var result = new TextBox { Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, Dock = DockStyle.Bottom, Height = 440, BorderStyle = BorderStyle.None, BackColor = Color.White, Font = new Font("Segoe UI", 10) };
        button.Click += async (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(search.Text)) return;
            try { var data = await _api.GetAsync<JsonElement>($"api/Pacientes/expediente/{Uri.EscapeDataString(search.Text.Trim())}"); result.Text = FormatRecord(data); }
            catch (Exception ex) { result.Text = UserMessage(ex); }
        };
        panel.Controls.Add(search); panel.Controls.Add(button); panel.Controls.Add(result); _content.Controls.Add(panel);
    }

    private async Task ShowPatientPortal()
    {
        var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 22, 0, 0), BackColor = Color.White };
        var profile = new Label { Dock = DockStyle.Top, Height = 85, ForeColor = Charcoal, Font = new Font("Segoe UI", 14, FontStyle.Bold), Padding = new Padding(20, 15, 0, 0) };
        var schedule = Button("Solicitar cita", Mauve, Color.White); schedule.Dock = DockStyle.Top; schedule.Height = 44;
        var upcoming = new DataGridView { Dock = DockStyle.Fill, BackgroundColor = Color.White, BorderStyle = BorderStyle.None, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, ReadOnly = true, RowHeadersVisible = false, EnableHeadersVisualStyles = false };
        panel.Controls.Add(upcoming); panel.Controls.Add(schedule); panel.Controls.Add(profile); _content.Controls.Add(panel);
        try
        {
            var me = await _api.GetAsync<JsonElement>("api/PortalPaciente/perfil");
            profile.Text = $"Hola, {me.GetProperty("nombre").GetString()} {me.GetProperty("apellido").GetString()}\r\nTu información personal y próximas citas";
            var appointments = await _api.GetAsync<List<JsonElement>>("api/PortalPaciente/citas");
            upcoming.DataSource = appointments.Select(c => new { Cita = c.GetProperty("citaId").GetInt32(), Fecha = c.GetProperty("fecha").GetDateTime().ToString("dd/MM/yyyy HH:mm"), Tipo = c.GetProperty("tipoCita").ToString(), Estado = c.GetProperty("estado").ToString() }).ToList();
        }
        catch (Exception ex) { profile.Text = UserMessage(ex); }
        schedule.Click += async (_, _) =>
        {
            using var form = new EntityDialog("Solicitar una cita"); var date = form.AddDateTime("Fecha y hora", DateTime.Now.AddDays(1)); var type = form.AddText("Tipo de cita", "Consulta"); var notes = form.AddText("Observaciones", multiline: true);
            if (form.ShowDialog(this) != DialogResult.OK) return;
            await Run(async () => { await _api.SendAsync(HttpMethod.Post, "api/PortalPaciente/citas", new { Fecha = date.Value, TipoCita = EmptyNull(type.Text), Observaciones = EmptyNull(notes.Text) }); MessageBox.Show(this, "Solicitud de cita enviada.", "Tanger Clinic", MessageBoxButtons.OK, MessageBoxIcon.Information); await ShowSection("Mi portal"); });
        };
    }

    private static string FormatRecord(JsonElement root)
    {
        var output = new System.Text.StringBuilder();
        if (root.TryGetProperty("paciente", out var p)) output.AppendLine($"PACIENTE\r\n{p.GetProperty("Nombre").GetString()} {p.GetProperty("Apellido").GetString()}  |  ID #{p.GetProperty("PacienteId").GetInt32()}\r\nCédula: {p.GetProperty("Cedula").ToString()}  |  Teléfono: {p.GetProperty("Telefono").ToString()}  |  Correo: {p.GetProperty("Email").ToString()}\r\n");
        AppendArray(root, "citas", "CITAS", output); AppendArray(root, "operaciones", "CIRUGÍAS", output); AppendArray(root, "fotografias", "FOTOGRAFÍAS", output);
        if (root.TryGetProperty("seguro", out var insurance)) output.AppendLine($"SEGURO\r\nTiene seguro: {insurance.GetProperty("tieneSeguro").GetBoolean()}\r\n");
        return output.ToString();
    }
    private static void AppendArray(JsonElement root, string key, string label, System.Text.StringBuilder output)
    {
        output.AppendLine(label);
        if (root.TryGetProperty(key, out var items) && items.ValueKind == JsonValueKind.Array && items.GetArrayLength() > 0)
            foreach (var item in items.EnumerateArray()) output.AppendLine("• " + item.ToString());
        else output.AppendLine("Sin registros.");
        output.AppendLine();
    }

    private async Task Run(Func<Task> operation)
    {
        try { await operation(); }
        catch (Exception ex) { MessageBox.Show(this, UserMessage(ex), "Tanger Clinic", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
    }
    private static string UserMessage(Exception ex) => ex is HttpRequestException ? "No se pudo conectar con la API. Verifica que esté iniciada y que su URL base sea correcta." : ex.Message;
    private static string? EmptyNull(string text) => string.IsNullOrWhiteSpace(text) ? null : text.Trim();
    private static Button Button(string text, Color back, Color fore) => new() { Text = text, BackColor = back, ForeColor = fore, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 10, FontStyle.Bold), Cursor = Cursors.Hand, UseVisualStyleBackColor = false };

    protected override void OnFormClosed(FormClosedEventArgs e) { _api.Dispose(); base.OnFormClosed(e); }
}
