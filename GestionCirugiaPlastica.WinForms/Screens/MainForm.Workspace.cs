using System.Drawing;
using GestionCirugiaPlastica.WinForms.Controls;

namespace GestionCirugiaPlastica.WinForms;

internal sealed partial class MainForm
{
    private async Task BuildWorkspace()
    {
        if (_loginResizeHandler is not null)
        {
            _root.Resize -= _loginResizeHandler;
            _loginResizeHandler = null;
        }
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
        var menu = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true, Padding = new Padding(0, 10, 0, 0), BackColor = Color.White };
        _sidebar.Controls.Add(menu);
        if (_api.IsDoctor || _api.IsSecretary)
        {
            AddNav(menu, "Resumen"); AddNav(menu, "Agenda"); AddNav(menu, "Pacientes"); AddNav(menu, "Pagos");
            if (_api.IsDoctor) { AddNav(menu, "Cirugías"); AddNav(menu, "Recetas"); AddNav(menu, "Aseguradoras"); AddNav(menu, "Configuración"); }
        }
        else if (_api.IsAssistant) { AddNav(menu, "Resumen"); AddNav(menu, "Expedientes"); AddNav(menu, "Cirugías"); AddNav(menu, "Recetas"); }
        else if (_api.IsPatient) { AddNav(menu, "Inicio"); AddNav(menu, "Mi perfil, citas y recetas"); }
        if (_api.IsDoctor) AddNav(menu, "Notificaciones");
        AddNav(menu, "Noticias");
        var footer = new Panel { Dock = DockStyle.Bottom, Height = 112, BackColor = Color.White, Padding = new Padding(0, 8, 0, 0) };
        footer.Paint += (_, e) => { using var pen = new Pen(Color.FromArgb(240, 236, 238)); e.Graphics.DrawLine(pen, 0, 0, footer.Width, 0); };
        footer.Controls.Add(new Label { Text = _api.UserName ?? "Usuario", Dock = DockStyle.Top, Height = 26, ForeColor = Charcoal, Font = new Font("Segoe UI", 10, FontStyle.Bold), TextAlign = ContentAlignment.MiddleLeft });
        footer.Controls.Add(new Label { Text = _api.Role ?? "", Dock = DockStyle.Top, Height = 24, ForeColor = Taupe, Font = new Font("Segoe UI", 9), TextAlign = ContentAlignment.MiddleLeft });
        var logout = Button("↪   Cerrar sesión", Color.White, Mauve); logout.Dock = DockStyle.Bottom; logout.Height = 40;
        logout.FlatAppearance.BorderSize = 0; logout.FlatAppearance.MouseOverBackColor = Color.FromArgb(248, 233, 237); logout.TextAlign = ContentAlignment.MiddleLeft;
        logout.Click += (_, _) => { _api.Logout(); ShowLogin(); };
        footer.Controls.Add(logout); _sidebar.Controls.Add(footer);
        var firstSection = _api.IsDoctor || _api.IsSecretary || _api.IsAssistant ? "Resumen" : "Inicio";
        await ShowSection(firstSection);
    }

    private void AddNav(FlowLayoutPanel menu, string name)
    {
        var icon = name switch { "Inicio" or "Resumen" => "◈", "Pacientes" => "♙", "Citas" or "Agenda" => "▦", "Cirugías" => "✚", "Expediente" or "Expedientes" => "▤", "Pagos" => "$", "Mi portal" or "Mi perfil, citas y recetas" => "○", "Recetas" => "Rx", "Aseguradoras" => "♡", "Configuración" => "⚙", "Notificaciones" => "♧", "Noticias" => "N", _ => "•" };
        var selectedStart = _api.IsDoctor || _api.IsSecretary || _api.IsAssistant ? "Resumen" : "Inicio";
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
            await ShowSection(name == "Agenda" ? "Citas" : name);
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
        if (name == "Noticias") { ShowModule(new NewsPanel(_api, _api.IsDoctor || _api.IsSecretary)); return; }
        if (name == "Notificaciones") { ShowModule(new NotificationsPanel(_api)); return; }
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
}
