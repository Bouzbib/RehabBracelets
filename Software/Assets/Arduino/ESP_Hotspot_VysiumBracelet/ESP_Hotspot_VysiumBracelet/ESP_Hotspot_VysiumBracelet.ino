#include <WiFi.h>
#include <WiFiUdp.h>

const char* SSID     = "ESP32c3_DataSent";
const char* PASSWORD = "12345678";

IPAddress localIP(192, 168, 4, 1);
IPAddress gateway(192, 168, 4, 1);
IPAddress subnet (255, 255, 255, 0);

const uint16_t RELAY_PORT   = 5005;  // everyone sends TO this
const uint16_t FORWARD_PORT = 5005;  // relay forwards TO this port on all clients

IPAddress clientIPs[8];
int       clientCount = 0;

WiFiUDP udp;
char buffer[256];

void registerClient(IPAddress ip) {
  for (int i = 0; i < clientCount; i++)
    if (clientIPs[i] == ip) return;
  if (clientCount >= 8) return;
  clientIPs[clientCount++] = ip;
  Serial.printf("New client: %s (total: %d)\n", ip.toString().c_str(), clientCount);
}

void setup() {
  Serial.begin(115200);
  delay(500);
  WiFi.softAP(SSID, PASSWORD);
  WiFi.softAPConfig(localIP, gateway, subnet);
  Serial.print("AP IP: ");
  Serial.println(WiFi.softAPIP());
  udp.begin(RELAY_PORT);
  Serial.printf("Relay on port %d, forwarding to port %d\n", RELAY_PORT, FORWARD_PORT);
}

void loop() {
  int packetSize = udp.parsePacket();
  if (packetSize <= 0) return;

  IPAddress senderIP = udp.remoteIP();
  int len = udp.read(buffer, sizeof(buffer) - 1);
  buffer[len] = '\0';

  Serial.printf("From %s -> \"%s\"\n", senderIP.toString().c_str(), buffer);

  registerClient(senderIP);

  for (int i = 0; i < clientCount; i++) {
    if (clientIPs[i] == senderIP) continue;
    udp.beginPacket(clientIPs[i], FORWARD_PORT);
    udp.write((uint8_t*)buffer, len);
    udp.endPacket();
    Serial.printf("  Forwarded to %s:%d\n", clientIPs[i].toString().c_str(), FORWARD_PORT);
  }
}
