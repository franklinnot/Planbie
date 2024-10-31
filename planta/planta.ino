#include <DHT.h>
#include <Wire.h>
#include <LiquidCrystal_I2C.h>
#include <ArduinoJson.h>

#define DHTPIN 7
#define DHTTYPE DHT11
DHT dht(DHTPIN, DHTTYPE);
int relay = 13;
//const int TRIG_PIN = 2;
//const int ECHO_PIN = 4;
const int BUZZER_PIN = 8;
const int RED_PIN = 9;
const int GREEN_PIN = 10;
const int BLUE_PIN = 11;
const int BUTTON_PIN = 5;
const int sensorPin = A0;
const int valorSeco = 1023;
const int valorHumedo = 0;
LiquidCrystal_I2C lcd(0x23, 16, 2);

long duration;
int distance;

void setup() {
  //pinMode(TRIG_PIN, OUTPUT);
  //pinMode(ECHO_PIN, INPUT);
  pinMode(BUZZER_PIN, OUTPUT);
  pinMode(RED_PIN, OUTPUT);
  pinMode(GREEN_PIN, OUTPUT);
  pinMode(BLUE_PIN, OUTPUT);
  pinMode(BUTTON_PIN, INPUT);
  pinMode(relay, OUTPUT);
  digitalWrite(relay, HIGH);
  lcd.init();
  lcd.backlight();

  dht.begin();
  Serial.begin(9600);
}

void loop() {
  // Aquí puedes realizar otras tareas, ya que los datos seriales se manejarán en `serialEvent()`
}

// Se ejecuta automáticamente cuando hay datos disponibles en el puerto serial
void serialEvent() {
  String request = Serial.readStringUntil('\n');

  if (request == "RECOLECTAR_DATOS") {
    float lectura = analogRead(sensorPin);
    float hum = map(lectura, valorSeco, valorHumedo, 0, 100);
    float humedad = constrain(hum, 0, 100);
    int temperatura = dht.readTemperature();
    //distance = medirDistanciaUltrasonido();
    String estadoBoton = "NO_REGANDO";
    if (digitalRead(BUTTON_PIN) == HIGH) {
      estadoBoton = "REGANDO";
    }


    StaticJsonDocument<1024> jsonDoc;
    jsonDoc["temperatura"] = temperatura;
    jsonDoc["humedad"] = humedad;
    //jsonDoc["distancia"] = distance;
    jsonDoc["boton"] = estadoBoton;

    String output;
    serializeJson(jsonDoc, output);
    Serial.println(output);

  } else if (request == "APAGAR_BUZZER") {
    digitalWrite(BUZZER_PIN, LOW);

  } else if (request == "ALERTA_BUZZER") {
    digitalWrite(BUZZER_PIN, HIGH);
    digitalWrite(RED_PIN, HIGH);
    digitalWrite(GREEN_PIN, LOW);
    digitalWrite(BLUE_PIN, LOW);
    lcd.clear();
    lcd.setCursor(0, 0);
    lcd.print("CUIDADO");

  } else if (request == "ALERTA_TEMPERATURA") {
    digitalWrite(RED_PIN, HIGH);
    digitalWrite(GREEN_PIN, HIGH);
    digitalWrite(BLUE_PIN, LOW);
    lcd.clear();
    lcd.setCursor(0, 0);
    lcd.print("NECESITAR REGAR");

  } else if (request == "ESTADO_CORRECTO") {
    digitalWrite(RED_PIN, LOW);
    digitalWrite(GREEN_PIN, HIGH);
    digitalWrite(BLUE_PIN, LOW);
    lcd.clear();
    lcd.setCursor(0, 0);
    lcd.print("BUEN ESTADO");
  } else if (request == "REGANDO") {
    digitalWrite(relay, LOW);
  }

  else if (request == "NO_REGAR") {
    digitalWrite(relay, HIGH);
  }
}
