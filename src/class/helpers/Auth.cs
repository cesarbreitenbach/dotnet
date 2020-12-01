using System;
using System.IO;
using dotenv.net.Utilities;
using Newtonsoft.Json;
using RestSharp;
using Serilog;

class Auth {

    public string GetToken()
    {
        EnvReader envReader = new EnvReader();

        string usuario = envReader.GetStringValue("USUARIO");
        string senha = envReader.GetStringValue("SENHA");

        var objetoSenha = new {
            cpf=usuario,
            password=senha,
        };

        var client = new RestClient(envReader.GetStringValue("API"));
            
        var request = new RestRequest("/sessions", Method.POST);

     //   request.AddHeader("auth", token);
        request.AddJsonBody(objetoSenha);

        IRestResponse restResponse = client.Execute(request);

        if (restResponse.StatusCode != System.Net.HttpStatusCode.OK )
        {
            Log.Error("Login na api falhou!: "+ restResponse.Content);
            throw new Exception("Login na API falhou!"+ restResponse.Content);
        }

        dynamic resultado = JsonConvert.DeserializeObject(restResponse.Content);

        return "Bearer "+resultado.token;
    }
   
}