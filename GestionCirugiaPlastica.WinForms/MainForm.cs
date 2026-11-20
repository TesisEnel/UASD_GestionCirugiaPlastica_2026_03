using System.Drawing;
using System.Globalization;
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
    private readonly Panel _pageContent = new() { Dock = DockStyle.Fill, BackColor = Color.FromArgb(244, 245, 247) };
    private readonly Panel _rightPanel = new() { Dock = DockStyle.Right, Width = 300, BackColor = Color.White, Padding = new Padding(18) };
    private readonly Label _status = new() { AutoSize = true, ForeColor = Mauve, Font = new Font("Segoe UI", 9) };
    private string _section = "Inicio";
    private string _surgeryStatusFilter = "Todos los estados";
    private DataGridView? _grid;
    private DataGridView? _actionGrid;
    private readonly Dictionary<string, Func<DataGridViewRow, Task>> _rowActions = [];
    private List<ApiPaciente> _patients = [];
    private DateTime _calendarMonth = DateTime.Today;
    private DateTime _calendarDay = DateTime.Today;
    private List<ApiCita> _calendarAppointments = [];
    private TableLayoutPanel? _calendarTable;
    private Label? _calendarTitle;
    private FlowLayoutPanel? _dayAppointments;
    private Label? _rightProfile;

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
        var user = new TextBox { PlaceholderText = "Usuario, correo, cédula o teléfono", Left = 0, Top = 190, Width = 375, Height = 40, Font = new Font("Segoe UI", 11), ForeColor = Charcoal, BackColor = Color.White };
        var password = new TextBox { PlaceholderText = "Contraseña", UseSystemPasswordChar = true, Left = 0, Top = 250, Width = 375, Height = 40, Font = new Font("Segoe UI", 11), ForeColor = Charcoal, BackColor = Color.White };
        var message = new Label { AutoSize = false, Left = 0, Top = 305, Width = 375, Height = 42, ForeColor = Color.Firebrick };
        var login = Button("Iniciar sesión", Mauve, Color.White); login.Left = 0; login.Top = 365; login.Width = 375; login.Height = 48;
        login.Click += async (_, _) =>
        {
            login.Enabled = false; message.Text = "Conectando con la API…"; message.ForeColor = Taupe;
            try { await _api.LoginAsync(user.Text.Trim(), password.Text); await BuildWorkspace(); }
            catch (Exception ex) { message.ForeColor = Color.Firebrick; message.Text = UserMessage(ex); }
            finally { login.Enabled = true; }
        };
        var register = Button("Crear cuenta de paciente", Color.White, Mauve); register.Left = 0; register.Top = 426; register.Width = 375; register.Height = 38; register.FlatAppearance.BorderColor = Color.FromArgb(231, 221, 224);
        register.Click += async (_, _) => await RegisterPatient();
        password.KeyDown += (_, e) => { if (e.KeyCode == Keys.Enter) login.PerformClick(); };
        card.Controls.AddRange([brand, title, subtitle, user, password, message, login, register]);
        _root.Controls.Add(card);
    }

    private async Task RegisterPatient()
    {
        using var form = new EntityDialog("Crear cuenta de paciente");
        var fullName = form.AddText("Nombre completo *"); var username = form.AddText("Nombre de usuario *");
        var password = form.AddText("Contraseña (mínimo 12 caracteres) *"); password.UseSystemPasswordChar = true;
        var email = form.AddText("Correo electrónico (opcional)"); var cedula = form.AddText("Cédula (opcional)"); var phone = form.AddText("Teléfono (opcional)");
        if (form.ShowDialog(this) != DialogResult.OK) return;
        if (string.IsNullOrWhiteSpace(fullName.Text) || username.Text.Trim().Length is < 3 or > 100 || password.Text.Length < 12)
        {
            MessageBox.Show(this, "Completa el nombre, un usuario de 3 a 100 caracteres y una contraseña de al menos 12 caracteres.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Information); return;
        }
        if (username.Text.Any(char.IsWhiteSpace) || username.Text.Any(c => !(char.IsLetterOrDigit(c) || c is '.' or '_' or '-')))
        {
            MessageBox.Show(this, "El nombre de usuario solo puede incluir letras, números, punto, guion y guion bajo.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Information); return;
        }
        try
        {
            await _api.SendAsync(HttpMethod.Post, "api/Auth/register", new { NombreCompleto = fullName.Text.Trim(), NombreUsuario = username.Text.Trim(), Password = password.Text, Email = EmptyNull(email.Text), Cedula = EmptyNull(cedula.Text), Telefono = EmptyNull(phone.Text) });
            MessageBox.Show(this, "La cuenta de paciente quedó registrada. Ya puedes iniciar sesión.", "Tanger Clinic", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex) { MessageBox.Show(this, UserMessage(ex), "No se pudo crear la cuenta", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
    }

    private async Task BuildWorkspace()
    {
        _root.Controls.Clear();
        _sidebar.Controls.Clear(); _content.Controls.Clear(); _pageContent.Controls.Clear(); _rightPanel.Controls.Clear();
        _sidebar.Paint += (_, e) => { using var pen = new Pen(Color.FromArgb(240, 236, 238)); e.Graphics.DrawLine(pen, _sidebar.Width - 1, 0, _sidebar.Width - 1, _sidebar.Height); };
        _root.Controls.Add(_content); _root.Controls.Add(_rightPanel); _root.Controls.Add(_sidebar);
        var topbar = new Panel { Dock = DockStyle.Top, Height = 84, BackColor = Color.White, Padding = new Padding(28, 15, 28, 12) };
        topbar.Paint += (_, e) => { using var pen = new Pen(Color.FromArgb(235, 231, 230)); e.Graphics.DrawLine(pen, 0, topbar.Height - 1, topbar.Width, topbar.Height - 1); };
        var welcome = new Label { Text = _api.IsAssistant ? "Área clínica" : _api.IsPatient ? "Mi portal de paciente" : "Bienvenido", Dock = DockStyle.Top, Height = 28, ForeColor = Charcoal, Font = new Font("Segoe UI", 16, FontStyle.Bold) };
        var welcomeCaption = new Label { Text = _api.IsAssistant ? "Expedientes y seguimiento médico" : _api.IsPatient ? "Consulta tu perfil, citas y recetas" : "Panel de control del consultorio", Dock = DockStyle.Top, Height = 25, ForeColor = Taupe, Font = new Font("Segoe UI", 9) };
        var globalSearch = new TextBox { PlaceholderText = "Buscar aquí…", Width = 230, Height = 34, Dock = DockStyle.Right, Top = 19, ForeColor = Charcoal, BackColor = Color.White };
        globalSearch.TextChanged += (_, _) => { if (_grid is not null) ApplySearch(globalSearch.Text); };
        topbar.Controls.Add(welcome); topbar.Controls.Add(welcomeCaption); topbar.Controls.Add(globalSearch);
        _content.Controls.Add(_pageContent); _content.Controls.Add(topbar);
        _rightPanel.Paint += (_, e) => { using var pen = new Pen(Color.FromArgb(240, 236, 238)); e.Graphics.DrawLine(pen, 0, 0, 0, _rightPanel.Height); };
        await BuildRightPanel();
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
        if (_api.IsDoctor || _api.IsSecretary)
        {
            AddNav(menu, "Resumen"); AddNav(menu, "Citas"); AddNav(menu, "Pacientes"); AddNav(menu, "Pagos");
            if (_api.IsDoctor) { AddNav(menu, "Cirugías"); AddNav(menu, "Recetas"); AddNav(menu, "Aseguradoras"); AddNav(menu, "Configuración"); }
        }
        else if (_api.IsAssistant) { AddNav(menu, "Expedientes"); AddNav(menu, "Cirugías"); AddNav(menu, "Recetas"); }
        else if (_api.IsPatient) { AddNav(menu, "Inicio"); AddNav(menu, "Mi perfil, citas y recetas"); }
        var footer = new Panel { Dock = DockStyle.Bottom, Height = 112, BackColor = Color.White, Padding = new Padding(0, 8, 0, 0) };
        footer.Paint += (_, e) => { using var pen = new Pen(Color.FromArgb(240, 236, 238)); e.Graphics.DrawLine(pen, 0, 0, footer.Width, 0); };
        footer.Controls.Add(new Label { Text = _api.UserName ?? "Usuario", Dock = DockStyle.Top, Height = 26, ForeColor = Charcoal, Font = new Font("Segoe UI", 10, FontStyle.Bold), TextAlign = ContentAlignment.MiddleLeft });
        footer.Controls.Add(new Label { Text = _api.Role ?? "", Dock = DockStyle.Top, Height = 24, ForeColor = Taupe, Font = new Font("Segoe UI", 9), TextAlign = ContentAlignment.MiddleLeft });
        var logout = Button("↪   Cerrar sesión", Color.White, Mauve); logout.Dock = DockStyle.Bottom; logout.Height = 40;
        logout.FlatAppearance.BorderSize = 0; logout.FlatAppearance.MouseOverBackColor = Color.FromArgb(248, 233, 237); logout.TextAlign = ContentAlignment.MiddleLeft;
        logout.Click += (_, _) => { _api.Logout(); ShowLogin(); };
        footer.Controls.Add(logout); _sidebar.Controls.Add(footer);
        var firstSection = _api.IsDoctor || _api.IsSecretary ? "Resumen" : _api.IsAssistant ? "Expedientes" : "Inicio";
        await ShowSection(firstSection);
    }

    private async Task BuildRightPanel()
    {
        var profile = new Panel { Dock = DockStyle.Top, Height = 108, BackColor = Color.White, Padding = new Padding(0, 6, 0, 12) };
        _rightProfile = new Label { Text = $"{_api.UserName}\r\n{_api.Role}", Dock = DockStyle.Top, Height = 50, ForeColor = Charcoal, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
        var profileButton = Button("Mi perfil", Color.White, Mauve); profileButton.Dock = DockStyle.Left; profileButton.Width = 110; profileButton.Height = 34; profileButton.Click += async (_, _) => await ShowSection("Mi perfil");
        var logoutButton = Button("Salir", Color.White, Mauve); logoutButton.Dock = DockStyle.Right; logoutButton.Width = 80; logoutButton.Height = 34; logoutButton.Click += (_, _) => { _api.Logout(); ShowLogin(); };
        profile.Controls.Add(profileButton); profile.Controls.Add(logoutButton); profile.Controls.Add(_rightProfile);
        _calendarTitle = new Label { Dock = DockStyle.Top, Height = 30, TextAlign = ContentAlignment.MiddleCenter, ForeColor = Charcoal, Font = new Font("Segoe UI", 11, FontStyle.Bold) };
        var monthNav = new Panel { Dock = DockStyle.Top, Height = 38 };
        var previous = Button("‹", Color.White, Mauve); previous.Dock = DockStyle.Left; previous.Width = 42; previous.FlatAppearance.BorderSize = 0;
        var next = Button("›", Color.White, Mauve); next.Dock = DockStyle.Right; next.Width = 42; next.FlatAppearance.BorderSize = 0;
        previous.Click += (_, _) => { _calendarMonth = _calendarMonth.AddMonths(-1); RefreshRightCalendar(); };
        next.Click += (_, _) => { _calendarMonth = _calendarMonth.AddMonths(1); RefreshRightCalendar(); };
        monthNav.Controls.Add(previous); monthNav.Controls.Add(next);
        _calendarTable = new TableLayoutPanel { Dock = DockStyle.Top, Height = 222, ColumnCount = 7, RowCount = 7, BackColor = Color.White, Margin = new Padding(0, 8, 0, 16) };
        for (var column = 0; column < 7; column++) _calendarTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 14.285f));
        for (var row = 0; row < 7; row++) _calendarTable.RowStyles.Add(new RowStyle(SizeType.Percent, 14.285f));
        _dayAppointments = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true, BackColor = Color.White, Padding = new Padding(0, 8, 0, 0) };
        var appointmentHeader = new Label { Text = "Citas del día", Dock = DockStyle.Top, Height = 44, ForeColor = Charcoal, Font = new Font("Segoe UI", 12, FontStyle.Bold), TextAlign = ContentAlignment.BottomLeft };
        _rightPanel.Controls.Add(_dayAppointments); _rightPanel.Controls.Add(appointmentHeader); _rightPanel.Controls.Add(_calendarTable); _rightPanel.Controls.Add(monthNav); _rightPanel.Controls.Add(_calendarTitle); _rightPanel.Controls.Add(profile);
        RefreshRightCalendar();
        try
        {
            var profileData = await _api.GetAsync<JsonElement>("api/Perfil");
            var displayName = ReadText(profileData, "nombreCompleto") ?? _api.UserName ?? "Usuario";
            _rightProfile.Text = $"{displayName}\r\n{ReadText(profileData, "rol") ?? _api.Role}";
        }
        catch (Exception ex) { _rightProfile.Text = $"{_api.UserName}\r\n{_api.Role}\r\n{UserMessage(ex)}"; }
        if (_api.IsDoctor || _api.IsSecretary)
        {
            try { _calendarAppointments = await _api.GetAsync<List<ApiCita>>("api/Citas"); RefreshRightCalendar(); }
            catch (Exception ex) { _dayAppointments.Controls.Add(new Label { Text = UserMessage(ex), Width = 245, Height = 58, ForeColor = Color.Firebrick }); }
        }
    }

    private void AttachStatusBar()
    {
        var statusBar = new Panel { Dock = DockStyle.Bottom, Height = 34 };
        _status.Text = ""; statusBar.Controls.Add(_status); _pageContent.Controls.Add(statusBar);
    }

    private void RefreshRightCalendar()
    {
        if (_calendarTitle is null || _calendarTable is null || _dayAppointments is null) return;
        _calendarTitle.Text = _calendarMonth.ToString("MMMM yyyy", CultureInfo.GetCultureInfo("es-DO"));
        _calendarTable.SuspendLayout(); _calendarTable.Controls.Clear();
        string[] weekdays = ["Do", "Lu", "Ma", "Mi", "Ju", "Vi", "Sá"];
        for (var i = 0; i < weekdays.Length; i++) _calendarTable.Controls.Add(new Label { Text = weekdays[i], Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter, ForeColor = Taupe, Font = new Font("Segoe UI", 8, FontStyle.Bold) }, i, 0);
        var first = new DateTime(_calendarMonth.Year, _calendarMonth.Month, 1);
        var start = first.AddDays(-(int)first.DayOfWeek);
        for (var i = 0; i < 42; i++)
        {
            var date = start.AddDays(i);
            var hasAppointments = _calendarAppointments.Any(c => c.Fecha.Date == date.Date && !string.Equals(c.Estado, "Cancelada", StringComparison.OrdinalIgnoreCase));
            var day = new Button { Text = date.Day + (hasAppointments ? " •" : ""), Dock = DockStyle.Fill, Margin = new Padding(1), FlatStyle = FlatStyle.Flat, FlatAppearance = { BorderSize = 0 }, BackColor = date.Date == _calendarDay.Date ? Color.FromArgb(248, 233, 237) : Color.White, ForeColor = date.Month == _calendarMonth.Month ? Charcoal : Taupe, Font = new Font("Segoe UI", 8, date.Date == _calendarDay.Date ? FontStyle.Bold : FontStyle.Regular), Tag = date };
            day.Click += (_, _) => { _calendarDay = (DateTime)day.Tag!; RefreshRightCalendar(); };
            _calendarTable.Controls.Add(day, i % 7, (i / 7) + 1);
        }
        _calendarTable.ResumeLayout();
        _dayAppointments.Controls.Clear();
        var selectedAppointments = _calendarAppointments.Where(c => c.Fecha.Date == _calendarDay.Date && !string.Equals(c.Estado, "Cancelada", StringComparison.OrdinalIgnoreCase)).OrderBy(c => c.Fecha).ToList();
        if (selectedAppointments.Count == 0) _dayAppointments.Controls.Add(new Label { Text = _api.IsDoctor || _api.IsSecretary ? "No hay citas programadas para este día." : "La agenda está disponible para Doctora y Secretaría.", Width = 245, Height = 46, ForeColor = Taupe });
        foreach (var appointment in selectedAppointments)
        {
            var patientName = appointment.Paciente is null ? $"Paciente #{appointment.PacienteId}" : $"{appointment.Paciente.Nombre} {appointment.Paciente.Apellido}";
            _dayAppointments.Controls.Add(new Label { Text = $"{appointment.Fecha:hh:mm tt}\r\n{appointment.TipoCita} · {patientName}\r\n{appointment.Estado}", Width = 250, Height = 65, Padding = new Padding(9), BackColor = Color.FromArgb(250, 247, 248), ForeColor = Charcoal, Font = new Font("Segoe UI", 9) });
        }
    }

    private void AddNav(FlowLayoutPanel menu, string name)
    {
        var icon = name switch { "Inicio" or "Resumen" => "◈", "Pacientes" => "♙", "Citas" => "▦", "Cirugías" => "✚", "Expediente" or "Expedientes" => "▤", "Pagos" => "$", "Mi portal" or "Mi perfil, citas y recetas" => "○", "Recetas" => "Rx", "Aseguradoras" => "♡", "Configuración" => "⚙", _ => "•" };
        var selectedStart = _api.IsDoctor || _api.IsSecretary ? "Resumen" : _api.IsAssistant ? "Expedientes" : "Inicio";
        var initialSelection = name == selectedStart;
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

    private async Task ShowSection(string name, int? patientId = null)
    {
        _section = name; _pageContent.Controls.Clear(); _grid = null;
        var header = new Panel { Dock = DockStyle.Top, Height = 86 };
        var pageName = name == "Expedientes" ? "Expediente clínico" : name == "Mi perfil, citas y recetas" ? "Mi perfil, citas y recetas" : name;
        header.Controls.Add(new Label { Text = pageName, Font = new Font("Segoe UI", 24, FontStyle.Bold), ForeColor = Charcoal, Dock = DockStyle.Top, Height = 47 });
        header.Controls.Add(new Label { Text = name switch { "Resumen" => "Consulta la actividad y el rendimiento de tu consultorio", "Cirugías" => "Control clínico y seguimiento de operaciones", "Recetas" => "Medicamentos e indicaciones para tus pacientes", "Expedientes" => "Consulta y seguimiento del historial clínico", _ => "Gestión segura y centralizada de Tanger Clinic" }, ForeColor = Taupe, Dock = DockStyle.Bottom, Height = 25 });
        if (name == "Cirugías")
        {
            var newSurgery = Button("Nueva cirugía", Mauve, Color.White); newSurgery.Width = 150; newSurgery.Height = 40; newSurgery.Left = header.Width - 150; newSurgery.Top = 4; newSurgery.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            newSurgery.Click += async (_, _) => await EditSurgery(null, () => LoadSection("Cirugías"));
            header.Controls.Add(newSurgery);
        }
        _pageContent.Controls.Add(header);
        if (name == "Resumen") { await ShowDashboard(); return; }
        if (name == "Inicio") { await ShowPatientHome(); return; }
        if (name is "Mi portal" or "Mi perfil, citas y recetas") { await ShowPatientPortal(); return; }
        if (name is "Expediente" or "Expedientes") { ShowRecordSearch(patientId); return; }
        if (name == "Recetas") { await ShowRecipes(); return; }
        if (name == "Aseguradoras") { await ShowInsurers(); return; }
        if (name == "Configuración") { await ShowConfiguration(); return; }
        if (name == "Mi perfil") { await ShowMyProfile(); return; }
        var toolbar = new Panel { Dock = DockStyle.Top, Height = 64, Padding = new Padding(0, 8, 0, 10) };
        var search = new TextBox { PlaceholderText = "Buscar en la lista…", Width = 310, Height = 36, Font = new Font("Segoe UI", 10), Dock = DockStyle.Left, ForeColor = Charcoal, BackColor = Color.White };
        toolbar.Controls.Add(search);
        if (name == "Cirugías")
        {
            var statusFilter = new ComboBox { Width = 235, Height = 36, DropDownStyle = ComboBoxStyle.DropDownList, DataSource = new[] { "Todos los estados", "Estudios preoperatorios", "Programada", "En proceso", "Postoperatorio / Recuperación", "Completada" }, SelectedItem = _surgeryStatusFilter, Dock = DockStyle.Left, ForeColor = Charcoal, BackColor = Color.White };
            statusFilter.SelectedValueChanged += async (_, _) =>
            {
                if (statusFilter.SelectedItem is string selected && selected != _surgeryStatusFilter)
                {
                    _surgeryStatusFilter = selected;
                    await LoadSection("Cirugías");
                }
            };
            toolbar.Controls.Add(statusFilter);
        }
        if (name is "Pacientes" or "Citas" or "Cirugías" or "Pagos")
        {
            var refresh = Button("Actualizar", Color.White, Mauve); refresh.Dock = DockStyle.Right; refresh.Width = 120;
            refresh.Click += async (_, _) => await LoadSection(name); toolbar.Controls.Add(refresh);
            if (name == "Cirugías")
            {
                var addProcedure = Button("+  Procedimiento", Color.White, Mauve); addProcedure.Dock = DockStyle.Right; addProcedure.Width = 165;
                addProcedure.Click += async (_, _) => { if (await CreateProcedure()) { await LoadSection("Cirugías"); await EditSurgery(null); } };
                toolbar.Controls.Add(addProcedure);
            }
            var add = Button(name == "Pacientes" ? "+  Nuevo paciente" : name == "Citas" ? "+  Nueva cita" : name == "Cirugías" ? "+  Nueva cirugía" : "+  Nuevo pago", Mauve, Color.White); add.Dock = DockStyle.Right; add.Width = 175;
            add.Click += async (_, _) => { if (name == "Pacientes") await EditPatient(null); else if (name == "Citas") await EditAppointment(null); else if (name == "Cirugías") await EditSurgery(null); else await EditPayment(null); };
            toolbar.Controls.Add(add);
        }
        _pageContent.Controls.Add(toolbar);
        _grid = CreateGrid(); _grid.Dock = DockStyle.Fill; _pageContent.Controls.Add(_grid); _pageContent.Controls.SetChildIndex(_grid, 1);
        var statusBar = new Panel { Dock = DockStyle.Bottom, Height = 36 }; _status.Text = ""; statusBar.Controls.Add(_status); _pageContent.Controls.Add(statusBar);
        search.TextChanged += (_, _) => ApplySearch(search.Text);
        await LoadSection(name);
    }

    private async Task ShowDashboard()
    {
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, Padding = new Padding(0, 8, 0, 0), BackColor = Color.Transparent };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 125));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        var dashboardStatus = new Label { Text = "Consultando información real de la API…", Dock = DockStyle.Fill, ForeColor = Taupe, TextAlign = ContentAlignment.MiddleLeft };
        var metricFlow = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, AutoScroll = true, BackColor = Color.Transparent, Padding = new Padding(0, 0, 0, 8) };
        var patientsCard = DashboardMetric("Total de pacientes", "—", "Registros en el sistema");
        var newPatientsCard = DashboardMetric("Pacientes nuevos", "—", "Registrados este mes");
        var appointmentsCard = DashboardMetric("Citas próximas", "—", "En los próximos 7 días");
        var revenueCard = DashboardMetric("Ingresos del mes", "—", "Pagos recibidos");
        foreach (var card in new[] { patientsCard, newPatientsCard, appointmentsCard, revenueCard }) metricFlow.Controls.Add(card);

        var recentCard = DashboardCard("Pacientes recientes", "Últimos pacientes registrados");
        var recentGrid = CreateGrid(); recentGrid.Dock = DockStyle.Fill; _grid = recentGrid;
        recentCard.Controls.Add(recentGrid);
        var activityCard = DashboardCard("Citas recientes", "Actividad de agenda reciente");
        var activityGrid = CreateGrid(); activityGrid.Dock = DockStyle.Fill;
        activityCard.Controls.Add(activityGrid);
        var lower = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, BackColor = Color.Transparent };
        lower.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55)); lower.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45));
        lower.Controls.Add(recentCard, 0, 0); lower.Controls.Add(activityCard, 1, 0);
        metricFlow.SizeChanged += (_, _) => { var width = Math.Max(145, (metricFlow.ClientSize.Width - 52) / 4); foreach (Control item in metricFlow.Controls) item.Width = width; };
        layout.Controls.Add(metricFlow, 0, 0); layout.Controls.Add(lower, 0, 1); layout.Controls.Add(dashboardStatus, 0, 2);
        _pageContent.Controls.Add(layout); _pageContent.Controls.SetChildIndex(layout, 1);
        try
        {
            var patientTask = _api.GetAsync<List<ApiPaciente>>("api/Pacientes");
            var appointmentTask = _api.GetAsync<List<ApiCita>>("api/Citas");
            var paymentTask = _api.GetAsync<List<ApiEstadoCuentaPago>>("api/Pagos/estado-cuenta");
            await Task.WhenAll(patientTask, appointmentTask, paymentTask);
            var patients = await patientTask;
            var appointments = await appointmentTask;
            var payments = await paymentTask;
            ((Label)patientsCard.Tag!).Text = patients.Count.ToString(CultureInfo.CurrentCulture);
            ((Label)newPatientsCard.Tag!).Text = patients.Count(p => p.FechaRegistro >= new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1)).ToString(CultureInfo.CurrentCulture);
            ((Label)appointmentsCard.Tag!).Text = appointments.Count(c => c.Fecha >= DateTime.Now && c.Fecha <= DateTime.Now.AddDays(7) && !string.Equals(c.Estado, "Cancelada", StringComparison.OrdinalIgnoreCase)).ToString(CultureInfo.CurrentCulture);
            ((Label)revenueCard.Tag!).Text = payments.Where(p => p.EsRecibido && p.Fecha.Month == DateTime.Today.Month && p.Fecha.Year == DateTime.Today.Year).Sum(p => p.Monto).ToString("C", CultureInfo.GetCultureInfo("es-DO"));
            recentGrid.DataSource = patients.OrderByDescending(p => p.FechaRegistro).Take(6).Select(p => new { p.PacienteId, Paciente = $"{p.Nombre} {p.Apellido}", Registrado = p.FechaRegistro.ToString("dd/MM/yyyy"), p.Telefono }).ToList();
            activityGrid.DataSource = appointments.OrderByDescending(c => c.Fecha).Take(6).Select(c => new { Cita = c.CitaId, Paciente = c.Paciente is null ? $"#{c.PacienteId}" : $"{c.Paciente.Nombre} {c.Paciente.Apellido}", Fecha = FormatDateTime12(c.Fecha), Motivo = c.TipoCita, c.Estado }).ToList();
            _calendarAppointments = appointments;
            RefreshRightCalendar();
            dashboardStatus.Text = "Información actualizada desde GestionCirugiaPlastica.Api";
        }
        catch (Exception ex)
        {
            dashboardStatus.ForeColor = Color.Firebrick; dashboardStatus.Text = "No se pudo cargar el resumen desde la API. " + UserMessage(ex);
            ((Label)patientsCard.Tag!).Text = ((Label)newPatientsCard.Tag!).Text = ((Label)appointmentsCard.Tag!).Text = ((Label)revenueCard.Tag!).Text = "—";
        }
    }

    private static Panel DashboardMetric(string title, string value, string caption)
    {
        var card = DashboardCard(title, caption);
        var valueLabel = new Label { Text = value, Dock = DockStyle.Top, Height = 37, ForeColor = Charcoal, Font = new Font("Segoe UI", 20, FontStyle.Bold) };
        card.Tag = valueLabel; card.Dock = DockStyle.None; card.Width = 218; card.Height = 108; card.Margin = new Padding(0, 0, 12, 0); card.Padding = new Padding(14);
        card.Controls.Add(valueLabel);
        return card;
    }

    private static Panel DashboardCard(string title, string subtitle)
    {
        var card = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(18), Margin = new Padding(0, 0, 12, 12) };
        card.Paint += (_, e) => { using var pen = new Pen(Color.FromArgb(231, 221, 224)); e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1); };
        card.Controls.Add(new Label { Text = subtitle, Dock = DockStyle.Top, Height = 24, ForeColor = Taupe, Font = new Font("Segoe UI", 9) });
        card.Controls.Add(new Label { Text = title, Dock = DockStyle.Top, Height = 27, ForeColor = Charcoal, Font = new Font("Segoe UI", 12, FontStyle.Bold) });
        return card;
    }

    private async Task LoadSection(string name)
    {
        try
        {
            _status.Text = "Cargando…";
            _grid?.Columns.Clear();
            switch (name)
            {
                case "Pacientes":
                    _patients = await _api.GetAsync<List<ApiPaciente>>("api/Pacientes");
                    _grid!.DataSource = _patients.Select(p => new { p.PacienteId, Nombre = $"{p.Nombre} {p.Apellido}", p.Cedula, p.Telefono, p.Email, Seguro = p.TieneSeguro ? "Sí" : "No", Registro = p.FechaRegistro.ToString("dd/MM/yyyy") }).ToList();
                    AddRowAction("Editar", async row => { var p = _patients.First(x => x.PacienteId == Convert.ToInt32(row.Cells["PacienteId"].Value)); await EditPatient(p); });
                    if (_api.IsDoctor) AddRowAction("Expediente", async row => await ShowSection("Expedientes", Convert.ToInt32(row.Cells["PacienteId"].Value)));
                    AddRowAction("Eliminar", async row => await DeleteRecord("api/Pacientes/" + row.Cells["PacienteId"].Value, "¿Eliminar este paciente?")); break;
                case "Citas":
                    var citas = await _api.GetAsync<List<ApiCita>>("api/Citas");
                    _grid!.DataSource = citas.Select(c => new { c.CitaId, Paciente = c.Paciente is null ? $"#{c.PacienteId}" : $"{c.Paciente.Nombre} {c.Paciente.Apellido}", c.TipoCita, Fecha = FormatDateTime12(c.Fecha), c.Estado, c.EstadoPago, c.Costo }).ToList();
                    AddRowAction("Editar", async row => { var c = citas.First(x => x.CitaId == Convert.ToInt32(row.Cells["CitaId"].Value)); await EditAppointment(c); });
                    AddRowAction("Estado", async row => await ChangeAppointmentStatus(Convert.ToInt32(row.Cells["CitaId"].Value)));
                    AddRowAction("Eliminar", async row => await DeleteRecord("api/Citas/" + row.Cells["CitaId"].Value, "¿Eliminar esta cita?")); break;
                case "Cirugías":
                    var surgeries = await _api.GetAsync<List<ApiCirugia>>("api/Cirugias");
                    var visibleSurgeries = surgeries.Where(s => _surgeryStatusFilter switch
                    {
                        "Todos los estados" => true,
                        _ => string.Equals(s.Estado, _surgeryStatusFilter, StringComparison.OrdinalIgnoreCase)
                    }).OrderByDescending(s => s.FechaCirugia).ToList();
                    _grid!.DataSource = visibleSurgeries.Select(c => new { c.CirugiaId, Paciente = c.Paciente is null ? $"#{c.PacienteId}" : $"{c.Paciente.Nombre} {c.Paciente.Apellido}", Procedimiento = c.Procedimiento?.Nombre, Fecha = FormatDateTime12(c.FechaCirugia), c.Estado, Descripcion = c.Observaciones }).ToList();
                    AddRowAction("Editar", async row => await EditSurgery(visibleSurgeries.First(x => x.CirugiaId == Convert.ToInt32(row.Cells["CirugiaId"].Value))));
                    AddRowAction("Eliminar", async row => await DeleteRecord("api/Cirugias/" + row.Cells["CirugiaId"].Value, "¿Eliminar esta cirugía?")); break;
                case "Pagos":
                    var accountEntries = await _api.GetAsync<List<ApiEstadoCuentaPago>>("api/Pagos/estado-cuenta");
                    _grid!.DataSource = accountEntries.Select(p => new { p.PagoId, Fecha = FormatDateTime12(p.Fecha), p.Paciente, p.Destino, p.Concepto, p.MetodoPago, p.Estado, Tipo = p.EsRecibido ? "Recibido" : "Saldo pendiente", p.Monto }).ToList();
                    AddRowAction("Registrar / editar", async row =>
                    {
                        var idValue = row.Cells["PagoId"].Value;
                        if (idValue is not null && idValue is not DBNull && Convert.ToInt32(idValue) > 0)
                        {
                            var payment = await _api.GetAsync<ApiPago>($"api/Pagos/{Convert.ToInt32(idValue)}");
                            await EditPayment(payment);
                        }
                        else
                        {
                            var citaId = row.Cells["Destino"].Value?.ToString();
                            var pending = accountEntries.First(x => x.PagoId is null && x.Destino == citaId);
                            await EditPayment(null, pending);
                        }
                    });
                    AddRowAction("Eliminar", async row =>
                    {
                        var idValue = row.Cells["PagoId"].Value;
                        if (idValue is null || idValue is DBNull) { MessageBox.Show(this, "Este saldo es calculado a partir del costo de la cita; no es una transacción guardada que se pueda eliminar."); return; }
                        await DeleteRecord("api/Pagos/" + idValue, "¿Eliminar este pago recibido?");
                    }); break;
            }
            _status.ForeColor = Mauve; _status.Text = $"{_grid?.Rows.Count ?? 0} registros";
        }
        catch (Exception ex)
        {
            if (_grid is not null) _grid.DataSource = null;
            _status.ForeColor = Color.Firebrick; _status.Text = UserMessage(ex);
            MessageBox.Show(this, UserMessage(ex), $"No se pudo cargar {name}", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
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
        _rowActions[column.Name] = action;
        if (!ReferenceEquals(_actionGrid, _grid))
        {
            _grid.CellContentClick += async (sender, e) =>
            {
                if (sender is not DataGridView clickedGrid || e.RowIndex < 0 || e.ColumnIndex < 0) return;
                var columnName = clickedGrid.Columns[e.ColumnIndex].Name;
                if (_rowActions.TryGetValue(columnName, out var rowAction)) await Run(() => rowAction(clickedGrid.Rows[e.RowIndex]));
            };
            _actionGrid = _grid;
        }
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
        var cost = form.AddText("Costo", current?.Costo.ToString("0.00") ?? "0.00"); var state = form.AddChoiceText("Estado", ["Pendiente", "Confirmada", "Llegó", "Atendida", "Completada", "Cancelada"], current?.Estado ?? "Pendiente");
        var payment = form.AddChoiceText("Estado de pago", ["Pendiente", "Pagado", "Parcial"], current?.EstadoPago ?? "Pendiente"); var notes = form.AddText("Observaciones", current?.Observaciones, multiline: true);
        if (form.ShowDialog(this) != DialogResult.OK) return;
        if (patient.SelectedItem is not ChoiceItem selected || !decimal.TryParse(cost.Text, out var amount)) { MessageBox.Show(this, "Selecciona un paciente e indica un costo válido.", "Validación"); return; }
        var payload = new { CitaId = current?.CitaId ?? 0, Fecha = date.Value, PacienteId = selected.Id, TipoCita = EmptyNull(type.Text), Costo = amount, Estado = state.Text, EstadoPago = payment.Text, Observaciones = EmptyNull(notes.Text), CitaAnteriorId = current?.CitaAnteriorId };
        await Run(async () => { await _api.SendAsync(current is null ? HttpMethod.Post : HttpMethod.Put, current is null ? "api/Citas" : $"api/Citas/{current.CitaId}", payload); MessageBox.Show(this, "Cita guardada correctamente.", "Tanger Clinic", MessageBoxButtons.OK, MessageBoxIcon.Information); await LoadSection("Citas"); });
    }

    private async Task EditSurgery(ApiCirugia? current, Func<Task>? refresh = null)
    {
        try
        {
            var patients = await _api.GetAsync<List<ApiPaciente>>("api/Pacientes/expediente/selector");
            var procedures = await _api.GetAsync<List<ApiProcedimiento>>("api/Procedimientos");
            if (patients.Count == 0)
            {
                MessageBox.Show(this, "El selector autorizado de pacientes no devolvió registros. No se puede asociar una cirugía sin un paciente.", "Sin pacientes disponibles", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (procedures.Count == 0)
            {
                if (MessageBox.Show(this, "El catálogo no tiene procedimientos. ¿Quieres agregar uno ahora (por ejemplo, Blefaroplastia)?", "Sin procedimientos disponibles", MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes && await CreateProcedure())
                {
                    if (refresh is null) await LoadSection("Cirugías"); else await refresh();
                    await EditSurgery(null, refresh);
                }
                return;
            }
            using var form = new EntityDialog(current is null ? "Registrar cirugía" : "Editar cirugía");
            var patient = form.AddChoice("Paciente", patients.Select(p => new ChoiceItem(p.PacienteId, $"{p.Nombre} {p.Apellido} · #{p.PacienteId}")).ToList(), current?.PacienteId);
            var procedure = form.AddChoice("Tipo de cirugía / procedimiento", procedures.Select(p => new ChoiceItem(p.ProcedimientoId, p.Nombre)).ToList(), current?.ProcedimientoId);
            var date = form.AddDateTime("Fecha", current?.FechaCirugia ?? DateTime.Now);
            var state = form.AddChoiceText("Estado", ["Programada", "Pendiente", "Completada", "Cancelada"], current?.Estado ?? "Programada");
            var notes = form.AddText("Descripción del procedimiento / observaciones médicas", current?.Observaciones, true);
            if (form.ShowDialog(this) != DialogResult.OK) return;
            if (patient.SelectedItem is not ChoiceItem selectedPatient || procedure.SelectedItem is not ChoiceItem selectedProcedure) throw new InvalidOperationException("Selecciona el paciente y el procedimiento.");
            var payload = new { CirugiaId = current?.CirugiaId ?? 0, PacienteId = selectedPatient.Id, ProcedimientoId = selectedProcedure.Id, FechaCirugia = date.Value, Estado = state.Text, DiagnosticoPreoperatorio = current?.DiagnosticoPreoperatorio, Observaciones = EmptyNull(notes.Text) };
            await Run(async () => { await _api.SendAsync(current is null ? HttpMethod.Post : HttpMethod.Put, current is null ? "api/Cirugias" : $"api/Cirugias/{current.CirugiaId}", payload); MessageBox.Show(this, "Cirugía guardada.", "Tanger Clinic"); if (refresh is null) await LoadSection("Cirugías"); else await refresh(); });
        }
        catch (Exception ex) { MessageBox.Show(this, UserMessage(ex), "No se pudo abrir el formulario", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
    }

    private async Task<bool> CreateProcedure()
    {
        var names = new[] { "Blefaroplastia", "Rinoplastia", "Abdominoplastia", "Liposucción", "Mamoplastia de aumento", "Mamoplastia de reducción", "Otoplastia", "Lifting facial", "Otro" };
        using var form = new EntityDialog("Agregar procedimiento al catálogo");
        var choice = form.AddChoiceText("Procedimiento", names, names[0]);
        var customName = form.AddText("Nombre personalizado (si seleccionaste Otro)");
        var description = form.AddText("Descripción", multiline: true);
        var price = form.AddText("Precio base (monto definido por la clínica)", "0.00");
        if (form.ShowDialog(this) != DialogResult.OK) return false;
        var name = choice.Text == "Otro" ? customName.Text.Trim() : choice.Text;
        if (string.IsNullOrWhiteSpace(name) || !decimal.TryParse(price.Text, NumberStyles.Number, CultureInfo.CurrentCulture, out var amount) || amount <= 0)
        {
            MessageBox.Show(this, "Indica el nombre y un precio base mayor que cero.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return false;
        }
        try
        {
            await _api.SendAsync(HttpMethod.Post, "api/Procedimientos", new { ProcedimientoId = 0, FechaRegistro = DateTime.Now, Nombre = name, Descripcion = EmptyNull(description.Text), PrecioBase = amount });
            MessageBox.Show(this, "Procedimiento agregado al catálogo.", "Tanger Clinic", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return true;
        }
        catch (Exception ex) { MessageBox.Show(this, UserMessage(ex), "No se pudo agregar el procedimiento", MessageBoxButtons.OK, MessageBoxIcon.Warning); return false; }
    }

    private async Task EditPayment(ApiPago? current, ApiEstadoCuentaPago? pending = null)
    {
        try
        {
            var patients = await _api.GetAsync<List<ApiPaciente>>("api/Pacientes");
            using var form = new EntityDialog(current is null ? "Registrar pago" : "Editar pago");
            var patient = form.AddChoice("Paciente", patients.Select(p => new ChoiceItem(p.PacienteId, $"{p.Nombre} {p.Apellido} · #{p.PacienteId}")).ToList(), current?.PacienteId ?? current?.Cita?.PacienteId ?? current?.Cirugia?.PacienteId ?? pending?.PacienteId);
            var serviceNames = new[]
            {
                "Consulta",
                "Cirugía: Blefaroplastia",
                "Cirugía: Liposucción",
                "Cirugía: Rinoplastia",
                "Cirugía: Abdominoplastia",
                "Cirugía: Mamoplastia de aumento",
                "Cirugía: Mamoplastia de reducción",
                "Cirugía: Otoplastia",
                "Cirugía: Lifting facial",
                "Cirugía: Otro procedimiento"
            };
            var serviceChoices = serviceNames.Select((name, index) => new ChoiceItem(index, name)).ToList();
            var selectedService = serviceChoices.FirstOrDefault(option => string.Equals(option.Label, current?.Concepto, StringComparison.OrdinalIgnoreCase));
            if (selectedService is null && !string.IsNullOrWhiteSpace(current?.Concepto))
            {
                selectedService = new ChoiceItem(serviceChoices.Count, current.Concepto);
                serviceChoices.Add(selectedService);
            }
            var defaultServiceId = selectedService?.Id ?? (current?.CirugiaId is not null ? serviceChoices.FindIndex(option => option.Label == "Cirugía: Otro procedimiento") : 0);
            var service = form.AddChoice("Tipo de servicio", serviceChoices, defaultServiceId);
            var amount = form.AddText("Monto recibido", current?.Monto.ToString("0.00") ?? "0.00"); var date = form.AddDateTime("Fecha", current?.Fecha ?? DateTime.Now);
            var method = form.AddChoiceText("Método de pago", ["Efectivo", "Tarjeta", "Transferencia", "Cheque", "Seguro", "Otro"], current?.MetodoPago ?? "Efectivo");
            var state = form.AddChoiceText("Estado", ["Pagado", "Pendiente"], current?.Estado ?? "Pagado");
            var details = form.AddText("Detalle adicional (opcional)", pending is null ? null : "Abono de consulta", true);
            if (form.ShowDialog(this) != DialogResult.OK) return;
            if (patient.SelectedItem is not ChoiceItem p || !decimal.TryParse(amount.Text, NumberStyles.Number, CultureInfo.CurrentCulture, out var value) || value <= 0) throw new InvalidOperationException("Selecciona un paciente e indica un monto mayor que cero.");
            var selectedServiceLabel = (service.SelectedItem as ChoiceItem)?.Label ?? throw new InvalidOperationException("Selecciona Consulta o un tipo de cirugía.");
            var detail = EmptyNull(details.Text);
            if (selectedServiceLabel == "Cirugía: Otro procedimiento" && detail is null)
                throw new InvalidOperationException("Describe el procedimiento seleccionado como otro.");
            var concept = detail is null ? selectedServiceLabel : $"{selectedServiceLabel} · {detail}";
            var citaId = current?.CitaId ?? pending?.CitaId;
            var surgeryId = current?.CirugiaId ?? pending?.CirugiaId;
            await Run(async () => { await _api.SendAsync(HttpMethod.Post, "api/Pagos", new { PagoId = current?.PagoId ?? 0, PacienteId = p.Id, CitaId = citaId, CirugiaId = surgeryId, Fecha = date.Value, Monto = value, MontoSeguro = 0m, Estado = state.Text, MetodoPago = method.Text, Concepto = concept }); MessageBox.Show(this, "Pago guardado.", "Tanger Clinic"); await LoadSection("Pagos"); });
        }
        catch (Exception ex) { MessageBox.Show(this, UserMessage(ex), "No se pudo abrir el formulario", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
    }

    private async Task DeleteRecord(string path, string prompt, Func<Task>? refresh = null)
    {
        if (MessageBox.Show(this, prompt, "Confirmar eliminación", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
        await Run(async () => { await _api.DeleteAsync(path); MessageBox.Show(this, "Registro eliminado.", "Tanger Clinic"); if (refresh is null) await LoadSection(_section); else await refresh(); });
    }

    private async Task ChangeAppointmentStatus(int id)
    {
        var states = new[] { "Pendiente", "Confirmada", "Llegó", "Atendida", "Completada", "Cancelada" };
        using var form = new EntityDialog($"Estado de la cita #{id}"); var state = form.AddChoiceText("Nuevo estado", states, states[0]);
        if (form.ShowDialog(this) != DialogResult.OK) return;
        await Run(async () => { await _api.SendAsync(HttpMethod.Patch, $"api/Citas/{id}/estado", state.Text); await LoadSection("Citas"); });
    }

    private async Task ShowRecipes()
    {
        AttachStatusBar();
        var tools = new Panel { Dock = DockStyle.Top, Height = 52, Padding = new Padding(0, 4, 0, 8) };
        var search = new TextBox { PlaceholderText = "Filtrar por paciente o medicamento", Dock = DockStyle.Left, Width = 320, ForeColor = Charcoal, BackColor = Color.White };
        var add = Button("Redactar receta", Mauve, Color.White); add.Dock = DockStyle.Right; add.Width = 160;
        var refresh = Button("Actualizar", Color.White, Mauve); refresh.Dock = DockStyle.Right; refresh.Width = 120;
        tools.Controls.Add(search); tools.Controls.Add(refresh); tools.Controls.Add(add);
        var grid = CreateGrid(); grid.Dock = DockStyle.Fill; _grid = grid;
        _pageContent.Controls.Add(grid); _pageContent.Controls.SetChildIndex(grid, 1); _pageContent.Controls.Add(tools);
        List<ApiReceta> recipes = [];
        async Task LoadRecipes()
        {
            try
            {
                var patients = await _api.GetAsync<List<ApiPaciente>>("api/Pacientes/expediente/selector");
                recipes = await _api.GetAsync<List<ApiReceta>>("api/Recetas");
                var names = patients.ToDictionary(p => p.PacienteId, p => $"{p.Nombre} {p.Apellido}");
                grid.Columns.Clear(); grid.DataSource = recipes.OrderByDescending(x => x.Fecha).Select(r => new { r.RecetaId, Paciente = names.GetValueOrDefault(r.PacienteId, $"Paciente #{r.PacienteId}"), Fecha = r.Fecha.ToString("dd/MM/yyyy"), Medicamentos = string.Join(", ", r.DetallesReceta.Select(d => d.Medicamento)), r.Indicaciones }).ToList();
                AddRowAction("Editar", async row => await EditRecipe(recipes.First(x => x.RecetaId == Convert.ToInt32(row.Cells["RecetaId"].Value)), LoadRecipes));
                _status.ForeColor = Mauve; _status.Text = $"{recipes.Count} recetas";
            }
            catch (Exception ex) { grid.DataSource = null; _status.ForeColor = Color.Firebrick; _status.Text = UserMessage(ex); MessageBox.Show(this, UserMessage(ex), "No se pudieron cargar recetas", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
        }
        add.Click += async (_, _) => await EditRecipe(null, LoadRecipes);
        refresh.Click += async (_, _) => await LoadRecipes();
        search.TextChanged += (_, _) => ApplySearch(search.Text);
        await LoadRecipes();
    }

    private async Task EditRecipe(ApiReceta? current, Func<Task> refresh)
    {
        try
        {
            var patients = await _api.GetAsync<List<ApiPaciente>>("api/Pacientes/expediente/selector");
            using var form = new EntityDialog(current is null ? "Redactar receta" : "Editar receta");
            var patient = form.AddChoice("Paciente", patients.Select(p => new ChoiceItem(p.PacienteId, $"{p.Nombre} {p.Apellido}")).ToList(), current?.PacienteId);
            var date = form.AddDate("Fecha", current?.Fecha ?? DateTime.Today);
            var instructions = form.AddText("Indicaciones médicas / postoperatorias", current?.Indicaciones, true);
            var details = new List<(ApiDetalleReceta Source, TextBox Drug, TextBox Dose, TextBox Freq, TextBox Duration, TextBox Notes)>();
            void AddDetail(ApiDetalleReceta? source = null)
            {
                source ??= new ApiDetalleReceta();
                details.Add((source, form.AddText("Medicamento", source.Medicamento), form.AddText("Dosis", source.Dosis), form.AddText("Frecuencia", source.Frecuencia), form.AddText("Duración", source.Duracion), form.AddText("Instrucciones", source.Instrucciones)));
            }
            foreach (var detail in current?.DetallesReceta ?? []) AddDetail(detail);
            if (details.Count == 0) AddDetail();
            form.AddAction("Agregar medicamento", () => AddDetail());
            if (form.ShowDialog(this) != DialogResult.OK) return;
            if (patient.SelectedItem is not ChoiceItem selected || details.Any(d => string.IsNullOrWhiteSpace(d.Drug.Text))) throw new InvalidOperationException("Selecciona un paciente e ingresa el nombre de cada medicamento.");
            var payload = new { RecetaId = current?.RecetaId ?? 0, Fecha = date.Value, PacienteId = selected.Id, ConsultaId = current?.ConsultaId, CirugiaId = current?.CirugiaId, Indicaciones = EmptyNull(instructions.Text), DetallesReceta = details.Select(d => new { d.Source.DetalleRecetaId, Medicamento = d.Drug.Text.Trim(), Dosis = EmptyNull(d.Dose.Text), Frecuencia = EmptyNull(d.Freq.Text), Duracion = EmptyNull(d.Duration.Text), Instrucciones = EmptyNull(d.Notes.Text) }).ToList() };
            await Run(async () => { await _api.SendAsync(HttpMethod.Post, "api/Recetas", payload); MessageBox.Show(this, "Receta guardada.", "Tanger Clinic", MessageBoxButtons.OK, MessageBoxIcon.Information); await refresh(); });
        }
        catch (Exception ex) { MessageBox.Show(this, UserMessage(ex), "No se pudo abrir el formulario", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
    }

    private async Task ShowInsurers()
    {
        AttachStatusBar();
        var tools = new Panel { Dock = DockStyle.Top, Height = 52, Padding = new Padding(0, 4, 0, 8) };
        var add = Button("Agregar aseguradora", Mauve, Color.White); add.Dock = DockStyle.Right; add.Width = 180;
        var refresh = Button("Actualizar", Color.White, Mauve); refresh.Dock = DockStyle.Right; refresh.Width = 120; tools.Controls.Add(add); tools.Controls.Add(refresh);
        var grid = CreateGrid(); grid.Dock = DockStyle.Fill; _grid = grid; _pageContent.Controls.Add(grid); _pageContent.Controls.SetChildIndex(grid, 1); _pageContent.Controls.Add(tools);
        List<ApiSeguro> insurers = [];
        async Task LoadInsurers()
        {
            try
            {
                insurers = await _api.GetAsync<List<ApiSeguro>>("api/Seguros");
                grid.Columns.Clear(); grid.DataSource = insurers.Select(s => new { s.SeguroId, Aseguradora = s.Nombre, s.Telefono, s.Email, s.Direccion, Estado = s.Activo ? "Activa" : "Inactiva" }).ToList();
                AddRowAction("Editar", async row => await EditInsurer(insurers.First(s => s.SeguroId == Convert.ToInt32(row.Cells["SeguroId"].Value)), LoadInsurers));
                AddRowAction("Estado", async row =>
                {
                    var insurer = insurers.First(s => s.SeguroId == Convert.ToInt32(row.Cells["SeguroId"].Value));
                    var action = insurer.Activo ? "desactivar" : "activar";
                    await Run(async () => { await _api.PatchAsync($"api/Seguros/{insurer.SeguroId}/{action}"); await LoadInsurers(); });
                });
                _status.ForeColor = Mauve; _status.Text = $"{insurers.Count} aseguradoras";
            }
            catch (Exception ex) { grid.DataSource = null; _status.ForeColor = Color.Firebrick; _status.Text = UserMessage(ex); MessageBox.Show(this, UserMessage(ex), "No se pudieron cargar aseguradoras", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
        }
        add.Click += async (_, _) => await EditInsurer(null, LoadInsurers);
        refresh.Click += async (_, _) => await LoadInsurers();
        await LoadInsurers();
    }

    private async Task EditInsurer(ApiSeguro? current, Func<Task> refresh)
    {
        using var form = new EntityDialog(current is null ? "Agregar aseguradora" : "Editar aseguradora");
        var name = form.AddText("Nombre *", current?.Nombre); var phone = form.AddText("Teléfono", current?.Telefono); var email = form.AddText("Correo", current?.Email); var address = form.AddText("Dirección", current?.Direccion);
        if (form.ShowDialog(this) != DialogResult.OK) return;
        if (string.IsNullOrWhiteSpace(name.Text)) { MessageBox.Show(this, "El nombre es obligatorio.", "Validación"); return; }
        await Run(async () => { await _api.SendAsync(current is null ? HttpMethod.Post : HttpMethod.Put, current is null ? "api/Seguros" : $"api/Seguros/{current.SeguroId}", new { SeguroId = current?.SeguroId ?? 0, FechaRegistro = current?.FechaRegistro ?? DateTime.Now, Nombre = name.Text.Trim(), Telefono = EmptyNull(phone.Text), Email = EmptyNull(email.Text), Direccion = EmptyNull(address.Text), Activo = true }); MessageBox.Show(this, "Aseguradora guardada.", "Tanger Clinic"); await refresh(); });
    }

    private async Task ShowConfiguration()
    {
        AttachStatusBar();
        var tools = new Panel { Dock = DockStyle.Top, Height = 50 };
        var search = new TextBox { PlaceholderText = "Buscar por nombre, usuario o correo", Dock = DockStyle.Left, Width = 330, ForeColor = Charcoal, BackColor = Color.White }; tools.Controls.Add(search);
        var refresh = Button("Actualizar", Color.White, Mauve); refresh.Dock = DockStyle.Right; refresh.Width = 120; tools.Controls.Add(refresh);
        var grid = CreateGrid(); grid.Dock = DockStyle.Fill; _grid = grid; _pageContent.Controls.Add(grid); _pageContent.Controls.SetChildIndex(grid, 1); _pageContent.Controls.Add(tools);
        try
        {
            var users = await _api.GetAsync<List<ApiUsuario>>("api/Usuarios"); var roles = await _api.GetAsync<List<ApiRol>>("api/Roles");
            var allowed = new[] { "Doctora", "AsistenteClinico", "Secretaria", "Paciente" };
            roles = roles.Where(r => allowed.Contains(r.Nombre, StringComparer.OrdinalIgnoreCase)).ToList();
            grid.DataSource = users.Select(u => new { u.UsuarioId, Usuario = $"{u.Nombre} {u.Apellido} (@{u.NombreUsuario})", u.Email, Rol = roles.FirstOrDefault(r => r.RolId == u.RolId)?.Nombre ?? $"#{u.RolId}", Estado = u.Activo ? "Activo" : "Inactivo" }).ToList();
            AddRowAction("Rol", async row =>
            {
                var user = users.First(u => u.UsuarioId == Convert.ToInt32(row.Cells["UsuarioId"].Value));
                using var form = new EntityDialog($"Cambiar rol · {user.Nombre} {user.Apellido}");
                var choice = form.AddChoice("Rol del usuario", roles.Select(r => new ChoiceItem(r.RolId, r.Nombre)).ToList(), user.RolId);
                if (form.ShowDialog(this) != DialogResult.OK || choice.SelectedItem is not ChoiceItem selected || selected.Id == user.RolId) return;
                await Run(async () => { await _api.PatchAsync($"api/Usuarios/{user.UsuarioId}/rol?nuevoRolId={selected.Id}"); await ShowSection("Configuración"); });
            });
            AddRowAction("Estado", async row =>
            {
                var user = users.First(u => u.UsuarioId == Convert.ToInt32(row.Cells["UsuarioId"].Value));
                var action = user.Activo ? "desactivar" : "activar";
                if (MessageBox.Show(this, $"¿Deseas {action} la cuenta de {user.Nombre} {user.Apellido}?", "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
                await Run(async () => { await _api.PatchAsync($"api/Usuarios/{user.UsuarioId}/{action}"); await ShowSection("Configuración"); });
            });
            _status.ForeColor = Mauve; _status.Text = $"{users.Count} usuarios";
            search.TextChanged += (_, _) => ApplySearch(search.Text);
            refresh.Click += async (_, _) => await ShowSection("Configuración");
        }
        catch (Exception ex) { grid.DataSource = null; _status.ForeColor = Color.Firebrick; _status.Text = UserMessage(ex); MessageBox.Show(this, UserMessage(ex), "No se pudo cargar la configuración", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
    }

    private async Task ShowMyProfile()
    {
        AttachStatusBar();
        var panel = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(24) };
        var message = new Label { Dock = DockStyle.Bottom, Height = 40, ForeColor = Mauve };
        panel.Controls.Add(message); _pageContent.Controls.Add(panel);
        try
        {
            var profile = await _api.GetAsync<JsonElement>("api/Perfil");
            var name = panelAdd("Nombre completo", ReadText(profile, "nombreCompleto"));
            var username = panelAdd("Nombre de usuario", ReadText(profile, "nombreUsuario"), true);
            var email = panelAdd("Correo electrónico", ReadText(profile, "email"), true);
            var role = panelAdd("Rol", ReadText(profile, "rol"), true);
            var currentPassword = panelAdd("Contraseña actual (opcional)", null, false, true);
            var newPassword = panelAdd("Nueva contraseña (mínimo 12 caracteres)", null, false, true);
            var save = Button("Guardar cambios", Mauve, Color.White); save.Dock = DockStyle.Top; save.Height = 42; save.Click += async (_, _) =>
            {
                if (string.IsNullOrWhiteSpace(name.Text)) { message.Text = "El nombre completo es obligatorio."; return; }
                if (!string.IsNullOrWhiteSpace(newPassword.Text) && (newPassword.Text.Length < 12 || string.IsNullOrWhiteSpace(currentPassword.Text))) { message.Text = "Para cambiar la contraseña debes introducir la actual y una nueva de al menos 12 caracteres."; return; }
                await Run(async () =>
                {
                    var result = await _api.SendForJsonAsync<JsonElement>(HttpMethod.Put, "api/Perfil", new { NombreCompleto = name.Text.Trim(), PasswordActual = EmptyNull(currentPassword.Text), PasswordNueva = EmptyNull(newPassword.Text) });
                    var token = ReadText(result, "accessToken"); if (!string.IsNullOrWhiteSpace(token)) _api.SetAccessToken(token);
                    _api.SetDisplayName(ReadText(result, "nombreCompleto") ?? name.Text.Trim());
                    if (_rightProfile is not null) _rightProfile.Text = $"{ReadText(result, "nombreCompleto")}\r\n{ReadText(result, "rol")}";
                    currentPassword.Clear(); newPassword.Clear(); message.Text = "Perfil actualizado.";
                });
            };
            var photo = Button("Seleccionar foto de perfil", Color.White, Mauve); photo.Dock = DockStyle.Top; photo.Height = 40; photo.Click += async (_, _) =>
            {
                using var picker = new OpenFileDialog { Filter = "Imágenes|*.jpg;*.jpeg;*.png;*.webp" };
                if (picker.ShowDialog(this) != DialogResult.OK) return;
                var info = new FileInfo(picker.FileName); if (info.Length > 5 * 1024 * 1024) { message.Text = "La imagen no puede superar 5 MB."; return; }
                await Run(async () => { await _api.SendFileAsync("api/Perfil/foto", picker.FileName); message.Text = "Foto de perfil actualizada."; });
            };
            panel.Controls.Add(photo); panel.Controls.Add(save);
            TextBox panelAdd(string label, string? value, bool readOnly = false, bool password = false)
            {
                var caption = new Label { Text = label, Dock = DockStyle.Top, Height = 23, ForeColor = Charcoal, Font = new Font("Segoe UI", 9, FontStyle.Bold) };
                var input = new TextBox { Text = value ?? "", Dock = DockStyle.Top, Height = 34, ReadOnly = readOnly, UseSystemPasswordChar = password, ForeColor = Charcoal, BackColor = readOnly ? Color.FromArgb(248, 247, 247) : Color.White, Margin = new Padding(0, 0, 0, 10) };
                panel.Controls.Add(input); panel.Controls.Add(caption); return input;
            }
        }
        catch (Exception ex) { message.Text = UserMessage(ex); }
    }

    private async Task ShowPatientHome()
    {
        var panel = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(24) };
        var greeting = new Label { Text = "Cargando información de tu portal…", Dock = DockStyle.Top, Height = 80, ForeColor = Charcoal, Font = new Font("Segoe UI", 17, FontStyle.Bold) };
        var details = new Label { Dock = DockStyle.Top, Height = 100, ForeColor = Taupe, Font = new Font("Segoe UI", 10) };
        var openPortal = Button("Ver mi perfil, citas y recetas", Mauve, Color.White); openPortal.Dock = DockStyle.Top; openPortal.Height = 42; openPortal.Click += async (_, _) => await ShowSection("Mi perfil, citas y recetas");
        panel.Controls.Add(openPortal); panel.Controls.Add(details); panel.Controls.Add(greeting); _pageContent.Controls.Add(panel);
        try
        {
            var profile = await _api.GetAsync<JsonElement>("api/PortalPaciente/perfil");
            var appointments = await _api.GetAsync<List<JsonElement>>("api/PortalPaciente/citas");
            greeting.Text = $"Hola, {ReadText(profile, "nombre")} {ReadText(profile, "apellido")}";
            details.Text = $"Tienes {appointments.Count(c => FindProperty(c, "fecha").ValueKind == JsonValueKind.String && DateTime.TryParse(ReadText(c, "fecha"), out var date) && date >= DateTime.Today)} cita(s) desde hoy.\r\nDesde tu portal puedes consultar citas, recetas, información personal y solicitar una cita.";
        }
        catch (Exception ex) { greeting.Text = "No se pudo cargar tu inicio"; details.Text = UserMessage(ex); }
    }

    private void ShowRecordSearch(int? preselectedPatientId = null)
    {
        var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(14), BackColor = Color.White };
        var toolbar = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 112, WrapContents = true, FlowDirection = FlowDirection.LeftToRight, BackColor = Color.White, Padding = new Padding(0, 4, 0, 8) };
        var patientPicker = new ComboBox { Width = 225, Height = 36, DropDownStyle = ComboBoxStyle.DropDownList, ForeColor = Charcoal, BackColor = Color.White, Margin = new Padding(0, 4, 8, 4) };
        var search = new TextBox { PlaceholderText = "Nombre, cédula o ID", Width = 205, Height = 36, ForeColor = Charcoal, BackColor = Color.White, Margin = new Padding(0, 4, 8, 4) };
        var button = Button("Consultar expediente", Mauve, Color.White); button.Width = 165; button.Height = 38; button.Margin = new Padding(0, 4, 8, 4);
        var result = new TextBox { Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, Dock = DockStyle.Fill, BorderStyle = BorderStyle.None, BackColor = Color.White, ForeColor = Charcoal, Font = new Font("Segoe UI", 10) };
        JsonElement? loadedRecord = null;
        button.Click += async (_, _) =>
        {
            var selected = patientPicker.SelectedItem as ChoiceItem;
            var criterion = selected is not null ? selected.Id.ToString() : search.Text.Trim();
            if (string.IsNullOrWhiteSpace(criterion)) { MessageBox.Show(this, "Selecciona un paciente o escribe un criterio de búsqueda."); return; }
            try { var data = await _api.GetAsync<JsonElement>($"api/Pacientes/expediente/{Uri.EscapeDataString(criterion)}"); loadedRecord = data; result.Text = FormatRecord(data); }
            catch (Exception ex) { result.Text = UserMessage(ex); }
        };
        var edit = Button("Editar antecedentes clínicos", Color.White, Mauve); edit.Width = 190; edit.Height = 38; edit.Margin = new Padding(0, 4, 8, 4); edit.Visible = _api.IsDoctor;
        edit.Click += async (_, _) => { if (loadedRecord is { } record) await EditClinicalHistory(record, result); else MessageBox.Show(this, "Consulta primero el expediente del paciente."); };
        var addConsultation = Button("Nueva consulta", Color.White, Mauve); addConsultation.Width = 150; addConsultation.Height = 38; addConsultation.Margin = new Padding(0, 4, 8, 4); addConsultation.Visible = _api.IsClinicalStaff;
        addConsultation.Click += async (_, _) => { if (loadedRecord is { } record) await AddConsultation(record, result); else MessageBox.Show(this, "Consulta primero el expediente del paciente."); };
        search.KeyDown += (_, e) => { if (e.KeyCode == Keys.Enter) button.PerformClick(); };
        toolbar.Controls.AddRange([patientPicker, search, button, edit, addConsultation]);
        panel.Controls.Add(result); panel.Controls.Add(toolbar); _pageContent.Controls.Add(panel);
        _ = LoadExpedientePatients(patientPicker, result, preselectedPatientId);
    }

    private async Task LoadExpedientePatients(ComboBox picker, TextBox result, int? preselectedPatientId = null)
    {
        try
        {
            var patients = await _api.GetAsync<List<ApiPaciente>>("api/Pacientes/expediente/selector");
            var choices = patients.Select(p => new ChoiceItem(p.PacienteId, $"{p.Nombre} {p.Apellido} · #{p.PacienteId}")).ToList();
            picker.DataSource = choices;
            if (preselectedPatientId.HasValue) picker.SelectedItem = choices.FirstOrDefault(choice => choice.Id == preselectedPatientId.Value);
            if (patients.Count == 0) result.Text = "No hay pacientes disponibles para consultar.";
        }
        catch (Exception ex)
        {
            result.Text = UserMessage(ex);
            MessageBox.Show(this, UserMessage(ex), "No se pudo cargar el selector de pacientes", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private async Task EditClinicalHistory(JsonElement record, TextBox result)
    {
        var patient = FindProperty(record, "paciente"); var history = FindProperty(record, "historial"); var patientId = ReadInt(patient, "pacienteId");
        if (patientId <= 0) { MessageBox.Show(this, "El expediente no contiene un paciente identificable."); return; }
        using var form = new EntityDialog("Antecedentes clínicos");
        var personal = form.AddText("Antecedentes personales", ReadText(history, "antecedentesPersonales") ?? ReadText(history, "antecedentes"), true);
        var family = form.AddText("Antecedentes familiares", ReadText(history, "antecedentesFamiliares"), true); var allergies = form.AddText("Alergias", ReadText(history, "alergias"), true);
        var medicine = form.AddText("Medicamentos actuales", ReadText(history, "medicamentos"), true); var notes = form.AddText("Notas clínicas", ReadText(history, "notasMedicas"), true);
        if (form.ShowDialog(this) != DialogResult.OK) return;
        await Run(async () =>
        {
            await _api.SendAsync(HttpMethod.Post, "api/HistorialClinico", new { HistoriaId = ReadInt(history, "historiaId"), PacienteId = patientId, FechaActualizacion = DateTime.UtcNow, Antecedentes = EmptyNull(personal.Text), AntecedentesPersonales = EmptyNull(personal.Text), AntecedentesFamiliares = EmptyNull(family.Text), Alergias = EmptyNull(allergies.Text), Medicamentos = EmptyNull(medicine.Text), NotasMedicas = EmptyNull(notes.Text) });
            MessageBox.Show(this, "Antecedentes clínicos guardados.", "Tanger Clinic"); var fresh = await _api.GetAsync<JsonElement>($"api/Pacientes/expediente/{patientId}"); result.Text = FormatRecord(fresh);
        });
    }

    private async Task AddConsultation(JsonElement record, TextBox result)
    {
        var patientId = ReadInt(FindProperty(record, "paciente"), "pacienteId");
        if (patientId <= 0) { MessageBox.Show(this, "El expediente no contiene un paciente identificable."); return; }
        using var form = new EntityDialog("Consulta / diagnóstico"); var date = form.AddDateTime("Fecha de consulta", DateTime.Now);
        var reason = form.AddText("Motivo de consulta"); var diagnosis = form.AddText("Diagnóstico", multiline: true); var notes = form.AddText("Observaciones", multiline: true);
        if (form.ShowDialog(this) != DialogResult.OK) return;
        if (string.IsNullOrWhiteSpace(reason.Text) && string.IsNullOrWhiteSpace(diagnosis.Text)) { MessageBox.Show(this, "Indica un motivo o diagnóstico.", "Validación"); return; }
        await Run(async () =>
        {
            await _api.SendAsync(HttpMethod.Post, "api/Consultas", new { ConsultaId = 0, PacienteId = patientId, CitaId = (int?)null, Fecha = date.Value, Motivo = EmptyNull(reason.Text), Diagnostico = EmptyNull(diagnosis.Text), Observaciones = EmptyNull(notes.Text) });
            MessageBox.Show(this, "Consulta registrada.", "Tanger Clinic"); var fresh = await _api.GetAsync<JsonElement>($"api/Pacientes/expediente/{patientId}"); result.Text = FormatRecord(fresh);
        });
    }

    private async Task ShowPatientPortal()
    {
        var tabs = new TabControl { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 10) };
        var profilePage = new TabPage("Perfil"); var appointmentsPage = new TabPage("Citas"); var insurancePage = new TabPage("Seguro"); var recipesPage = new TabPage("Recetas");
        var profile = new Label { Dock = DockStyle.Fill, ForeColor = Charcoal, Font = new Font("Segoe UI", 11), Padding = new Padding(22), AutoEllipsis = true };
        profilePage.Controls.Add(profile);
        var schedule = Button("Solicitar cita", Mauve, Color.White); schedule.Dock = DockStyle.Top; schedule.Height = 42;
        var appointmentGrid = CreateGrid(); appointmentGrid.Dock = DockStyle.Fill;
        appointmentsPage.Controls.Add(appointmentGrid); appointmentsPage.Controls.Add(schedule);
        var insuranceGrid = CreateGrid(); insuranceGrid.Dock = DockStyle.Fill; insurancePage.Controls.Add(insuranceGrid);
        var recipeGrid = CreateGrid(); recipeGrid.Dock = DockStyle.Fill; recipesPage.Controls.Add(recipeGrid);
        tabs.TabPages.AddRange([profilePage, appointmentsPage, insurancePage, recipesPage]);
        var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 12, 0, 0), BackColor = Color.White }; panel.Controls.Add(tabs); _pageContent.Controls.Add(panel);
        try
        {
            var me = await _api.GetAsync<JsonElement>("api/PortalPaciente/perfil");
            profile.Text = $"Hola, {ReadText(me, "nombre")} {ReadText(me, "apellido")}\r\n\r\nCédula: {ReadText(me, "cedula") ?? "—"}\r\nTeléfono: {ReadText(me, "telefono") ?? "—"}\r\nCorreo: {ReadText(me, "email") ?? "—"}\r\nTiene seguro: {(FindProperty(me, "tieneSeguro").ValueKind == JsonValueKind.True ? "Sí" : "No")}";
            var appointments = await _api.GetAsync<List<JsonElement>>("api/PortalPaciente/citas");
            appointmentGrid.DataSource = appointments.OrderBy(c => ReadDate(c, "fecha")).Select(c => new { Cita = ReadInt(c, "citaId"), Fecha = FormatDateTime12(ReadDate(c, "fecha")), Tipo = ReadText(c, "tipoCita"), Estado = ReadText(c, "estado") }).ToList();
            var insurance = await _api.GetAsync<List<JsonElement>>("api/PortalPaciente/seguro");
            insuranceGrid.DataSource = insurance.Select(p => new { Aseguradora = ReadText(p, "seguro"), Poliza = ReadText(p, "numeroPoliza"), Vence = ReadText(p, "fechaVencimiento"), Observaciones = ReadText(p, "observaciones") }).ToList();
            var recipes = await _api.GetAsync<List<JsonElement>>("api/PortalPaciente/recetas");
            recipeGrid.DataSource = recipes.Select(r => new { Receta = ReadInt(r, "recetaId"), Fecha = ReadDate(r, "fecha").ToString("dd/MM/yyyy"), Indicaciones = ReadText(r, "indicaciones"), Medicamentos = FindProperty(r, "medicamentos").ValueKind == JsonValueKind.Array ? string.Join(", ", FindProperty(r, "medicamentos").EnumerateArray().Select(m => ReadText(m, "medicamento"))) : "" }).ToList();
        }
        catch (Exception ex) { profile.Text = UserMessage(ex); }
        schedule.Click += async (_, _) =>
        {
            using var form = new EntityDialog("Solicitar una cita"); var date = form.AddDateTime("Fecha y hora", DateTime.Now.AddDays(1)); var type = form.AddText("Tipo de cita", "Consulta"); var notes = form.AddText("Observaciones", multiline: true);
            if (form.ShowDialog(this) != DialogResult.OK) return;
            await Run(async () => { await _api.SendAsync(HttpMethod.Post, "api/PortalPaciente/citas", new { Fecha = date.Value, TipoCita = EmptyNull(type.Text), Observaciones = EmptyNull(notes.Text) }); MessageBox.Show(this, "Solicitud de cita enviada.", "Tanger Clinic", MessageBoxButtons.OK, MessageBoxIcon.Information); await ShowSection("Mi perfil, citas y recetas"); });
        };
    }

    private static DateTime ReadDate(JsonElement value, string name) => FindProperty(value, name).ValueKind == JsonValueKind.String && DateTime.TryParse(ReadText(value, name), out var date) ? date : DateTime.MinValue;

    private static string FormatRecord(JsonElement root)
    {
        var output = new System.Text.StringBuilder();
        var p = FindProperty(root, "paciente");
        if (p.ValueKind == JsonValueKind.Object) output.AppendLine($"PACIENTE\r\n{ReadText(p, "nombre")} {ReadText(p, "apellido")}  |  ID #{ReadInt(p, "pacienteId")}\r\nCédula: {ReadText(p, "cedula")}  |  Teléfono: {ReadText(p, "telefono")}  |  Correo: {ReadText(p, "email")}\r\n");
        var insurance = FindProperty(root, "seguro"); var policies = FindProperty(insurance, "polizas");
        if (insurance.ValueKind == JsonValueKind.Object) output.AppendLine($"SEGURO\r\nTiene seguro: {FindProperty(insurance, "tieneSeguro").ToString()}\r\nPólizas: {policies}");
        AppendArray(root, "citas", "CITAS", output); AppendArray(root, "operaciones", "CIRUGÍAS", output); AppendArray(root, "consultas", "CONSULTAS Y DIAGNÓSTICOS", output); AppendArray(root, "historial", "HISTORIAL CLÍNICO", output); AppendArray(root, "fotografias", "FOTOGRAFÍAS", output); AppendArray(root, "preoperatorios", "EVALUACIONES PREOPERATORIAS", output); AppendArray(root, "controles", "CONTROLES POSTOPERATORIOS", output); AppendArray(root, "recetas", "RECETAS", output); AppendArray(root, "documentos", "DOCUMENTOS", output);
        return output.ToString();
    }
    private static void AppendArray(JsonElement root, string key, string label, System.Text.StringBuilder output)
    {
        output.AppendLine(label);
        var items = FindProperty(root, key);
        if (items.ValueKind == JsonValueKind.Array && items.GetArrayLength() > 0)
            foreach (var item in items.EnumerateArray()) output.AppendLine("• " + item.ToString());
        else if (items.ValueKind == JsonValueKind.Object) output.AppendLine(items.ToString());
        else output.AppendLine("Sin registros.");
        output.AppendLine();
    }

    private static JsonElement FindProperty(JsonElement value, string name)
    {
        if (value.ValueKind == JsonValueKind.Object)
            foreach (var property in value.EnumerateObject()) if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase)) return property.Value;
        return default;
    }
    private static int ReadInt(JsonElement value, string name) { var item = FindProperty(value, name); return item.ValueKind == JsonValueKind.Number && item.TryGetInt32(out var number) ? number : 0; }
    private static string? ReadText(JsonElement value, string name) { var item = FindProperty(value, name); return item.ValueKind == JsonValueKind.String ? item.GetString() : null; }

    private async Task Run(Func<Task> operation)
    {
        try { await operation(); }
        catch (Exception ex) { MessageBox.Show(this, UserMessage(ex), "Tanger Clinic", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
    }
    private string UserMessage(Exception ex) => ex is HttpRequestException ? $"No se pudo conectar con la API en {_api.BaseAddress}. Verifica que esté iniciada y que su URL base sea correcta." : ex.Message;
    private static string? EmptyNull(string text) => string.IsNullOrWhiteSpace(text) ? null : text.Trim();
    private static string FormatDateTime12(DateTime value) => value.ToString("dd/MM/yyyy hh:mm tt", CultureInfo.InvariantCulture);
    private static Button Button(string text, Color back, Color fore) => new() { Text = text, BackColor = back, ForeColor = fore, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 10, FontStyle.Bold), Cursor = Cursors.Hand, UseVisualStyleBackColor = false };

    protected override void OnFormClosed(FormClosedEventArgs e) { _api.Dispose(); base.OnFormClosed(e); }
}
