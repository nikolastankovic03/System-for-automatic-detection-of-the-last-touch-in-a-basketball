#include <SPI.h>
#include <WiFi.h>
#include <DW1000.h>

const uint8_t PIN_RST = 4;
const uint8_t PIN_IRQ = 27;
const uint8_t PIN_SS = 5;

#define POLL 0
#define POLL_ACK 1
#define RANGE 2
#define RANGE_REPORT 3
#define RANGE_FAILED 255

volatile byte expectedMsgId = POLL;
volatile boolean sentAck = false;
volatile boolean receivedAck = false;
volatile byte lastSentMsgId = 255;
boolean protocolFailed = false;

DW1000Time timePollSent;
DW1000Time timePollReceived;
DW1000Time timePollAckSent;
DW1000Time timePollAckReceived;
DW1000Time timeRangeSent;
DW1000Time timeRangeReceived;
DW1000Time timeComputedRange;

#define LEN_DATA 16
byte data[LEN_DATA];

uint32_t lastActivity;
uint32_t resetPeriod = 250;
uint16_t replyDelayTimeUS = 3000;
uint16_t successRangingCount = 0;
uint32_t rangingCountPeriod = 0;
float samplingRate = 0;

float lastDistance = 0;
bool haveNewDistance = false;
uint32_t lastPrintTime = 0;
const uint32_t printInterval = 1000;

void setup() {
    Serial.begin(115200);
    delay(1000);

    WiFi.mode(WIFI_OFF);
    SPI.begin(18, 19, 23, 5);
    delay(500);

    Serial.println(F("### DW1000-arduino-ranging-anchor-2 ###"));

    DW1000.begin(PIN_IRQ, PIN_RST);
    DW1000.select(PIN_SS);
    Serial.println(F("DW1000 initialized ..."));

    DW1000.newConfiguration();
    DW1000.setDefaults();
    

    DW1000.setDeviceAddress(3); 
    
    DW1000.setNetworkId(10);
    DW1000.enableMode(DW1000.MODE_LONGDATA_RANGE_LOWPOWER);
    DW1000.commitConfiguration();
    Serial.println(F("Committed configuration ..."));

    DW1000.attachSentHandler(handleSent);
    DW1000.attachReceivedHandler(handleReceived);

    receiver();
    noteActivity();
    rangingCountPeriod = millis();
    lastPrintTime = millis();
}

void noteActivity() {
    lastActivity = millis();
}

void resetInactive() {
    expectedMsgId = POLL;
    receiver();
    noteActivity();
}

void handleSent() {
    sentAck = true;
    lastSentMsgId = data[0];
}

void handleReceived() {
    receivedAck = true;
}

void transmitPollAck() {
    DW1000.newTransmit();
    DW1000.setDefaults();
    data[0] = POLL_ACK;
    DW1000Time deltaTime = DW1000Time(replyDelayTimeUS, DW1000Time::MICROSECONDS);
    DW1000.setDelay(deltaTime);
    DW1000.setData(data, LEN_DATA);
    DW1000.startTransmit();
}

void transmitRangeReport(float curRange) {
    DW1000.newTransmit();
    DW1000.setDefaults();
    data[0] = RANGE_REPORT;
    memcpy(data + 1, &curRange, 4);
    DW1000.setData(data, LEN_DATA);
    DW1000.startTransmit();
}

void transmitRangeFailed() {
    DW1000.newTransmit();
    DW1000.setDefaults();
    data[0] = RANGE_FAILED;
    DW1000.setData(data, LEN_DATA);
    DW1000.startTransmit();
}

void receiver() {
    DW1000.newReceive();
    DW1000.setDefaults();
    DW1000.receivePermanently(true);
    DW1000.startReceive();
}

void computeRangeAsymmetric() {
    DW1000Time round1 = (timePollAckReceived - timePollSent).wrap();
    DW1000Time reply1 = (timePollAckSent - timePollReceived).wrap();
    DW1000Time round2 = (timeRangeReceived - timePollAckSent).wrap();
    DW1000Time reply2 = (timeRangeSent - timePollAckReceived).wrap();

    DW1000Time denom = round1 + round2 + reply1 + reply2;
    if (denom.getAsMicroSeconds() == 0) {
        timeComputedRange.setTimestamp((int64_t)0);
        return;
    }

    DW1000Time tof = (round1 * round2 - reply1 * reply2) / denom;
    timeComputedRange.setTimestamp(tof);
}

void loop() {
    int32_t curMillis = millis();

    if (sentAck) {
        sentAck = false;
        if (lastSentMsgId == POLL_ACK) {
            DW1000.getTransmitTimestamp(timePollAckSent);
            noteActivity();
        }
    }

    if (receivedAck) {
        receivedAck = false;
        DW1000.getData(data, LEN_DATA);
        byte msgId = data[0];
        if (msgId != expectedMsgId) {
            protocolFailed = true;
        }
        if (msgId == POLL) {
            protocolFailed = false;
            DW1000.getReceiveTimestamp(timePollReceived);
            expectedMsgId = RANGE;
            transmitPollAck();
            noteActivity();
        }
        else if (msgId == RANGE) {
            DW1000.getReceiveTimestamp(timeRangeReceived);
            expectedMsgId = POLL;
            if (!protocolFailed) {
                timePollSent.setTimestamp(data + 1);
                timePollAckReceived.setTimestamp(data + 6);
                timeRangeSent.setTimestamp(data + 11);
                computeRangeAsymmetric();

                float distanceMeters = timeComputedRange.getAsMeters();
                transmitRangeReport(distanceMeters);

                lastDistance = distanceMeters;
                haveNewDistance = true;

                successRangingCount++;
                if (curMillis - rangingCountPeriod > 1000) {
                    samplingRate = (1000.0f * successRangingCount) / (curMillis - rangingCountPeriod);
                    rangingCountPeriod = curMillis;
                    successRangingCount = 0;
                }
            } else {
                transmitRangeFailed();
            }
            noteActivity();
        }
    }

    if (!sentAck && !receivedAck) {
        if (curMillis - lastActivity > resetPeriod) {
            resetInactive();
        }
    }

    if (curMillis - lastPrintTime >= printInterval) {
        lastPrintTime = curMillis;
        if (haveNewDistance) {
            Serial.print("Udaljenost do lopte (Anchor 2): ");
            Serial.print(lastDistance);
            Serial.print(" m  |  Rate: ");
            Serial.print(samplingRate);
            Serial.println(" Hz");
        } else {
            Serial.println("Nema jos merenja...");
        }
    }

    delay(2);
}