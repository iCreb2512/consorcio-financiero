namespace Consorcio;

public static class Theme
{
    public static readonly Color Navy=Color.FromArgb(19,37,57), Teal=Color.FromArgb(0,116,112),
        Background=Color.FromArgb(243,246,249), Muted=Color.FromArgb(85,105,121);
    public static Label Label(string text,int size=11,bool bold=false) => new() {
        Text=text,AutoSize=true,ForeColor=Navy,Font=new Font("Segoe UI",size,bold?FontStyle.Bold:FontStyle.Regular),Margin=new Padding(0,8,0,5)
    };
    public static Button Button(string text,Action action,bool primary=false)
    {
        var b=new Button { Text=text,AutoSize=true,Height=38,MinimumSize=new Size(105,38),FlatStyle=FlatStyle.Flat,
            BackColor=primary?Teal:Color.White,ForeColor=primary?Color.White:Navy,Cursor=Cursors.Hand,Padding=new Padding(12,4,12,4),Margin=new Padding(0,0,10,0)};
        b.FlatAppearance.BorderColor=Color.FromArgb(208,218,227);
        b.Click+=(_,_)=>action(); return b;
    }
    public static void Setup(Form f,string title,int width,int height)
    {
        f.Text=title; f.ClientSize=new Size(width,height); f.StartPosition=FormStartPosition.CenterScreen;
        f.Font=new Font("Segoe UI",10); f.BackColor=Background; f.AutoScaleMode=AutoScaleMode.Dpi;
    }
    public static void Guard(Action action)
    {
        try { action(); }
        catch(ValidationException e) { MessageBox.Show(e.Message,"Revisa la información",MessageBoxButtons.OK,MessageBoxIcon.Warning); }
        catch(Microsoft.Data.SqlClient.SqlException) { MessageBox.Show("No se pudo completar la operación en SQL Server. Verifica la conexión y vuelve a intentar.","Conexión",MessageBoxButtons.OK,MessageBoxIcon.Error); }
        catch(Exception e) when(e is IOException or UnauthorizedAccessException) { MessageBox.Show(e.Message,"Archivo",MessageBoxButtons.OK,MessageBoxIcon.Error); }
    }
}
