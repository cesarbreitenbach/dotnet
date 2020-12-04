
using System.Net.Http;
using RestSharp;
using dotenv.net.Utilities;
using System;

class Sync{

     private  Serilog.Core.Logger log;

    public Sync(Serilog.Core.Logger pLog){
        this.log = pLog;
    }

    public void Marcar(int Codigo, string token){

        var envReader = new EnvReader();
        
         var client = new RestClient(envReader.GetStringValue("API"));
        RestRequest request = new  RestRequest($@"/sync/{Codigo}", Method.PUT);
        request.AddHeader("auth", token);

        var resposta = client.Execute(request);

        if (resposta.StatusCode != System.Net.HttpStatusCode.Created && resposta.StatusCode != System.Net.HttpStatusCode.OK)
        {
            log.Error("Não confirmei a sincronização!!! ");
            log.Error(resposta.Content);
            throw new Exception(resposta.Content);
        }

        log.Information("Fiz a marcação de sucesso da sincronização.");
        
    }
}