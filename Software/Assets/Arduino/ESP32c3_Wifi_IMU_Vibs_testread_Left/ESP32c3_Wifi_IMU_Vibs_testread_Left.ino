// ============================================================
//  ESP32-C3 Haptic + IMU Controller
//  - Wire(6,5) started after WiFi connects
//  - String-based command protocol (Unity → ESP on port 5005)
//  - IMU stream (ESP → Unity on port 5007)
//  - Calibration via "CALIBRATE" command
//
//  Commands Unity can send (plain UTF-8 strings):
//    "CONNECT"            — acknowledge connection, reply "READY"
//    "CALIBRATE"          — snapshot accel offsets, reply "CAL:ax:ay:az:gx:gy:gz"
//    "MOTOR:idx:intensity:durationMs"  — fire a motor (durationMs=0 → hold)
//    "ALLOFF"             — stop all motors
//
//  ESP replies (on port 5006):
//    "READY"              — sent on CONNECT
//    "CAL:ax:ay:az:..."   — sent on CALIBRATE
//    "ACK:idx:intensity"  — sent on MOTOR
//
//  IMU stream (on port 5007, ~50Hz):
//    "IMU:ax:ay:az:gx:gy:gz"
// ============================================================

#include <WiFi.h>
#include <WiFiUdp.h>

#include "I2Cdev.h"
#include "MPU6050.h"

MPU6050 mpu;
// ─── CONFIG ──────────────────────────────────────────────────
const char* SSID     = "ESP32c3_DataSent";
const char* PASSWORD = "12345678";
const int   UDP_PORTL = 5004;   // receive commands from Unity
const int   ACK_PORTL = 5008;   // replies + calibration
const int   IMU_PORTL = 5009;   // IMU stream to Unity

IPAddress localIP(192, 168,   4, 101);
IPAddress gateway (192, 168,   4,   1);
IPAddress subnet  (255, 255, 255,   0);

// ─── MOTOR CONFIG ────────────────────────────────────────────
const int NUM_MOTORS    = 4;           // change to 8 if needed
const int MOTOR_PINS[]  = {2, 3, 4, 10};
const int PWM_FREQ      = 1000;        // Hz — good for ERMs
const int PWM_RES       = 8;           // 8-bit → 0–255

const int LED_PIN      = 8;   // status LED — change if needed

// ─── STRING COMMANDS (Unity → ESP) ───────────────────────────
// "CONNECT"                     → reply READY, confirm link
// "CALIBRATE"                   → snapshot accel, reply CAL:...
// "MOTOR:idx:intensity:durMs"   → fire motor
// "ALLOFF"                      → stop all motors

// ─── IMU STATE ───────────────────────────────────────────────
const float    ACCEL_SCALE    = 16384.0f;  // ±2g → LSB/g
const float    GYRO_SCALE     = 131.0f;    // ±250°/s → LSB/°/s
const uint32_t IMU_INTERVAL   = 20;        // ms between IMU packets (~50Hz)
uint32_t       lastImuSend    = 0;
bool           imuReady       = false;

float          calAx = 0, calAy = 0, calAz = 0;
bool           calibrated     = false;

IPAddress      unityIP(0, 0, 0, 0);
bool           unityKnown     = true;

// ─── STATE ───────────────────────────────────────────────────
struct MotorState {
  uint8_t  intensity;
  uint32_t offAt;
  bool     active;
};
MotorState motors[4];

WiFiUDP udp;       // receives commands on port 5005
WiFiUDP udpSend;   // sends replies and IMU stream (no bound port needed)
bool    udpStarted = false;

// LED blink state
enum LedMode { LED_OFF, LED_SLOW_BLINK, LED_FAST_BLINK, LED_SOLID };
LedMode      ledMode      = LED_OFF;
uint32_t     lastLedToggle = 0;
bool         ledState      = false;

// ─── IMU RAW READINGS ────────────────────────────────────────
int16_t ax, ay, az;
int16_t gx, gy, gz;

