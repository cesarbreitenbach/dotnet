using System.Data.SqlClient;

public class Conexao
{
    
    public Conexao()
    {

    }
    public static SqlConnection GetConnectionSqlServer()
        {
            
                var ConexaoHomologacao = "Server=localhost;Database=barracao;User Id=sa;Password=387060cesaR";
                var  conHomologacao = new SqlConnection(ConexaoHomologacao); 
            
            return conHomologacao;
        }                  

}