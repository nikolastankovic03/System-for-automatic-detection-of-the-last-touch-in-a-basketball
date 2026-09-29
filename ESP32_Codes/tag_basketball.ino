#include <SPI.h>
#include <Wire.h>
#include <WiFi.h>
#include <DW1000.h>
#include <Adafruit_GFX.h>
#include <Adafruit_SH110X.h>
#include <HTTPClient.h>

const char* WIFI_SSID = "XXXXX";
const char* WIFI_PASSWORD = "XXXXXXX";
const char* SERVER_URL = "XXXXXXXXXXXXXXXXXX";
const char* IME_IGRACA = "Lopta_Modul_2";
uint32_t poslednjiPokusajWifi = 0;
const uint32_t WIFI_RETRY_INTERVAL = 10000;

const uint8_t PIN_RST = 4;
const uint8_t PIN_IRQ = 27;
const uint8_t PIN_SS = 5;

#define SCREEN_WIDTH 128
#define SCREEN_HEIGHT 64
Adafruit_SH1106G display(SCREEN_WIDTH, SCREEN_HEIGHT, &Wire, -1);

#define IMU_ADDR 0x68
#define REG_PWR_MGMT0 0x1F
#define REG_ACCEL_DATA_X1 0x0B
#define UDAR_PRAG 2200

#define POLL 0
#define POLL_ACK 1
#define RANGE 2
#define RANGE_REPORT 3
#define RANGE_FAILED 255

volatile byte expectedMsgId = POLL_ACK;
volatile boolean sentAck = false;
volatile boolean receivedAck = false;
volatile byte lastSentMsgId = 255;

DW1000Time timePollSent;
DW1000Time timePollAckReceived;
DW1000Time timeRangeSent;

#define LEN_DATA 16
byte data[LEN_DATA];

uint32_t lastActivity;
uint32_t resetPeriod = 1000;
uint16_t replyDelayTimeUS = 3000;

float lastDistance = 0;
bool haveNewDistance = false;
uint32_t lastPrintTime = 0;
const uint32_t printInterval = 1000;

uint32_t lastOledUpdate = 0;
const uint32_t oledInterval = 200;

uint32_t lastImuRead = 0;
const uint32_t imuInterval = 20;
long zadnjaMagnituda = 0;
int brojUdaraca = 0;

bool udaracUToku = false;
uint32_t vremeZadnjegUdarca = 0;
const uint32_t trajanjeprikazaUdarca = 500;

uint32_t countPollSent = 0;
uint32_t countPollAckRecv = 0;
uint32_t countRangeSent = 0;
uint32_t countRangeReportRecv = 0;
uint32_t countRangeFailedRecv = 0;
uint32_t countProtocolMismatch = 0;
uint32_t countResets = 0;
uint32_t countTransmitCalls = 0;
uint32_t countUnknownMsgId = 0;

void connectWiFi() {
    WiFi.mode(WIFI_STA);
    WiFi.begin(WIFI_SSID, WIFI_PASSWORD);
}

void initUWB() {
    DW1000.begin(PIN_IRQ, PIN_RST);
    DW1000.select(PIN_SS);
    DW1000.newConfiguration();
    DW1000.setDefaults();
    DW1000.setDeviceAddress(2);
    DW1000.setNetworkId(10);
    DW1000.enableMode(DW1000.MODE_LONGDATA_RANGE_LOWPOWER);
    DW1000.commitConfiguration();
    DW1000.attachSentHandler(handleSent);
    DW1000.attachReceivedHandler(handleReceived);
}

void writeRegister(uint8_t reg, uint8_t value) {
    Wire.beginTransmission(IMU_ADDR);
    Wire.write(reg);
    Wire.write(value);
    Wire.endTransmission();
}

void noteActivity() {
    lastActivity = millis();
}

void resetInactive() {
    Serial.println("DW1000 blokiran - Resetujem SAMO UWB modul...");
    countResets++;

    pinMode(PIN_RST, OUTPUT);
    digitalWrite(PIN_RST, LOW);
    delay(10);
    pinMode(PIN_RST, INPUT);
    delay(20);

    initUWB();

    expectedMsgId = POLL_ACK;
    sentAck = false;
    receivedAck = false;

    transmitPoll();
    noteActivity();
}

