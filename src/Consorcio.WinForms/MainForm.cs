using System.Data;
using System.Text;
using static Consorcio.Database;

namespace Consorcio;

public sealed class MainForm : Form
{
    readonly Service service;
    readonly Panel content=new(){Dock=DockStyle.Fill,Padding=new Padding(30,24,30,20)};
    DataGridView? table;
    TextBox? search;
    ComboBox? institution;
    Label? status;
    string entity="Clientes";
    int page;
    Button? next;
    public MainForm(Service service)
    {
        this.service=service;
        Theme.Setup(this,"Consorcio | Gestión financiera",1280,790);
        MinimumSize=new Size(1050,700);
        var side=new FlowLayoutPanel {Dock=DockStyle.Left,Width=235,BackColor=Theme.Navy,FlowDirection=FlowDirection.TopDown,WrapContents=false,Padding=new Padding(20,28,20,20)};
        var brand=Theme.Label("CONSORCIO",18,true); brand.ForeColor=Color.White; side.Controls.Add(brand);
        var intro=Theme.Label("GESTIÓN FINANCIERA",9); intro.ForeColor=Color.LightGray; intro.Margin=new Padding(0,0,0,32); side.Controls.Add(intro);
        void Nav(string title,Action action)
        {
            var b=Theme.Button(title,()=>Theme.Guard(action)); b.Width=195; b.Height=44;
            b.Margin=new Padding(0,0,0,10); b.BackColor=Theme.Navy; b.ForeColor=Color.White; b.FlatAppearance.BorderSize=0; side.Controls.Add(b);
        }
        Nav("Resumen",Dashboard); foreach(var name in new[]{"Clientes","Instituciones","Consorcios"}) Nav(name,()=>ShowList(name));
        if(service.IsAdmin) { Nav("Usuarios",()=>ShowList("Usuarios")); Nav("Auditoría",()=>ShowList("Auditoria")); }
        Nav("Ayuda",Help); Nav("Cerrar sesión",()=>Close());
        var account=Theme.Label(service.User!.Nombre+"\n"+service.User.Rol,10); account.ForeColor=Color.LightGray; account.Margin=new Padding(0,45,0,0); side.Controls.Add(account);
        Controls.Add(content); Controls.Add(side); Dashboard();
    }
    void ClearContent()
    {
        foreach(Control c in content.Controls.Cast<Control>().ToArray()) c.Dispose();
        content.Controls.Clear();
    }
    void Dashboard()
    {
        ClearContent();
        var flow=new FlowLayoutPanel {Dock=DockStyle.Fill,FlowDirection=FlowDirection.TopDown,WrapContents=false,AutoScroll=true}; content.Controls.Add(flow);
        flow.Controls.Add(Theme.Label("Una visión de tu consorcio",28,true));
        flow.Controls.Add(Theme.Label("Instituciones conectadas, carteras organizadas y datos consistentes.",11));
        var totals=service.Db.Query("SELECT (SELECT COUNT(*) FROM dbo.Consorcios) Consorcios,(SELECT COUNT(*) FROM dbo.Instituciones) Instituciones,(SELECT COUNT(*) FROM dbo.Clientes) Clientes,(SELECT COUNT(*) FROM dbo.Afiliaciones) Afiliaciones");
        var cards=new FlowLayoutPanel {AutoSize=true,Margin=new Padding(0,28,0,25),WrapContents=true,MaximumSize=new Size(1000,200)};
        foreach(DataColumn col in totals.Columns)
        {
            var card=new Panel {Size=new Size(190,115),BackColor=Color.White,Margin=new Padding(0,0,14,12),Padding=new Padding(18)};
            var number=Theme.Label(totals.Rows[0][col].ToString()!,30,true); number.Dock=DockStyle.Top; number.ForeColor=Theme.Teal;
            var label=Theme.Label(col.ColumnName); label.Dock=DockStyle.Bottom; card.Controls.Add(number); card.Controls.Add(label); cards.Controls.Add(card);
        }
        flow.Controls.Add(cards); flow.Controls.Add(Theme.Label("Cartera por institución",17,true));
        var data=service.Db.Query("SELECT i.Nombre Institución,i.Tipo,COUNT(a.ClienteId) Clientes FROM dbo.Instituciones i LEFT JOIN dbo.Afiliaciones a ON a.InstitucionId=i.Id GROUP BY i.Id,i.Nombre,i.Tipo ORDER BY Clientes DESC,i.Nombre");
        var overview=Grid(); overview.Size=new Size(930,300); overview.DataSource=data; flow.Controls.Add(overview);
        flow.SizeChanged+=(_,_)=>overview.Width=Math.Max(650,flow.ClientSize.Width-25);
        var open=Theme.Button("Ver clientes",()=>Theme.Guard(()=>ShowList("Clientes")),true); open.Margin=new Padding(0,20,0,10); flow.Controls.Add(open);
        flow.Controls.Add(Theme.Label("Datos iniciales ficticios para prácticas y demostraciones.",10));
    }
    public void ShowList(string name)
    {
        entity=name; page=0; ClearContent();
        var header=new FlowLayoutPanel {Dock=DockStyle.Top,Height=145,FlowDirection=FlowDirection.TopDown,WrapContents=false};
        header.Controls.Add(Theme.Label(name=="Auditoria"?"Historial de cambios":name,27,true));
        header.Controls.Add(Theme.Label(name=="Clientes"?"Consulta, registra y vincula clientes a sus instituciones.":"Administra los registros con información consistente.",11));
        var tools=new FlowLayoutPanel {Height=45,AutoSize=true,Margin=new Padding(0,12,0,0)};
        search=new TextBox {Width=225,PlaceholderText="Buscar por inicio de nombre"};
        if(name=="Clientes") search.PlaceholderText="Inicio de nombre o DUI";
        search.KeyDown+=(_,e)=> { if(e.KeyCode==Keys.Enter) { page=0; Theme.Guard(RefreshList); e.SuppressKeyPress=true; } };
        tools.Controls.Add(search); tools.Controls.Add(Theme.Button("Buscar",()=>Theme.Guard(()=>{page=0;RefreshList();})));
        institution=null;
        if(name=="Clientes")
        {
            var data=service.Db.Query("SELECT Id,Nombre FROM dbo.Instituciones ORDER BY Nombre"); var all=data.NewRow(); all["Id"]=0; all["Nombre"]="Todas las instituciones"; data.Rows.InsertAt(all,0);
            institution=new ComboBox {Width=245,DropDownStyle=ComboBoxStyle.DropDownList,DisplayMember="Nombre",ValueMember="Id",DataSource=data};
            institution.SelectedIndexChanged+=(_,_)=>Theme.Guard(()=>{page=0;RefreshList();}); tools.Controls.Add(institution);
        }
        header.Controls.Add(tools);
        var bottom=new FlowLayoutPanel {Dock=DockStyle.Bottom,Height=100,FlowDirection=FlowDirection.TopDown,WrapContents=false};
        var actions=new FlowLayoutPanel {AutoSize=true};
        if(name!="Auditoria")
        {
            var add=Theme.Button("Nuevo",()=>Edit(null),true); var edit=Theme.Button("Ver / editar",()=>Theme.Guard(()=>Edit(Selected())));
            add.Enabled=service.CanWrite && (name is not ("Consorcios" or "Usuarios") || service.IsAdmin);
            actions.Controls.Add(add); actions.Controls.Add(edit);
            if(name=="Clientes") actions.Controls.Add(Theme.Button("Instituciones del cliente",()=>Theme.Guard(()=> {
                using var f=new AffiliationsForm(service,Selected()); f.ShowDialog(this); RefreshList();
            })));
            if(service.IsAdmin) actions.Controls.Add(Theme.Button("Eliminar",()=>Theme.Guard(()=> {
                var id=Selected();
                if(MessageBox.Show($"¿Eliminar el registro #{id}? Esta acción no se puede deshacer. Si tiene relaciones, primero debes retirarlas.","Confirmar eliminación",MessageBoxButtons.YesNo,MessageBoxIcon.Warning)==DialogResult.Yes)
                { service.Delete(entity,id); RefreshList(); }
            })));
        }
        actions.Controls.Add(Theme.Button("Exportar página CSV",Export)); bottom.Controls.Add(actions);
        var pager=new FlowLayoutPanel {AutoSize=true,Margin=new Padding(0,12,0,0)};
        pager.Controls.Add(Theme.Button("Anterior",()=>Theme.Guard(()=>{page=Math.Max(0,page-1);RefreshList();})));
        next=Theme.Button("Siguiente",()=>Theme.Guard(()=>{page++;RefreshList();})); pager.Controls.Add(next);
        status=Theme.Label("",10); pager.Controls.Add(status); bottom.Controls.Add(pager);
        table=Grid(); table.Dock=DockStyle.Fill; table.CellDoubleClick+=(_,e)=> { if(e.RowIndex>=0 && entity!="Auditoria") Theme.Guard(()=>Edit(Selected())); };
        content.Controls.Add(table); content.Controls.Add(bottom); content.Controls.Add(header); RefreshList();
    }
    public static DataGridView Grid() => new() {
        ReadOnly=true,AllowUserToAddRows=false,AllowUserToDeleteRows=false,SelectionMode=DataGridViewSelectionMode.FullRowSelect,MultiSelect=false,
        AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill,BackgroundColor=Color.White,BorderStyle=BorderStyle.None,RowHeadersVisible=false,
        EnableHeadersVisualStyles=false,ColumnHeadersHeight=42,ColumnHeadersHeightSizeMode=DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
        ColumnHeadersDefaultCellStyle=new DataGridViewCellStyle {BackColor=Color.FromArgb(227,236,240),ForeColor=Theme.Navy,Font=new Font("Segoe UI",10,FontStyle.Bold)},
        DefaultCellStyle=new DataGridViewCellStyle {SelectionBackColor=Color.FromArgb(210,238,235),SelectionForeColor=Theme.Navy,Padding=new Padding(5),ForeColor=Theme.Navy},
        AlternatingRowsDefaultCellStyle=new DataGridViewCellStyle {BackColor=Color.FromArgb(248,250,252)},RowTemplate={Height=36}
    };
    void RefreshList()
    {
        if(table is null || search is null) return;
        var data=service.List(entity,search.Text,page,institution?.SelectedValue is int id?id:0);
        if(data.Rows.Count==0 && page>0) { page--; RefreshList(); return; }
        table.DataSource=data;
        foreach(DataGridViewColumn col in table.Columns) { col.MinimumWidth=col.Name=="Id"?48:90; if(col.ValueType==typeof(DateTime)) col.DefaultCellStyle.Format=col.Name=="Fecha"?"dd/MM/yyyy HH:mm":"dd/MM/yyyy"; }
        if(entity=="Clientes")
        {
            foreach(var field in new[]{"Nacimiento","Genero","Complemento"}) table.Columns[field].Visible=false;
            table.Columns["Nombre"].FillWeight=220; table.Columns["DUI"].FillWeight=120;
            table.Columns["Municipio"].FillWeight=190; table.Columns["Departamento"].FillWeight=140;
            table.Columns["Id"].FillWeight=50; table.Columns["Instituciones"].FillWeight=90;
        }
        status!.Text=$"Página {page+1} · {data.Rows.Count} registros · 50 por página";
        next!.Enabled=data.Rows.Count==50;
    }
    int Selected()
    {
        if(table?.CurrentRow?.DataBoundItem is not DataRowView row) throw new ValidationException("Selecciona un registro de la lista.");
        return Convert.ToInt32(row["Id"]);
    }
    void Edit(int? id) => Theme.Guard(()=> { using var form=new EditorForm(service,entity,id); if(form.ShowDialog(this)==DialogResult.OK) RefreshList(); });
    void Export() => Theme.Guard(()=> {
        if(table?.DataSource is not DataTable data) return;
        using var dialog=new SaveFileDialog {Filter="CSV UTF-8|*.csv",FileName=$"{entity}-pagina-{page+1}.csv"};
        if(dialog.ShowDialog()!=DialogResult.OK) return;
        static string Cell(object value) { var s=Convert.ToString(value)??""; if(s.Length>0 && "=+-@\t\r".Contains(s[0])) s="'"+s; return "\""+s.Replace("\"","\"\"")+"\""; }
        var lines=new List<string>{string.Join(';',data.Columns.Cast<DataColumn>().Select(c=>Cell(c.ColumnName)))};
        lines.AddRange(data.Rows.Cast<DataRow>().Select(r=>string.Join(';',r.ItemArray.Select(x=>Cell(x??"")))));
        File.WriteAllLines(dialog.FileName,lines,new UTF8Encoding(true)); MessageBox.Show("Se exportó la página visible.","Exportación");
    });
    void Help() => MessageBox.Show("1. Registra un consorcio con una cuenta administradora.\n2. Registra instituciones y selecciona su consorcio.\n3. Registra un cliente y su dirección.\n4. Selecciona el cliente y abre Instituciones del cliente.\n5. Agrega las instituciones a su cartera.\n\nLas búsquedas funcionan por inicio de nombre o DUI.\nConsulta el manual incluido para instalación, índices y respaldo.","Guía rápida",MessageBoxButtons.OK,MessageBoxIcon.Information);
}
