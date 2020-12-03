
// Exemplo de uso do linq
// var qtd = produtos.OrderBy( p => p.NomeProduto).Where(p => p.NomeProduto.Contains("COLORAMA")).Count();
using dotenv.net;
using Utils;

DotEnv.Config(false);

 var token = new Auth().GetToken();

 Serilog.Core.Logger log =  Log.CriarLogger();

 

new ReplicaProdutos.ApiProdutos(log).Processar(token);


new ReplicaEstoque.ApiEstoque(log).Processar(token);

