using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;


class Program
{
    static async Task Main()
    {
        string accessToken = "pk.eyJ1IjoiaGFuZHJpZWxtYXJ0aW5leiIsImEiOiJjbTY2bHd2b2QwMWwzMm9va3IwanhwZDFyIn0.2xdC-rwxL3Guor3N8K5DOQ";
        string origin = "-73.985428,40.748817";
        string destination = "-73.935242,40.730610";
        string url = $"https://api.mapbox.com/directions/v5/mapbox/driving/{origin};{destination}?access_token={accessToken}";

         HttpClient client = new HttpClient();
        var response = await client.GetStringAsync(url);

        // Deserializar el JSON
        var routeData = JsonConvert.DeserializeObject<RouteResponse>(response);

        // Extraer información
        Console.WriteLine("Duración: " + routeData.routes[0].duration / 60 + " minutos");
        Console.WriteLine("Distancia: " + routeData.routes[0].distance / 1000 + " km");
        Console.WriteLine("Ruta: " + routeData.routes[0].geometry);
        Console.ReadKey();// Incluye coordenadas y detalles de la ruta
    }
}

// Clases para deserializar
public class RouteResponse
{
    public Route[] routes { get; set; }
}

public class Route
{
    public double duration { get; set; }
    public double distance { get; set; }
    public string geometry { get; set; }
}