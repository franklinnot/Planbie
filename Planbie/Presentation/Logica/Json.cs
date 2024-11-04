using LiveCharts.Defaults;
using LiveCharts;
using System;
using System.Windows;
using System.Windows.Input;
using LiveCharts.Wpf;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using System.IO.Ports;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics.Tracing;
using System.Windows.Media;
using SharpVectors.Converters;
using SharpVectors.Renderers.Wpf;
using SharpVectors.Renderers;
using System;

namespace Presentation.Logica
{

    class Json
    {
        public static string? ArchivoJson { get; set; }

        public Json(string? jsonFile = null) { 
            ArchivoJson = string.IsNullOrEmpty(jsonFile) ? "temperatures.json" : jsonFile;
        }

        // devuelve una lista de temperaturas a partir del json
        public static List<TempData> LeerDatosJson()
        {
            if (!File.Exists(ArchivoJson))
            {
                var listaVacia = new List<TempData>();
                File.WriteAllText(ArchivoJson, JsonConvert.SerializeObject(listaVacia, Formatting.Indented));
                return listaVacia;
            }

            string json = File.ReadAllText(ArchivoJson);
            return JsonConvert.DeserializeObject<List<TempData>>(json);
        }

        // registra una nuevo objeto de Temperatura en el Json
        public static async Task AgregarRegistroJson(TempData nuevoRegistro)
        {
            List<TempData> registros = LeerDatosJson();
            registros.Add(nuevoRegistro);

            Debug.WriteLine(JsonConvert.SerializeObject(registros, Formatting.Indented));

            // guardar la lista de temperaturas en el json
            string json = JsonConvert.SerializeObject(registros, Formatting.Indented);
            File.WriteAllText(ArchivoJson, json);

        }
    }
}