void posaljiPodatke(long magnituda) {
    if (WiFi.status() != WL_CONNECTED) return;

    detachInterrupt(digitalPinToInterrupt(PIN_IRQ));
    DW1000.idle();

    float slanjeUdaljenosti = haveNewDistance ? lastDistance : 0.0;

    HTTPClient http;
    http.begin(SERVER_URL);
    http.addHeader("Content-Type", "application/json");
    http.setTimeout(1500); 
    
    String jsonPayload = "{\"igrac\":\"" + String(IME_IGRACA) +
                          "\",\"magnituda\":" + String(magnituda) +
                          ",\"udaljenost\":" + String(slanjeUdaljenosti, 2) + "}";
    http.POST(jsonPayload);
    http.end();

    resetInactive();
}

void setup() {
    Serial.begin(115200);
    delay(1000);

    Wire.begin(21, 22);
    delay(100);
    if (!display.begin(0x3C, true)) {
        Serial.println("OLED nije pronadjen!");
    }
    display.clearDisplay();
    display.setTextColor(SH110X_WHITE);
    display.setTextSize(1);
    display.setCursor(0, 0);
    display.println("Inicijalizacija...");
    display.display();

    writeRegister(REG_PWR_MGMT0, 0x0F);
    delay(100);

    connectWiFi();

    SPI.begin(18, 19, 23, 5);
    SPI.setFrequency(4000000);
    delay(500);

    Serial.println(F("### DW1000-arduino-ranging-tag sa OLED i IMU ###"));

    initUWB();
    Serial.println(F("Committed configuration ..."));

    transmitPoll();
    noteActivity();
    lastPrintTime = millis();

    display.clearDisplay();
    display.setCursor(0, 0);
    display.println("Spreman.");
    display.display();
}

void handleSent() {
    sentAck = true;
    lastSentMsgId = data[0];
}

void handleReceived() {
    receivedAck = true;
}

void transmitPoll() {
    countTransmitCalls++;
    DW1000.idle();
    DW1000.newTransmit();
    DW1000.setDefaults();
    data[0] = POLL;
    DW1000.setData(data, LEN_DATA);
    DW1000.startTransmit();
}

void transmitRange() {
    DW1000.idle();
    DW1000.newTransmit();
    DW1000.setDefaults();
    data[0] = RANGE;
    DW1000Time deltaTime = DW1000Time(replyDelayTimeUS, DW1000Time::MICROSECONDS);
    timeRangeSent = DW1000.setDelay(deltaTime);
    timePollSent.getTimestamp(data + 1);
    timePollAckReceived.getTimestamp(data + 6);
    timeRangeSent.getTimestamp(data + 11);
    DW1000.setData(data, LEN_DATA);
    DW1000.startTransmit();
}

void receiver() {
    DW1000.idle();
    DW1000.newReceive();
    DW1000.setDefaults();
    DW1000.receivePermanently(true);
    DW1000.startReceive();
}

void updateImu() {
    Wire.beginTransmission(IMU_ADDR);
    Wire.write(REG_ACCEL_DATA_X1);
    Wire.endTransmission(false);
    Wire.requestFrom(IMU_ADDR, 6);

    if (Wire.available() == 6) {
        int16_t ax = (Wire.read() << 8) | Wire.read();
        int16_t ay = (Wire.read() << 8) | Wire.read();
        int16_t az = (Wire.read() << 8) | Wire.read();

        if (abs(az) >= 500) {
            zadnjaMagnituda = abs(ax) + abs(ay) + abs(az);
            bool sadaPrekoraceno = zadnjaMagnituda > UDAR_PRAG;

            if (sadaPrekoraceno && !udaracUToku) {
                brojUdaraca++;
                vremeZadnjegUdarca = millis();
                Serial.print("Detektovan udarac! Mag: ");
                Serial.println(zadnjaMagnituda);
                
                posaljiPodatke(zadnjaMagnituda);
            }
            udaracUToku = sadaPrekoraceno;
        } else {
            udaracUToku = false;
        }
    }
}