// ─── LED ─────────────────────────────────────────────────────
void setLedMode(LedMode mode) {
  ledMode = mode;
  if (mode == LED_SOLID) {
    digitalWrite(LED_PIN, HIGH);
    ledState = true;
  } else if (mode == LED_OFF) {
    digitalWrite(LED_PIN, LOW);
    ledState = false;
  }
}

void updateLed() {
  if (ledMode == LED_SOLID || ledMode == LED_OFF) return;
  uint32_t interval = (ledMode == LED_SLOW_BLINK) ? 800 : 150;
  if (millis() - lastLedToggle >= interval) {
    lastLedToggle = millis();
    ledState = !ledState;
    digitalWrite(LED_PIN, ledState ? HIGH : LOW);
  }
}

// ─── MOTOR HELPERS ───────────────────────────────────────────
void motorWrite(int idx, uint8_t value) {
  ledcWrite(MOTOR_PINS[idx], value);
}

void setMotor(int idx, uint8_t intensity) {
  if (idx < 0 || idx >= NUM_MOTORS) return;
  motorWrite(idx, intensity);
  motors[idx].intensity = intensity;
  motors[idx].active    = (intensity > 0);
}

void stopMotor(int idx) {
  if (idx < 0 || idx >= NUM_MOTORS) return;
  motorWrite(idx, 0);
  motors[idx].intensity = 0;
  motors[idx].active    = false;
  motors[idx].offAt     = 0;
}

void stopAllMotors() {
  for (int i = 0; i < NUM_MOTORS; i++) stopMotor(i);
}

void initMotors() {
  for (int i = 0; i < NUM_MOTORS; i++) {
    ledcAttach(MOTOR_PINS[i], PWM_FREQ, PWM_RES);
    ledcWrite(MOTOR_PINS[i], 0);
    motors[i] = {0, 0, false};
  }
  Serial.printf("[Motors] %d motors initialized (PWM %dHz, %d-bit)\n",
                NUM_MOTORS, PWM_FREQ, PWM_RES);
}

// ─── WIFI ────────────────────────────────────────────────────
void connectWiFi() {
  udpStarted  = false;
  unityKnown  = true;
  imuReady    = true;
  setLedMode(LED_FAST_BLINK); // fast blink = trying to connect

  WiFi.disconnect(true);
  delay(300);
  WiFi.mode(WIFI_STA);
  delay(200);

  // Scan until our network is visible
  Serial.printf("[WiFi] Scanning for '%s'...\n", SSID);
  while (true) {
    WiFi.scanDelete();
    int  n     = WiFi.scanNetworks(false, false);
    bool found = false;
    Serial.printf("[WiFi] %d network(s):\n", n);
    for (int i = 0; i < n; i++) {
      Serial.printf("  %s (RSSI: %d)\n", WiFi.SSID(i).c_str(), WiFi.RSSI(i));
      if (WiFi.SSID(i) == SSID) found = true;
    }
    if (found) break;
    Serial.println("[WiFi] Not found, retrying in 3s...");
    // Keep blinking while we wait
    uint32_t wait = millis() + 3000;
    while (millis() < wait) { updateLed(); delay(10); }
  }

  setLedMode(LED_SLOW_BLINK); // slow blink = network found, connecting
  WiFi.config(localIP, gateway, subnet);
  WiFi.begin(SSID, PASSWORD);
  Serial.print("[WiFi] Connecting");

  int attempts = 0;
  while (WiFi.status() != WL_CONNECTED) {
    updateLed();
    delay(500);
    Serial.print(".");
    if (++attempts > 40) {
      Serial.println("\n[WiFi] Timeout, restarting...");
      connectWiFi();
      return;
    }
  }

  delay(500);
  setLedMode(LED_SOLID); // solid = connected

  Serial.printf("\n[WiFi] Connected! IP: %s  RSSI: %d dBm\n",
                WiFi.localIP().toString().c_str(), WiFi.RSSI());

  udp.stop();
  delay(100);
  if (udp.begin(UDP_PORTL)) {
    udpStarted = true;
    Serial.printf("[UDP] Listening on port %d\n", UDP_PORTL);
    Serial.println("[READY] Waiting for Unity packets...\n");

    // HERE PUT IMU SETUP
    // initWire();

  } else {
    Serial.println("[UDP] Failed to start!");
  }
}


