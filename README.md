# LibriaDbSync
Synchronization engine and the RSS Generator to produce RSS feeds over Anilibria.tv (unofficial)

# Justification
This app has been created long before the ability to read RSS appeared on Libria (API 2.0). As of today it simply provides greater flexibility, allowing to get the feed over particular event categories.

# Usage
The app has been removed from Azure  on 17 July 2024 due to CloudFlare block of Azure edge points from Libria side. It will not be reinstated.

# Feed tech specs
Complies with RSS 2.0 specification. However, feed is not 100% compatible because of HTML content and some other details. Does not contain any other content than from Anilibria (i.e. no ads). Refresh frequency - 15 minutes.

# Architecture
 - Azure Sql DB
 - Azure function, timer triggered - synchronizer. Each 15 minutes gets last 50 updated releases from Anilibria API, and updates the information on them to the Sql DB.
     - Initial synchronization was performed using the Anilibria UWP application Azure API (it has a bug and always returns *entire* DB :)) to minimize the public site impact.
     - Unfortunately because of the bug, it is not possible to use it for the regular updates.
     - Libria API v.2.x.x is used; for emergency cases, backward-compatible www.anilibria.tv/index.php-based Extractor v1 is preserved in the code.
 - Azure function, HTTP endpoint, at the above mentioned feed address. Manually generates the RSS feed from the last 50 episodes recorded on the Sql DB.
 - Azure function, HTTP endpoint, at the above mentioned feed address. Manually generates the RSS feed from the last 100 torrents (50 releases) recorded on the Sql DB.
 Note: Azure function runtime is updated to v4/NET6.
