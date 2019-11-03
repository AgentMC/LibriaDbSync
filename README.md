# LibriaDbSync
Механизм синхронизации и генератор RSS для сайта Anilibria.tv (неофициальный)

Synchronization engine and the RSS Generator to produce RSS feed over Anilibria.tv (unofficial)

# Использование / Usage
Добавьте следующий фид в вашу читалку RSS (Thunderbird например): https://getlibriarss.azurewebsites.net/api/RssOnline
Фид наполняется сообщениями по мере появления серий *в плеере*, и не будет реагировать на изменения описания релиза или добавления/изменения торрентов.

Add the following feed to your RSS reader (e.g. Thunderbird): https://getlibriarss.azurewebsites.net/api/RssOnline
The feed is populated when the episodes show up *in the player*, and ignores when release description changes or new torrent appears.

# Тех. данные фида / Feed tech specs
Соответствует спецификации RSS 2.0. Однако не является 100% совместимым из-за HTML содержимого и некоторых других деталей. Не содержит другой информации помимо данных с Анилибрии (т.е. никакой рекламы). Частота обновления - 15 минут.

Complies with RSS 2.0 specification. However, feed is not 100% compatible because of HTML content and some other details. Does not contain any other content than from Anilibria (i.e. no ads). Refresh frequency - 15 minutes.

# Время добавления серии / Episode upload time
К сожалению, API Анилибрии не возвращает время создания серии на сервере (в плеере), поэтому время добавления эпизода считается как время заливки самого свежего торрента — или время обновления релиза, в зависимости от того, что свежее. Также, если ни то, ни другое не находится в пределах 36 часов (серия появилась в плеере, а ни релиз не обновился, ни торрент не залили), то используется текущее время обнаружения нового эпизода. Поэтому время, которое отображает ваш RSS клиент не является моментом появления серии в плеере.

Unfortunately, the Anilibria API does not return the episode creation timestamp, and the time the episode was added is calculated as the most recent torrent upload time — or the release update timestamp, if it's fresher. Also, if neither lies within 36 hours (an episode was added but neither release was updated nor new torrent uploaded), then the current timestamp is used. That's why the time your RSS client shows is not the moment the episode appeared in the player.

# Architecture
 - Azure Sql DB
 - Azure function, timer triggered - synchronizer. Each 15 minutes gets last 50 updated releases from Anilibria API, and updates the information on them to the Sql DB.
     - Initial synchronization was performed using the Anilibria UWP application Azure API (it has a bug and always returns *entire* DB :)) to minimize the public site impact.
     - Unfortunately because of the bug, it is not possible to use it for the regular updates.
 - Azure function, HTTP endpoint, at the above mentioned feed address. Manually generates the RSS feed from the last 24 (two site pages) episodes recorded on the Sql DB.
 - I am considering to create a second RSS stream - over the last torrents updated/added, but not sure if that will work as expected.
