# LibriaDbSync
Механизм синхронизации и генератор RSS для сайта Anilibria.tv (неофициальный)

Synchronization engine and the RSS Generator to produce RSS feeds over Anilibria.tv (unofficial)

# Оправдание / Justification
Это приложение было создано задолго до того, как на Либрии появилась возможность читать RSS (API 2.0). На сегодняшний день оно просто немного расширяет функционал, позволяя получать обновления только по выбранным категориям событий.

This app has been created long before the ability to read RSS appeared on Libria (API 2.0). As of today it simply provides greater flexibility, allowing to get the feed over particular event categories.

# Использование / Usage
Добавьте один из следующих фидов (ну или все) в вашу читалку RSS (Thunderbird например):
- https://getlibriarss.azurewebsites.net/api/RssOnline Фид наполняется сообщениями по мере появления серий *в плеере*, и не будет реагировать на изменения описания релиза или добавления/изменения торрентов.
- https://getlibriarss.azurewebsites.net/api/RssTorrent Фид наполняется сообщениями по мере появления новых торрентов, и не будет реагировать на изменения описания релиза или появление серии в онлайне.

Add one of the following feeds (or all of them) to your RSS reader (e.g. Thunderbird):
- https://getlibriarss.azurewebsites.net/api/RssOnline The feed is populated when the episodes show up *in the player*, and ignores when release description changes or new torrent appears.
- https://getlibriarss.azurewebsites.net/api/RssTorrent The feed is populated when the new torrent is added, and ignores when release description changes or new episodes upload to the player.

# Тех. данные фида / Feed tech specs
Соответствует спецификации RSS 2.0. Однако не является 100% совместимым из-за HTML содержимого и некоторых других деталей. Не содержит другой информации помимо данных с Анилибрии (т.е. никакой рекламы). Частота обновления - 15 минут.

Complies with RSS 2.0 specification. However, feed is not 100% compatible because of HTML content and some other details. Does not contain any other content than from Anilibria (i.e. no ads). Refresh frequency - 15 minutes.

# TODO
* Pass through the episode update time from API v2.

# Architecture
 - Azure Sql DB
 - Azure function, timer triggered - synchronizer. Each 15 minutes gets last 50 updated releases from Anilibria API, and updates the information on them to the Sql DB.
     - Initial synchronization was performed using the Anilibria UWP application Azure API (it has a bug and always returns *entire* DB :)) to minimize the public site impact.
     - Unfortunately because of the bug, it is not possible to use it for the regular updates.
     - Libria API v.2.x.x is used; for emergency cases, backward-compatible www.anilibria.tv/index.php-based Extractor v1 is preserved in the code.
 - Azure function, HTTP endpoint, at the above mentioned feed address. Manually generates the RSS feed from the last 50 episodes recorded on the Sql DB.
 - Azure function, HTTP endpoint, at the above mentioned feed address. Manually generates the RSS feed from the last 100 torrents (50 releases) recorded on the Sql DB.
 Note: Azure function runtime is updated to v4/NET6.