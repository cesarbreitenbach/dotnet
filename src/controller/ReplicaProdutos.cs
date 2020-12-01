using System;
using System.Collections.Generic;
using Dapper;
using System.Linq;
using System.Threading.Tasks;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using Utils;
using dotenv.net.Utilities;
using System.Net.Http;
using RestSharp;

namespace ReplicaProdutos
{
    class ApiProdutos
    {
        public string ARQUIVO_SERIALIZADO = "prod_old.bin";
        private static Serilog.Core.Logger log = Log.CriarLogger();

        IDictionary<int, Produto> produtoAnterior;
     
        private void CarregarSerializado()
        {
            if (File.Exists(ARQUIVO_SERIALIZADO))
            {
                log.Information("Deserializando....");
                using var arquivo = new FileStream(ARQUIVO_SERIALIZADO, FileMode.OpenOrCreate);
                var formatter = new BinaryFormatter();
                var dicionario = formatter.Deserialize(arquivo)
                    as IDictionary<int, Produto>;
                produtoAnterior = dicionario;
            }
        }

        public void Processar(string token)
        {
            try
            {

                log.Information("Iniciou o processamento dos produtos...");
                CarregarSerializado();

                var alterados = new List<Produto>();
                log.Information("Buscando produtos...");
                var produtos = BuscarProdutos();
                log.Information("Quantidade de produtos buscados: "+ produtos.Count());

                if (produtoAnterior is not null)
                {
                    log.Debug("Existe produto anterior... ");
                    log.Information("Vou comparar lista...");
                    Parallel.ForEach( produtos, prod => 
                    {
                        if (produtoAnterior.ContainsKey(prod.CodigoProduto))
                        {
                            if ( !prod.Equals(produtoAnterior[prod.CodigoProduto]))
                            {
                                log.Information("Esse produto foi alterado: "+prod.CodigoProduto+" "+ prod.NomeProduto);
                                alterados.Add(prod);
                            }
                        }
                        else
                        {
                            log.Information("Produto novo..: "+prod.CodigoProduto+" "+ prod.NomeProduto);
                            alterados.Add(prod);
                        }
                    });
                    AtualizarNuvem(alterados, token);  
                }

                produtoAnterior = ConverterEmDicionario(produtos);

                using var arquivo = new FileStream(ARQUIVO_SERIALIZADO, FileMode.OpenOrCreate);
                var formatter = new BinaryFormatter();
                log.Information("Serigalizando o arquivo...");
                formatter.Serialize(arquivo, produtoAnterior);
                log.Information("Processo finalizado..");

            }
            catch (Exception e)
            {
                log.Information("Erro.. "+e.Message);
                throw;
            }
        }

        public void AtualizarNuvem(List<Produto> produtos, string token)
        {   

            EnvReader envReader = new EnvReader();
            

            var http = new HttpClient();

            log.Information("Tenho que fazer alguma coisa com os produtos: ");
            foreach(var p in produtos)
            {
                log.Information("Codigo produto alterado: "+ p.CodigoProduto);
                
                var jjjhonson = new {
                        codigo_barras= p.CodigoBarra,
                        nome=p.NomeProduto,
                        id_grupo= p.CodigoGrupo,
                        id_sessao=p.CodigoSessao,
                        principio=p.Formula,
                    };

                var client = new RestClient(envReader.GetStringValue("API"));
            
                var request = new RestRequest($@"/produtos?id={p.CodigoProduto}", Method.PUT);

                request.AddHeader("auth", token);
                request.AddJsonBody(jjjhonson);

                var resposta = client.Execute(request);

                if (resposta.StatusCode != System.Net.HttpStatusCode.Created)
                {
                    log.Error("Não atualizei produto!! "+resposta.Content);
                    throw new Exception(resposta.Content);
                }

            }
        }

        public IEnumerable<Produto> BuscarProdutos()
        {
            using var con = Conexao.GetConnectionSqlServer();
            var vSql = "SELECT p.CodigoProduto, p.CodigoBarra, p.NomeProduto, p.CodigoSessao, p.CodigoGrupo, pf.NomeFormula as formula FROM produto p "+
                  " LEFT JOIN Produto_Formula pf ON p.CodigoFormula = pf.CodigoFormula "+
                  "WHERE p.CodigoBarra <> '' AND p.CodigoBarra IS NOT NULL ";

            con.Open();
            var produtos = con.Query<Produto>(vSql);
            con.Close();
            return produtos;
        }

        public IDictionary<int, Produto> ConverterEmDicionario(IEnumerable<Produto> produtos)
        {
            var dicionario = produtos.ToDictionary(p => p.CodigoProduto,  p => p);
            return dicionario;
        }
    }
}

