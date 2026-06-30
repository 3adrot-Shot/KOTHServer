# KOTHServer

> Эмулятор API для режима **KOTH (King of the Hill)** с хранением профилей, статистики, банов и бонус-кодов.

<p align="center">
  <img alt=".NET Framework" src="https://img.shields.io/badge/.NET_Framework-4.8-512BD4?style=for-the-badge&logo=dotnet&logoColor=white">
  <img alt="Database" src="https://img.shields.io/badge/Database-MySQL-4479A1?style=for-the-badge&logo=mysql&logoColor=white">
  <img alt="API" src="https://img.shields.io/badge/API-HttpListener-5C2D91?style=for-the-badge">
  <img alt="Status" src="https://img.shields.io/badge/Status-Active-success?style=for-the-badge">
</p>

---

## О проекте

**KOTHServer** — это серверное приложение, которое эмулирует API, используемое модом KOTH. Проект предназначен для приема и обработки игровых запросов, сохранения данных игроков в базе и выдачи ответов в ожидаемом клиентом формате.

Сервер поддерживает:
- получение и обновление профиля игрока;
- массовое обновление профилей;
- сохранение пресетов;
- прием и выдачу статистики;
- получение активных банов;
- работу с бонус-кодами;
- подробное логирование входящих запросов и ошибок.

---

## Что умеет сервер

- авторизация запросов через `X-AUTH-TOKEN`;
- работа по HTTP через `HttpListener`;
- хранение данных в MySQL;
- автоматическое создание базового профиля при первом запросе;
- сохранение инвентаря, пресетов и статистики;
- выдача активных банов;
- активация и получение бонусов;
- вывод детализированных логов запросов в консоль, включая ложные постукивания от аналитических сервисов

---

## Доступные endpoints

### Профили
- `GET /apiprofile`
- `POST /apiprofile`
- `POST /apiprofiles`

### Пресеты
- `POST /apipreset/{playerId}`

### Статистика
- `POST /apistats/playersstats`
- `GET /apistats/playerstats`

### Баны
- `GET /apiactiveBans`

### Бонусы
- `GET /apibonus`
- `POST /apibonusCode`

---

## Структура данных

Пример структуры базы данных уже добавлен в репозиторий:

- `DB/Koth.sql`

SQL-файл содержит таблицы для:
- игроков;
- предметов игрока;
- пресетов игрока;
- статистики игрока;
- банов;
- бонус-кодов;
- активированных бонусов игроков.

---

## Быстрый запуск

### 1. Подготовить базу данных
1. Создать пустую базу данных.
2. Импортировать SQL-дамп из файла `DB/Koth.sql`.
3. Убедиться, что у приложения есть доступ к этой базе.

### 2. Настроить сервер
Открыть файл `KOTHServer/Settings.cs` и заполнить основные параметры:
- строку подключения к базе данных в `_connectionString`;
- API-ключ в `_apiKey`;
- адрес сервера в `_hostAdress`.

Текущий формат настроек:

```csharp
public const string _connectionString = "Server=adress;Port=Port;Database=DataBase;Uid=UserName;Pwd=Password;Charset=utf8mb4;";
public const string _apiKey = "MY_TEST_API_KEY_123";
public const string _hostAdress = "http://localhost:5555/";
```

### 3. Собрать проект
1. Восстановить NuGet-пакеты.
2. Собрать проект `KOTHServer`.

### 4. Запустить сервер
1. Запустить приложение `KOTHServer`.
2. После старта сервер начнет слушать адрес, указанный в `_hostAdress`.
3. Для обращений к API передавать заголовок `X-AUTH-TOKEN` со значением из настроек.

---

## Как это работает

При запуске приложение:
1. создает подключение к MySQL через `PlayerRepository`;
2. поднимает HTTP-сервер через `HttpServer`;
3. принимает запросы и передает их в `RequestHandler`;
4. валидирует API-ключ;
5. логирует запросы и ответы;
6. читает или обновляет данные игрока в базе.

Если профиль игрока не найден, сервер может создать базовую запись автоматически.

---

## Основные файлы проекта

- `KOTHServer/Program.cs` — точка входа;
- `KOTHServer/HttpServer.cs` — запуск HTTP-сервера;
- `KOTHServer/RequestHandler.cs` — обработка маршрутов и запросов;
- `KOTHServer/PlayerRepository.cs` — работа с MySQL;
- `KOTHServer/Settings.cs` — базовые настройки;
- `DB/Koth.sql` — пример структуры базы данных.

---

## Используемые технологии

- .NET Framework 4.8 (По возможности времени будет переход на .NET 10)
- C#
- MySQL
- MySql.Data
- System.Text.Json
- HttpListener

---
