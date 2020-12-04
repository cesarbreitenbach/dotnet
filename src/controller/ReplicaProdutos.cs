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
        private  Serilog.Core.Logger log;

        IDictionary<int, Produto> produtoAnterior;

        public ApiProdutos(Serilog.Core.Logger pLog){

            this.log = pLog;

        }
     
        private void CarregarSerializado()
        {
            if (File.Exists(ARQUIVO_SERIALIZADO))
            {
         //       log.Information("Deserializando....");
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
                log.Information("Inicio da sincronização de produtos");
                CarregarSerializado();

                var alterados = new List<Produto>();
                log.Information("Buscando produtos...");
                var produtos = BuscarProdutos();
                log.Information("Quantidade de produtos encontrados: "+ produtos.Count());

                if (produtoAnterior is not null)
                {
                    log.Debug("Existe produto anterior... ");
              //      log.Information("Vou comparar lista...");
                    Parallel.ForEach( produtos, prod => 
                    {
                        if (produtoAnterior.ContainsKey(prod.CodigoProduto))
                        {
                            if ( !prod.Equals(produtoAnterior[prod.CodigoProduto]))
                            {
                                log.Information("Esse produto foi alterado: "+prod.CodigoProduto+" "+ prod.NomeProduto);
                                prod.Novo = 0;
                                alterados.Add(prod);
                            }
                        }
                        else
                        {
                            log.Information("Este é um produto novo..: "+prod.CodigoProduto+" "+ prod.NomeProduto);
                            prod.Novo = 1;
                            alterados.Add(prod);
                        }
                    });
                    AtualizarNuvem(alterados, token);  
                }

                produtoAnterior = ConverterEmDicionario(produtos);

                using var arquivo = new FileStream(ARQUIVO_SERIALIZADO, FileMode.OpenOrCreate);
                var formatter = new BinaryFormatter();
          //      log.Information("Serializando os produtos.....");
                formatter.Serialize(arquivo, produtoAnterior);
                log.Information("Sincronização de produtos finalizada!");

            }
            catch (Exception e)
            {
                log.Error("Erro: "+e.Message);
                throw;
            }
        }

        public void AtualizarNuvem(List<Produto> produtos, string token)
        {   

            EnvReader envReader = new EnvReader();
            

            var http = new HttpClient();

            foreach(var p in produtos)
            {
                log.Information("Codigo produto: "+ p.CodigoProduto +" Nome: "+p.NomeProduto);
                var jjjhonson = new {
                        codigo_produto=p.CodigoProduto,
                        codigo_barras= p.CodigoBarra,
                        nome=p.NomeProduto,
                        id_grupo= p.CodigoGrupo,
                        id_sessao=p.CodigoSessao,
                        principio=p.Formula,
                    };

                var client = new RestClient(envReader.GetStringValue("API"));


            
                RestRequest request;
                if (p.Novo == 0){
                    request = new RestRequest($@"/produtos?id={p.CodigoProduto}", Method.PUT);
                }else{
                    request = new RestRequest("/produtos/rep", Method.POST);
                }

                request.AddHeader("auth", token);
                request.AddJsonBody(jjjhonson);

                var resposta = client.Execute(request);

                 if (resposta.StatusCode != System.Net.HttpStatusCode.Created && resposta.StatusCode != System.Net.HttpStatusCode.OK)
                {
                    log.Error("Não atualizei produto!! ");
                    log.Error(resposta.Content);
                    System.Console.WriteLine(jjjhonson);
                    throw new Exception(resposta.Content.ToString());
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

