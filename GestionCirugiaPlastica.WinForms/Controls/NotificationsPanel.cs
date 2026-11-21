using System.Drawing;
using GestionCirugiaPlastica.WinForms;

namespace GestionCirugiaPlastica.WinForms.Controls;

internal sealed class NotificationsPanel : UserControl
{
    private readonly ClinicApiClient _api;
    private readonly DataGridView _grid = new();
    private readonly Label _status = new() { Dock = DockStyle.Bottom, Height = 32, ForeColor = ClinicTheme.Mauve };
    private List<NotificationItem> _items = [];

    public NotificationsPanel(ClinicApiClient api)
    {
        _api = api;
        Padding = new Padding(16);
        BackColor = Color.White;
        var toolbar = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 48, WrapContents = false };
        AddButton(toolbar, "Actualizar", async () => await LoadAsync());
        AddButton(toolbar, "Marcar todas como leídas", async () =>
        {
            try { await _api.PatchAsync("api/Notificaciones/leer-todas"); await LoadAsync(); }
            catch (Exception ex) { _status.Text = ex.Message; }
        }, true);
        AddButton(toolbar, "Marcar seleccionada como leída", async () =>
        {
            if (_grid.CurrentRow?.Index is not int index || index < 0 || index >= _items.Count) return;
            try { await _api.PatchAsync($"api/Notificaciones/{_items[index].NotificacionId}/leer"); await LoadAsync(); }
            catch (Exception ex) { _status.Text = ex.Message; }
        });
        _grid.Dock = DockStyle.Fill;
        _grid.ReadOnly = true;
        _grid.AllowUserToAddRows = false;
        _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _grid.MultiSelect = false;
        _grid.BackgroundColor = Color.White;
        _grid.BorderStyle = BorderStyle.None;
        _grid.RowTemplate.Height = 42;
        Controls.Add(_grid); Controls.Add(_status); Controls.Add(toolbar);
        _ = LoadAsync();
    }

    private static void AddButton(Control parent, string text, Func<Task> action, bool primary = false)
    {
        var button = new Button { Text = text, AutoSize = true, Height = 36, Margin = new Padding(0, 0, 8, 6), BackColor = primary ? ClinicTheme.Mauve : Color.White, ForeColor = primary ? Color.White : ClinicTheme.Mauve, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9, FontStyle.Bold), Cursor = Cursors.Hand };
        button.Click += async (_, _) => await action();
        parent.Controls.Add(button);
    }

    private async Task LoadAsync()
    {
        try
        {
            _items = await _api.GetAsync<List<NotificationItem>>("api/Notificaciones");
            _grid.DataSource = _items.Select(x => new { x.NotificacionId, x.Titulo, x.Mensaje, Fecha = x.FechaCreacion.ToString("dd/MM/yyyy HH:mm"), Estado = x.Leida ? "Leída" : "Pendiente", x.CitaId }).ToList();
            _status.Text = $"{_items.Count(x => !x.Leida)} notificaciones sin leer de {_items.Count}.";
        }
        catch (Exception ex) { _status.Text = ex.Message; }
    }

    private sealed class NotificationItem
    {
        public int NotificacionId { get; set; }
        public string Tipo { get; set; } = "";
        public string Titulo { get; set; } = "";
        public string Mensaje { get; set; } = "";
        public int? CitaId { get; set; }
        public DateTime FechaCreacion { get; set; }
        public bool Leida { get; set; }
    }
}
