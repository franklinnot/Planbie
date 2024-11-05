using HandyControl.Tools.Converter;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO.Ports;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Presentation
{
    public partial class EmergenteWindow : Window
    {
        public static SerialPort? puertoSerial;
        public static string? puerto;
        private string[] puertosAnteriores = { "Carga de puertos" };
        private System.Timers.Timer? puertoTimer;

        public EmergenteWindow()
        {
            InitializeComponent();
            ActualizarPuertos();

            // si el puertoSerial ya esta abierto, modificamos el diseño
            if (puertoSerial != null && puertoSerial.IsOpen)
            {
                UIPuertoAbierto();
            }

        }

        #region Cargar los puertos disponibles en el ComboBox

        private void ActualizarPuertos()
        {
            puertoTimer = new System.Timers.Timer(300);
            puertoTimer.AutoReset = true; // Esta línea asegura que el temporizador se reinicie automáticamente
            puertoTimer.Elapsed += async (sender, e) => await CargarPuertosDisponibles();
            puertoTimer.Start();
        }

        // metodo para cargar los puertos
        private async Task CargarPuertosDisponibles()
        {
            string[] puertosActuales = SerialPort.GetPortNames();

            // compara si hay un cambio en la lista de puertos
            if (!puertosActuales.SequenceEqual(puertosAnteriores))
            {
                puertosAnteriores = puertosActuales;

                // actualizar el ComboBox en el hilo de la interfaz
                await Dispatcher.InvokeAsync(() =>
                {
                    if (puertosActuales.Length > 0)
                    {
                        Debug.WriteLine("Se cargaron nuevos puertos");
                        cmbPuertos.ItemsSource = puertosActuales;
                        btnAbrirPuerto.IsEnabled = true;
                    }
                    else
                    {
                        Debug.WriteLine("No se han encontrado puertos disponibles");
                        cmbPuertos.ItemsSource = new string[] { "No hay puertos disponibles" };
                        btnAbrirPuerto.IsEnabled = false;
                    }

                    // si el puerto ya esta abierto, seleccionarlo en el combo
                    if (!string.IsNullOrEmpty(puerto) && puertosActuales.Contains(puerto))
                    {
                        cmbPuertos.SelectedItem = puerto;
                    }
                    else
                    {
                        cmbPuertos.SelectedIndex = 0;
                    }

                    cmbPuertos.SelectedIndex = 0;
                });
            }

        }

        #endregion

        #region Abrir/cerrar el puerto serial al hacer clic en el botón

        private void AbrirPuerto_Click(object sender, RoutedEventArgs e)
        {
            // si el puerto ya esta abierto, cerrarlo
            if (puertoSerial != null && puertoSerial.IsOpen)
            {
                CerrarPuertoSerial();
            }
            // si no ha sido abierto ningun puerto, abrimos uno
            else
            {
                AbrirPuertoSerial();
            }
        }

        private void AbrirPuertoSerial()
        {
            string? puertoSeleccionado = cmbPuertos.SelectedItem.ToString();

            if (!string.IsNullOrEmpty(puertoSeleccionado))
            {
                try
                {
                    // inicializar el puerto serial y abrirlo
                    puertoSerial = new SerialPort(puertoSeleccionado);
                    puertoSerial.Open();
                    puerto = puertoSeleccionado; // variable que almacena el puerto

                    MessageBox.Show($"Puerto {puertoSeleccionado} abierto con éxito.");

                    // Deshabilitar el ComboBox y cambiar el texto del botón
                    UIPuertoAbierto();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al abrir el puerto: {ex.Message}");
                }
            }
            else
            {
                MessageBox.Show("Selecciona un puerto válido.");
            }
        }

        private void CerrarPuertoSerial()
        {
            try
            {
                // lo cerramos
                puertoSerial?.Close();
                puertoSerial = null;
                puerto = null;

                MessageBox.Show("Puerto cerrado con éxito.");

                // restaurar el estado de la interfaz al cerrar el puerto
                cmbPuertos.IsEnabled = true;
                btn_portText.Text = "Abrir puerto";
                btnAbrirPuerto.Background = (Brush)new BrushConverter().ConvertFrom("#FF660CA7");

                MainWindow.cts?.Cancel();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cerrar el puerto: {ex.Message}");
            }
        }

        #endregion

        // metodo para inhabilitar el combobox y modificar el color y texto del boton si el puerto esta abierto
        private void UIPuertoAbierto()
        {
            cmbPuertos.SelectedItem = puerto; 
            cmbPuertos.IsEnabled = false;     
            btn_portText.Text = "Cerrar puerto";
            btnAbrirPuerto.Background = (Brush)new BrushConverter().ConvertFrom("#FFD62935");
        }

        // Cerrar  la ventana
        private void btn_cerrar_Click(object sender, RoutedEventArgs e)
        {
            puertoTimer?.Stop();
            puertoTimer?.Dispose();
            base.OnClosed(e);

            this.Close();
        }

        #region enventos enter & leave en el boton de abrir puerto
        private void btnAbrirPuerto_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            hoverBotonPort("enter");
        }

        private void btnAbrirPuerto_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            hoverBotonPort("leave");
        }

        private void hoverBotonPort(string accion)
        {
            if (btn_portText.Text == "Cerrar puerto") {
                if (accion == "enter")
                {
                    btnAbrirPuerto.Background = (Brush)new BrushConverter().ConvertFrom("#FFe63946");
                } 
                else if (accion == "leave") {
                    btnAbrirPuerto.Background = (Brush)new BrushConverter().ConvertFrom("#FFD62935");
                }
            }
            else if (btn_portText.Text == "Abrir puerto")
            {
                if (accion == "enter")
                {
                    btnAbrirPuerto.Background = (Brush)new BrushConverter().ConvertFrom("#FF740DBF");
                }
                else if (accion == "leave")
                {
                    btnAbrirPuerto.Background = (Brush)new BrushConverter().ConvertFrom("#FF660CA7");
                }
            }
        }

        #endregion

    }
}
