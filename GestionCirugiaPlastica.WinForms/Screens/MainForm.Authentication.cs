using System.Drawing;
using GestionCirugiaPlastica.WinForms.Forms;

namespace GestionCirugiaPlastica.WinForms;

internal sealed partial class MainForm
{
    private EventHandler? _loginResizeHandler;

    private void ShowLogin()
    {
        if (_loginResizeHandler is not null)
        {
            _root.Resize -= _loginResizeHandler;
            _loginResizeHandler = null;
        }

        _root.Controls.Clear();

        var card = new Panel
        {
            Size = new Size(460, 590),
            BackColor = Color.White,
            Padding = new Padding(40)
        };

        void CenterCard()
        {
            card.Left = Math.Max(0, (_root.ClientSize.Width - card.Width) / 2);
            card.Top = Math.Max(0, (_root.ClientSize.Height - card.Height) / 2);
        }

        CenterCard();

        _loginResizeHandler = (_, _) => CenterCard();
        _root.Resize += _loginResizeHandler;

        var brand = new Label
        {
            Text = "TANGER CLINIC",
            Font = new Font("Segoe UI", 13, FontStyle.Bold),
            ForeColor = Mauve,
            AutoSize = true,
            Top = 38
        };

        var title = new Label
        {
            Text = "Bienvenido",
            Font = new Font("Segoe UI", 26, FontStyle.Bold),
            ForeColor = Charcoal,
            AutoSize = true,
            Top = 82
        };

        var subtitle = new Label
        {
            Text = "Accede al sistema de gestión clínica",
            ForeColor = Taupe,
            AutoSize = true,
            Top = 128
        };

        var user = new TextBox
        {
            PlaceholderText = "Usuario, correo, cédula o teléfono",
            Left = 0,
            Top = 190,
            Width = 375,
            Height = 40,
            Font = new Font("Segoe UI", 11),
            ForeColor = Charcoal,
            BackColor = Color.White
        };

        var password = new TextBox
        {
            PlaceholderText = "Contraseña",
            UseSystemPasswordChar = true,
            Left = 0,
            Top = 250,
            Width = 375,
            Height = 40,
            Font = new Font("Segoe UI", 11),
            ForeColor = Charcoal,
            BackColor = Color.White
        };

        var showPassword = new CheckBox
        {
            Text = "Mostrar",
            AutoSize = false,
            Width = 72,
            Height = 28,
            ForeColor = Taupe,
            BackColor = Color.Transparent,
            TextAlign = ContentAlignment.MiddleLeft,
            Cursor = Cursors.Hand,
            Top = 256
        };

        showPassword.CheckedChanged += (_, _) =>
            password.UseSystemPasswordChar = !showPassword.Checked;

        var message = new Label
        {
            AutoSize = false,
            Left = 0,
            Top = 305,
            Width = 375,
            Height = 42,
            ForeColor = Color.Firebrick
        };

        var login = Button("Iniciar sesión", Mauve, Color.White);
        login.Left = 0;
        login.Top = 350;
        login.Width = 375;
        login.Height = 48;

        login.Click += async (_, _) =>
        {
            var identifier = user.Text.Trim();

            if (string.IsNullOrWhiteSpace(identifier) ||
                string.IsNullOrWhiteSpace(password.Text))
            {
                message.ForeColor = Color.Firebrick;
                message.Text = "Ingresa tu usuario (o correo) y contraseña.";
                return;
            }

            login.Enabled = false;
            message.Text = "Conectando con la API…";
            message.ForeColor = Taupe;

            try
            {
                await _api.LoginAsync(identifier, password.Text);
                await BuildWorkspace();
            }
            catch (Exception ex)
            {
                message.ForeColor = Color.Firebrick;
                message.Text = UserMessage(ex);
            }
            finally
            {
                login.Enabled = true;
            }
        };

        var register = Button("Crear cuenta de paciente", Color.White, Mauve);
        register.Left = 0;
        register.Top = 410;
        register.Width = 375;
        register.Height = 38;
        register.FlatAppearance.BorderColor = Color.FromArgb(231, 221, 224);

        register.Click += async (_, _) => await RegisterPatient();

        var booking = Button("Agendar cita", Color.White, Mauve);
        booking.Left = 0;
        booking.Top = 460;
        booking.Width = 180;
        booking.Height = 38;
        booking.FlatAppearance.BorderColor = Color.FromArgb(231, 221, 224);

        booking.Click += (_, _) =>
        {
            using var form = new PublicAppointmentForm(_api);
            form.ShowDialog(this);
        };

        var news = Button("Noticias", Color.White, Mauve);
        news.Left = 195;
        news.Top = 460;
        news.Width = 180;
        news.Height = 38;
        news.FlatAppearance.BorderColor = Color.FromArgb(231, 221, 224);

        news.Click += async (_, _) => await ShowPublicNews();

        // Navegación con la tecla Enter.
        user.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                password.Focus();
            }
        };

        password.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                login.PerformClick();
            }
        };

        // Organización y centrado del contenido.
        var contentWidth = 376;
        var contentLeft = (card.ClientSize.Width - contentWidth) / 2;

        brand.AutoSize = false;
        brand.Left = 0;
        brand.Width = card.ClientSize.Width;
        brand.Height = 26;
        brand.TextAlign = ContentAlignment.MiddleCenter;

        title.AutoSize = false;
        title.Left = 0;
        title.Width = card.ClientSize.Width;
        title.Height = 46;
        title.Top = 78;
        title.TextAlign = ContentAlignment.MiddleCenter;

        subtitle.AutoSize = false;
        subtitle.Left = 0;
        subtitle.Width = card.ClientSize.Width;
        subtitle.Height = 26;
        subtitle.TextAlign = ContentAlignment.MiddleCenter;

        user.Left = contentLeft;
        password.Left = contentLeft;
        login.Left = contentLeft;
        register.Left = contentLeft;

        user.Width = contentWidth;
        password.Width = contentWidth - showPassword.Width - 8;
        login.Width = contentWidth;
        register.Width = contentWidth;

        showPassword.Left = contentLeft + password.Width + 8;

        message.Left = contentLeft;
        message.Width = contentWidth;
        message.TextAlign = ContentAlignment.MiddleCenter;

        booking.Left = contentLeft;
        news.Left = contentLeft + 196;

        card.Controls.AddRange([
            brand,
            title,
            subtitle,
            user,
            password,
            showPassword,
            message,
            login,
            register,
            booking,
            news
        ]);

        _root.Controls.Add(card);
    }

    private async Task RegisterPatient()
    {
        using var form = new EntityDialog("Crear cuenta de paciente");

        var fullName = form.AddText("Nombre completo *");
        var username = form.AddText("Nombre de usuario *");
        var password = form.AddText("Contraseña (mínimo 12 caracteres) *");

        password.UseSystemPasswordChar = true;

        var email = form.AddText("Correo electrónico (opcional)");
        var cedula = form.AddText("Cédula (opcional)");
        var phone = form.AddText("Teléfono (opcional)");

        if (form.ShowDialog(this) != DialogResult.OK)
            return;

        if (string.IsNullOrWhiteSpace(fullName.Text) ||
            username.Text.Trim().Length is < 3 or > 100 ||
            password.Text.Length < 12)
        {
            MessageBox.Show(
                this,
                "Completa el nombre, un usuario de 3 a 100 caracteres y una contraseña de al menos 12 caracteres.",
                "Validación",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            return;
        }

        if (username.Text.Any(char.IsWhiteSpace) ||
            username.Text.Any(c =>
                !(char.IsLetterOrDigit(c) || c is '.' or '_' or '-')))
        {
            MessageBox.Show(
                this,
                "El nombre de usuario solo puede incluir letras, números, punto, guion y guion bajo.",
                "Validación",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            return;
        }

        try
        {
            await _api.SendAsync(
                HttpMethod.Post,
                "api/Auth/register",
                new
                {
                    NombreCompleto = fullName.Text.Trim(),
                    NombreUsuario = username.Text.Trim(),
                    Password = password.Text,
                    Email = EmptyNull(email.Text),
                    Cedula = EmptyNull(cedula.Text),
                    Telefono = EmptyNull(phone.Text)
                });

            MessageBox.Show(
                this,
                "La cuenta de paciente quedó registrada. Ya puedes iniciar sesión.",
                "Tanger Clinic",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                this,
                UserMessage(ex),
                "No se pudo crear la cuenta",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
    }
}
