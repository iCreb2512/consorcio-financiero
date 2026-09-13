namespace Consorcio;

internal static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        try
        {
            var path=Path.Combine(AppContext.BaseDirectory,"conexion.txt");
            var connection=Environment.GetEnvironmentVariable("CONSORCIO_CONNECTION") ?? File.ReadAllText(path).Trim();
            var service=new Service(new Database(connection));
            if(args.Contains("--self-test")) { IntegrationTests.Run(service); return; }
            if(args.Contains("--render-demo")) { RenderDemo(service); return; }
            while(true)
            {
                using var login=new LoginForm(service);
                if(login.ShowDialog()!=DialogResult.OK) break;
                using var main=new MainForm(service); Application.Run(main); service.Logout();
            }
        }
        catch(Exception ex)
        {
            if(args.Contains("--self-test")) { File.WriteAllText(Path.Combine(AppContext.BaseDirectory,"test-results.txt"),ex.ToString()); Environment.ExitCode=1; return; }
            MessageBox.Show("No se pudo iniciar el sistema. Revisa conexion.txt y los scripts SQL.\n\n"+ex.Message,"Consorcio",MessageBoxButtons.OK,MessageBoxIcon.Error);
        }
    }
    static void RenderDemo(Service service)
    {
        var folder=Environment.GetEnvironmentVariable("CONSORCIO_PREVIEWS") ?? Path.Combine(AppContext.BaseDirectory,"previews");
        Directory.CreateDirectory(folder);
        void Capture(Form form,string name)
        {
            form.StartPosition=FormStartPosition.Manual; form.Location=new Point(-20000,-20000); form.ShowInTaskbar=false;
            form.Show(); Application.DoEvents(); form.PerformLayout();
            using var bitmap=new Bitmap(form.Width,form.Height); form.DrawToBitmap(bitmap,form.ClientRectangle with { Width=form.Width,Height=form.Height });
            bitmap.Save(Path.Combine(folder,name+".png")); form.Hide();
        }
        using var login=new LoginForm(service); Capture(login,"01-acceso");
        service.Login("admin","ConsorcioDemo!2026");
        using var main=new MainForm(service); Capture(main,"02-resumen");
        main.ShowList("Clientes"); Capture(main,"03-clientes");
        using var editor=new EditorForm(service,"Clientes",1); Capture(editor,"04-formulario");
        using var affiliates=new AffiliationsForm(service,1); Capture(affiliates,"05-asociaciones");
        main.ShowList("Instituciones");Capture(main,"06-instituciones");
    }
}
