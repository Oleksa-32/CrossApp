# CrossApp

Наскрізний навчальний проєкт з дисципліни «Крос-платформене програмування».
Усі лабораторні роботи курсу — етапи розвитку цього одного репозиторію.

Студент: Грабань Олекса Андрійович, група ФЕІ-32.

## Структура solution

```
CrossApp/
  CrossApp.sln
  README.md
  .gitignore
  src/
    Core/                   бібліотека класів (net8.0;net10.0)
      Core.csproj
      EnvironmentInfo.cs    EnvironmentReport + EnvironmentInfo.Collect()
      Dto/                  record-типи формату даних (тиждень 3)
      Domain/               сутності з поведінкою та інваріантами (тиждень 4)
      Storage/              реалізації сховищ (тиждень 5)
    Cli/                    консольний застосунок (net10.0)
      Cli.csproj            містить ProjectReference на Core
      Program.cs            лише виклик Core і форматування виводу
```

Напрямок залежності односторонній: **Cli → Core**. Core не знає про Cli, тому
його згодом можна буде підключити до проєктів `Api` (тиждень 10) і Blazor-клієнта
(тиждень 12). Правило «жодної бізнес-логіки в Program.cs» діє з цієї лабораторної.

Каталоги `Dto/`, `Domain/`, `Storage/` поки порожні й тримаються в репозиторії
через файли-заглушки `.gitkeep` — git не зберігає порожніх каталогів.

## Предметна область

**Замовлення** — оформлення замовлень клієнтів і підрахунок їхніх сум.

| Сутність | Призначення |
|---|---|
| `Customer` | клієнт, який оформлює замовлення |
| `Product` | товар з ціною, доступний до замовлення |
| `Order` | замовлення клієнта (дата, статус, підсумкова сума) |
| `OrderLine` | рядок замовлення: товар, кількість, ціна на момент продажу |

Домен обрано на першому тижні й не змінюється до кінця семестру.

## Збірка і запуск

```bash
dotnet build                            # усе рішення
dotnet build src/Core/Core.csproj       # лише бібліотека (обидва TFM)
dotnet run --project src/Cli            # запуск застосунку
dotnet run --project src/Cli -- --json  # ті самі дані одним рядком JSON
```

`dotnet run --project src/Core` не працює і не має працювати: classlib не має
точки входу `Main`, її результат — `Core.dll` для інших проєктів.

## Multi-targeting

`src/Core/Core.csproj` містить `<TargetFrameworks>net8.0;net10.0</TargetFrameworks>`,
тому бібліотека компілюється двічі, і в `bin/Debug/` з'являються два підкаталоги:

```
src/Core/bin/Debug/net8.0/Core.dll
src/Core/bin/Debug/net10.0/Core.dll
```

Хоча встановлено лише runtime .NET 10, збірка під net8.0 вдалася: для компіляції
потрібен не runtime, а targeting pack `Microsoft.NETCore.App.Ref` 8.0.x, який
`dotnet restore` завантажує з NuGet.

Відмінність між TFM показано умовною компіляцією в `EnvironmentInfo`:

```csharp
#if NET10_0_OR_GREATER
    private const string TargetFrameworkNote = "збірка під net10.0";
#else
    private const string TargetFrameworkNote = "збірка під net8.0";
#endif
```

Cli залишається на `net10.0`, тому при збірці solution обирається `net10.0`-варіант
бібліотеки, і у виводі видно рядок «збірка під net10.0».

## Публікація

```bash
dotnet publish src/Cli -c Release -r osx-arm64  --self-contained true
dotnet publish src/Cli -c Release -r osx-arm64  --self-contained false
dotnet publish src/Cli -c Release -r linux-x64  --self-contained true
```

Розмір каталогу:

```bash
du -sh src/Cli/bin/Release/net10.0/osx-arm64/publish
```

### Self-contained vs framework-dependent

**Self-contained** публікація містить код застосунку, його залежності **і копію
.NET runtime** для конкретного RID. Застосунок працює на машині без встановленого
.NET, але каталог важить десятки мегабайтів і придатний лише для однієї платформи.

**Framework-dependent** публікація містить лише код застосунку і залежності, без
runtime. Каталог у сотні разів менший, але на цільовій машині має бути
встановлений сумісний .NET 10.

| RID | Режим | Розмір publish | Байтів | Файлів | Потрібен runtime |
|---|---|---|---|---|---|
| `osx-arm64` | self-contained | 78 МіБ | 81 882 458 | 193 | ні |
| `osx-arm64` | framework-dependent | 162 КіБ | 165 407 | 7 | так (.NET 10) |
| `linux-x64` | self-contained | 79 МіБ | 82 639 142 | 194 | ні |
| `osx-arm64` | self-contained + `PublishSingleFile` | 72 МіБ | 75 344 776 | 3 | ні |
| `osx-arm64` | self-contained + `SingleFile` + `Trimmed` | 13 МіБ | 13 425 220 | 3 | ні |

Self-contained каталог більший за framework-dependent приблизно у **495 разів**,
хоча власного коду в обох однаково — близько 16 КБ (`Cli.dll` 6 КБ + `Core.dll` 10 КБ).
Уся різниця — це runtime.

### Запуск з каталогу publish