void updateOled() {
    display.clearDisplay();
    display.setTextSize(1);
    display.setCursor(0, 0);
    display.println("Status lopte:");

    display.setCursor(0, 12);
    display.setTextSize(2);
    if (millis() - vremeZadnjegUdarca < trajanjeprikazaUdarca) {
        display.println("UDARAC!");
    } else {
        display.println("Mirovanje");
    }

    display.setTextSize(1);
    display.setCursor(0, 32);
    display.print("Mag: ");
    display.println(zadnjaMagnituda);

    display.setCursor(0, 44);
    display.print("Udaljenost: ");
    if (haveNewDistance) {
        display.print(lastDistance, 2);
        display.println(" m");
    } else {
        display.println("N/A");
    }

    display.setCursor(0, 56);
    display.print("Broj udaraca: ");
    display.println(brojUdaraca);

    display.display();
}

void loop() {
    uint32_t curMillis = millis();

    if (sentAck) {
        sentAck = false;
        if (lastSentMsgId == POLL) {
            DW1000.getTransmitTimestamp(timePollSent);
            countPollSent++;
            receiver();
        } else if (lastSentMsgId == RANGE) {
            DW1000.getTransmitTimestamp(timeRangeSent);
            countRangeSent++;
            noteActivity();
            receiver();
        }
    }

    if (receivedAck) {
        receivedAck = false;
        DW1000.getData(data, LEN_DATA);
        byte msgId = data[0];

        if (msgId != expectedMsgId) {
            countProtocolMismatch++;
            expectedMsgId = POLL_ACK;
            transmitPoll();
        } else if (msgId == POLL_ACK) {
            countPollAckRecv++;
            DW1000.getReceiveTimestamp(timePollAckReceived);
            expectedMsgId = RANGE_REPORT;
            transmitRange();
            noteActivity();
        } else if (msgId == RANGE_REPORT) {
            countRangeReportRecv++;
            expectedMsgId = POLL_ACK;
            float curRange;
            memcpy(&curRange, data + 1, 4);

            lastDistance = curRange;
            haveNewDistance = true;
            transmitPoll();
            noteActivity();
        } else if (msgId == RANGE_FAILED) {
            countRangeFailedRecv++;
            expectedMsgId = POLL_ACK;
            transmitPoll();
            noteActivity();
        } else {
            countUnknownMsgId++;
        }
    }

    if (!sentAck && !receivedAck) {
        if (curMillis - lastActivity > resetPeriod) {
            resetInactive();
        }
    }

    if (curMillis - lastPrintTime >= printInterval) {
        lastPrintTime = curMillis;
        Serial.print("Dist: "); Serial.print(lastDistance);
        Serial.print(" m | PollSent:"); Serial.print(countPollSent);
        Serial.print(" PollAckRecv:"); Serial.print(countPollAckRecv);
        Serial.print(" RangeSent:"); Serial.print(countRangeSent);
        Serial.print(" ReportRecv:"); Serial.print(countRangeReportRecv);
        Serial.print(" Failed:"); Serial.print(countRangeFailedRecv);
        Serial.print(" Mismatch:"); Serial.print(countProtocolMismatch);
        Serial.print(" Unknown:"); Serial.print(countUnknownMsgId);
        Serial.print(" Resets:"); Serial.println(countResets);
    }

    if (curMillis - lastImuRead >= imuInterval) {
        lastImuRead = curMillis;
        updateImu();
    }

    if (curMillis - lastOledUpdate >= oledInterval) {
        lastOledUpdate = curMillis;
        updateOled();
    }

    if (WiFi.status() != WL_CONNECTED && curMillis - poslednjiPokusajWifi > WIFI_RETRY_INTERVAL) {
        poslednjiPokusajWifi = curMillis;
        connectWiFi();
    }

    delay(2);
}