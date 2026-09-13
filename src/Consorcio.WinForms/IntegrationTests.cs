using System.Data;
using static Consorcio.Database;
namespace Consorcio;

internal static class IntegrationTests
{
    public static void Run(Service s)
    {
        var report=new List<string>();
        void Check(string name,Action action) { action(); report.Add("PASS | "+name); }
        void Reject(Action action) { try { action(); } catch(ValidationException) { return; } throw new Exception("Se aceptó una operación inválida."); }
        int co=0,ins=0,client=0;
        var tag="Test "+Guid.NewGuid().ToString("N")[..10];
        var dui="88"+Random.Shared.Next(100000,999999)+"-0";
        Dictionary<string,object> Customer(string name) => new(){["Nombre"]=name,["DUI"]=dui,["Nacimiento"]=new DateTime(1994,4,20),["Genero"]="Otro",["MunicipioId"]=1,["Complemento"]="Dirección ficticia de prueba"};
        try
        {
            Check("Rechazo de credenciales incorrectas",()=>Reject(()=>s.Login("admin","incorrecta")));
            s.Login("admin","ConsorcioDemo!2026");
            Check("Alta de consorcio",()=>co=s.Save("Consorcios",new(){["Nombre"]=tag}));
            Check("Consorcio duplicado",()=>Reject(()=>s.Save("Consorcios",new(){["Nombre"]=tag})));
            Check("Institución asociada a un consorcio",()=>ins=s.Save("Instituciones",new(){["Nombre"]=tag+" banco",["ConsorcioId"]=co,["Tipo"]="Banco",["Fundacion"]=new DateTime(2000,1,1)}));
            Check("Alta de cliente",()=>client=s.Save("Clientes",Customer(tag)));
            Check("DUI duplicado",()=>Reject(()=>s.Save("Clientes",Customer(tag+" copia"))));
            Check("DUI mal formado",()=> { var v=Customer(tag);v["DUI"]="123"; Reject(()=>s.Save("Clientes",v)); });
            Check("Fecha futura",()=> { var v=Customer(tag);v["Nacimiento"]=DateTime.Today.AddDays(1); Reject(()=>s.Save("Clientes",v)); });
            Check("Municipio inexistente",()=> { var v=Customer(tag);v["DUI"]="87999999-0";v["MunicipioId"]=int.MaxValue; Reject(()=>s.Save("Clientes",v)); });
            Check("Afiliación y muchos a muchos",()=>{s.Affiliate(client,ins); s.Affiliate(client,1);});
            Check("Afiliación duplicada",()=>Reject(()=>s.Affiliate(client,ins)));
            Check("Borrado de institución con cartera bloqueado",()=>Reject(()=>s.Delete("Instituciones",ins)));
            Check("Borrado de consorcio con institución bloqueado",()=>Reject(()=>s.Delete("Consorcios",co)));
            Check("Edición persistente",()=> { var v=Customer(tag+" editado");s.Save("Clientes",v,client);if((string)s.Db.Query("SELECT Nombre FROM dbo.Clientes WHERE Id=@id",P("@id",client)).Rows[0][0]!=tag+" editado") throw new Exception("Sin persistencia"); });
            Check("Filtro de cartera",()=>{if(s.List("Clientes",tag,0,ins).Rows.Count!=1) throw new Exception("Filtro inválido");});
            Check("Edición concurrente detectada",()=> {
                var version=s.Db.Query("SELECT Version FROM dbo.Clientes WHERE Id=@id",P("@id",client)).Rows[0][0];
                var v=Customer(tag+" editado");v["_Version"]=version;s.Save("Clientes",v,client);
                Reject(()=>s.Save("Clientes",v,client));
            });
            Check("Paginación de 50 sin repetición",()=> {
                var a=s.List("Clientes","",0).Rows.Cast<DataRow>().Select(r=>(int)r["Id"]).ToArray();
                var b=s.List("Clientes","",1).Rows.Cast<DataRow>().Select(r=>(int)r["Id"]).ToArray();
                if(a.Length!=50 || b.Length!=50 || a.Intersect(b).Any()) throw new Exception("Paginación inválida");
            });
            Check("Texto SQL tratado como dato",()=>{if(s.List("Clientes","'; DROP TABLE Clientes;--",0).Rows.Count!=0) throw new Exception("Búsqueda inválida");});
            Check("Consulta no puede modificar",()=> {s.Login("consulta","ConsorcioDemo!2026");Reject(()=>s.Save("Clientes",Customer(tag)));Reject(()=>s.Affiliate(client,2));});
            Check("Consulta no accede a usuarios",()=>Reject(()=>s.List("Usuarios","",0)));
            Check("Operador no administra consorcios",()=>{s.Login("operador","ConsorcioDemo!2026");Reject(()=>s.Save("Consorcios",new(){["Nombre"]=tag+" X"}));});
            Check("Operador no puede eliminar",()=>Reject(()=>s.Delete("Clientes",client)));
            s.Login("admin","ConsorcioDemo!2026");
            Check("Administrador protege su cuenta",()=>Reject(()=>s.Delete("Usuarios",s.User!.Id)));
            Check("Auditoría transaccional",()=>{if(s.Db.Query("SELECT Id FROM dbo.Auditoria WHERE Entidad=N'Clientes' AND RegistroId=@id",P("@id",client)).Rows.Count<4) throw new Exception("Falta auditoría");});
            Check("Retiro de asociaciones",()=>{s.Affiliate(client,ins,true);s.Affiliate(client,1,true);});
            Check("Borrado sin relaciones",()=>{s.Delete("Clientes",client); client=0;s.Delete("Instituciones",ins);ins=0;s.Delete("Consorcios",co);co=0;});
            report.Add($"RESULTADO: {report.Count} pruebas correctas. {DateTime.Now:O}");
        }
        finally
        {
            s.Login("admin","ConsorcioDemo!2026");
            // Solo retira los registros creados por esta ejecución, identificados por ID.
            if(client!=0) { using var c=s.Db.Open(); using var cmd=c.CreateCommand();cmd.CommandText="DELETE dbo.Afiliaciones WHERE ClienteId=@id; DELETE dbo.Clientes WHERE Id=@id";cmd.Parameters.Add(P("@id",client));cmd.ExecuteNonQuery(); }
            if(ins!=0) s.Delete("Instituciones",ins);
            if(co!=0) s.Delete("Consorcios",co);
            File.WriteAllLines(Path.Combine(AppContext.BaseDirectory,"test-results.txt"),report);
        }
    }
}