void initWire()
{
  #if I2CDEV_IMPLEMENTATION == I2CDEV_ARDUINO_WIRE
    Wire.begin(7, 9);
  #elif I2CDEV_IMPLEMENTATION == I2CDEV_BUILTIN_FASTWIRE
    Fastwire::setup(400, true);
  #endif

  Serial.println("[IMU] Initializing MPU6050...");
  mpu.initialize();

  if (!mpu.testConnection()) {
    Serial.println("[IMU] MPU6050 connection FAILED — check wiring on pins 7/9");
    imuReady = false;
    return;  // don't halt — WiFi + motors still work
  }

  Serial.println("[IMU] MPU6050 connected");

  mpu.setXAccelOffset(0);
  mpu.setYAccelOffset(0);
  mpu.setZAccelOffset(0);
  mpu.setXGyroOffset(0);
  mpu.setYGyroOffset(0);
  mpu.setZGyroOffset(0);

  imuReady = true;
  Serial.println("[IMU] Ready — streaming will start once Unity connects");
}

// ─── IMU STREAM ──────────────────────────────────────────────
void sendIMU()
{
  mpu.getMotion6(&ax, &ay, &az, &gx, &gy, &gz);

  float fax = ax / ACCEL_SCALE;
  float fay = ay / ACCEL_SCALE;
  float faz = az / ACCEL_SCALE;
  float fgx = gx / GYRO_SCALE;
  float fgy = gy / GYRO_SCALE;
  float fgz = gz / GYRO_SCALE;

  if (calibrated) { fax -= calAx; fay -= calAy; faz -= calAz; }

  char buf[80];
  snprintf(buf, sizeof(buf), "IMU:%.4f:%.4f:%.4f:%.2f:%.2f:%.2f",
           fax, fay, faz, fgx, fgy, fgz);

  udpSend.beginPacket(unityIP, IMU_PORTL);
  udpSend.print(buf);
  udpSend.endPacket();
}

// ─── CALIBRATE ───────────────────────────────────────────────
void doCalibrate()
{
  if (!imuReady) {
    Serial.println("[CAL] Ignored — IMU not ready");
    return;
  }

  mpu.getMotion6(&ax, &ay, &az, &gx, &gy, &gz);
  calAx = ax / ACCEL_SCALE;
  calAy = ay / ACCEL_SCALE;
  calAz = az / ACCEL_SCALE;
  calibrated = true;

  Serial.printf("[CAL] Offsets saved: ax=%.3f ay=%.3f az=%.3f\n", calAx, calAy, calAz);

  // Reply with zeroed accel (right after cal it's always ~0) + raw gyro
  float fgx = gx / GYRO_SCALE;
  float fgy = gy / GYRO_SCALE;
  float fgz = gz / GYRO_SCALE;

  char buf[80];
  snprintf(buf, sizeof(buf), "CAL:0.0000:0.0000:0.0000:%.2f:%.2f:%.2f",
           fgx, fgy, fgz);

  udpSend.beginPacket(unityIP, ACK_PORTL);
  udpSend.print(buf);
  udpSend.endPacket();
  Serial.printf("[CAL] Sent: %s\n", buf);
}

