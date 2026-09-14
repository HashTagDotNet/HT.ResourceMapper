
|**Queue**|**Consumer(s)**|**Producer(s)**|**Message Type(s)**|
|--|--|--|--|
| `app-deletion-request` | AppDeletionRequestService | AppConfiguration (handlers) | |  
| `app-history-process-queue` | PingHistoryHandler | AppConfiguration (handlers) | `MobileUserLocation_Extended` |  
| `email-external-sender-intake` | EmailService | | |  
| `email-sender-intake` | EmailService | Web, * | |  
| `enhanced-status-message-process-queue` | | LocationIntakeHandler | |  
| `form-customer-callbacks-process-queue` | FormCustomerCallbacks | AppConfiguration (handlers) | |  
| `geocodelonglockqueue` | GeocodeProcessor | GeocodeProducer | |  
| `handlers-communication-received` | CommunicationReceivedHandler | AppConfiguration (handlers) | |  
| `handlers-mobile-user-phone-info-queue` | MobileUserPhoneInfoUpdate | AppConfiguration (handlers) | |  
| `handlers-register-mobile-device` | RegisterMobileDeviceHandler | AppConfiguration (handlers) | |  
| `location-intake-queue` | | Mobile Web<br/>AppConfiguration (handlers) | |  
| `location-process-queue` | LocationHandler? | LocationIntakeHandler | |  
| `macropoint-alerting-prod-compliance-manager` | ComplianceManager | | |  
| `macropoint-alerting-prod-trigger-aggregator` | TriggerAggregator | | |  
| `macropoint-services-prod-fraud-detection` | FraudDetection | | |  
| `monday-update-intake` | CarrierIntegration | CarrierIntegration | |  
| `order-status-callback-queue` | Notification? BulkLoad? | | |  
| `penalty-box-queue` | PenaltyBoxService | | |  
| `phone-notification-android` | PhoneNotificationProcessor | PhoneNotificationProcessor | |  
| `phone-notification-intake` | PhoneNotificationProcessor | OrdersNeedingUpdate, * | |  
| `phone-notification-ios` | PhoneNotificationProcessor | PhoneNotificationProcessor | |  
| `phone-notification-sms-fallback` | PhoneNotificationProcessor | | |  
| `refresh-user-activeloads-cache` | RefreshActiveLoadCacheProcessor | | |  
| `service-fabric-data-migration-intake` | ServiceFabricDataMigration | ServiceFabricDataMigration | `StartServiceMessage` |  
| `spoofing-order-updater` | SpoofingDetectionService | AppConfiguration (handlers) | |  
| `stop-event-process-mobile-queue` | | AppConfiguration (handlers) | |  
| `stop-event-process-queue` | | LocationIntakeHandler | |  
| `visibility-smsforwarder.q` | SmsQueueService | | |