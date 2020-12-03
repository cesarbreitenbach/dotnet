using System;
using System.Collections.Generic;
using Dapper;
using System.Linq;
using System.Threading.Tasks;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using Utils;
using System.Net.Http;
using System.Text.Json;
using RestSharp;
using dotenv.net.Utilities;

namespace ReplicaEstoque
{
    class ApiEstoque
    {
        public string ARQUIVO_SERIALIZADO = "est_old.bin";
        private  Serilog.Core.Logger log;

        IDictionary<int, ClassEstoque> estoqueAnterior;
     
        private void CarregarSerializado()
        {
            if (File.Exists(ARQUIVO_SERIALIZADO))
            {
                using var arquivo = new FileStream(ARQUIVO_SERIALIZADO, FileMode.OpenOrCreate);
                var formatter = new BinaryFormatter();
                var dicionario = formatter.Deserialize(arquivo)
                    as IDictionary<int, ClassEstoque>;
                estoqueAnterior = dicionario;
            }
        }

        public ApiEstoque(Serilog.Core.Logger pLog){

            this.log = pLog;

        }

        public void Processar(String token)
        {
            try
            {

                log.Information("Inicio da sincronização de estoque ");
                CarregarSerializado();

                var alterados = new List<ClassEstoque>();
                log.Information("Buscando estoque...");
                var estoques = BuscaEstoque();
                log.Information("Quantidade de estoques encontrados: "+ estoques.Count());

                if (estoqueAnterior is not null)
                {
                    log.Debug("Existe produto anterior... ");
                   // log.Information("Vou comparar lista...");
                    Parallel.ForEach( estoques, est => 
                    {
                        if (estoqueAnterior.ContainsKey(est.CodigoEstoque))
                        {
                            if ( !est.Equals(estoqueAnterior[est.CodigoEstoque]))
                            {
                                log.Information("Esse estoque foi alterado: "+est.CodigoEstoque+"  codigo do produto: "+ est.CodigoProduto);
                                est.Novo = 0;
                                alterados.Add(est);
                            }
                        }
                        else
                        {
                            log.Information("Estoque novo..: "+est.CodigoEstoque+"  produto: "+ est.CodigoProduto);
                            est.Novo = 1;
                            alterados.Add(est);
                        }
                    });
                    AtualizarNuvem(alterados, token);  
                }

                estoqueAnterior = ConverterEmDicionario(estoques);

                using var arquivo = new FileStream(ARQUIVO_SERIALIZADO, FileMode.OpenOrCreate);
                var formatter = new BinaryFormatter();
            //    log.Information("Serigalizando o arquivo...");
                formatter.Serialize(arquivo, estoqueAnterior);
                log.Information("Sincronização de estoque finalizada!");

            }
            catch (Exception e)
            {
                log.Information("Erro.. "+e.Message);
                throw;
            }
        }

        public void AtualizarNuvem(List<ClassEstoque> produtos, string token)
        {

            EnvReader envReader = new EnvReader();
            

            var http = new HttpClient();

            log.Information("Verificando se preciso atualizar o estoque na nuvem... ");
            foreach(var p in produtos)
            {
                log.Information("Codigo Estoque: "+ p.CodigoEstoque+" Produto: "+p.CodigoProduto);
                
                var jjjhonson = new {
                        id_loja=p.CodigoEmpresa,
                        codigo_produto=p.CodigoProduto,
                        codigo_barras= p.CodigoBarra,
                        qtd_estoque= p.Estoque,
                        preco_venda=p.PrecoVenda,
                        preco_promocao=p.PrecoPromocao,
                        fabricante=p.Fabricante,
                        status=p.Ativo,
                    };

                var client = new RestClient(envReader.GetStringValue("API"));

                RestRequest request;

                if (p.Novo == 0){
                    log.Information("Alterar estoque: "+jjjhonson.ToString());
                    request = new RestRequest($@"/estoque/{p.CodigoEmpresa}/{p.CodigoProduto}", Method.PUT);
                }else{
                     log.Information("Adicionar estoque: "+jjjhonson.ToString());
                     request = new RestRequest("/estoque/", Method.POST);
                }

                request.AddHeader("auth", token);
                request.AddJsonBody(jjjhonson); 

                var resposta = client.Execute(request);

                if (resposta.StatusCode != System.Net.HttpStatusCode.Created && resposta.StatusCode != System.Net.HttpStatusCode.OK)
                {
                    log.Error("Não atualizei o estoque: ");
                    log.Error(resposta.Content);
                    throw new Exception(resposta.Content);
                }
            }

        }

        public IEnumerable<ClassEstoque> BuscaEstoque()
        {
            using var con = Conexao.GetConnectionSqlServer();

            var vSql = $@"
                                SELECT pe.CodigoEstoque, p.codigoProduto, pe.CodigoEmpresa, p.CodigoBarra, pe.Estoque, pe.PrecoVenda, pe.PrecoPromocao, REPLACE(pf.NomeFabricante, '''','') AS Fabricante, pe.Ativo
                                FROM Produto_Estoque pe
                                INNER JOIN Produto p ON pe.CodigoProduto = p.CodigoProduto
                                INNER JOIN Produto_Fabricante pf ON p.CodigoFabricante = pf.CodigoFabricante
                                WHERE  p.CodigoBarra <> '' AND p.CodigoBarra IS NOT NULL";

          //  con.Open();
            var estoque = con.Query<ClassEstoque>(vSql);
           // con.Close();
            return estoque;
        }

        public IDictionary<int, ClassEstoque> ConverterEmDicionario(IEnumerable<ClassEstoque> estoque)
        {
            var dicionario = estoque.ToDictionary(e => e.CodigoEstoque,  e => e);
            return dicionario;
        }
    }
}