// ─── STRING COMMAND PARSER ────────────────────────────────────
void handleCommand(String cmd, IPAddress sender)
{
  cmd.trim();
  Serial.printf("[CMD] Received: '%s'\n", cmd.c_str());

  // ── CONNECT ─────────────────────────────────────────────
  if (cmd == "CONNECT") {
    unityIP    = sender;
    unityKnown = true;
    udpSend.beginPacket(sender, ACK_PORTL);
    udpSend.print("READY");
    udpSend.endPacket();
    Serial.println("[CMD] CONNECT → READY sent");

  // ── CALIBRATE ───────────────────────────────────────────
  } else if (cmd == "CALIBRATE") {
    doCalibrate();
  }
  else if (cmd == "INITIMU") {
    initWire();
    
  // ── ALLOFF ──────────────────────────────────────────────
  } else if (cmd == "ALLOFF") {
    stopAllMotors();
    Serial.println("[CMD] ALLOFF");

  // ── MOTOR:idx:intensity:durationMs ──────────────────────
  } else if (cmd.startsWith("MOTOR:")) {
    // parse "MOTOR:0:200:500"
    int a = cmd.indexOf(':', 6);
    int b = cmd.indexOf(':', a + 1);
    if (a < 0 || b < 0) { Serial.println("[CMD] MOTOR parse error"); return; }

    int idx        = cmd.substring(6, a).toInt();
    int intensity  = cmd.substring(a + 1, b).toInt();
    int durationMs = cmd.substring(b + 1).toInt();

    if (idx < 0 || idx >= NUM_MOTORS) {
      Serial.printf("[CMD] Invalid motor index %d\n", idx); return;
    }
    intensity = constrain(intensity, 0, 255);

    setMotor(idx, (uint8_t)intensity);
    if (durationMs > 0) {
      motors[idx].offAt = millis() + durationMs;
      Serial.printf("[CMD] Motor %d | %d | %dms\n", idx, intensity, durationMs);
    } else {
      motors[idx].offAt = 0;
      Serial.printf("[CMD] Motor %d | %d | hold\n", idx, intensity);
    }

    String ack = "ACK:" + String(idx) + ":" + String(intensity);
    udpSend.beginPacket(sender, ACK_PORTL);
    udpSend.print(ack);
    udpSend.endPacket();

  } else {
    Serial.printf("[CMD] Unknown command: '%s'\n", cmd.c_str());
  }
}

// ─── SETUP ───────────────────────────────────────────────────
void setup() {
  // Init motors FIRST — before anything else so they don't
  // run during the USB enumeration delay
  for (int i = 0; i < NUM_MOTORS; i++) {
    pinMode(MOTOR_PINS[i], OUTPUT);
    digitalWrite(MOTOR_PINS[i], LOW);
  }
  pinMode(LED_PIN, OUTPUT);
  digitalWrite(LED_PIN, LOW);

  delay(2000); // USB CDC enumeration
  Serial.begin(115200);
  delay(300);

  Serial.println("\n============================");
  Serial.println(" ESP32-C3 Haptic Controller");
  Serial.println("============================");

  initMotors();
  connectWiFi();
}

// ─── LOOP ────────────────────────────────────────────────────
void loop() {
  updateLed();

  // ── WiFi watchdog ────────────────────────────────────────
  static uint32_t lastCheck = 0;
  if (millis() - lastCheck > 5000) {
    lastCheck = millis();
    if (WiFi.status() != WL_CONNECTED) {
      Serial.println("[WiFi] Lost connection, reconnecting...");
      stopAllMotors();
      connectWiFi();
      return;
    }
  }

  if (!udpStarted) return;

  // ── IMU stream ───────────────────────────────────────────
  if (millis() - lastImuSend >= IMU_INTERVAL) {
    lastImuSend = millis();
    sendIMU();
  }

  // ── Handle UDP commands ──────────────────────────────────
  int pktSize = udp.parsePacket();
  if (pktSize > 0) {
    char buf[64];
    int len = udp.read(buf, sizeof(buf) - 1);
    if (len < 1) return;
    buf[len] = '\0';

    handleCommand(String(buf), udp.remoteIP());
  }

  // ── Timed pulse auto-off ─────────────────────────────────
  uint32_t now = millis();
  for (int i = 0; i < NUM_MOTORS; i++) {
    if (motors[i].active && motors[i].offAt > 0 && now >= motors[i].offAt) {
      stopMotor(i);
      Serial.printf("[Timer] Motor %d auto-off\n", i);
    }
  }
}
