using Microsoft.Data.SqlClient;
using System.Data;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using static Consorcio.Database;

namespace Consorcio;

public sealed record Session(int Id, string Nombre, string Rol);
public sealed class ValidationException(string message) : Exception(message);

public sealed class Service(Database db)
{
    public Database Db { get; } = db;
    public Session? User { get; private set; }
    public bool CanWrite => User?.Rol is "Administrador" or "Operador";
    public bool IsAdmin => User?.Rol == "Administrador";
    private int attempts;
    private DateTime lockedUntil;
    public static byte[] Hash(string password, byte[] salt) =>
        Rfc2898DeriveBytes.Pbkdf2(password, salt, 260000, HashAlgorithmName.SHA256, 32);

    public void Login(string name, string password)
    {
        if(DateTime.UtcNow < lockedUntil) throw new ValidationException("Espera un minuto antes de volver a intentar.");
        var table = Db.Query("SELECT Id,Nombre,Rol,Salt,ClaveHash FROM dbo.Usuarios WHERE Nombre=@n",P("@n",name.Trim()));
        var row = table.Rows.Count == 1 ? table.Rows[0] : null;
        var salt = row is null ? new byte[16] : (byte[])row["Salt"];
        var expected = row is null ? new byte[32] : (byte[])row["ClaveHash"];
        var valid = CryptographicOperations.FixedTimeEquals(Hash(password,salt),expected);
        if(row is null || !valid)
        {
            if(++attempts >= 5) { lockedUntil = DateTime.UtcNow.AddMinutes(1); attempts = 0; }
            throw new ValidationException("Usuario o contraseña incorrectos.");
        }
        attempts = 0;
        User = new Session((int)row["Id"],(string)row["Nombre"],(string)row["Rol"]);
    }
    public void Logout() => User = null;
    private void Authorize(bool admin = false)
    {
        if(!CanWrite || (admin && !IsAdmin)) throw new ValidationException("Tu rol no permite esta operación.");
    }
    public static string Required(object? value, string label, int min = 3, int max = 100)
    {
        var s = Convert.ToString(value)?.Trim() ?? "";
        if(s.Length < min || s.Length > max) throw new ValidationException($"{label}: ingresa entre {min} y {max} caracteres.");
        return s;
    }
    public static DateTime ValidDate(object value, string label, int minYear)
    {
        if(value is not DateTime d || d.Date > DateTime.Today || d.Year < minYear)
            throw new ValidationException($"{label}: selecciona una fecha entre {minYear} y hoy.");
        return d.Date;
    }
    static int Id(object value, string label)
    {
        if(!int.TryParse(Convert.ToString(value),out var id) || id <= 0) throw new ValidationException($"Selecciona {label}.");
        return id;
    }
    public int Save(string entity, Dictionary<string,object> input, int? id = null)
    {
        Authorize(entity is "Consorcios" or "Usuarios");
        var v = new Dictionary<string,object>();
        object Get(string key) => input.TryGetValue(key,out var x) ? x : "";
        switch(entity)
        {
            case "Consorcios": v["Nombre"] = Required(Get("Nombre"),"Nombre"); break;
            case "Instituciones":
                v["Nombre"] = Required(Get("Nombre"),"Nombre");
                v["ConsorcioId"] = Id(Get("ConsorcioId"),"un consorcio");
                v["Tipo"] = Get("Tipo");
                if(!new[]{"Banco","Cooperativa"}.Contains(v["Tipo"])) throw new ValidationException("Selecciona Banco o Cooperativa.");
                v["Fundacion"] = ValidDate(Get("Fundacion"),"Fundación",1753); break;
            case "Clientes":
                v["Nombre"] = Required(Get("Nombre"),"Nombre",3,120);
                v["DUI"] = Convert.ToString(Get("DUI"))!.Trim();
                if(!Regex.IsMatch((string)v["DUI"],@"\A[0-9]{8}-[0-9]\z")) throw new ValidationException("DUI: usa el formato 00000000-0.");
                v["Nacimiento"] = ValidDate(Get("Nacimiento"),"Nacimiento",1900);
                v["Genero"] = Get("Genero");
                if(!new[]{"Femenino","Masculino","Otro","Prefiero no indicarlo"}.Contains(v["Genero"])) throw new ValidationException("Selecciona un género.");
                v["MunicipioId"] = Id(Get("MunicipioId"),"un municipio");
                v["Complemento"] = Required(Get("Complemento"),"Complemento",5,250); break;
            case "Usuarios":
                v["Nombre"] = Required(Get("Nombre"),"Usuario",3,40);
                v["Rol"] = Get("Rol");
                if(!new[]{"Administrador","Operador","Consulta"}.Contains(v["Rol"])) throw new ValidationException("Selecciona un rol.");
                if(id == User!.Id && (string)v["Rol"] != "Administrador") throw new ValidationException("No puedes retirarte el rol de administrador.");
                var password = Convert.ToString(Get("Password")) ?? "";
                if(id is null || password.Length > 0)
                {
                    Required(password,"Contraseña",10,128);
                    var salt = RandomNumberGenerator.GetBytes(16);
                    v["Salt"] = salt; v["ClaveHash"] = Hash(password,salt);
                }
                break;
            default: throw new ValidationException("Entidad inválida.");
        }
        return Transaction((c,t) =>
        {
            string sql = id is null
              ? $"INSERT INTO dbo.{entity}({string.Join(',',v.Keys)}) OUTPUT INSERTED.Id VALUES({string.Join(',',v.Keys.Select(k=>"@"+k))})"
              : $"UPDATE dbo.{entity} SET {string.Join(',',v.Keys.Select(k=>k+"=@"+k))} OUTPUT INSERTED.Id WHERE Id=@Id";
            using var cmd = new SqlCommand(sql,c,t);
            if(id is not null && input.TryGetValue("_Version",out var version))
            { cmd.CommandText+=" AND Version=@version"; cmd.Parameters.Add(P("@version",version)); }
            foreach(var kv in v) cmd.Parameters.Add(P("@"+kv.Key,kv.Value));
            if(id is not null) cmd.Parameters.Add(P("@Id",id));
            var result = cmd.ExecuteScalar();
            if(result is null) throw new ValidationException("El registro cambió o ya no existe. Cierra la ficha y ábrela nuevamente para revisar los datos actuales.");
            var saved = (int)result;
            Audit(c,t,id is null ? "Crear":"Editar",entity,saved);
            return saved;
        });
    }
    public void Delete(string entity,int id)
    {
        Authorize(true);
        if(!new[]{"Consorcios","Instituciones","Clientes","Usuarios"}.Contains(entity)) throw new ValidationException("Entidad inválida.");
        if(entity=="Usuarios" && id==User!.Id) throw new ValidationException("No puedes eliminar tu propia cuenta.");
        Transaction((c,t) =>
        {
            using var cmd = new SqlCommand($"DELETE FROM dbo.{entity} WHERE Id=@id",c,t);
            cmd.Parameters.Add(P("@id",id));
            if(cmd.ExecuteNonQuery()==0) throw new ValidationException("El registro ya no existe.");
            Audit(c,t,"Eliminar",entity,id); return 0;
        });
    }
    public void Affiliate(int client, int institution, bool remove = false)
    {
        Authorize();
        Transaction((c,t) =>
        {
            using var cmd = new SqlCommand(remove
                ? "DELETE FROM dbo.Afiliaciones WHERE ClienteId=@c AND InstitucionId=@i"
                : "INSERT INTO dbo.Afiliaciones(ClienteId,InstitucionId) VALUES(@c,@i)",c,t);
            cmd.Parameters.AddRange([P("@c",client),P("@i",institution)]);
            if(cmd.ExecuteNonQuery()==0) throw new ValidationException("La asociación ya no existe.");
            Audit(c,t,remove?"Desafiliar":"Afiliar","Clientes",client); return 0;
        });
    }
    T Transaction<T>(Func<SqlConnection,SqlTransaction,T> action)
    {
        using var c = Db.Open();
        using var t = c.BeginTransaction();
        try { var result = action(c,t); t.Commit(); return result; }
        catch(SqlException e) when(e.Number is 2601 or 2627) { throw new ValidationException("Ya existe ese nombre, DUI o asociación. No se guardaron cambios."); }
        catch(SqlException e) when(e.Number == 547) { throw new ValidationException("Los datos no cumplen una regla o el registro tiene relaciones activas. Revisa sus instituciones y afiliaciones."); }
    }
    void Audit(SqlConnection c, SqlTransaction t, string action,string entity,int id)
    {
        using var cmd = new SqlCommand("INSERT INTO dbo.Auditoria(Usuario,Accion,Entidad,RegistroId) VALUES(@u,@a,@e,@r)",c,t);
        cmd.Parameters.AddRange([P("@u",User!.Nombre),P("@a",action),P("@e",entity),P("@r",id)]);
        cmd.ExecuteNonQuery();
    }
    public DataTable List(string entity,string search,int page,int institution=0)
    {
        if(User is null) throw new ValidationException("Inicia sesión.");
        string query = entity switch
        {
            "Clientes" => "SELECT c.Id,c.DUI,c.Nombre,c.Nacimiento,c.Genero,d.Nombre Departamento,m.Nombre Municipio,c.Complemento,(SELECT COUNT(*) FROM dbo.Afiliaciones a WHERE a.ClienteId=c.Id) Instituciones FROM dbo.Clientes c JOIN dbo.Municipios m ON m.Id=c.MunicipioId JOIN dbo.Departamentos d ON d.Id=m.DepartamentoId WHERE (c.Nombre LIKE @q OR c.DUI LIKE @q) AND (@i=0 OR EXISTS(SELECT 1 FROM dbo.Afiliaciones a WHERE a.ClienteId=c.Id AND a.InstitucionId=@i)) ORDER BY c.Nombre,c.Id",
            "Instituciones" => "SELECT i.Id,i.Nombre,i.Tipo,i.Fundacion,c.Nombre Consorcio,(SELECT COUNT(*) FROM dbo.Afiliaciones a WHERE a.InstitucionId=i.Id) Clientes FROM dbo.Instituciones i JOIN dbo.Consorcios c ON c.Id=i.ConsorcioId WHERE i.Nombre LIKE @q ORDER BY i.Nombre,i.Id",
            "Consorcios" => "SELECT c.Id,c.Nombre,(SELECT COUNT(*) FROM dbo.Instituciones i WHERE i.ConsorcioId=c.Id) Instituciones FROM dbo.Consorcios c WHERE c.Nombre LIKE @q ORDER BY c.Nombre,c.Id",
            "Usuarios" when IsAdmin => "SELECT Id,Nombre,Rol FROM dbo.Usuarios WHERE Nombre LIKE @q ORDER BY Nombre,Id",
            "Auditoria" when IsAdmin => "SELECT Id,Fecha,Usuario,Accion,Entidad,RegistroId FROM dbo.Auditoria WHERE Usuario LIKE @q ORDER BY Fecha DESC,Id DESC",
            _ => throw new ValidationException("Acceso no permitido.")
        };
        // Búsqueda por prefijo para aprovechar índices. Escape de comodines de LIKE.
        var q = search.Trim().Replace("[","[[]").Replace("%","[%]").Replace("_","[_]")+"%";
        return Db.Query(query+" OFFSET @offset ROWS FETCH NEXT 50 ROWS ONLY", P("@q",q),P("@i",institution),P("@offset",Math.Max(0,page)*50));
    }
}
