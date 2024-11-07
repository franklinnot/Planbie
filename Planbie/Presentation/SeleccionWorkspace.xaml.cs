using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using LiveCharts;
using LiveCharts.Wpf;
using MQTT;
using Newtonsoft.Json.Linq;
using Presentation.Logica;
using System.Diagnostics;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Presentation
{

    public partial class SeleccionWorkspace : Window
    {

        public static string workspace = string.Empty;

        public SeleccionWorkspace()
        {
            InitializeComponent();

            if (!string.IsNullOrEmpty(workspace)) {
                EstablecerTipoConexion(workspace);
            }
        }


        private void btnMqtt_Click(object sender, RoutedEventArgs e)
        {
            EstablecerTipoConexion("MQTT");
            Debug.WriteLine($"Tipo de conexion '{workspace}' establecida");
        }

        private void btnArduino_Click(object sender, RoutedEventArgs e)
        {
            EstablecerTipoConexion("ARDUINO");
            Debug.WriteLine($"Tipo de conexion '{workspace}' establecida");
        }

        private async void btn_ConexionActiva_Click(object sender, RoutedEventArgs e)
        {
            if (workspace == "ARDUINO")
            {
                EmergenteWindow.conexionCerrada = true;
                ArduinoControl.Instancia.Disconnect();
            }
            else
            {
                btn_ConexionActiva.IsEnabled = false;
                await ConnectionMQTT.Instancia.Disconnect();
                btn_ConexionActiva.IsEnabled = true;
            }


            Debug.WriteLine($"Tipo de conexion '{workspace}' cancelada");
            MainWindow.cts?.Cancel();
            workspace = string.Empty;

            panel_conexiones.Visibility = Visibility.Visible;
            panel_ConexionActiva.Visibility = Visibility.Hidden;

        }

        private void EstablecerTipoConexion(string conexion) 
        {
            workspace = conexion;
            txt_ConexionActiva.Text = workspace;
            panel_conexiones.Visibility = Visibility.Hidden;
            panel_ConexionActiva.Visibility = Visibility.Visible;
        }

        private void btn_cerrar_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

    }
}
