// ============================================================
//  ESP32-C3 Haptic Controller — Final version
//  - Direct GPIO + ledcWrite PWM (no PCA9685)
//  - ULN2003 driver (GPIO HIGH = motor ON)
//  - Motors off immediately on boot
//  - WiFi auto-reconnect with scan loop
//  - UDP command receiver + ACK back to Unity
// ============================================================

#include <WiFi.h>
#include <WiFiUdp.h>

// ─── CONFIG ──────────────────────────────────────────────────
const char* SSID     = "ESP32c3_DataSent";
const char* PASSWORD = "12345678";
const int   UDP_PORT = 5005;
const int   ACK_PORT = 5006;

IPAddress localIP(192, 168,   4, 100);
IPAddress gateway (192, 168,   4,   1);
IPAddress subnet  (255, 255, 255,   0);

// ─── MOTOR CONFIG ────────────────────────────────────────────
const int NUM_MOTORS    = 8;           // change to 8 if needed
const int MOTOR_PINS[]  = {2, 3, 4, 5, 6, 7, 10, 18};
const int PWM_FREQ      = 1000;        // Hz — good for ERMs
const int PWM_RES       = 8;           // 8-bit → 0–255
// LEDC channels (one per motor, ESP32-C3 has 6 channels max)

const int LEDC_CHANNELS[] = {0, 1, 2, 3, 4, 5, 6, 7};

const int LED_PIN      = 8;   // status LED — change if needed

// ─── PACKET FORMAT ───────────────────────────────────────────
// Byte 0:   0x01 = motor command  |  0x02 = all off
// Byte 1:   motor index (0–7)
// Byte 2:   intensity (0–255)
// Byte 3-4: duration ms big-endian (0 = hold)
#define CMD_MOTOR   0x01
#define CMD_ALL_OFF 0x02

// ─── STATE ───────────────────────────────────────────────────
struct MotorState {
  uint8_t  intensity;
  uint32_t offAt;
  bool     active;
};
MotorState motors[8];

WiFiUDP udp;
bool    udpStarted = false;

// LED blink state
enum LedMode { LED_OFF, LED_SLOW_BLINK, LED_FAST_BLINK, LED_SOLID };
LedMode      ledMode      = LED_OFF;
uint32_t     lastLedToggle = 0;
bool         ledState      = false;

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
  udpStarted = false;
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
  if (udp.begin(UDP_PORT)) {
    udpStarted = true;
    Serial.printf("[UDP] Listening on port %d\n", UDP_PORT);
    Serial.println("[READY] Waiting for Unity packets...\n");
  } else {
    Serial.println("[UDP] Failed to start!");
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

  // ── WiFi watchdog (no scanning while connected) ──────────
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

  // ── Handle UDP packets ───────────────────────────────────
  if (!udpStarted) return;

  int pktSize = udp.parsePacket();
  if (pktSize > 0) {
    uint8_t buf[16];
    int len = udp.read(buf, sizeof(buf));
    if (len < 1) return;

    uint8_t cmd = buf[0];

    if (cmd == CMD_ALL_OFF) {
      stopAllMotors();
      Serial.println("[CMD] All motors OFF");

    } else if (cmd == CMD_MOTOR && len >= 5) {
      uint8_t  idx        = buf[1];
      uint8_t  intensity  = buf[2];
      uint16_t durationMs = ((uint16_t)buf[3] << 8) | buf[4];

      if (idx >= NUM_MOTORS) {
        Serial.printf("[CMD] Invalid motor index %d\n", idx);
        return;
      }

      setMotor(idx, intensity);

      if (durationMs > 0) {
        motors[idx].offAt = millis() + durationMs;
        Serial.printf("[CMD] Motor %d | intensity %d | %dms pulse\n",
                      idx, intensity, durationMs);
      } else {
        motors[idx].offAt = 0;
        Serial.printf("[CMD] Motor %d | intensity %d | hold\n",
                      idx, intensity);
      }

      // ACK back to Unity
      String ack = "ACK:" + String(idx) + ":" + String(intensity);
      udp.beginPacket(udp.remoteIP(), ACK_PORT);
      udp.print(ack);
      udp.endPacket();
    }
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
