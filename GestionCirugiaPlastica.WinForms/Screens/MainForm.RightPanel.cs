using System.Drawing;
using System.Globalization;
using System.Text.Json;

namespace GestionCirugiaPlastica.WinForms;

internal sealed partial class MainForm
{
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
        if (_api.IsDoctor || _api.IsSecretary || _api.IsAssistant)
        {
            try { _calendarAppointments = await _api.GetAsync<List<ApiCita>>("api/Citas"); RefreshRightCalendar(); }
            catch (Exception ex) { _dayAppointments.Controls.Add(new Label { Text = UserMessage(ex), Width = 245, Height = 58, ForeColor = Color.Firebrick }); }
        }
        else if (_api.IsPatient)
        {
            try { _calendarAppointments = await _api.GetAsync<List<ApiCita>>("api/PortalPaciente/citas"); RefreshRightCalendar(); }
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
        if (selectedAppointments.Count == 0) _dayAppointments.Controls.Add(new Label { Text = _api.IsDoctor || _api.IsSecretary || _api.IsAssistant ? "No hay citas programadas para este día." : "La agenda está disponible para el equipo clínico.", Width = 245, Height = 46, ForeColor = Taupe });
        foreach (var appointment in selectedAppointments)
        {
            var patientName = appointment.Paciente is null ? $"Paciente #{appointment.PacienteId}" : $"{appointment.Paciente.Nombre} {appointment.Paciente.Apellido}";
            _dayAppointments.Controls.Add(new Label { Text = $"{appointment.Fecha:hh:mm tt}\r\n{appointment.TipoCita} · {patientName}\r\n{appointment.Estado}", Width = 250, Height = 65, Padding = new Padding(9), BackColor = Color.FromArgb(250, 247, 248), ForeColor = Charcoal, Font = new Font("Segoe UI", 9) });
        }
    }
}