```bash
cd src/Cli/bin/Release/net10.0/osx-arm64/publish
./Cli
```

Вивід збігається з `dotnet run`, змінюються лише рядки `Каталог` і `Поточний каталог`.

На цій машині .NET встановлено через Homebrew
(`/opt/homebrew/Cellar/dotnet/10.0.400/libexec`), а не в типовому
`/usr/local/share/dotnet`, тому framework-dependent бінарник сам runtime не
знаходить і завершується повідомленням «You must install .NET to run this
application». Запуск відбувається після вказання шляху:

```bash
DOTNET_ROOT=/opt/homebrew/Cellar/dotnet/10.0.400/libexec ./Cli
# або
dotnet Cli.dll
```

Self-contained бінарник із того самого коду запускається без жодних змінних
середовища — у цьому і полягає практична різниця між режимами.

На Linux після копіювання бінарника може знадобитися `chmod +x ./Cli`.

## Середовище

| Параметр | Значення |
|---|---|
| ОС | macOS 26.4.1 (Darwin 25.4.0), Apple Silicon |
| RID | `osx-arm64` |
| .NET SDK | 10.0.400 |
| Runtime | .NET 10.0.11 |
| TFM | Core: `net8.0;net10.0`, Cli: `net10.0` |
| Редактор | Visual Studio Code 1.134.0 + C# Dev Kit |

## Додаткові завдання (лабораторна 2)

### 1. `PublishSingleFile`

```bash
dotnet publish src/Cli -c Release -r osx-arm64 --self-contained true -p:PublishSingleFile=true
```

Замість 193 файлів у каталозі залишаються 3: виконуваний `Cli` і два файли
символів `.pdb` (їх можна прибрати через `-p:DebugType=none`). Розмір навіть
менший за звичайну self-contained публікацію — 72 МіБ проти 78 МіБ, бо
однофайлове пакування стискає вміст. Застосунок запускається і дає той самий вивід.

### 2. `PublishTrimmed`

```bash
dotnet publish src/Cli -c Release -r osx-arm64 --self-contained true \
    -p:PublishSingleFile=true -p:PublishTrimmed=true
```

Розмір падає з 72 МіБ до **13 МіБ** (у 5,6 раза), але збірка видає попередження:

```
warning IL2026: Using member 'System.Text.Json.JsonSerializer.Serialize<TValue>(...)'
which has 'RequiresUnreferencedCodeAttribute' can break functionality when trimming
```

Попередження виявилося не теоретичним. Звичайний режим працює, а режим `--json` падає:

```
$ ./Cli --json
Unhandled exception. System.InvalidOperationException: Reflection-based serialization
has been disabled for this application.
```

Причина: trimming видаляє код, на який немає статичних посилань. `JsonSerializer`
у режимі відображення (reflection) звертається до типів під час виконання, тому
аналізатор не може довести, що вони потрібні, — і вони зникають зі збірки. Саме
тому trimming небезпечний для коду з рефлексією. Правильне вирішення — source
generator (`JsonSerializerContext`) замість reflection-based серіалізації.

### 3. Запуск publish під чужу RID

```
$ file /tmp/pub-linux/Cli
ELF 64-bit LSB pie executable, x86-64, ... for GNU/Linux 3.2.0, stripped
$ /tmp/pub-linux/Cli
exec format error
```

Публікація під `linux-x64` дає ELF-бінарник для x86-64. macOS (Mach-O, arm64) не
може його запустити: помиляється не .NET, а завантажувач ядра, який не впізнає
формат файлу. Це наочно пояснює, чому self-contained публікація прив'язана до RID.

## Додаткові завдання (лабораторна 1)

### Self-contained публікація під дві RID

| RID | Розмір каталогу `publish` | Байтів | Файлів |
|---|---|---|---|
| `osx-arm64` | 79 MiB | 81 864 607 | 191 |
| `linux-x64` | 80 MiB | 82 621 287 | 192 |

Різниця — близько 0,7 МБ (≈0,9 %). Обидва каталоги важать під 80 МБ, бо
self-contained складання тягне із собою весь runtime і базові бібліотеки:
власного коду там лише кілька кілобайт.

### Прапорець `--json`

Якщо серед аргументів є `--json`, застосунок виводить ті самі дані одним рядком
через `System.Text.Json` (`JsonNamingPolicy.CamelCase`), інакше — таблицею.
Обидва режими читають один і той самий `EnvironmentReport`, тож формати
не можуть розійтися між собою.

### Запуск у контейнері

```bash
docker run --rm -v ${PWD}:/src -w /src mcr.microsoft.com/dotnet/sdk:10.0 \
    dotnet run --project src/Cli
```

| Рядок | macOS (локально) | Контейнер |
|---|---|---|
| `ОС` | `macOS 26.4.1` | `Ubuntu 24.04.5 LTS` |
| `Архітектура` | `Arm64` | `Arm64` |
| `RID` | `osx-arm64` | `linux-arm64` |
| `Runtime` | `.NET 10.0.11` | `.NET 10.0.12` |

Змінилися ОС, RID і шляхи, але архітектура залишилась `Arm64`, бо контейнер
використовує процесор хоста. Вихідний код не змінювався жодного рядка.
