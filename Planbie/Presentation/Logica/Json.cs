using Newtonsoft.Json;
using System.Diagnostics;
using System.IO;

namespace Presentation.Logica
{

    class Json
    {
        public static string? ArchivoJson { get; set; }

        public Json(string? jsonFile = null) { 
            ArchivoJson = string.IsNullOrEmpty(jsonFile) ? "temperatures.json" : jsonFile;
        }

        // devuelve una lista de temperaturas a partir del json
        public static async Task<List<TempData>> LeerDatosJson()
        {
            try
            {
                // si el archivo no existe, se crea
                if (!File.Exists(ArchivoJson))
                {
                    var listaVacia = new List<TempData>();
                    string jsonVacio = JsonConvert.SerializeObject(listaVacia, Formatting.Indented);
                    await File.WriteAllTextAsync(ArchivoJson, jsonVacio); 
                    return listaVacia;
                }

                // leemos el contenido
                string jsonData = await File.ReadAllTextAsync(ArchivoJson);

                // desearilzamos el json a una lista de objetos TemData
                var registros = JsonConvert.DeserializeObject<List<TempData>>(jsonData) ?? new List<TempData>();

                DateTime tiempoLimite = DateTime.Now.AddHours(-1);

                var registrosFiltrados = registros.Where(r => r.Fecha > tiempoLimite).ToList();

                return registrosFiltrados;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error al leer el archivo JSON: {ex.Message}");
                return new List<TempData>(); // Si hay un error, restorna una lista vacia
            }
        }


        // registra una nuevo objeto de Temperatura en el Json
        public static async Task RegistrarTemperaturaJson(TempData nuevoRegistro)
        {
            try
            {
                List<TempData> registros = new List<TempData>();

                if (File.Exists(ArchivoJson))
                {
                    registros = await LeerDatosJson(); // obtenemos la lista de registros
                }

                // agregar el nuevo registro a la lista
                registros.Add(nuevoRegistro);

                // guardar la lista actualizada en el archivo JSON
                string json = JsonConvert.SerializeObject(registros, Formatting.Indented);
                await File.WriteAllTextAsync(ArchivoJson, json);

                Debug.WriteLine("Registro guardado exitosamente:");
                Debug.WriteLine($"JSON: {json.Take(24)}..."); // mostramos los primeros 24 caracteres
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error al registrar la temperatura: {ex.Message}");
            }
        }


    }
}
