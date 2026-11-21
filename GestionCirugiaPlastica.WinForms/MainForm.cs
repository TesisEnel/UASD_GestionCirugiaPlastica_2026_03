using System.Drawing;
using System.Globalization;
using System.Text.Json;
using GestionCirugiaPlastica.WinForms.Controls;
using GestionCirugiaPlastica.WinForms.Forms;

namespace GestionCirugiaPlastica.WinForms;

internal sealed partial class MainForm : Form
{
    private static readonly Color Charcoal = ClinicTheme.Charcoal;
    private static readonly Color Taupe = ClinicTheme.Taupe;
    private static readonly Color Mauve = ClinicTheme.Mauve;
    private static readonly Color Rose = ClinicTheme.Rose;
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

    private sealed class ApiDashboardExpense { public DateTime Fecha { get; set; } public decimal Monto { get; set; } }

    protected override void OnFormClosed(FormClosedEventArgs e) { _api.Dispose(); base.OnFormClosed(e); }
}
