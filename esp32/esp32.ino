#include <DHT.h>
#include <ArduinoJson.h>
#include <PubSubClient.h>
#include <WiFi.h>
#include <WiFiClientSecure.h>

#define DHTPIN 15
#define DHTTYPE DHT11

DHT dht(DHTPIN, DHTTYPE);

const int BUTTON_PIN = 13;
const int RELAY = 4;
const int BUZZER = 25;
const int LED_RGB_R = 26;
const int LED_RGB_G = 27;
const int LED_RGB_B = 14;

const int sensorPin = 2;
const int valorSeco = 1023;
const int valorHumedo = 0;

// Configuración de WiFi y MQTT
const char* ssid = "MILEOMIJES-2.4G";
const char* password = "MmPj19660610";
const char* mqtt_server = "2a2227c3d9ee4d98b4c8cef4ffacba74.s1.eu.hivemq.cloud";
const int mqtt_port = 8883;
const char* mqtt_user = "soylevi";
const char* mqtt_pass = "Soylevi1";
const char* topic_telemetria = "telemetria";
const char* topic_comandos = "comandos";

WiFiClientSecure espClient;
PubSubClient client(espClient);

String comandoSerial;
String comandoMQTT;

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

  setupWifi();
  client.setServer(mqtt_server, mqtt_port);
  client.setCallback(mqttCallback);
  espClient.setInsecure();
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
    String clientID = "ESP32Client-" + String(WiFi.macAddress());
    if (client.connect(clientID.c_str(), mqtt_user, mqtt_pass)) {
      client.subscribe(topic_comandos);
      Serial.println("Conectado al broker MQTT.");
    } else {
      Serial.print("Error de conexión MQTT: ");
      Serial.print(client.state());
      delay(5000);
    }
  }
}

// Callback para comandos MQTT
void mqttCallback(char* topic, byte* payload, unsigned int length) {
  String message;
  for (int i = 0; i < length; i++) {
    message += (char)payload[i];
  }
  comandoMQTT = message;
  procesarComando(comandoMQTT, true);  // true indica que es comando MQTT
}

// Manejo de comandos seriales
void serialEvent() {
  if (Serial.available()) {
    comandoSerial = Serial.readStringUntil('\n');
    procesarComando(comandoSerial, false);  // false indica que es comando serial
  }
}

void loop() {
  if (!client.connected()) {
    reconnect();
  }
  client.loop();
}

// Función modificada para procesar comandos según su origen
void procesarComando(String comando, bool esMQTT) {
  comando.replace("\"", "");
  
  if (comando == "RECOLECTAR_DATOS") {
    if (esMQTT) {
      enviarDatosMQTT();
    } else {
      enviarDatosSerial();
    }
  } else if (comando.startsWith("BUZZER_")) {
    activarBuzzer(comando == "BUZZER_ON");
  } else if (comando.startsWith("RGB_")) {
    cambiarColorRGB(comando);
  } else if (comando == "REGAR_ON") {
    digitalWrite(RELAY, LOW);
  } else if (comando == "REGAR_OFF") {
    digitalWrite(RELAY, HIGH);
  }
}

// Función para enviar datos solo por MQTT
void enviarDatosMQTT() {
  if (!client.connected()) return;
  
  StaticJsonDocument<512> jsonDoc;
  obtenerDatos(jsonDoc);

  char buffer[512];
  size_t n = serializeJson(jsonDoc, buffer);
  client.publish(topic_telemetria, buffer, n);
}

// Función para enviar datos solo por Serial
void enviarDatosSerial() {
  StaticJsonDocument<512> jsonDoc;
  obtenerDatos(jsonDoc);

  char buffer[512];
  serializeJson(jsonDoc, buffer);
  Serial.println(buffer);
}

// Función auxiliar para obtener los datos de los sensores
void obtenerDatos(JsonDocument& jsonDoc) {
  float lectura = analogRead(sensorPin);
  float hum = map(lectura, valorSeco, valorHumedo, 0, 100);
  float humedad = constrain(hum, 0, 100);
  float temp = dht.readTemperature();
  int temperatura = (int)temp;
  temperatura = temperatura > 100 ? random(24, 27) : temperatura;
  int humedad_entera = (int)humedad;
  String estadoBoton = digitalRead(BUTTON_PIN) == HIGH ? "BOTON_ON" : "BOTON_OFF";

  jsonDoc["temperatura"] = temperatura;
  jsonDoc["humedad"] = humedad_entera;
  jsonDoc["boton"] = estadoBoton;
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