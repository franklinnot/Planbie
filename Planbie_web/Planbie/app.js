if (window.location.pathname.includes('index.html')) {
    document.querySelector('.btn_connect').addEventListener('click', () => {
        const clusterURL = "2a2227c3d9ee4d98b4c8cef4ffacba74.s1.eu.hivemq.cloud";
        const port = "8883";
        const username = "soylevi";
        const password = "Soylevi1";
        const commandTopic = "comandos";
        const telemetryTopic = "telemetria";

        if (!clusterURL || !port || !username || !password || !commandTopic || !telemetryTopic) {
            alert('Por favor, completa todos los campos');
            return;
        }

        // Guardar datos de conexión en LocalStorage
        sessionStorage.setItem('mqttConfig', JSON.stringify({
            clusterURL,
            port,
            username,
            password,
            commandTopic,
            telemetryTopic,
        }));

        // Redirigir a main.html
        window.location.href = 'main.html';
    });
}


//main.html
if (window.location.pathname.includes('main.html')) {
    window.addEventListener('DOMContentLoaded', () => {
        const mqttConfig = JSON.parse(sessionStorage.getItem('mqttConfig'));

        if (!mqttConfig) {
            alert('No se encontraron datos de conexión. Por favor, conéctate primero.');
            window.location.href = 'index.html';
            return;
        }

        const brokerURL = `http://2a2227c3d9ee4d98b4c8cef4ffacba74.s1.eu.hivemq.cloud:8884/mqtt`;

        // Configuración de cliente MQTT con TLS
        const client = mqtt.connect(brokerURL, {
            username: mqttConfig.username,
            password: mqttConfig.password,
            protocol: 'wss',
            reconnectPeriod: 1000, // Reintentar conexión cada 1s
            rejectUnauthorized: false // Deshabilitar verificación de certificados (solo para pruebas)
        });

        // Manejo de eventos
        client.on('connect', () => {
            console.log('Conexión exitosa al broker.');
            client.subscribe([mqttConfig.commandTopic, mqttConfig.telemetryTopic], (err) => {
                if (err) {
                    console.error('Error al suscribirse:', err);
                } else {
                    console.log('Suscripción exitosa a tópicos.');
                }
            });

            // Inicia la publicación periódica de "RECOLECTAR_DATOS"
            startDataCollection(client, mqttConfig.commandTopic);
        });

        client.on('error', (err) => {
            console.error('Error de conexión:', err);
        });

        client.on('message', (topic, message) => {
            if (topic === mqttConfig.telemetryTopic) {
                console.log(`Mensaje recibido en ${topic}: ${message.toString()}`);
                handleTelemetryMessage(message);
            }
        });

        function handleTelemetryMessage(message) {
            try {
                const data = JSON.parse(message.toString());
                const { temperatura, humedad, boton } = data;

                updateGauge(parseInt(temperatura));
                updateProgress(parseInt(humedad));
                updateLineChart(temperatura);
                updatePromedioTemperatura(temperatura);

                publicar(temperatura, humedad, boton);
            } catch (error) {
                console.error('Error al procesar el mensaje:', error);
            }
        }

        async function publicar(temperatura, humedad, boton) {
            try {
                if (temperatura >= 40 && humedad < 60) {
                    await client.publish(mqttConfig.commandTopic, 'REGAR_ON');
                    await client.publish(mqttConfig.commandTopic, 'RGB_BLUE');
                    await client.publish(mqttConfig.commandTopic, 'BUZZER_ON');
                } else if (boton === 'BOTON_ON') {
                    await client.publish(mqttConfig.commandTopic, 'REGAR_ON');
                } else {
                    await client.publish(mqttConfig.commandTopic, 'REGAR_OFF');
                    await client.publish(mqttConfig.commandTopic, 'RGB_VERDE');
                }
            } catch (error) {
                console.error('Error al publicar mensajes:', error);
            }
        }

        function startDataCollection(client, topic) {
            setInterval(() => {
                (async () => {
                    try {
                        console.log('Enviando comando: RECOLECTAR_DATOS');
                        await client.publish(topic, 'RECOLECTAR_DATOS');
                    } catch (error) {
                        console.error('Error al publicar RECOLECTAR_DATOS:', error);
                    }
                })();
            }, 5000);
        }
        

        
        
        
        
        function updateGauge(value) {
            // Encuentra el elemento del gauge
            const gauge = document.querySelector('.gauge');
            const gaugeValue = document.getElementById('gaugeValue');
        
            // Limita el valor entre 0 y 100
            const clampedValue = Math.min(Math.max(value, 0), 100);
        
            // Calcula la rotación (mapea el rango 0-100 a 0-180 grados)
            const rotation = (clampedValue / 100) * 360;
        
            // Actualiza el estilo del gauge y el texto del valor
            gauge.style.setProperty('--rotation', `${rotation}deg`);
            gaugeValue.textContent = `${clampedValue} °C`;
        }
        
        function updateProgress(slider){
            var value = slider;
            var wave = document.querySelector(".wave");
            wave.style.top = (33+value*-1.23)+"%";
            document.querySelector(".progress-title").innerHTML = value+"%";
        }

        // Almacenar las últimas temperaturas
        let temperaturasUltimaHora = [];
        let tiempoLimite = 60;  // Definir el tiempo de una hora (en minutos, en este caso 1 por cada minuto)

        function updatePromedioTemperatura(temperatura) {
            temperaturasUltimaHora.push(temperatura);

            // Mantener solo las temperaturas de la última hora
            if (temperaturasUltimaHora.length > tiempoLimite) {
                temperaturasUltimaHora.shift();  // Elimina el primer valor (más antiguo)
            }

            // Calcular el promedio
            const sumaTemperaturas = temperaturasUltimaHora.reduce((acc, temp) => acc + temp, 0);
            const promedio = sumaTemperaturas / temperaturasUltimaHora.length;

            // Actualizar el label con el promedio
            document.getElementById('prom-temp-value').innerText = `${promedio.toFixed(2)}°C`;
        }

        
        ///line chart

        // Crear un gráfico de línea para mostrar la temperatura
        const ctx = document.getElementById('lineChart').getContext('2d');
        const lineChart = new Chart(ctx, {
            type: 'line',
            data: {
                labels: [], // Aquí van las etiquetas del tiempo (e.g., horas)
                datasets: [{
                    label: 'Temperatura (°C)',
                    data: [], // Aquí se guardarán las temperaturas
                    borderColor: 'rgba(75, 192, 192, 1)', // Color de la línea
                    backgroundColor: 'rgba(75, 192, 192, 0.2)', // Color de fondo bajo la línea
                    borderWidth: 2, // Grosor de la línea
                    fill: true, // Rellenar el área bajo la línea
                    tension: 0.4, // Suaviza la línea
                }]
            },
            options: {
                responsive: true,
                scales: {
                    x: {
                        type: 'linear',
                        position: 'bottom',
                        title: {
                            display: true,
                            text: 'Tiempo (h)'
                        }
                    },
                    y: {
                        min: 0, // Mínimo valor en el eje Y
                        max: 100, // Máximo valor en el eje Y
                        title: {
                            display: true,
                            text: 'Temperatura (°C)'
                        }
                    }
                },
                plugins: {
                    legend: {
                        display: true,
                        position: 'top',
                    },
                },
            }
        });

        // Actualización de los datos en el gráfico
        let tiempo = 0;
        function updateLineChart(temperatura) {
            tiempo += 1;  // Aumenta el tiempo por cada nuevo dato
            lineChart.data.labels.push(tiempo);  // Agrega el tiempo al eje X
            lineChart.data.datasets[0].data.push(temperatura);  // Agrega la temperatura al eje Y

            // Limita el número de puntos a 10 (puedes ajustar este valor)
            if (lineChart.data.labels.length > 10) {
                lineChart.data.labels.shift();  // Elimina el primer valor (tiempo)
                lineChart.data.datasets[0].data.shift(); // Elimina la primera temperatura
            }

            // Actualiza el gráfico
            lineChart.update();
        }

    });
}