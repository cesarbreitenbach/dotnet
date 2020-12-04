
// Exemplo de uso do linq
// var qtd = produtos.OrderBy( p => p.NomeProduto).Where(p => p.NomeProduto.Contains("COLORAMA")).Count();
using System.Collections.Generic;
using dotenv.net;
using System;
using Utils;

DotEnv.Config(false);

try
{
 var token = new Auth().GetToken();

 Serilog.Core.Logger log =  Log.CriarLogger();
 
    new ReplicaProdutos.ApiProdutos(log).Processar(token);
    new ReplicaEstoque.ApiEstoque(log).Processar(token);
    new Sync(log).Marcar(1, token);
    
}
catch (System.Exception e) 
{
    var listaEmail = new List<string>{"cesar.eucatur@gmail.com", "cesar.breitenbach@gmail.com", "paulo@approachmobile.com", "paulortonialfilho@gmail.com", "paulortonial@hotmail.com" };
    var corpo =e.Message+" \n "+e.StackTrace;
   new Mail().EnviarEmail("Erro na sincronização Appharma!", corpo, listaEmail);

    throw;
}
