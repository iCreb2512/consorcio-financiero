using System.Data;
using static Consorcio.Database;

namespace Consorcio;

public sealed class EditorForm : Form
{
    private readonly Dictionary<string,Control> fields=[];
    private readonly TableLayoutPanel grid=new(){ Dock=DockStyle.Top,AutoSize=true,ColumnCount=1,Padding=new Padding(30,12,30,20) };
    private readonly ErrorProvider errors=new(){BlinkStyle=ErrorBlinkStyle.NeverBlink};
    public EditorForm(Service service,string entity,int? id=null)
    {
        Theme.Setup(this,(id is null?"Nuevo registro":"Editar registro")+" | "+entity,590,entity=="Clientes"?800:570);
        MinimumSize=new Size(540,500); MaximizeBox=false;
        errors.ContainerControl=this;
        var scroll=new Panel {Dock=DockStyle.Fill,AutoScroll=true}; scroll.Controls.Add(grid); Controls.Add(scroll);
        grid.Controls.Add(Theme.Label(id is null?"Nuevo registro":"Editar #"+id,22,true));
        grid.Controls.Add(Theme.Label("Los campos son obligatorios salvo indicación.",10));
        DataRow? row=null;
        if(id is not null) row=service.Db.Query($"SELECT * FROM dbo.{entity} WHERE Id=@id",P("@id",id)).Rows.Cast<DataRow>().FirstOrDefault();
        if(id is not null && row is null) throw new ValidationException("El registro ya no existe.");
        object? Value(string key) => row?.Table.Columns.Contains(key)==true?row[key]:null;
        void TextField(string key,string label,int max=100,bool secret=false)
        {
            var t=new TextBox {Width=490,MaxLength=max,Text=Convert.ToString(Value(key))??"",UseSystemPasswordChar=secret}; Add(key,label,t);
        }
        ComboBox Combo(string key,string label,object[] items)
        {
            var c=new ComboBox {Width=490,DropDownStyle=ComboBoxStyle.DropDownList}; c.Items.AddRange(items);
            c.SelectedItem=Value(key); Add(key,label,c); return c;
        }
        ComboBox Lookup(string key,string label,DataTable data)
        {
            var c=new ComboBox {Width=490,DropDownStyle=ComboBoxStyle.DropDownList,DisplayMember="Nombre",ValueMember="Id",DataSource=data};
            c.SelectedIndex=-1; if(Value(key) is object v) c.SelectedValue=v;
            Shown+=(_,_)=> { if(Value(key) is object existing) c.SelectedValue=existing; else c.SelectedIndex=-1; };
            Add(key,label,c); return c;
        }
        void DateField(string key,string label,int min)
        {
            var d=new DateTimePicker {Width=490,Format=DateTimePickerFormat.Custom,CustomFormat="dd/MM/yyyy",MinDate=new DateTime(min,1,1),MaxDate=DateTime.Today,ShowCheckBox=true,Checked=false};
            if(Value(key) is DateTime old) { d.Value=old; d.Checked=true; } Add(key,label,d);
        }
        TextField("Nombre",entity=="Usuarios"?"Nombre de usuario":"Nombre",entity=="Clientes"?120:entity=="Usuarios"?40:100);
        if(entity=="Instituciones")
        {
            Lookup("ConsorcioId","Consorcio",service.Db.Query("SELECT Id,Nombre FROM dbo.Consorcios ORDER BY Nombre"));
            Combo("Tipo","Tipo de institución",["Banco","Cooperativa"]); DateField("Fundacion","Fecha de fundación",1753);
        }
        if(entity=="Clientes")
        {
            TextField("DUI","DUI · 00000000-0",10); DateField("Nacimiento","Fecha de nacimiento",1900);
            Combo("Genero","Género",["Femenino","Masculino","Otro","Prefiero no indicarlo"]);
            var dep=Lookup("DepartamentoId","Departamento",service.Db.Query("SELECT Id,Nombre FROM dbo.Departamentos ORDER BY Nombre"));
            var mun=Lookup("MunicipioId","Municipio",service.Db.Query("SELECT Id,Nombre FROM dbo.Municipios WHERE 1=0"));
            void LoadMunicipalities()
            {
                var selected=dep.SelectedValue is int val?val:0;
                mun.DataSource=service.Db.Query("SELECT Id,Nombre FROM dbo.Municipios WHERE DepartamentoId=@d ORDER BY Nombre",P("@d",selected));
                mun.SelectedIndex=-1;
            }
            dep.SelectedIndexChanged+=(_,_)=>Theme.Guard(LoadMunicipalities);
            if(Value("MunicipioId") is int mid)
            {
                var d=service.Db.Query("SELECT DepartamentoId FROM dbo.Municipios WHERE Id=@id",P("@id",mid));
                dep.SelectedValue=d.Rows[0][0]; LoadMunicipalities(); mun.SelectedValue=mid;
                Shown+=(_,_)=> { dep.SelectedValue=d.Rows[0][0]; LoadMunicipalities(); mun.SelectedValue=mid; };
            }
            TextField("Complemento","Complemento de dirección",250);
            ((TextBox)fields["Complemento"]).Multiline=true; fields["Complemento"].Height=60;
        }
        if(entity=="Usuarios")
        {
            Combo("Rol","Rol",["Administrador","Operador","Consulta"]);
            TextField("Password",id is null?"Contraseña · mínimo 10 caracteres":"Nueva contraseña · deja vacío para conservar",128,true);
        }
        var buttons=new FlowLayoutPanel {AutoSize=true,Margin=new Padding(0,22,0,10)};
        var save=Theme.Button("Guardar registro",()=>Theme.Guard(()=> {
            errors.Clear();
            var values=new Dictionary<string,object>(); bool missing=false;
            foreach(var (key,control) in fields)
            {
                object val=control switch {
                    ComboBox c=>c.DataSource is null?c.SelectedItem??"":c.SelectedValue??"",
                    DateTimePicker d=>d.Checked?d.Value.Date:"",
                    _=>control.Text.Trim()
                };
                values[key]=val;
                if(string.IsNullOrWhiteSpace(Convert.ToString(val)) && !(key=="Password" && id is not null))
                {errors.SetError(control,"Completa este campo."); missing=true;}
            }
            if(missing) throw new ValidationException("Completa los campos marcados antes de guardar.");
            if(row is not null) values["_Version"]=row["Version"];
            service.Save(entity,values,id); DialogResult=DialogResult.OK; Close();
        }),true);
        buttons.Controls.Add(save); buttons.Controls.Add(Theme.Button("Cancelar",()=>Close())); grid.Controls.Add(buttons); AcceptButton=save;
        if(!service.CanWrite || (entity is "Consorcios" or "Usuarios" && !service.IsAdmin))
        { foreach(var control in fields.Values) control.Enabled=false; save.Enabled=false; }
    }
    private void Add(string key,string label,Control c)
    {
        grid.Controls.Add(Theme.Label(label)); c.AccessibleName=label; c.Margin=new Padding(0,0,16,4); grid.Controls.Add(c); fields[key]=c;
    }
    protected override void Dispose(bool disposing) { if(disposing) errors.Dispose(); base.Dispose(disposing); }
}
