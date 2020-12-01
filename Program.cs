
// Exemplo de uso do linq
// var qtd = produtos.OrderBy( p => p.NomeProduto).Where(p => p.NomeProduto.Contains("COLORAMA")).Count();
using dotenv.net;

 DotEnv.Config(false);

 var token = new Auth().GetToken();

 

new ReplicaProdutos.ApiProdutos().Processar(token);


new ReplicaEstoque.ApiEstoque().Processar(token);

