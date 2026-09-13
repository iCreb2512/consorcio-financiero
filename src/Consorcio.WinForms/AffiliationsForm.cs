using System.Data;
using static Consorcio.Database;
namespace Consorcio;

public sealed class AffiliationsForm : Form
{
    public AffiliationsForm(Service service,int client)
    {
        Theme.Setup(this,"Instituciones del cliente",790,530); MinimumSize=new Size(700,450);
        var name=service.Db.Query("SELECT Nombre,DUI FROM dbo.Clientes WHERE Id=@id",P("@id",client));
        if(name.Rows.Count==0) throw new ValidationException("El cliente ya no existe.");
        var title=Theme.Label(name.Rows[0]["Nombre"]+" · "+name.Rows[0]["DUI"],16,true); title.Dock=DockStyle.Top; title.Padding=new Padding(18);
        var grid=MainForm.Grid(); grid.Dock=DockStyle.Fill;
        var bottom=new FlowLayoutPanel {Dock=DockStyle.Bottom,Height=95,Padding=new Padding(15)};
        var options=new ComboBox {Width=375,DropDownStyle=ComboBoxStyle.DropDownList,DisplayMember="Nombre",ValueMember="Id"};
        void Reload()
        {
            grid.DataSource=service.Db.Query("SELECT i.Id,i.Nombre,i.Tipo,c.Nombre Consorcio,a.Fecha FROM dbo.Afiliaciones a JOIN dbo.Instituciones i ON i.Id=a.InstitucionId JOIN dbo.Consorcios c ON c.Id=i.ConsorcioId WHERE a.ClienteId=@id ORDER BY i.Nombre",P("@id",client));
            options.DataSource=service.Db.Query("SELECT i.Id,i.Nombre FROM dbo.Instituciones i WHERE NOT EXISTS(SELECT 1 FROM dbo.Afiliaciones a WHERE a.ClienteId=@id AND a.InstitucionId=i.Id) ORDER BY i.Nombre",P("@id",client));
            options.SelectedIndex=-1;
        }
        bottom.Controls.Add(options);
        var add=Theme.Button("Asociar",()=>Theme.Guard(()=> {
            if(options.SelectedValue is not int id) throw new ValidationException("Selecciona una institución.");
            service.Affiliate(client,id); Reload();
        }),true);
        var remove=Theme.Button("Retirar seleccionada",()=>Theme.Guard(()=> {
            if(grid.CurrentRow?.DataBoundItem is not DataRowView r) throw new ValidationException("Selecciona una afiliación.");
            if(MessageBox.Show("¿Retirar al cliente de esta institución?","Confirmar",MessageBoxButtons.YesNo)==DialogResult.Yes)
            { service.Affiliate(client,(int)r["Id"],true); Reload(); }
        }));
        add.Enabled=remove.Enabled=service.CanWrite; bottom.Controls.Add(add); bottom.Controls.Add(remove);
        Controls.Add(grid); Controls.Add(bottom); Controls.Add(title); Reload();
    }
}
