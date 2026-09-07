# CrossApp

Наскрізний навчальний проєкт з дисципліни «Крос-платформене програмування».
Усі лабораторні роботи курсу — етапи розвитку цього одного репозиторію.

Студент: Грабань Олекса Андрійович, група ФЕІ-32.

## Предметна область

**Замовлення** — оформлення замовлень клієнтів і підрахунок їхніх сум.

| Сутність | Призначення |
|---|---|
| `Customer` | клієнт, який оформлює замовлення |
| `Product` | товар з ціною, доступний до замовлення |
| `Order` | замовлення клієнта (дата, статус, підсумкова сума) |
| `OrderLine` | рядок замовлення: товар, кількість, ціна на момент продажу |

Домен обрано на першому тижні й не змінюється до кінця семестру.

## Запуск

```bash
dotnet build
dotnet run --project src/Cli
```

Той самий набір даних одним рядком JSON:

```bash
dotnet run --project src/Cli -- --json
```

## Середовище

| Параметр | Значення |
|---|---|
| ОС | macOS 26.4.1 (Darwin 25.4.0), Apple Silicon |
| RID | `osx-arm64` |
| .NET SDK | 10.0.400 |
| Runtime | .NET 10.0.11 |
| TFM | `net10.0` |
| Редактор | Visual Studio Code 1.134.0 + C# Dev Kit |

## Додаткові завдання

### 1. Self-contained публікація під дві RID

```bash
dotnet publish src/Cli -c Release -r osx-arm64 --self-contained true
dotnet publish src/Cli -c Release -r linux-x64 --self-contained true
```

| RID | Розмір каталогу `publish` | Байтів | Файлів |
|---|---|---|---|
| `osx-arm64` | 79 MiB | 81 864 607 | 191 |
| `linux-x64` | 80 MiB | 82 621 287 | 192 |

Різниця — близько 0,7 МБ (≈0,9 %). Обидва каталоги важать під 80 МБ, бо
self-contained складання тягне із собою весь runtime і базові бібліотеки:
власного коду там лише кілька кілобайт. Різниця в розмірі й у кількості файлів
пояснюється тим, що нативні бібліотеки runtime під кожну платформу свої
(`.dylib` проти `.so`), і набір їх не збігається один в один.

Перевірка, що це справді збірки під різні платформи:

```
$ file src/Cli/bin/Release/net10.0/osx-arm64/publish/Cli
Mach-O 64-bit executable arm64
$ file src/Cli/bin/Release/net10.0/linux-x64/publish/Cli
ELF 64-bit LSB pie executable, x86-64, ... for GNU/Linux 3.2.0, stripped
```

Запуск бінарника без `dotnet run`:

```bash
./src/Cli/bin/Release/net10.0/osx-arm64/publish/Cli
```

### 2. Прапорець `--json`

Якщо серед аргументів є `--json`, застосунок виводить ті самі дані одним рядком
через `System.Text.Json` (`JsonNamingPolicy.CamelCase`), інакше — таблицею:

```
{"osDescription":"macOS 26.4.1","osVersion":"Unix 26.4.1","processArchitecture":"Arm64", ... }
```

Обидва режими читають один і той самий об'єкт `EnvironmentInfo`, тож формати
не можуть розійтися між собою.
