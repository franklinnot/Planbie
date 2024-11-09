#include <DHT.h>
#include <ArduinoJson.h>
#include <PubSubClient.h>
#include <WiFi.h>

#define DHTPIN 15
#define DHTTYPE DHT11

DHT dht(DHTPIN, DHTTYPE);

const int BUTTON_PIN = 13;  // boton
const int RELAY = 4;        // motor de agua
const int BUZZER = 5;       // buzzer
const int LED_RGB_R = 26;
const int LED_RGB_G = 27;
const int LED_RGB_B = 14;

const int sensorPin = 2;
const int valorSeco = 1023;
const int valorHumedo = 0;
// Configuración de MQTT
const char* ssid = "K50";
const char* password = "72655470K";
const char* mqtt_server = "243823b870f449cf81b31d53147af60e.s1.eu.hivemq.cloud";  // Cambia al host de tu broker MQTT
const int mqtt_port = 8883;
const char* mqtt_user = "franklin";
const char* mqtt_pass = "dotnot";
const char* topic_telemetria = "telemetria";
const char* topic_comandos = "comandos";

WiFiClient espClient;
PubSubClient client(espClient);

String comandoActual;

String estadoBoton = "NO_REGANDO";
String estadoBotonAnterior = "NO_REGANDO";

void setup() {
  Serial.begin(9600);
  dht.begin();

  pinMode(BUTTON_PIN, INPUT);
  pinMode(RELAY, OUTPUT);
  pinMode(BUZZER, OUTPUT);
  pinMode(LED_RGB_R, OUTPUT);
  pinMode(LED_RGB_G, OUTPUT);
  pinMode(LED_RGB_B, OUTPUT);
  digitalWrite(RELAY, HIGH);

  //setupWifi();
  //client.setServer(mqtt_server, mqtt_port);
  //client.setCallback(mqttCallback);
}

void setupWifi() {
  delay(10);
  WiFi.begin(ssid, password);
  while (WiFi.status() != WL_CONNECTED) {
    delay(500);
    Serial.print(".");
  }
  Serial.println("Conectado a la red WiFi.");
}

void reconnect() {
  while (!client.connected()) {
    if (client.connect("ArduinoClient", mqtt_user, mqtt_pass)) {
      client.subscribe(topic_comandos);
      Serial.println("Conectado al broker MQTT.");
    } else {
      delay(5000);
    }
  }
}

void mqttCallback(char* topic, byte* payload, unsigned int length) {
  String message;
  for (int i = 0; i < length; i++) {
    message += (char)payload[i];
  }
  comandoActual = message;
  procesarComando(comandoActual);
}

void serialEvent() {
  if (Serial.available()) {
    comandoActual = Serial.readStringUntil('\n');
    procesarComando(comandoActual);
  }
}

void loop() {
  //if (!client.connected()) {
  //reconnect();
  //}
  //client.loop(); // MQTT loop para recibir mensajes y mantener la conexión

  String estadoActual = digitalRead(BUTTON_PIN) == HIGH ? "BOTON_ON" : "BOTON_OFF";

  // solo cambia el estadoBoton si el estado actual es diferente al anterior
  if (estadoActual != estadoBotonAnterior) {
    estadoBoton = estadoActual;
    estadoBotonAnterior = estadoActual;
  }
  delay(100);
}

// Función principal para procesar comandos
void procesarComando(String comando) {
  if (comando == "RECOLECTAR_DATOS") {
    enviarDatos();
  } else if (comando.startsWith("BUZZER_")) {
    activarBuzzer(comando == "BUZZER_ON");
  } else if (comando.startsWith("RGB_")) {
    cambiarColorRGB(comando);
  } else if (comando == "REGAR_ON") {
    digitalWrite(RELAY, LOW);  // Activar motor de agua
  } else if (comando == "REGAR_OFF") {
    digitalWrite(RELAY, HIGH);  // Apagar motor de agua
  }
}

void enviarDatos() {
  float lectura = analogRead(sensorPin);
  float hum = map(lectura, valorSeco, valorHumedo, 0, 100);
  float humedad = constrain(hum, 0, 100);
  float temp = dht.readTemperature();
  int temperatura = (int)temp;
  int humedad_entera = (int)humedad;

  StaticJsonDocument<200> jsonDoc;
  jsonDoc["temperatura"] = temperatura;
  jsonDoc["humedad"] = humedad_entera;
  jsonDoc["boton"] = estadoBoton;

  char buffer[256];
  size_t n = serializeJson(jsonDoc, buffer);
  Serial.println(buffer);  // Envío por Serial

  if (client.connected()) {
    client.publish(topic_telemetria, buffer, n);
  }
}

void activarBuzzer(bool estado) {
  digitalWrite(BUZZER, estado ? HIGH : LOW);
}

void cambiarColorRGB(String colorComando) {
  if (colorComando == "RGB_VERDE") {
    digitalWrite(LED_RGB_R, LOW);
    digitalWrite(LED_RGB_G, HIGH);
    digitalWrite(LED_RGB_B, LOW);
  } else if (colorComando == "RGB_BLUE") {
    digitalWrite(LED_RGB_R, LOW);
    digitalWrite(LED_RGB_G, LOW);
    digitalWrite(LED_RGB_B, HIGH);
  } else {
    digitalWrite(LED_RGB_R, LOW);
    digitalWrite(LED_RGB_G, LOW);
    digitalWrite(LED_RGB_B, LOW);
  }
}