# О программе
Сервер опроса приборов для АСКУЭ  
# Запуск
Для работы сервера необходимо проверить:
- наличие установленного NetFramework версии 3.5
- наличие конфигурационного файл "PoolServer.exe" с расширением *.config в дирректории /PoolServer/bin/Debug. Если файл отсутствует, следует скопировать шаблон "PoolServer_deploy.exe" расширения *.config из дирректории /Etc и переименовать его соответственно.
- параметры соединения с базой данных в файле PoolServer.exe.config. Для подключения к основной БД PostgreSQL следует проверить и при необходимости исправить равнозначные параметры "generalConnection" и "PoolServer.Properties.Settings.ConnectionString".
# Категории параметров
1. Текущие - опрос приборов с максимально доступной частотой
2. Суточные
3. Месячные
4. Архивные
5. Часовые срезы
6. Получасовые срезы

- При настройке счетчика, предназначенного для работы с параметрами 4, 5, 6 - следует обязательно указывать дату установки прибора. 
- При настройке порта любого типа, следует обращать внимание на таймаут чтения. Его оптимальное значение для большинства устройств - 600.



