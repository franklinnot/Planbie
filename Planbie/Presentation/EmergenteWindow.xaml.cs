using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.IO.Ports;


namespace Presentation
{
    /// <summary>
    /// Lógica de interacción para EmergenteWindow.xaml
    /// </summary>
    public partial class EmergenteWindow : Window
    {
        private SerialPort puertoSerial;

        public EmergenteWindow()
        {
            InitializeComponent();
            cmbPuertos.ItemsSource = SerialPort.GetPortNames();

        }

        private void AbrirPuerto_Click(object sender, RoutedEventArgs e)
        {
            // Si el puerto ya está abierto, cerrarlo
            if (puertoSerial != null && puertoSerial.IsOpen)
            {
                try
                {
                    puertoSerial.Close();
                    MessageBox.Show("Puerto cerrado con éxito.");
                    // Rehabilitar el ComboBox y cambiar el texto del botón
                    cmbPuertos.IsEnabled = true;
                    (sender as Button).Content = "Abrir Conexión";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al cerrar el puerto: {ex.Message}");
                }
            }
            else
            {
                string puertoSeleccionado = cmbPuertos.SelectedItem as string;

                if (!string.IsNullOrEmpty(puertoSeleccionado))
                {
                    try
                    {
                        // Inicializar el puerto serial y abrirlo
                        puertoSerial = new SerialPort(puertoSeleccionado);
                        puertoSerial.Open();

                        MessageBox.Show($"Puerto {puertoSeleccionado} abierto con éxito.");
                        // Deshabilitar el ComboBox y cambiar el texto del botón
                        cmbPuertos.IsEnabled = false;
                        (sender as Button).Content = "Cerrar Conexión";
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error al abrir el puerto: {ex.Message}");
                    }
                }
                else
                {
                    MessageBox.Show("Selecciona un puerto :)");
                }
            }
        }
    }
}
