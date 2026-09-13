namespace Consorcio;

public sealed class LoginForm : Form
{
    public LoginForm(Service service)
    {
        Theme.Setup(this,"Consorcio | Acceso",500,465);
        FormBorderStyle=FormBorderStyle.FixedDialog; MaximizeBox=false;
        var layout=new FlowLayoutPanel { Dock=DockStyle.Fill,FlowDirection=FlowDirection.TopDown,WrapContents=false,Padding=new Padding(44,30,44,25) };
        Controls.Add(layout);
        layout.Controls.Add(Theme.Label("CONSORCIO",26,true));
        layout.Controls.Add(Theme.Label("Gestión de instituciones y clientes",11));
        layout.Controls.Add(Theme.Label("Usuario"));
        var user=new TextBox { Width=400,MaxLength=40,AccessibleName="Usuario" }; layout.Controls.Add(user);
        layout.Controls.Add(Theme.Label("Contraseña"));
        var password=new TextBox { Width=400,UseSystemPasswordChar=true,MaxLength=128,AccessibleName="Contraseña" }; layout.Controls.Add(password);
        var error=Theme.Label("",10); error.ForeColor=Color.Firebrick; error.MaximumSize=new Size(400,65); layout.Controls.Add(error);
        var enter=Theme.Button("Iniciar sesión",()=> {
            try { service.Login(user.Text,password.Text); DialogResult=DialogResult.OK; Close(); }
            catch(ValidationException e) { error.Text=e.Message; password.Clear(); password.Focus(); }
            catch(Microsoft.Data.SqlClient.SqlException) { error.Text="No se pudo conectar. Revisa conexion.txt y ejecuta los scripts SQL de instalación."; }
        },true);
        enter.Width=400; layout.Controls.Add(enter); AcceptButton=enter;
        layout.Controls.Add(Theme.Label("Entorno académico · Datos de demostración",10));
    }
}
