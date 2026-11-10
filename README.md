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
      DomainException.cs    порушення бізнес-правила (тиждень 8)
      EnvironmentInfo.cs    EnvironmentReport + EnvironmentInfo.Collect()
      Dto/                  record-типи формату даних (тиждень 3)
        ProductDto.cs       рядок прайса: Id, Name, Price, Note?
        CustomerDto.cs      клієнт: Id, Name, Email, Phone?
        ImportResult.cs     ImportResult<T>(Items, Errors) + статистика
        MixedImportResult.cs    два типи даних з одного файлу + помилки
        OrderDto.cs         OrderDto + OrderLineDto — формат зберігання замовлення (тиждень 4)
      Import/               розбір вхідних файлів (тиждень 3)
        ProductCsvImporter.cs   Load(path) + ParseLine(line) через switch expression
        ProductJsonImporter.cs  той самий ImportResult на System.Text.Json
        MixedCsvImporter.cs     різнорідні рядки за префіксом типу P / C
      Domain/               сутності з поведінкою та інваріантами (тиждень 4)
        Order.cs            замовлення: Create, AddLine, Confirm, Cancel, ToDto/FromDto
        OrderLine.cs        рядок замовлення; створює і змінює лише Order
        OrderStatus.cs      Draft / Confirmed / Cancelled
        Product.cs          товар каталогу: Create, ChangePrice, ToDto/FromDto
        ProductCatalog.cs   ImportResult<ProductDto> → ImportResult<Product> (додаткові завдання)
      Configuration/        налаштування застосунку (тиждень 9)
        AppOptions.cs       AppOptions → Storage / Logging / Orders; StorageOptions.ResolveFullPath
      Abstractions/         контракти (тиждень 5)
        IOrderStore.cs      List / GetById / Add / Update / Remove
        INotificationSink.cs    одержувач сповіщень через конструктор (тиждень 6, додатково)
        IOrderImporter.cs       асинхронний контракт імпорту: LoadAsync(path, ct) (тиждень 8)
        IOrderStreamImporter.cs потоковий імпорт: IAsyncEnumerable<Order> (тиждень 8, додатково)
      Storage/              реалізації сховищ (тиждень 5)
        InMemoryOrderStore.cs   словник у пам'яті, початкові дані через конструктор
        FileOrderStore.cs       кеш + JSON-файл, запис після кожної зміни; шлях — з конфігурації (тиждень 9)
        OrderJsonFile.cs        формат файлу колекції: camelCase, кирилиця без \uXXXX, атомарний запис (тиждень 9)
        CachingOrderStore.cs    декоратор над будь-яким IOrderStore (додаткове завдання)
        JsonOrderImporter.cs    async-читання JSON: FileStream(useAsync), DeserializeAsync (тиждень 8)
        ProgressReadStream.cs   відсоток прочитаного файлу для IProgress<int> (тиждень 8, додатково)
      Events/               дані доменних подій (тиждень 6)
        OrderChangeKind.cs  Created / LineAdded / Confirmed / Cancelled / Imported
        OrderChangedEventArgs.cs  OrderId, Kind, LinesCount, Total, Delta, At
      Services/
        OrderService.cs     CreateOrder / AddLine / Confirm / Cancel / Find / Search + event Changed;
                            ImportAsync / ImportStreamAsync / WithTimeoutAsync (тиждень 8)
      Reports/              звіти LINQ to Objects (тиждень 7)
        OrderRow.cs         record-DTO рядка звіту про замовлення
        GroupSummary.cs     record-DTO групи: Key, Count, Total
        CustomerTotal.cs    record-DTO клієнта в топ-N
        ProductSales.cs     record-DTO товару: штук і виручка
        CustomerActivity.cs record-DTO GroupJoin клієнтів із замовленнями (додатково)
        CustomerOrderRow.cs record-DTO Join замовлень з іменами клієнтів (додатково)
        ReportService.cs    усі запити: фільтр, топ-N, GroupBy, агрегати, Join/GroupJoin
        EnumerableExtensions.cs  метод-розширення TopBy (додатково)
        ReportVariants.cs   варіанти звітів для вимірювань Stopwatch (додатково)
      SampleData.cs         10 товарів, 8 клієнтів, 20 замовлень за три місяці; Generate(100 000)
    Cli/                    консольний застосунок (net8.0;net10.0)
      Cli.csproj            ProjectReference на Core, пакети конфігурації, UserSecretsId (тиждень 9)
      appsettings.json      базові налаштування — у git, без секретів (тиждень 9)
      appsettings.Development.json  детальніший лог (Debug)
      appsettings.Production.json   лог Warning, дані в LocalApplicationData
      AppConfiguration.cs   ConfigurationBuilder: json → json.{Env} → user-secrets → CROSSAPP_* → args
      AppLog.cs             консольний журнал з порогом Logging:MinimumLevel
      ExchangeCommands.cs   підкоманди config / export / import (тиждень 9)
      Samples.cs            шлях до вхідних прикладів від каталогу застосунку
      Program.cs            composition root + вибір режиму
      DomainDemo.cs         сценарії «успіх» і «порушення інваріантів» (тиждень 4)
      OrderScenario.cs      сценарій сервісу і сценарій відмови (тиждень 5)
      EventScenario.cs      підписники, відписка, відмова без події (тиждень 6, --events)
      ChangeLog.cs          формат рядка журналу і безпечний дозапис у файл (тиждень 6)
      ConsoleSink.cs        INotificationSink → консоль (тиждень 6, додатково)
      FileSink.cs           INotificationSink → logs/sink.log (тиждень 6, додатково)
      SinkScenario.cs       сценарій з одержувачем через конструктор (--sink-console / --sink-file)
      ReportCommands.cs     підкоманди report-* і вивід звітів (тиждень 7)
      DeferredScenario.cs   демо відкладеного виконання (report-deferred)
      BenchmarkScenario.cs  Stopwatch на 100 000 записів (report-bench, додатково)
      ImportCommand.cs      підкоманда import-orders: режими, таймаут, великий файл (тиждень 8)
      ConsoleProgress.cs    IProgress<int> → смуга відсотків у консолі (тиждень 8, додатково)
  tests/
    Core.Tests/             xUnit (net10.0), посилається лише на Core (тиждень 8)
      TestData.cs           фіксований набір замовлень і FakeTimeProvider
      FakeOrderImporter.cs  імпортери без диска і синхронний IProgress для тестів
      OrderServiceTests.cs  інваріанти, DomainException, подія, сума замовлення
      ReportServiceTests.cs звіти: Theory з порогами, детермінований топ-N, агрегати
      ImportTests.cs        async-імпорт, скасування, потік, таймаут, прогрес
      StorageOptionsTests.cs    побудова шляху до файлу даних (тиждень 9)
      FileOrderStoreTests.cs    формат файлу, кирилиця, атомарний запис, зіпсований JSON (тиждень 9)
  data/
    sample.csv              13 рядків даних, з них 3 навмисно пошкоджені
    sample.json             той самий домен у JSON (додаткове завдання)
    mixed.csv               товари і клієнти в одному файлі (додаткове завдання)
    catalog.csv             каталог для перевірки інваріантів Product (тиждень 4)
    orders-import.json      17 замовлень для async-імпорту (тиждень 8)
    orders-broken.json      запис 3 з нульовою кількістю — «Дані відхилено» (тиждень 8)
```

Напрямок залежності односторонній: **Cli → Core**. Core не знає про Cli, тому
його згодом можна буде підключити до проєктів `Api` (тиждень 10) і Blazor-клієнта
(тиждень 12). Правило «жодної бізнес-логіки в Program.cs» діє з цієї лабораторної.

Файл сховища `data/orders.json` з'являється не в корені репозиторію, а поруч зі
зібраним застосунком (`src/Cli/bin/Debug/net10.0/data/`), бо шлях будується від
`AppContext.BaseDirectory`; з лабораторної 9 каталог і ім'я файлу задає конфігурація
(`Storage:DataPath`, `Storage:FileName`, змінна `CROSSAPP_DATA_PATH`). У git він не потрапляє — `bin/` ігнорується.
Так само поруч зі збіркою створюються журнали `logs/app.log`, `logs/sink.log` і
`logs/import-skipped.log` (тиждень 6).

## Імпорт даних (лабораторна 3)

```bash
dotnet run --project src/Cli -f net10.0 -- --import             # data/sample.csv за замовчуванням
dotnet run --project src/Cli -f net10.0 -- data/sample.json      # той самий домен у JSON
dotnet run --project src/Cli -f net10.0 -- --mixed                # товари і клієнти з одного файлу
dotnet run --project src/Cli -f net10.0 -- data/nope.csv         # повідомлення + код виходу 1
dotnet run --project src/Cli -f net10.0 -- --env                 # режим лабораторної 2
```

### Формат `data/sample.csv`

| Параметр | Значення |
|---|---|
| Роздільник | `;` (крапка з комою) |
| Колонки | `id;name;price` |
| Заголовок | допустимий, але не обов'язковий (рядок 1, що починається на `id`) |
| Кодування | UTF-8, задане явно в `File.ReadAllLines(path, Encoding.UTF8)` |
| Числа | `CultureInfo.InvariantCulture`, десятковий роздільник — крапка |
| Коментарі | рядки, що починаються з `#`, і порожні рядки пропускаються без помилки |

Роздільник `;` обрано свідомо: кома зустрічається і в назвах товарів, і як
десятковий роздільник в українській локалі, тому як роздільник колонок вона
ненадійна.

### Пошкоджені рядки

Три останні рядки файлу зіпсовані навмисно — це тестові дані, а не недбалість.
Кожен демонструє окрему гілку `switch expression` у `ParseLine`:

| Рядок | Вміст | Патерн, що спрацював | Повідомлення |
|---|---|---|---|
| 12 | `P-011;Підставка для ноутбука` | `{ Length: < 3 }` | очікую 3 колонки, отримав 2 |
| 13 | `P-012;Картридер SD;безкоштовно` | `[_, _, var price] when !decimal.TryParse(...)` | ціна 'безкоштовно' не є невід'ємним числом |
| 14 | `P-013;;560.00` | `["", _, _] or [_, "", _]` | ідентифікатор або назва порожні |

Імпорт не переривається на першій помилці: `Load` повертає
`ImportResult<ProductDto>` — прочитані записи **разом** із переліком помилок,
де кожна помилка містить номер рядка у файлі.

```
Завантажено записів: 10
  P-001  Ноутбук Lenovo IdeaPad 15          24999.00 грн
  ...
Пропущено рядків: 3
  ! рядок 12: очікую 3 колонки, отримав 2
  ! рядок 13: ціна 'безкоштовно' не є невід'ємним числом
  ! рядок 14: ідентифікатор або назва порожні
Підсумок: усього 13, прийнято 10, пропущено 3 (23.1 % помилок)
```

### record чи class

`ProductDto` і `CustomerDto` — `record`, бо це форма даних одного рядка файлу:
їх порівнюють за значеннями, копіюють через `with` і не змінюють після
створення. Сутності з поведінкою та інваріантами (`Order`, `OrderLine`)
з'являться на четвертому тижні в `Core/Domain` і будуть саме класами.

Знак `?` стоїть лише в `Note` і `Phone` — полях, значення яких у файлі справді
може не бути. Позначати nullable «про запас» шкідливо: кожне звернення до такого
поля потребуватиме зайвої перевірки, і підказка перетвориться на шум.

### Змішаний файл: два типи з одного проходу

`data/mixed.csv` містить рядки різних типів, які розрізняються **префіксом у
першій колонці**: `P` — товар, `C` — клієнт.

```
P;P-001;Ноутбук Lenovo IdeaPad 15;24999.00
C;C-001;Іван Петренко;ivan.petrenko@example.com;+380671234567
```

`MixedCsvImporter` розбирає такий файл одним `switch expression`, у якому
префікс — це **константний патерн на першій позиції патерна списку**:

```csharp
["P", var id, var name, var price] when decimal.TryParse(...) => new ParseProduct(...),
["C", var id, var name, var email, var phone]                 => new ParseCustomer(...),
["C", var id, var name, var email]                            => new ParseCustomer(...),
["P", ..] => new ParseFailed($"товар: очікую 4 колонки ..."),
[var tag, ..] => new ParseFailed($"невідомий префікс типу '{tag}'"),
```

Дві нові речі порівняно з основним завданням:

- **зріз `..`** — «будь-яка кількість решти елементів»: `["P", ..]` читається як
  «рядок починається з P, а що далі — неважливо». Це дає змогу відповісти
  «префікс правильний, але форма рядка не та» замість загального «невідомий тип»;
- **два різні типи результату з одного switch**: ієрархія `ParseOutcome`
  має три різновиди — `ParseProduct`, `ParseCustomer` і `ParseFailed`, а метод
  `Load` розводить їх по своїх списках патерном типу.

Клієнт приймається і з телефоном, і без нього — саме тому для нього дві гілки
різної довжини, а `Phone` у `CustomerDto` оголошено як `string?`.

Результат повертає `MixedImportResult(Products, Customers, Errors)`:
узагальнений `ImportResult<T>` тут не підходить, бо `T` мав би бути один, а
рядки файлу описують два різні типи.

```
Завантажено товарів: 5, клієнтів: 5
Пропущено рядків: 4
  ! рядок 12: товар: очікую 4 колонки (P;id;назва;ціна), отримав 3
  ! рядок 13: клієнт: ідентифікатор, ім'я або пошта порожні
  ! рядок 14: невідомий префікс типу 'W': очікую P або C
  ! рядок 15: товар: ціна 'задорого' не є невід'ємним числом
Підсумок: усього 14, прийнято 10, пропущено 4 (28.6 % помилок)
```

## Доменна модель (лабораторна 4)

```bash
dotnet run --project src/Cli -f net10.0 -- --domain                     # data/catalog.csv за замовчуванням
dotnet run --project src/Cli -f net10.0 -- --domain data/sample.csv     # каталог з іншого файлу
```

Record-типи з `Dto/` лишилися **форматом даних**: вони переносять значення між
файлом і програмою і нічого не гарантують. Правила живуть у класах `Core/Domain`:
об'єкт неможливо створити в некоректному стані або перевести в нього, а спроба
порушити правило закінчується винятком зі зрозумілим повідомленням.

| Сутність | Роль | Як змінюється стан |
|---|---|---|
| `Order` | замовлення клієнта, головна сутність | `AddLine`, `Confirm`, `Cancel` |
| `OrderLine` | рядок замовлення з ціною на момент продажу | лише через `Order.AddLine` |
| `Product` | товар каталогу | `ChangePrice` |

`Order` володіє своїми рядками: список `_lines` приватний, назовні віддається
`IReadOnlyList<OrderLine>` через `AsReadOnly()`, а фабрика `OrderLine.Create`
має модифікатор `internal` — з `Cli` створити рядок в обхід замовлення неможливо.
Ціна копіюється з `Product` у момент `AddLine`, тому `ChangePrice` у каталозі не
змінює суми вже оформленого замовлення.

Усі сутності мають приватний конструктор і статичну фабрику `Create`; жодна
властивість не має публічного `set`. Дата замовлення передається у `Create`
ззовні, а не береться з `DateTime.Now`.

### Інваріанти

| # | Правило | Виняток | Де перевіряється |
|---|---|---|---|
| 1 | Номер замовлення не порожній | `ArgumentException` | `Order.Create` |
| 2 | Ідентифікатор клієнта не порожній | `ArgumentException` | `Order.Create` |
| 3 | Дата створення задана | `ArgumentException` | `Order.Create` |
| 4 | Ідентифікатор і назва товару в рядку не порожні | `ArgumentException` | `OrderLine.Create` (з `Order.AddLine`) |
| 5 | Кількість у рядку більша за нуль | `ArgumentOutOfRangeException` | `OrderLine.Create`, `OrderLine.Increase` |
| 6 | Ціна в рядку не від'ємна | `ArgumentOutOfRangeException` | `OrderLine.Create` |
| 7 | Рядки додаються лише до чернетки | `InvalidOperationException` | `Order.AddLine` |
| 8 | Порожнє замовлення підтвердити не можна | `InvalidOperationException` | `Order.Confirm` |
| 9 | Допустимі лише переходи Draft → Confirmed, Draft → Cancelled, Confirmed → Cancelled | `InvalidOperationException` | `Order.Confirm`, `Order.Cancel` |
| 10 | Ідентифікатор товару не порожній і складається лише з літер, цифр і дефіса | `ArgumentException` | `Product.Create` |
| 11 | Назва товару не порожня | `ArgumentException` | `Product.Create` |
| 12 | Ціна товару не від'ємна (і при створенні, і при зміні) | `ArgumentOutOfRangeException` | `Product.Create`, `Product.ChangePrice` |
| 13 | Стан у DTO — один із Draft / Confirmed / Cancelled | `ArgumentException` | `Order.FromDto` |

Правило вибору типу винятку: `Argument*` — вхідне значення некоректне саме по
собі; `InvalidOperationException` — аргументи прийнятні, але операцію забороняє
поточний стан об'єкта. На десятому тижні Api перетворить їх на 400 і 409.

Кожен метод спершу виконує всі перевірки, а вже потім змінює поля: після будь-якої
відмови об'єкт лишається в попередньому стані.

### ToDto / FromDto

`Product.ToDto()` повертає `ProductDto` тижня 3, `Order.ToDto()` — новий
`OrderDto` з рядками `OrderLineDto`. Стан у `OrderDto` зберігається рядком, щоб
формат файлу не залежав від enum доменної моделі.

`FromDto` **не** викликає приватний конструктор напряму: `Product.FromDto` іде
через `Create`, а `Order.FromDto` відтворює життя замовлення — `Create`, потім
`AddLine` для кожного рядка, потім `Confirm` або `Cancel`. Тому пошкоджений файл
не відновить ні рядка з нульовою кількістю, ні підтвердженого порожнього замовлення.
Саме ці методи використає сховище п'ятого тижня.

### Вивід

```
=== Сценарій 1: успіх ===
O-001 клієнт C-001 від 2026-10-05 — чернетка, рядків 2, сума 27697.50 грн
  P-001  Ноутбук Lenovo IdeaPad 15          1 ×  24999.00 =   24999.00 грн
  P-002  Миша бездротова Logitech M330      3 ×    899.50 =    2698.50 грн
Після ChangePrice(799.00) і Confirm(): P-002 Миша бездротова Logitech M330 — 799.00 грн
O-001 клієнт C-001 від 2026-10-05 — підтверджене, рядків 2, сума 27697.50 грн
...
=== Сценарій 2: порушення інваріантів ===
  нульова кількість у рядку: ArgumentOutOfRangeException — Кількість товару P-002 у рядку має бути більшою за нуль (Parameter 'quantity') Actual value was 0.
  підтвердження порожнього замовлення: InvalidOperationException — Замовлення O-002 порожнє: підтвердити можна лише замовлення з рядками
  рядок у підтверджене замовлення: InvalidOperationException — Замовлення O-001 має стан «підтверджене»: додавати рядки можна лише в чернетці
  ...
Стан після всіх відмов:
  O-001 клієнт C-001 від 2026-10-05 — підтверджене, рядків 2, сума 27697.50 грн
```

Дванадцять спроб порушити правила — дванадцять винятків, і жодна не змінила
об'єкт: після всіх відмов `O-001` так само підтверджене, з двома рядками і тією
самою сумою.

Спроба обійти правила на рівні компілятора не збирається:

```
error CS0200: Property or indexer 'Product.Price' cannot be assigned to -- it is read only
error CS0200: Property or indexer 'Order.Status' cannot be assigned to -- it is read only
error CS1729: 'Order' does not contain a constructor that takes 3 arguments
error CS1061: 'IReadOnlyList<OrderLine>' does not contain a definition for 'Add'
```

Пошук `Console.` і `File.` у `src/Core/Domain` порожній: доменна модель не
залежить ні від консолі, ні від файлової системи, ні від `Cli`.

### Додаткові завдання

**1. Імпорт → сутності.** `ProductCatalog.FromImport(ImportResult<ProductDto>)`
повертає `ImportResult<Product>` — та сама ідея «дані + помилки», що й на тижні 3.
Помилки формату з імпортера переносяться як є, до них додаються записи, які
розібралися, але не пройшли інваріанти `Product`:

```
Сутностей створено: 4
Відхилено: 3
  ! рядок 7: ціна 'дорого' не є невід'ємним числом
  ! товар 'P 003': Ідентифікатор товару 'P 003' може містити лише літери, цифри та дефіс (Parameter 'id')
  ! товар 'p-002': ідентифікатор P-002 уже належить іншому товару
```

Перший рядок відхилив імпортер (формат), другий — сутність (інваріант), третій —
правило про два товари одночасно.

**2. Інваріант, що охоплює дві сутності.** «Ідентифікатор товару унікальний у
каталозі». Окремий `Product` про інших товарів не знає, тому перевірити це
правило в `Product.Create` неможливо: потрібен той, хто бачить увесь набір. Зараз
це `ProductCatalog`, на п'ятому тижні — сервіс, який перед додаванням запитає
сховище. Такі правила виносять у сервіс, бо сутність мала б інакше отримати доступ
до сховища, а домен перестав би бути незалежним від інфраструктури. Нормалізація
`ToUpperInvariant` тут важлива: `p-002` і `P-002` — той самий ключ.

**3. Явний стан-перелічування.** `OrderStatus { Draft, Confirmed, Cancelled }`
замість прапорця `IsConfirmed`; допустимі переходи описано одним switch expression
за кортежем `(Status, target)` у `Order.EnsureCanMoveTo` — усе, чого немає в
таблиці, заборонено.

## Сервісний шар (лабораторна 5)

```bash
dotnet run --project src/Cli -f net10.0                       # InMemoryOrderStore, після виходу дані зникають
dotnet run --project src/Cli -f net10.0 -- --file             # FileOrderStore, data/orders.json, дані накопичуються
dotnet run --project src/Cli -f net10.0 -- --file --cache     # те саме через декоратор CachingOrderStore
```

Доменна логіка не залежить від способу зберігання: сервіс працює з
**інтерфейсом** сховища, а яку реалізацію підставити, вирішує лише точка входу.

| Роль | Тип | Де |
|---|---|---|
| Контракт сховища | `IOrderStore` | `src/Core/Abstractions/IOrderStore.cs` |
| Реалізація в пам'яті | `InMemoryOrderStore` | `src/Core/Storage/InMemoryOrderStore.cs` |
| Файлова реалізація | `FileOrderStore` | `src/Core/Storage/FileOrderStore.cs` |
| Декоратор (додатково) | `CachingOrderStore` | `src/Core/Storage/CachingOrderStore.cs` |
| Сервіс | `OrderService` | `src/Core/Services/OrderService.cs` |
| Composition root | top-level `Program.cs` | `src/Cli/Program.cs` |

### Контракт `IOrderStore`

```csharp
public interface IOrderStore
{
    IReadOnlyList<Order> List();
    Order? GetById(string id);
    void Add(Order item);
    void Update(Order item);
    bool Remove(string id);
}
```

П'ять методів — рівно те, що потрібно сервісу, і нічого «про запас»: кожен
зайвий метод мусила б підтримувати кожна реалізація, включно з майбутньою БД.

- `Order?` — запису з таким id може не бути, і це нормальний результат пошуку.
- `IReadOnlyList<Order>` — зовнішній код не може змінити сховище в обхід `Add` / `Update` / `Remove`.
- `Add` кидає `InvalidOperationException` на дубль id, `Update` — на невідомий id.
  Однакова поведінка в обох реалізаціях — частина контракту, а не деталь реалізації.
- Ні `Flush`, ні `SaveToFile` у контракті немає: це специфіка файлу.

Агрегат зберігається як одне ціле: `GetById` повертає замовлення **разом із
рядками**, а окремого сховища для `OrderLine` немає.

### Дві реалізації

| | `InMemoryOrderStore` | `FileOrderStore` |
|---|---|---|
| Де дані | `Dictionary<string, Order>` | кеш у пам'яті + файл JSON |
| Скільки живуть | до виходу з програми | між запусками |
| Початкові дані | `IEnumerable<Order>? seed` у конструкторі (`SampleData.Orders()`, 20 зразків з тижня 7) | те, що є у файлі |
| Читання | зі словника | файл читається один раз, при першому зверненні |
| Запис | у словник | у кеш, потім `Flush()` — увесь файл після кожної зміни |
| Шлях | — | `AppContext.BaseDirectory/data/orders.json` |
| Формат | — | JSON-масив `OrderDto` з вкладеними `OrderLineDto`, `WriteIndented` |

Обидва словники порівнюють ключі через `StringComparer.OrdinalIgnoreCase`:
`Find("o-001")` і `Find("O-001")` знаходять те саме замовлення.

На диск ідуть DTO, а не сутності: `Order` має приватний конструктор і властивості
без `set`, серіалізувати його напряму незручно, а формат файлу не повинен
диктувати форму домену. Мапінг — `order.ToDto()` / `Order.FromDto(dto)` з
тижня 4. `FromDto` відтворює замовлення через `Create` → `AddLine` → `Confirm`,
тому пошкоджений файл не обходить інваріантів.

Фрагмент `data/orders.json` (кирилиця екранована як `Н…` — штатна поведінка
`System.Text.Json`, налаштуємо на тижні 9):

```json
[
  {
    "Id": "O-6B33F382",
    "CustomerId": "C-007",
    "CreatedOn": "2026-10-13T00:02:58.342071",
    "Status": "Confirmed",
    "Lines": [
      { "ProductId": "P-001", "ProductName": "Ноу...", "UnitPrice": 24999.00, "Quantity": 1 },
      ...
```

Якщо файл має старий формат, обірваний JSON або запис, що порушує інваріанти,
`FileOrderStore` загортає `JsonException` / `ArgumentException` /
`InvalidOperationException` у єдиний `InvalidDataException`, а `Cli` друкує:

```
Файл .../data/orders.json не вдалося прочитати: Замовлення O-6B33F382 порожнє: підтвердити можна лише замовлення з рядками
Видаліть .../data/orders.json і запустіть знову.
```

### Сервіс

```csharp
public sealed class OrderService(IOrderStore store, TimeProvider clock)
```

Обидва параметри первинного конструктора — абстракції: `IOrderStore` і
`TimeProvider` (вбудований у .NET 8 абстрактний «годинник»). Сервіс не містить
жодного `new …Store`, і слів «File» чи «Dictionary» в ньому немає.

| Операція | Що робить |
|---|---|
| `CreateOrder(customerId)` | id `O-` + 8 символів `Guid`, дата з `clock`; `Order.Create` → `store.Add` |
| `AddLine(orderId, product, qty)` | `Get` → `order.AddLine` → `store.Update` |
| `Confirm(orderId)` / `Cancel(orderId)` | `Get` → метод сутності → `store.Update` |
| `All()` / `Find(id)` | `store.List()` / `store.GetById(id)` |
| `Search(Func<Order, bool>)` | довільна умова (додаткове завдання 2) |

Розподіл перевірок без дублювання: інваріанти — у сутності (`Order.Create`,
`AddLine`, `Confirm`); унікальність id — у сховищі (`Add`); «запис не знайдено» —
у сервісі (`Get` через `?? throw`), бо для `Find` відсутність — нормальний `null`,
а для операції зміни — відмова.

### Схема залежностей

```
Cli (Program.cs) ──> OrderService ──> IOrderStore <── InMemoryOrderStore
       │                                   ^ ^─────── FileOrderStore
       │                                   └───────── CachingOrderStore ──> IOrderStore (будь-який)
       └── new … лише тут (composition root)
```

`Cli → OrderService → IOrderStore → InMemoryOrderStore | FileOrderStore`.
Стрілки від реалізацій ведуть **до** інтерфейсу: і сервіс, і сховища залежать від
абстракції (Dependency Inversion), а об'єкт сховища передається сервісу ззовні
через конструктор (Dependency Injection, вручну, без контейнера).

### Composition root

```csharp
bool useFile = args.Contains("--file", StringComparer.OrdinalIgnoreCase);
string dataPath = Path.Combine(AppContext.BaseDirectory, "data", "orders.json");

IOrderStore store = useFile
    ? new FileOrderStore(dataPath)
    : new InMemoryOrderStore(SampleData.Orders());
if (args.Contains("--cache", StringComparer.OrdinalIgnoreCase))
    store = new CachingOrderStore(store);
var service = new OrderService(store, TimeProvider.System);
```

Це єдине місце з конкретними класами: пошук `new [A-Za-z]*OrderStore` по `src`
знаходить лише ці рядки `Program.cs`. `Microsoft.Extensions.DependencyInjection`
не підключено — контейнер з'явиться на тижні 10 разом з Api.

### Вивід: пам'ять

```
Сховище: InMemoryOrderStore (у пам'яті)
Замовлень на старті: 20

=== Додати і змінити ===
CreateOrder("C-007") → O-77B58E7C
AddLine ×3, Confirm:
  O-77B58E7C клієнт C-007 від 2026-10-13 — підтверджене, рядків 2, сума 27697.50 грн
    P-001  Ноутбук Lenovo IdeaPad 15          1 ×  24999.00 =   24999.00 грн
    P-002  Миша бездротова Logitech M330      3 ×    899.50 =    2698.50 грн

=== Знайти за id ===
Find("o-77b58e7c") → O-77B58E7C клієнт C-007 від 2026-10-13 — підтверджене, рядків 2, сума 27697.50 грн
Find("O-404") → не знайдено (null — нормальний результат пошуку)

=== Список ===
  O-001 клієнт C-001 від 2026-09-01 — підтверджене, рядків 3, сума 27048.50 грн
  ...
  O-77B58E7C клієнт C-007 від 2026-10-13 — підтверджене, рядків 2, сума 27697.50 грн
Усього: 21
```

Повторний запуск знову починає з 20 зразків: створене замовлення зникло разом із процесом.

### Вивід: файл, два запуски поспіль

```
Сховище: FileOrderStore (.../src/Cli/bin/Debug/net10.0/data/orders.json)
Замовлень на старті: 0
...
Усього: 1
```

```
Сховище: FileOrderStore (.../src/Cli/bin/Debug/net10.0/data/orders.json)
Замовлень на старті: 1
...
=== Список ===
  O-6B33F382 клієнт C-007 від 2026-10-13 — підтверджене, рядків 2, сума 27697.50 грн
  O-04BAD002 клієнт C-007 від 2026-10-13 — підтверджене, рядків 2, сума 27697.50 грн
Усього: 2
```

Той самий сервіс, той самий сценарій — два різні режими роботи. Файл не
обнуляється: кожен запуск дописує одне замовлення.

### Сценарій відмови

```
=== Сценарій відмови ===
  Confirm("O-404"): помилка — Немає замовлення з id=O-404.
  AddLine("O-404", ...): помилка — Немає замовлення з id=O-404.
  AddLine у підтверджене замовлення: помилка — Замовлення O-77B58E7C має стан «підтверджене»: додавати рядки можна лише в чернетці
  AddLine з нульовою кількістю: помилка — Кількість товару P-002 у рядку має бути більшою за нуль (Parameter 'quantity') Actual value was 0.
  CreateOrder без клієнта: помилка — Замовлення O-91519A26: ідентифікатор клієнта обов'язковий (Parameter 'customerId')
  додати дубль id у сховище: помилка — Замовлення з id=O-77B58E7C уже існує.
Після відмов: O-77B58E7C клієнт C-007 від 2026-10-13 — підтверджене, рядків 2, сума 27697.50 грн
```

Винятки перехоплює `try/catch` у `Cli` (`OrderScenario.TryDo`) і друкує
повідомлення замість стека. Після всіх відмов замовлення не змінилося.

### Самоперевірка

| Перевірка | Результат |
|---|---|
| `grep -rn FileOrderStore src/Core` | лише `Storage/FileOrderStore.cs` |
| `grep -rn "Console\." src/Core` | порожньо |
| Два запуски з `--file` | 1, потім 2 замовлення — накопичення |
| Core без `FileOrderStore.cs` | `dotnet build src/Core/Core.csproj` — 0 помилок, `OrderService` не залежить від файлу |
| Тип параметра конструктора сервісу | `IOrderStore` — інтерфейс |
| `dotnet build` (net8.0 і net10.0) | 0 Warning(s), 0 Error(s) |

### Що зміниться на тижні 11 (БД)

З'явиться третя реалізація, наприклад `EfOrderStore(AppDbContext db) : IOrderStore`,
у якій `Add` / `Update` / `Remove` стануть операціями `DbSet<Order>` і
`SaveChanges()`, а `GetById` — запитом з `Include(o => o.Lines)`, бо агрегат має
завантажуватися з рядками. Знадобиться налаштування мапінгу (сутність з
приватними сетерами і полем `_lines`, або окремі entity-класи) і рядок
підключення. У composition root зміниться один вираз вибору реалізації.
`IOrderStore`, `OrderService`, `Order` і `OrderScenario` не змінюються.

### Додаткові завдання

**1. Декоратор `CachingOrderStore`.** Приймає будь-який `IOrderStore` у
конструкторі й реалізує той самий контракт. Читання (`List`, `GetById`, навіть
відсутній запис) кешуються, зміни спершу йдуть у внутрішнє сховище і лише після
успіху скидають кеш. Лічильники `Hits` / `Misses` показують ефект:

```
Сховище: CachingOrderStore → FileOrderStore (.../data/orders.json)
...
Кеш: влучань 6, промахів 8
```

Ні сервіс, ні сценарій про декоратор не знають — його додає один рядок у
composition root.

**2. Пошук з `Func<Order, bool>`.** `OrderService.Search(predicate)` фільтрує
`store.List()` за довільною умовою; сценарій шукає підтверджені замовлення на
суму понад 10 000 грн:

```csharp
service.Search(order => order.Status == OrderStatus.Confirmed && order.Total > 10000m);
```

## Доменні події (лабораторна 6)

```bash
dotnet run --project src/Cli -f net10.0 -- --events            # сховище в пам'яті
dotnet run --project src/Cli -f net10.0 -- --events --file     # те саме з FileOrderStore
cat src/Cli/bin/Debug/net10.0/logs/app.log                     # журнал подій
```

`OrderService` сповіщає зовнішній код про зміни подією `Changed`, але не знає,
хто і як ці сповіщення обробить. Зараз підписники — консоль і файл
`logs/app.log` у `Cli`; на тижні 10 підпишеться логер проєкту Api, на тижні 12 —
компонент Blazor, що оновлює таблицю замовлень. Core для цього не змінюється.

| Роль | Тип | Де |
|---|---|---|
| Вид зміни | `enum OrderChangeKind { Created, LineAdded, Confirmed, Cancelled }` | `src/Core/Events/OrderChangeKind.cs` |
| Дані події | `OrderChangedEventArgs : EventArgs` | `src/Core/Events/OrderChangedEventArgs.cs` |
| Видавець | `OrderService.Changed` + `OnChanged` | `src/Core/Services/OrderService.cs` |
| Підписники, відписка, журнал | `EventScenario`, `PrepareLogPath` | `src/Cli/EventScenario.cs`, `src/Cli/Program.cs` |

### Які зміни — події

Подією є кожна успішна операція, що змінює замовлення: створення, додавання
рядка, підтвердження, скасування. Саме вони змінюють те, що бачить користувач
(суму, кількість рядків, стан). Читання (`Find`, `All`, `Search`) подій не дає.
`OrderChangedEventArgs` несе `OrderId`, `Kind`, `LinesCount`, `Total` (нова
сума), `Delta` (на скільки змінилася сума) і `At`. `At` береться з
`TimeProvider` сервісу, а не з `DateTimeOffset.Now`, щоб тести тижня 8 могли
перевірити й час.

### Оголошення і підняття

```csharp
public event EventHandler<OrderChangedEventArgs>? Changed;

private void OnChanged(Order order, OrderChangeKind kind, decimal delta) =>
    Changed?.Invoke(this, new OrderChangedEventArgs(
        order.Id, kind, order.Lines.Count, order.Total, delta, _clock.GetLocalNow()));

public Order AddLine(string orderId, Product product, int quantity)
{
    Order order = Get(orderId);
    decimal totalBefore = order.Total;
    order.AddLine(product, quantity);   // якщо інваріант кине виняток — події не буде
    _store.Update(order);
    OnChanged(order, OrderChangeKind.LineAdded, order.Total - totalBefore);
    return order;
}
```

`OnChanged` стоїть останнім рядком перед `return`: після інваріантів сутності
і після запису в сховище. Якщо впав будь-який із цих кроків, сповіщення немає.

### Підписники

```csharp
void OnOrderChanged(object? sender, OrderChangedEventArgs e)
{
    string line = ChangeLog.Format(e);   // "{e.At:HH:mm:ss} {e.Kind,-9} {e.OrderId} {e.Delta,10:+0.00;-0.00} → сума …"
    Console.WriteLine(line);
    ChangeLog.TryAppend(logPath, line);  // помилку диска перехоплено, операція сервісу не ламається
}

service.Changed += OnOrderChanged;          // 1: журнал (локальна функція)
service.Changed += (_, _) => changes++;     // 2: лічильник (лямбда із замиканням)
```

Шлях журналу будує `Program.PrepareLogPath("app.log")`:
`Path.Combine(AppContext.BaseDirectory, "logs", "app.log")` і
`Directory.CreateDirectory(...)`. Кожен запуск дописує рядок `--- запуск … ---`.

### Вивід

```
Сховище: InMemoryOrderStore (у пам'яті)
Журнал: .../src/Cli/bin/Debug/net10.0/logs/app.log

=== Підписка: журнал (локальна функція), потім лічильник (лямбда) ===
00:11:37 Created   O-3FD12257      +0.00 → сума 0.00 грн, рядків 0
00:11:37 LineAdded O-3FD12257  +24999.00 → сума 24999.00 грн, рядків 1
00:11:37 LineAdded O-3FD12257   +1799.00 → сума 26798.00 грн, рядків 2
Подій отримано: 3
Порядок: журнал під час останньої події бачив лічильник = 2, лямбда збільшила його вже після журналу

=== Сценарій відмови: виняток є, рядка події немає ===
  AddLine з нульовою кількістю: помилка — Кількість товару P-002 у рядку має бути більшою за нуль (Parameter 'quantity') Actual value was 0.
  AddLine("O-404", ...): помилка — Немає замовлення з id=O-404.
  Confirm("O-404"): помилка — Немає замовлення з id=O-404.
Подій отримано: 3 (нових після відмов: 0)

=== Відписка: service.Changed -= OnOrderChanged ===
Подій отримано: 4 (+1 лише від лямбди-лічильника)
Дані змінилися: O-3FD12257 клієнт C-012 від 2026-10-20 — підтверджене, рядків 2, сума 26798.00 грн
Після «-= (_, _) => changes++» і Cancel: 5 — лічильник досі підписаний

=== Func замість події: Search(order => order.Total < 2000) ===
  O-007 клієнт C-002 від 2026-09-25 — чернетка, рядків 1, сума 1799.00 грн
  O-010 клієнт C-004 від 2026-10-07 — чернетка, рядків 0, сума 0.00 грн
  O-015 клієнт C-003 від 2026-10-27 — скасоване, рядків 0, сума 0.00 грн
  O-018 клієнт C-005 від 2026-11-08 — чернетка, рядків 1, сума 249.90 грн
  O-020 клієнт C-006 від 2026-11-16 — скасоване, рядків 1, сума 899.50 грн
Знайдено: 5
```

`logs/app.log` після запуску:

```
--- запуск 2026-10-20 00:11:37 ---
00:11:37 Created   O-3FD12257      +0.00 → сума 0.00 грн, рядків 0
00:11:37 LineAdded O-3FD12257  +24999.00 → сума 24999.00 грн, рядків 1
00:11:37 LineAdded O-3FD12257   +1799.00 → сума 26798.00 грн, рядків 2
```

Підтвердження і скасування після `-=` у журнал не потрапили, хоча стан
замовлення змінився. Рядок `-= (_, _) => changes++` підписку не знімає: нова
лямбда є іншим екземпляром делегата, тому `Cancel` знову збільшив лічильник.

### Подія чи прямий виклик логера

Якби сервіс сам викликав логер, Core залежав би від конкретного способу виводу:
кожен новий одержувач (логер Api, таблиця Blazor) вимагав би правки й
перекомпіляції `OrderService`. Подія перевертає залежність: Core лише каже
«замовлення змінилося», а `Cli`, Api чи Blazor підписуються самі. Друге:
у тесті достатньо підписатися лямбдою і перевірити отримані `OrderChangedEventArgs`,
без підміни консолі чи читання файлу. Ціна — обробники викликаються синхронно,
по черзі, і виняток в одному з них зупинить наступні та дійде до того, хто
викликав операцію сервісу.

### Самоперевірка

| Перевірка | Результат |
|---|---|
| `grep -rn "Console\|logs" src/Core` | порожньо |
| Кожна успішна операція | рівно один рядок журналу / +1 до лічильника |
| Операція з порушеним інваріантом або неіснуючим id | помилка є, рядка події немає, лічильник не змінився |
| Після `-= OnOrderChanged` | журнал мовчить, дані змінюються |
| `Changed` | `public event EventHandler<OrderChangedEventArgs>?`, не поле делегата |
| `Func` у сигнатурі | `OrderService.Search(Func<Order, bool> predicate)` |
| `dotnet build` (net8.0 і net10.0) | 0 Warning(s), 0 Error(s) |

### Додаткові завдання

**1. `INotificationSink` замість події.** Контракт `Core/Abstractions/INotificationSink.cs`
з одним методом `void Notify(OrderChangedEventArgs change)`; реалізації
`ConsoleSink` і `FileSink(path)` лежать у `Cli`, бо Core не знає про консоль і
файли. Cli передає одержувача сервісу через конструктор — ручний DI, як сховище
на тижні 5:

```csharp
public sealed class OrderService(IOrderStore store, TimeProvider clock, INotificationSink? sink = null)

INotificationSink? sink = (sinkToFile, sinkToConsole) switch
{
    (true, _) => new FileSink(PrepareLogPath("sink.log")),
    (_, true) => new ConsoleSink(),
    _ => null
};
var service = new OrderService(store, TimeProvider.System, sink);
```

```bash
dotnet run --project src/Cli -f net10.0 -- --sink-console    # сповіщення в консоль
dotnet run --project src/Cli -f net10.0 -- --sink-file       # сповіщення в logs/sink.log
```

```
Одержувач сповіщень (через конструктор): ConsoleSink (консоль)

=== Операції ===
12:29:15 Created   O-94FCD2BD      +0.00 → сума 0.00 грн, рядків 0
12:29:15 LineAdded O-94FCD2BD  +24999.00 → сума 24999.00 грн, рядків 1
12:29:15 LineAdded O-94FCD2BD   +1799.00 → сума 26798.00 грн, рядків 2
12:29:15 Confirmed O-94FCD2BD      +0.00 → сума 26798.00 грн, рядків 2

=== Сценарій відмови: виняток є, сповіщення немає ===
  AddLine у підтверджене замовлення: помилка — Замовлення O-94FCD2BD має стан «підтверджене»: додавати рядки можна лише в чернетці
```

| | Подія `Changed` | `INotificationSink` через конструктор |
|---|---|---|
| Скільки одержувачів | 0…N | один (або `null`) |
| Коли задається | будь-коли, `+=` / `-=` | один раз, під час створення сервісу |
| Відписка | `-=` | немає; лише новий сервіс |
| Залежність сервісу | лише від `EventArgs` | від інтерфейсу `INotificationSink` |
| Видно з конструктора | ні | так — залежність явна |
| Тест | підписатися лямбдою | передати фейковий sink |
| Підходить для | сповіщень «кому цікаво» (UI, лог) | обов'язкової дії, без якої операція неповна |

Обидва варіанти зберігають головне: Core не знає, куди йде повідомлення.
Подія гнучкіша для випадкових підписників (таблиця Blazor з'являється і зникає
разом зі сторінкою), sink — для одного обов'язкового одержувача, якого
DI-контейнер тижня 10 підставить сам.

**2. `Action<string>` у методі імпорту.** `ProductCsvImporter.Load(path, Action<string>? onSkipped = null)`
і так само `ProductJsonImporter.Load` викликають `onSkipped` для кожного
пропущеного рядка. Core лише повідомляє; Cli передає лямбду, що пише звіт у
`logs/import-skipped.log`:

```csharp
Action<string> reportSkipped = reason => ChangeLog.TryAppend(skippedReportPath, reason);
ImportResult<ProductDto> result = ProductCsvImporter.Load(path, reportSkipped);
```

```
--- імпорт .../data/sample.csv 2026-10-20 12:29:19 ---
рядок 12: очікую 3 колонки, отримав 2
рядок 13: ціна 'безкоштовно' не є невід'ємним числом
рядок 14: ідентифікатор або назва порожні
```

Тут `Action`, а не `Func`: від викликача відповідь не потрібна, і не подія:
одержувач потрібен лише на час одного виклику `Load`, підписуватися назавжди
немає на що (метод статичний).

**3. Чому async-обробник події — це `async void` і чим це небезпечно.**
Делегат `EventHandler<T>` повертає `void`, тому асинхронний обробник можна
записати лише як `service.Changed += async (_, e) => await SaveAsync(e);` — це
`async void`. Наслідки:

- `Changed?.Invoke` не чекає завершення: керування повертається сервісу на
  першому `await`, і операція вважається завершеною, хоча запис ще триває;
  порядок обробників перестає бути гарантованим.
- Винятку після `await` нікуди повернутися: `Task` немає, `try/catch` навколо
  операції сервісу його не зловить. Виняток іде в `SynchronizationContext`, а в
  консолі — у пул потоків, і це аварійно завершує процес.
- Тест не може дочекатися обробника: `await` на `async void` неможливий.

Тому в async-обробнику весь код загортають у `try/catch`, а коли результат
справді треба дочекатися, замість події беруть `Func<OrderChangedEventArgs, Task>`
або асинхронний інтерфейс на кшталт `Task NotifyAsync(...)` (детально — тиждень 8).

**Безпечний підписник.** Обробник викликається синхронно всередині операції
сервісу, тож виняток у ньому зламав би вже успішну операцію і зупинив би
наступних підписників. Тому запис у файл іде через `ChangeLog.TryAppend`, що
перехоплює `IOException` / `UnauthorizedAccessException`. Перевірка: файл
`logs/app.log` лише для читання (`chmod 444`) — програма друкує
`! не вдалося записати в …/app.log: Access … is denied.`, а сценарій
доходить до кінця з кодом виходу 0.

## Звіти через LINQ (лабораторна 7)

```bash
dotnet run --project src/Cli -f net10.0 -- report-top          # топ-5 клієнтів за сумою
dotnet run --project src/Cli -f net10.0 -- report-expensive    # замовлення дорожчі за 10 000 грн
dotnet run --project src/Cli -f net10.0 -- report-groups       # групування за станом
dotnet run --project src/Cli -f net10.0 -- report-months       # виручка за місяцями
dotnet run --project src/Cli -f net10.0 -- report-sum          # кількість, виручка, середній чек, максимум
dotnet run --project src/Cli -f net10.0 -- report-deferred     # демо відкладеного виконання
dotnet run --project src/Cli -f net10.0 -- report-groups --file    # ті самі звіти над FileOrderStore
dotnet run --project src/Cli -f net10.0 -- report-sum --empty      # порожнє сховище — без винятку
dotnet run --project src/Cli -f net10.0 -- report                  # перелік підкоманд
```

Варіант «Замовлення»: топ-5 клієнтів за сумою, замовлення дорожчі за N,
групування за станом і за місяцем, середній чек і максимальне замовлення.

| Роль | Тип | Де |
|---|---|---|
| Рядок звіту про замовлення | `record OrderRow(Id, CustomerId, CreatedOn, Status, LinesCount, Total)` | `src/Core/Reports/OrderRow.cs` |
| Підсумок групи | `record GroupSummary(Key, Count, Total)` | `src/Core/Reports/GroupSummary.cs` |
| Підсумок клієнта / товару | `record CustomerTotal`, `record ProductSales` | `src/Core/Reports/` |
| Усі запити | `static class ReportService` | `src/Core/Reports/ReportService.cs` |
| Підкоманди і вивід | `ReportCommands`, `DeferredScenario`, `BenchmarkScenario` | `src/Cli/` |

Звіти лежать у Core і приймають `IEnumerable<Order>`, а не сховище: це чисті
функції, які однаково працюють над `InMemoryOrderStore`, `FileOrderStore` чи
100 000 згенерованих записів. Повертають `IReadOnlyList<DTO>` — уже готовий
список, тому повторний `Count` у Cli нічого не перераховує. Звіти не знають про
`Console`, а в `Program.cs` немає жодного `Where` чи `GroupBy`: він лише
передає підкоманду в `ReportCommands`.

### Перелік запитів

| Метод | Питання | Оператори |
|---|---|---|
| `ExpensiveOrders(source, threshold)` | Які замовлення дорожчі за N? | `Where`, `OrderByDescending`, `ThenBy`, `Select` → `OrderRow` |
| `TopCustomers(source, n)` | Які клієнти принесли найбільше грошей? | `Where`, `GroupBy`, `Count`, `Sum`, `TopBy` |
| `TopOrders(source, n)` | Які замовлення найбільші? | `TopBy` (`OrderByDescending` + `ThenBy` + `Take`), `Select` |
| `ByStatus(source)` | Скільки замовлень і на яку суму в кожному стані? | `GroupBy`, `Select` → `GroupSummary`, `OrderByDescending`, `ThenBy` |
| `ByMonth(source)` | Яка виручка за кожен місяць? | `Where`, `GroupBy` за кортежем `(Year, Month)`, `OrderBy`, `Select` |
| `TopProducts(source, n)` | Які товари продаються на найбільшу суму? | `Where`, `SelectMany`, `GroupBy`, `First`, `Sum`, `TopBy` |
| `Totals(source)` | Скільки підтверджених, виручка, середній чек, найбільше замовлення? | `Where`, `Select`, `ToList`, `Sum`, `Average`, `Max` |
| `ByStatusQuery(source)` | те саме, що `ByStatus` (додатково 3) | синтаксис запитів `from … group … into … orderby … select` |
| `CustomerActivity(customers, orders)` | Скільки замовлень у кожного клієнта довідника, включно з тими, хто нічого не замовив? (додатково 1) | `GroupJoin`, `Count`, `Where`, `Sum`, `OrderByDescending`, `ThenBy` |
| `OrdersWithCustomers(customers, orders, threshold)` | Чиї замовлення дорожчі за N — з іменами? (додатково 1) | `Where`, `Join`, `OrderByDescending`, `ThenBy` |

Гроші приносять лише підтверджені замовлення, тому `TopCustomers`, `ByMonth`,
`TopProducts` і `Totals` спершу відбирають `Status == Confirmed`. Фільтр стоїть
**перед** групуванням і сортуванням: сортувати й групувати зайві елементи
дорожче (виміряно нижче).

### GroupBy з проєкцією

```csharp
public static IReadOnlyList<GroupSummary> ByStatus(IEnumerable<Order> source) =>
    source.GroupBy(order => order.Status.ToString())
          .Select(g => new GroupSummary(g.Key, g.Count(), g.Sum(order => order.Total)))
          .OrderByDescending(summary => summary.Total).ThenBy(summary => summary.Key, StringComparer.Ordinal)
          .ToList();
```

`GroupBy` розкладає замовлення на групи за значенням ключа (рядком стану, а не
посиланням на об'єкт). У `Select` приходить уже група: `g.Key` — значення ключа
(`"Confirmed"`), `g.Count()` — кількість замовлень у групі, `g.Sum(...)` — сума
полів її елементів. Після `Select` кожен елемент — `GroupSummary`, і
`OrderByDescending` сортує готові підсумки. `ByMonth` групує за кортежем
`(order.CreatedOn.Year, order.CreatedOn.Month)` — кортежі порівнюються за
значенням, тому всі вересневі замовлення потрапляють в одну групу.

### Чому DTO, а не Order

`OrderRow` фіксує форму рядка звіту: шість полів, стан рядком, як в `OrderDto`.
`Order` має поведінку (`AddLine`, `Confirm`), список рядків і обчислювану суму —
віддавши його назовні, звіт дозволив би викликачу змінити замовлення і прив'язав
би відповідь Api (тиждень 10) до внутрішньої будови сутності. Проєкція описана
в одному місці, `ReportService.ToRow`: зміниться сутність — зміниться лише цей метод.

### Топ-N зі стабільним порядком

```csharp
source.OrderByDescending(keySelector).ThenBy(tieBreaker, StringComparer.Ordinal).Take(n);
```

Другий ключ — унікальний id, порівняний `Ordinal`, тому порядок не залежить ні
від порядку елементів у словнику сховища, ні від мови ОС. Два запуски
`report-top` дають побайтно однаковий вивід (перевірено порівнянням рядків).

### Вивід

```
> report-top
Топ-5 клієнтів за сумою підтверджених замовлень:
  C-006  замовлень:   2   сума:   63127.78 грн
  C-001  замовлень:   3   сума:   51759.80 грн
  C-002  замовлень:   3   сума:   46688.90 грн
  C-007  замовлень:   1   сума:   27248.00 грн
  C-003  замовлень:   1   сума:   11748.90 грн

> report-orders
Топ-5 замовлень за сумою:
  O-008  C-006  2026-09-29 Confirmed  рядків  2   56397.98 грн
  O-019  C-002  2026-11-12 Confirmed  рядків  2   32889.90 грн
  O-017  C-007  2026-11-04 Confirmed  рядків  2   27248.00 грн
  O-001  C-001  2026-09-01 Confirmed  рядків  3   27048.50 грн
  O-004  C-001  2026-09-13 Confirmed  рядків  2   19561.80 грн

> report-expensive
Замовлення дорожчі за 10000 грн:
  O-008  C-006  2026-09-29 Confirmed  рядків  2   56397.98 грн
  O-019  C-002  2026-11-12 Confirmed  рядків  2   32889.90 грн
  O-017  C-007  2026-11-04 Confirmed  рядків  2   27248.00 грн
  O-001  C-001  2026-09-01 Confirmed  рядків  3   27048.50 грн
  O-004  C-001  2026-09-13 Confirmed  рядків  2   19561.80 грн
  O-009  C-003  2026-10-03 Confirmed  рядків  2   11748.90 грн
  O-002  C-002  2026-09-05 Confirmed  рядків  1   11499.00 грн

> report-groups
Замовлення за станом:
  Confirmed  замовлень:  12   сумарно:  207222.97 грн
  Cancelled  замовлень:   4   сумарно:   11990.39 грн
  Draft      замовлень:   4   сумарно:    6798.70 грн

> report-months
Виручка за місяцями (підтверджені замовлення):
  2026-09    замовлень:   5   сумарно:  117706.98 грн
  2026-10    замовлень:   5   сумарно:   29378.09 грн
  2026-11    замовлень:   2   сумарно:   60137.90 грн

> report-products
Топ-5 товарів за виручкою:
  P-001  Ноутбук Lenovo IdeaPad 15           4 шт   99996.00 грн
  P-006  Навушники Sony WH-1000XM4           4 шт   45996.00 грн
  P-004  Монітор Dell P2422H 24"             3 шт   23672.70 грн
  P-005  SSD Samsung 980 1TB                 3 шт    9599.97 грн
  P-003  Клавіатура механічна Keychron K2    2 шт    8500.00 грн

> report-sum
  підтверджених 12, виручка 207222.97 грн, середній чек 17268.58 грн, макс 56397.98 грн
```

Перед кожним звітом друкується рядок `Сховище: …` — видно, над яким джерелом
працює запит. Невідома підкоманда друкує перелік і завершується з кодом 1.

### Порожнє джерело

`Average` і `Max` для `decimal` на порожній послідовності кидають
`InvalidOperationException`, `Sum` і `Count` повертають 0. `Totals` перевіряє
`totals.Count == 0` і повертає кортеж нулів, не доходячи до `Average`:

```
> report-sum --empty
  підтверджених 0, виручка 0.00 грн, середній чек 0.00 грн, макс 0.00 грн
> report-expensive --empty
Замовлення дорожчі за 10000 грн:
  (порожньо)
```

Прапорець `--empty` змінює лише composition root:
`new InMemoryOrderStore(empty ? [] : SampleData.Orders())`. Без винятку
відпрацьовують усі підкоманди.

### Відкладене виконання

```
> report-deferred
Запит побудовано, умову перевірено разів: 0
Дорожчих за 10000 до змін: 7 (перевірок: 20)
Дорожчих за 10000 після source.Add: 8 (перевірок: 41)
Після source.Clear(): query.Count() = 0, у знімку лишилось: 8
snapshot.Count — властивість: перевірок після ToList не додалося (0)
```

Рядок `var query = source.Where(...)` нічого не виконує: лічильник у лямбді
після нього дорівнює 0. Запит виконується лише тоді, коли його перебирають, —
тут це кожен виклик `query.Count()` і `ToList()`. Перший `Count()` пройшов 20
замовлень і знайшов 7; після `source.Add` другий `Count()` виконав той самий
запит **заново** над 21 елементом (перевірок стало 41) і побачив нове
замовлення O-999 — тому 7 і 8. `ToList()` зафіксував 8 елементів в окремому
списку: після `source.Clear()` запит дає 0, а `snapshot.Count` — властивість,
що читає збережене число, — так і лишається 8. У `ReportService` кожен метод
закінчується `ToList` (або агрегатом), тому звіт виконується рівно один раз, у
момент виклику, і Cli отримує вже знімок.

### Самоперевірка

| Перевірка | Результат |
|---|---|
| `grep -nE "\.Where\|GroupBy" src/Cli/Program.cs` | порожньо: шляхи і підкоманда — через `FirstOrDefault` |
| Два запуски `report-top` | вивід однаковий |
| `report-expensive --empty`, `report-sum --empty` | `(порожньо)` і нулі, код виходу 0 |
| Типи результатів | `IReadOnlyList<OrderRow / CustomerTotal / GroupSummary / ProductSales / …>`, кортеж у `Totals` — жодного `Order` |
| `grep -rn AsParallel src` | порожньо |
| `grep -rn "Console\." src/Core` | порожньо |
| Попередні режими (`--events`, `--domain`, `--import`, `--file`) | працюють; `report-*` теж приймає `--file` |
| `dotnet build` (net8.0 і net10.0) | 0 Warning(s), 0 Error(s) |

### Додаткові завдання

**1. Join і GroupJoin.** `SampleData.Customers()` — довідник восьми клієнтів
(`CustomerDto` тижня 3); C-008 навмисно без замовлень.

```csharp
customers.GroupJoin(orders,
    customer => customer.Id,
    order => order.CustomerId,
    (customer, customerOrders) => new CustomerActivity(customer.Id, customer.Name,
        customerOrders.Count(), customerOrders.Where(IsConfirmed).Sum(order => order.Total)),
    StringComparer.OrdinalIgnoreCase)
```

```
> report-customers
Клієнти довідника та їхні замовлення (GroupJoin):
  C-006  Софія Ткаченко   замовлень:   3   виручка:   63127.78 грн
  C-001  Іван Петренко    замовлень:   3   виручка:   51759.80 грн
  ...
  C-005  Тарас Мельник    замовлень:   3   виручка:    3199.70 грн
  C-008  Ірина Савчук     замовлень:   0   виручка:       0.00 грн

> report-join
Замовлення дорожчі за 10000 грн з іменами клієнтів (Join):
  Софія Ткаченко   O-008  Confirmed    56397.98 грн
  Олена Коваль     O-019  Confirmed    32889.90 грн
  ...
```

`GroupJoin` віддає кожному клієнту групу його замовлень, і клієнт без
замовлень теж лишається (порожня група → 0, як `LEFT JOIN`). `Join` — внутрішнє
з'єднання: пара утворюється лише за збігу ключів, тож C-008 у `report-join`
потрапити не може. Сортування імен свідомо не використано: `Ordinal` ставить
`І` (U+0406) перед `А` (U+0410), а культурне порівняння залежало б від ОС.

**2. Метод-розширення `TopBy`.** `src/Core/Reports/EnumerableExtensions.cs`:

```csharp
public static IEnumerable<T> TopBy<T, TKey>(this IEnumerable<T> source,
    Func<T, TKey> keySelector, Func<T, string> tieBreaker, int n)
```

Використано в трьох звітах: `TopCustomers`, `TopOrders`, `TopProducts`. Другий
ключ зроблено обов'язковим параметром, тому забути `ThenBy` у новому топ-N
неможливо. Метод не містить `yield`, тому `ArgumentOutOfRangeException` на
від'ємне `n` виникає одразу під час виклику, а сам запит лишається відкладеним
до `ToList`.

**3. Синтаксис запитів.** `ReportService.ByStatusQuery`:

```csharp
from order in source
group order by order.Status.ToString() into g
let total = g.Sum(order => order.Total)
orderby total descending, g.Key
select new GroupSummary(g.Key, g.Count(), total)
```

`report-query` друкує обидва варіанти і `Результати збігаються: True` —
record порівнюються `SequenceEqual` за значенням. Порівняння читабельності:

| | Методи | Запити |
|---|---|---|
| Групування з підсумком | ключ і агрегати в одному `Select(g => …)` | `group … by … into g` читається як речення, `let` дає ім'я сумі, і `g.Sum` рахується один раз для сортування і для результату |
| Кілька ключів сортування | `OrderByDescending(...).ThenBy(...)` | `orderby total descending, g.Key` — коротше |
| Компаратор (`StringComparer.Ordinal`) | передається другим аргументом | **передати неможливо**: `orderby g.Key` бере стандартне порівняння рядків з урахуванням культури |
| `Take`, `First`, `ToList`, `TopBy` | частина ланцюжка | лише через дужки й виклик методу після запиту |

Для звіту з групуванням у центрі синтаксис запитів справді читається легше. Для
`TopCustomers`, `ExpensiveOrders` і всього з `Take`/`TopBy` ланцюжок методів
природніший, а неможливість задати компаратор — вагома причина лишити в
основному коді методи. Ключі станів — латиниця, тому тут результати збігаються.

**4. Stopwatch на 100 000 записів.** `SampleData.Generate(100_000)` створює
замовлення через ті самі `Order.Create` / `AddLine` / `Confirm` з фіксованим
зерном `Random`. `BenchmarkScenario` для кожного варіанта робить 5 прогрівних
запусків, перед кожним із 10 вимірів викликає `GC.Collect()` і друкує
найкращий час. Варіанти — у `src/Core/Reports/ReportVariants.cs`.

```bash
dotnet run --project src/Cli -c Release -f net10.0 -- report-bench
```

```
Згенеровано замовлень: 100000 за 102 мс
Звіт «дорожчі за 30000 грн», найкращий час з 10 запусків:
  Where → OrderBy → Select → ToList                20.44 мс   записів: 36563
  Where → ToList → OrderBy → Select → ToList       16.81 мс   записів: 36563
  OrderBy → Where → Select → ToList (помилка)      48.48 мс   записів: 36563
  результати однакові: True
Агрегати (кількість, сума, середнє, максимум):
  один ToList, потім 4 агрегати                     4.00 мс   записів: 69945
  4 агрегати по відкладеному запиту                12.18 мс   записів: 69945
  результати однакові: True
```

| Порівняння | Час, мс (3 запуски) | Висновок |
|---|---|---|
| без `ToList` у середині | 20.4 – 25.7 | еталон |
| з `ToList` після `Where` | 16.8 – 17.4 | на ~18 % **швидше** |
| `OrderBy` до `Where` | 48.2 – 50.2 | у ~2,4 раза повільніше |
| 4 агрегати по відкладеному запиту | 12.1 – 12.3 | у ~3 рази повільніше за один `ToList` |

- `OrderBy` до `Where` сортує всі 100 000 елементів замість 36 563 і рахує
  `Order.Total` для кожного — підтверджено, що фільтрувати треба спочатку.
- Відкладений запит у `TotalsDeferred` проходиться чотири рази (`Any` — лише до
  першого елемента), і кожного разу заново фільтрує й перераховує `Order.Total`
  з рядків. Один `ToList` у `Totals` — один прохід.
- Проміжний `ToList` після `Where` на цих даних не шкодить, а трохи пришвидшує.
  Ймовірна причина: `OrderBy` над `List<T>` знає кількість елементів, тому і його
  буфер, і підсумковий `ToList` після `Select` виділяються одразу потрібного
  розміру, а над ітератором `Where` довжина невідома і масиви ростуть подвоєнням.
  Ціна — ще один список на 36 563 посилання в пам'яті. Тобто «зайвий» `ToList`
  перед буферизувальним оператором (`OrderBy`, `GroupBy`) майже нічого не
  коштує, бо той однаково збирає всю послідовність.
- Перше вимірювання без прогріву давало протилежний результат: повільнішим
  виходив той варіант, що запускався першим, хоч би яким він був (перевірено
  перестановкою). Причина — багаторівнева JIT-компіляція і збирання сміття,
  тому прогрів і `GC.Collect()` перед виміром обов'язкові.

### Що зміниться на тижні 11 (IQueryable)

Над `IQueryable<Order>` з EF Core ті самі `Where`, `GroupBy`, `OrderBy`, `Select`
провайдер перекладе на SQL (`WHERE`, `GROUP BY`, `ORDER BY`), і лямбди стануть
деревами виразів, а не делегатами. Тому в них не можна буде викликати
довільні методи C#: `Order.Total` (сума з рядків у пам'яті), `IsConfirmed`,
`DateOnly.ToString("yyyy-MM")` і `StringComparer` у `GroupBy` SQL-провайдер
не перекладе — їх доведеться замінити на вирази над колонками (`Sum` по
`Lines`, `Year`/`Month`) або виконати після `ToList`/`AsEnumerable`. `ToList`
наприкінці лишиться точкою, де відбувається запит до бази.

## Асинхронний імпорт, скасування, доменні винятки, тести (лабораторна 8)

```bash
dotnet test                                                            # 28 тестів, лише Core
dotnet run --project src/Cli -f net10.0 -- import-orders               # data/orders-import.json → 17 записів
dotnet run --project src/Cli -f net10.0 -- import-orders --repeat      # + повторний імпорт: 0 (ідемпотентність)
dotnet run --project src/Cli -f net10.0 -- import-orders data/orders-broken.json   # «Дані відхилено: Запис 3: …»
dotnet run --project src/Cli -f net10.0 -- import-orders nope.json     # «Файл не знайдено: …»
dotnet run --project src/Cli -f net10.0 -- import-orders --stream      # потоковий імпорт (IAsyncEnumerable)
```

### Як перевірити скасування

```bash
# таймаут: 600 000 замовлень (~260 МБ, файл створюється один раз у тимчасовому каталозі ОС)
dotnet run --project src/Cli -f net10.0 -- import-orders --big=600000 --timeout=2000
# Ctrl+C: запустити і натиснути Ctrl+C, поки малюється смуга «Читання файлу»
dotnet run --project src/Cli -f net10.0 -- import-orders --big=600000
```

Обидва сценарії завершуються повідомленням, а не стеком виклику: таймаут —
«Імпорт скасовано: спрацював таймаут. Операція не завершилася за 2 с.», Ctrl+C —
«Імпорт скасовано.» (код виходу 2). Значення прапорців пишуться через `=`
(`--timeout=2000`), щоб число не стало позиційним аргументом — шляхом до файлу.

### Що стало асинхронним, а що — ні

| Операція | Як | Чому |
|---|---|---|
| Читання файлу імпорту | `FileStream(..., useAsync: true)` + `JsonSerializer.DeserializeAsync` / `DeserializeAsyncEnumerable` | справжнє введення-виведення: потік не блокується, поки диск віддає дані |
| `OrderService.ImportAsync` / `ImportStreamAsync` | `async Task<int>`, токен іде в `LoadAsync` і перевіряється в циклі | «асинхронність на всіх рівнях»: метод викликає асинхронний імпортер |
| `Program.cs` → `RunImportAsync` | top-level `await`, `Console.CancelKeyPress` | точка входу теж асинхронна — без `.Result` / `.Wait()` |
| `IOrderStore`, `InMemoryOrderStore` | синхронно | словник у пам'яті не чекає на диск; асинхронний контракт сховища з'явиться з EF Core (`ToListAsync`, `SaveChangesAsync`) на тижні 11 |
| `FileOrderStore`, CSV-імпортери тижня 3 | синхронно | зміна контракту `IOrderStore` зачепила б усі сценарії лаб 5–7; `Task.Run` навколо `File.ReadAllText` асинхронності не дав би |
| `ReportService` | синхронно | чисті обчислення над списком у пам'яті — чекати нема на що |

Фрагмент `JsonOrderImporter.LoadAsync`:

```csharp
ct.ThrowIfCancellationRequested();                       // скасований токен — файл навіть не відкривається
await using FileStream stream = new(path, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize, useAsync: true);
dtos = await JsonSerializer.DeserializeAsync<List<OrderDto?>>(Track(stream), Options, ct)
       ?? throw new DomainException($"Файл {sourceName} не містить списку замовлень.");
for (int i = 0; i < dtos.Count; i++)
{
    ct.ThrowIfCancellationRequested();
    result.Add(ToOrder(dtos[i], i + 1));                 // ArgumentException → DomainException("Запис N: …", inner)
}
```

Ланцюжок токена: `Program.cs` (`CancellationTokenSource` + Ctrl+C) →
`ImportCommand` → `OrderService.WithTimeoutAsync` (зв'язаний токен) →
`OrderService.ImportAsync` → `JsonOrderImporter.LoadAsync` → `DeserializeAsync`.
Перевірка `ThrowIfCancellationRequested` на початку `LoadAsync` важлива: без неї
тест із заздалегідь скасованим токеном і неіснуючим `any.json` отримав би
`FileNotFoundException`, а не скасування.

Обробка в Cli — порядок `catch` має значення:

```csharp
catch (OperationCanceledException) { Console.WriteLine("Імпорт скасовано."); return 2; }
catch (TimeoutException ex)        { Console.WriteLine($"Імпорт скасовано: спрацював таймаут. {ex.Message}"); return 2; }
catch (DomainException ex)         { Console.WriteLine($"Дані відхилено: {ex.Message}"); return 1; }
catch (FileNotFoundException ex)   { Console.WriteLine($"Файл не знайдено: {ex.FileName}"); return 1; }
catch (IOException ex)             { Console.WriteLine($"Помилка введення-виведення: {ex.Message}"); return 1; }
```

### DomainException чи ArgumentException

`Core/DomainException.cs` — порушення **бізнес-правила**: дія недопустима в
поточному стані предметної області, хоча аргументи самі по собі коректні.
`ArgumentException` (`ArgumentOutOfRangeException`) — некоректний **аргумент**
методу, помилка того, хто викликає.

| Ситуація | Виняток |
|---|---|
| `AddLine` у підтверджене замовлення | `DomainException`: «Замовлення O-001 вже підтверджено: додавати рядки можна лише в чернетці» |
| Повторне `Confirm` | `DomainException`: «Замовлення O-001 вже підтверджено» |
| `Confirm` порожнього, перехід «скасоване → підтверджене» | `DomainException` |
| `Confirm("O-404")`, `AddLine("O-404", …)` у сервісі | `DomainException`: «Немає замовлення з id=O-404.» |
| Запис файлу імпорту з порушеним інваріантом, файл — не JSON-масив | `DomainException("Запис 3: …", inner)` — початковий виняток в `InnerException` |
| `AddLine(…, quantity: 0)`, від'ємна ціна, порожній `customerId` | `ArgumentOutOfRangeException` / `ArgumentException` |
| Дубль id у сховищі | `InvalidOperationException` — контракт сховища, а не правило домену |
| Немає файлу, помилка диска | `FileNotFoundException` / `IOException` — технічні, не маскуються |
| Ctrl+C, таймаут | `OperationCanceledException` проходить наскрізь; таймаут → `TimeoutException` |

Під час імпорту `ArgumentException` із `Order.FromDto` стає `DomainException`
з номером запису: для `FromDto` це некоректний аргумент, але для імпорту — це
відхилені дані користувача. `JsonException` загортається так само, і
`InnerException` зберігає номер рядка і позицію помилки в JSON.

### Тести

`tests/Core.Tests` (xUnit, net10.0) посилається лише на `src/Core`. Сховище —
`InMemoryOrderStore`, час — `FakeTimeProvider`, імпорт — `FakeOrderImporter` або
`MemoryStream`; набір `TestData.Orders()` оголошено в проєкті тестів. Диск
використовує один тест — тимчасовий файл видаляється у `finally`.

| Тест | Що перевіряє | Очікуваний результат |
|---|---|---|
| `AddLine_ToConfirmedOrder_ThrowsDomainException_AndKeepsTotal` | інваріант варіанта «вже підтверджено» | `DomainException`, сума лишилася 10 000 |
| `AddLine_WithZeroQuantity_ThrowsArgumentOutOfRange_AndKeepsLines` | аргумент, а не бізнес-правило | `ArgumentOutOfRangeException`, рядків 0 |
| `Confirm_EmptyOrder_ThrowsDomainException` | порожнє замовлення не підтверджується | `DomainException` |
| `Confirm_MissingOrder_ThrowsDomainException` | операція над неіснуючим id | `DomainException` |
| `CreateOrder_RaisesChangedExactlyOnce` | подія тижня 6 | рівно одна подія `Created` з id замовлення |
| `FailedOperation_RaisesNoEvent` | відмова без події | 0 подій |
| `Total_AfterAddingLines_IsSumOfLineTotals` ×3 | сума після додавання рядків (варіант) | 10 000 / 15 000 / 27 500 |
| `ExpensiveOrders_CountsOnlyAboveThreshold` ×4 | звіт на кількох порогах | 4 / 3 / 1 / 0 |
| `TopOrders_IsDeterministic` | топ-N з однаковими сумами | `O-BIG`, `O-MID` (а не `O-TIE`) |
| `Totals_CountOnlyConfirmedOrders`, `Totals_OnEmptySource_ReturnsZeros` | агрегати | `(3, 20000, 6666.67, 10000)`, нулі |
| `ImportAsync_WhenAlreadyCancelled_Throws` | скасування до початку | `OperationCanceledException`, диск не зачеплено |
| `ImportAsync_FileWithInvalidRecord_ThrowsDomainException_WithRecordNumber` | тимчасовий файл із записом quantity = 0 | «Запис 2: …», `InnerException` — `ArgumentOutOfRangeException`, сховище порожнє |
| `LoadAsync_MalformedJson_WrapsJsonException` | обірваний JSON | `DomainException` з `JsonException` усередині |
| `LoadAsync_ValidJson_ReturnsOrdersThroughInvariants` | розбір трьох записів | id, сума 201, стан `Cancelled` |
| `ImportAsync_RaisesEventPerOrder_AndSecondRunAddsNothing` | подія на кожен запис, ідемпотентність | 5 і 0, п'ять подій `Imported` |
| `ImportAsync_CancelledMidway_StopsAfterCurrentRecord` | токен перевіряється в циклі | скасування, у сховищі 1 запис |
| `StreamAsync_*`, `ImportStreamAsync_AddsEveryStreamedOrder` | додаткове 1 | порядок файлу; `WithCancellation` зупиняє після першого |
| `WithTimeoutAsync_WhenTimeoutElapses_ThrowsTimeoutException` | додаткове 2 | `TimeoutException`, усередині — `OperationCanceledException` |
| `WithTimeoutAsync_WhenUserCancels_ThrowsOperationCanceled_NotTimeout` | додаткове 2, контроль | `OperationCanceledException`, не таймаут |
| `LoadAsync_ReportsGrowingProgress_EndingAt100` | додаткове 3 | відсотки зростають, останній — 100 |

```
Passed!  - Failed:     0, Passed:    28, Skipped:     0, Total:    28
```

### Самоперевірка

| Перевірка | Результат |
|---|---|
| `grep -rnE "\.Result\b\|\.Wait\(\)\|async void\|Task\.Run" src tests --include='*.cs'` | лише коментар «без Task.Run» у `JsonOrderImporter` |
| `dotnet test` двічі поспіль | 28 / 28 обидва рази |
| Тести і `data/` | жоден тест не читає `data/`; тимчасовий файл — у `finally` |
| `ProjectReference` у `Core.Tests.csproj` | лише `src/Core/Core.csproj` |
| `dotnet build` | 0 Warning(s), 0 Error(s) |
| Попередні режими (`--domain`, `--events`, `report-*`, без аргументів) | працюють; у сценаріях відмов тепер видно `DomainException` |

### Додаткові завдання

**1. Потоковий імпорт.** `IOrderStreamImporter.StreamAsync` повертає
`IAsyncEnumerable<Order>`: `JsonOrderImporter` читає масив через
`JsonSerializer.DeserializeAsyncEnumerable` і віддає замовлення по одному, а
`OrderService.ImportStreamAsync` додає їх через `await foreach` без проміжного
списку. Параметр токена позначено `[EnumeratorCancellation]`, тому токен, переданий
через `.WithCancellation(ct)`, теж доходить до ітератора — це доводить тест
`StreamAsync_TokenFromWithCancellation_StopsEnumeration`. `yield return` не можна
ставити в `try` з `catch`, тому в `try` лише `MoveNextAsync` — для загортання `JsonException`.

**2. Зв'язані токени.** `OrderService.WithTimeoutAsync(operation, timeout, ct)`
створює `CancellationTokenSource(timeout, TimeProvider)` і
`CreateLinkedTokenSource(ct, timeoutCts.Token)`. Якщо скасовано зв'язаний токен, а
токен користувача — ні, спрацював саме таймаут, і сервіс кидає `TimeoutException`
з `OperationCanceledException` усередині. Таймер бере час із `TimeProvider` сервісу,
тож тест просуває `FakeTimeProvider.Advance(2 с)` і не чекає реальних двох секунд.

**3. IProgress&lt;int&gt;.** `JsonOrderImporter(progress)` обгортає потік у
`ProgressReadStream`, що після кожного читання повідомляє відсоток прочитаних байтів
(лише коли відсоток змінився). Підписник у Cli — `ConsoleProgress` — малює смугу
`[##########----------]  33 %` (у файл чи конвеєр — рядок на кожні 25 %).
`Progress<T>` не використано: у консолі немає `SynchronizationContext`, і його
обробники пішли б у пул потоків — смуга могла б домалюватися після підсумку.

**4. Покриття.**

```bash
dotnet test --collect:"XPlat Code Coverage"     # tests/Core.Tests/TestResults/<guid>/coverage.cobertura.xml
```

| Частина Core | Рядків покрито |
|---|---|
| `Services/OrderService.cs` | 85 % |
| `Storage/JsonOrderImporter.cs` | 82 % |
| `Domain/OrderLine.cs`, `Domain/Order.cs` | 79 %, 77 % |
| `Storage/InMemoryOrderStore.cs` | 79 % |
| Увесь Core | 36 % (330 з 905) |

Не покрито: CSV/JSON-імпортери товарів тижня 3, `FileOrderStore` і
`CachingOrderStore`, `SampleData`, `EnvironmentInfo`, `ReportVariants` для
Stopwatch і частина звітів тижня 7 (GroupJoin, за місяцями). Це прийнятно: тести
лабораторної покривають ключову логіку — інваріанти, сервіс, події, імпорт і
скасування; `FileOrderStore` працює з диском і є кандидатом на інтеграційні
тести, а демо-дані й інформація про середовище не містять бізнес-правил.

### Що зміниться на тижнях 10–11

Minimal API прийме `CancellationToken` параметром обробника — той самий токен
`HttpContext.RequestAborted` піде в `ImportAsync`. На тижні 11 `IOrderStore`
отримає асинхронні методи поверх `ToListAsync` / `SaveChangesAsync`, а `ImportAsync`
зможе зберегти всі записи однією транзакцією: зараз скасування посеред імпорту
лишає вже додані замовлення (повторний запуск їх пропустить — імпорт ідемпотентний).

## Шляхи, JSON, конфігурація, secrets (лабораторна 9)

```bash
dotnet run --project src/Cli -f net10.0 -- config                    # звідки взялися налаштування
dotnet run --project src/Cli -f net10.0 -- export                    # уся колекція → файл даних
dotnet run --project src/Cli -f net10.0 -- export out/orders.json    # + копія у довільний файл
dotnet run --project src/Cli -f net10.0 -- import                    # samples/orders-import.json → сховище
dotnet run --project src/Cli -f net10.0 -- import data/orders-broken.json   # «Дані відхилено: …, запис 3: …»
dotnet test                                                          # 40 тестів
```

Очікуваний вивід (профіль Development):

```
[debug] Середовище: Development (CROSSAPP_ENVIRONMENT)
[debug] Поточний каталог : …/CrossApp
[debug] Каталог застосунку: …/CrossApp/src/Cli/bin/Debug/net10.0/
Файл даних : …/CrossApp/src/Cli/bin/Debug/net10.0/data/orders.json
ApiKey: присутній, довжина 13
Експортовано 20 записів.
```

### Профілі

Профіль обирає змінна `CROSSAPP_ENVIRONMENT` (без неї — `Development`). Спершу читається
`appsettings.json`, потім `appsettings.{профіль}.json` перекриває його ключі.

| | Development | Production |
|---|---|---|
| `Logging:MinimumLevel` | `Debug` — видно рядки `[debug]` | `Warning` — лише попередження |
| `Storage:Location` | `App` → `AppContext.BaseDirectory` | `LocalAppData` → `SpecialFolder.LocalApplicationData` |
| `Storage:DataPath` | `data` | `CrossApp` |
| Файл даних (macOS) | `src/Cli/bin/Debug/net10.0/data/orders.json` | `~/Library/Application Support/CrossApp/orders.json` |
| user-secrets | підключаються | ні — секрет лише зі змінної середовища |

### Ключі конфігурації

| Ключ | Типово | Змінна середовища | Опис |
|---|---|---|---|
| `EnvironmentName` | `Production` / профіль | `CROSSAPP_EnvironmentName` | назва профілю у виводі `config` |
| `Logging:MinimumLevel` | `Information` | `CROSSAPP_Logging__MinimumLevel` | `Trace`, `Debug`, `Information`, `Warning`, `Error` |
| `Storage:Location` | `App` | `CROSSAPP_Storage__Location` | від чого рахується відносний `DataPath` |
| `Storage:DataPath` | `data` | `CROSSAPP_Storage__DataPath` або `CROSSAPP_DATA_PATH` | каталог даних; абсолютний шлях — як є |
| `Storage:FileName` | `orders.json` | `CROSSAPP_Storage__FileName` | ім'я файлу сховища |
| `Orders:Currency` | `UAH` | `CROSSAPP_Orders__Currency` | валюта у виводі сум (ключ варіанта «Замовлення») |
| `Integration:ApiKey` | — | `CROSSAPP_Integration__ApiKey` | секрет: лише user-secrets або змінна, **ніколи** не appsettings.json |

Порядок джерел (пізніше перекриває раніше): `appsettings.json` → `appsettings.{Env}.json` →
user-secrets (лише Development) → змінні `CROSSAPP_*` → аргументи `--Розділ:Ключ=значення` →
проста змінна `CROSSAPP_DATA_PATH` (тільки для шляху). Двокрапка у змінних середовища
замінюється подвійним підкресленням: `Storage:DataPath` → `CROSSAPP_Storage__DataPath`.

### Приклади змінних

bash / zsh (змінна діє лише на одну команду):

```bash
CROSSAPP_DATA_PATH=/tmp/crossapp-data dotnet run --project src/Cli -f net10.0 -- export
CROSSAPP_ENVIRONMENT=Production dotnet run --project src/Cli -f net10.0 -- config
CROSSAPP_Storage__FileName=orders-test.json dotnet run --project src/Cli -f net10.0 -- export
CROSSAPP_Integration__ApiKey=from-env dotnet run --project src/Cli -f net10.0 -- config
dotnet run --project src/Cli -f net10.0 -- config --Storage:DataPath=tmp --Orders:Currency=EUR
```

PowerShell (змінна діє до кінця сесії терміналу; прибрати — `Remove-Item Env:CROSSAPP_DATA_PATH`):

```powershell
$env:CROSSAPP_DATA_PATH = "$env:TEMP\crossapp-data"
$env:CROSSAPP_ENVIRONMENT = "Production"
$env:CROSSAPP_Integration__ApiKey = "from-env"
dotnet run --project src/Cli -f net10.0 -- export
```

### Secret `Integration:ApiKey`

```bash
dotnet user-secrets init --project src/Cli                                  # один раз: UserSecretsId у Cli.csproj
dotnet user-secrets set "Integration:ApiKey" "<значення-ключа>" --project src/Cli
dotnet user-secrets list --project src/Cli
```

У репозиторії лише `<UserSecretsId>` у `Cli.csproj`; саме значення лежить поза ним:
`~/.microsoft/usersecrets/<UserSecretsId>/secrets.json` (Linux, macOS) або
`%APPDATA%\Microsoft\UserSecrets\<UserSecretsId>\secrets.json` (Windows). Шлях до цього
файлу друкує `config`. Застосунок читає `config["Integration:ApiKey"]` і показує лише
«присутній, довжина N». Перевірка: `git status` після `set` — чистий, `git grep "<значення-ключа>"` — порожньо.

### Шляхи

- Файл даних: `StorageOptions.ResolveFullPath(baseDir)` — `Path.Combine` + `Path.GetFullPath`,
  каталог створюється, якщо його видалили. Базовий каталог — `AppContext.BaseDirectory`
  (або `LocalApplicationData`), тому `dotnet run` з кореня solution, з `src/Cli` чи
  `dotnet …/Cli.dll` з будь-якого каталогу дає той самий файл.
- `appsettings*.json` копіюються в `bin/` і `publish/` (`CopyToOutputDirectory` у `Cli.csproj`).
- Вхідні приклади з `data/` копіюються в `bin/…/samples/`: типові файли лаб 3–8 більше не
  шукаються відносно поточного каталогу. Шлях, переданий аргументом, — відносно поточного
  каталогу, як у будь-якої консольної утиліти.
- Пошук `C:\`, `/home/`, `"\\"` у `src` — порожній.

### JSON і атомарний запис

`OrderJsonFile` — один набір `JsonSerializerOptions` для сховища, експорту й імпорту:
camelCase-імена (`PropertyNamingPolicy`), регістронезалежне читання (відкриваються й файли
попередніх лабораторних з `"Id"`), `UnsafeRelaxedJsonEscaping` — кирилиця у файлі як є,
а не `\u041D…`. Запис: повний вміст у `orders.json.tmp`, потім `File.Move(tmp, path,
overwrite: true)` — збій посеред запису псує лише `.tmp`, основний файл лишається цілим.
Зіпсований JSON чи запис, що порушує інваріанти, дає «Дані відхилено: Файл …, запис N: …»
і код виходу 1 замість стека виклику.

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
dotnet build                                   # усе рішення, обидва TFM
dotnet build src/Core/Core.csproj              # лише бібліотека
dotnet run --project src/Cli -f net10.0        # запуск застосунку
dotnet run --project src/Cli -f net10.0 -- --env --json   # середовище одним рядком JSON
```

Обидва проєкти багатоцільові, тому `-f` обовʼязковий: без нього `dotnet run`
відповідає «Your project targets multiple frameworks», а `dotnet publish` —
помилкою `NETSDK1129`.

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

### Різниця у виводі між двома TFM

`Cli` теж багатоцільовий (`net8.0;net10.0`), тому застосунок збирається й
запускається під обидва фреймворки. У системі встановлено лише runtime .NET 10,
тож для net8.0 є два робочі шляхи.

**1. Запустити net8.0-збірку на наявному runtime .NET 10** (roll-forward):

```bash
DOTNET_ROLL_FORWARD=Major dotnet run --project src/Cli -f net8.0
```

Без змінної команда завершиться повідомленням «You must install or update .NET»,
бо збірка просить саме `Microsoft.NETCore.App 8.0.0`.

**2. Self-contained публікація зі справжнім runtime .NET 8** (runtime pack
завантажується з NuGet):

```bash
dotnet publish src/Cli -c Release -f net8.0  -r osx-arm64 --self-contained true -o /tmp/sc8
dotnet publish src/Cli -c Release -f net10.0 -r osx-arm64 --self-contained true -o /tmp/sc10
/tmp/sc8/Cli
/tmp/sc10/Cli
```

Порівняння трьох запусків:

| Рядок | net8.0 на runtime 10 | net8.0 self-contained | net10.0 |
|---|---|---|---|
| `TFM бібліотеки Core` | `збірка під net8.0` | `збірка під net8.0` | `збірка під net10.0` |
| `Runtime` | `.NET 10.0.11` | `.NET 8.0.30` | `.NET 10.0.11` |
| `ОС` | `macOS 26.4.1` | `Darwin 25.4.0 ... RELEASE_ARM64_T6020` | `macOS 26.4.1` |

Перший стовпець показує, що `TargetFrameworkNote` обирає **компілятор**, а не
runtime: значення `збірка під net8.0` зберігається навіть тоді, коли код виконує
.NET 10. Директива `#if` спрацювала один раз під час збірки.

Третій рядок — несподіваний побічний результат. `RuntimeInformation.OSDescription`
повернув **різні рядки для того самого коду на тій самій машині**: .NET 8 віддає
сирий рядок з `uname`, .NET 10 — оброблений `macOS 26.4.1`. Причому різниця
зʼявляється лише в self-contained варіанті, тобто залежить саме від версії
runtime, а не від TFM збірки. Змінилася не наша логіка, а реалізація API
всередині .NET.

Це найкращий практичний аргумент на користь multi-targeting: код компілюється під
обидва TFM без жодної помилки, але **поводиться по-різному**, і виявити це можна
лише реальним запуском під кожен цільовий фреймворк, а не самою лише успішною
збіркою.

## Публікація

```bash
dotnet publish src/Cli -c Release -f net10.0 -r osx-arm64  --self-contained true
dotnet publish src/Cli -c Release -f net10.0 -r osx-arm64  --self-contained false
dotnet publish src/Cli -c Release -f net10.0 -r linux-x64  --self-contained true
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
dotnet publish src/Cli -c Release -f net10.0 -r osx-arm64 --self-contained true -p:PublishSingleFile=true
```

Замість 193 файлів у каталозі залишаються 3: виконуваний `Cli` і два файли
символів `.pdb` (їх можна прибрати через `-p:DebugType=none`). Застосунок
запускається і дає той самий вивід.

Розмір — 71,8 МіБ проти 78,1 МіБ у звичайної self-contained публікації.
Стисненням це **не** пояснюється: у .NET воно типово вимкнене. Різниця в ~6 МБ
виникає тому, що в бандл не потрапляють діагностичні нативні компоненти
(`libmscordaccore.dylib`, `libmscordbi.dylib`, `createdump` — разом 4,6 МБ),
а також окремі `Cli.deps.json` і `Cli.runtimeconfig.json`, які вбудовуються
всередину.

Стиснення вмикається окремим ключем і дає значно більший ефект:

```bash
dotnet publish src/Cli -c Release -f net10.0 -r osx-arm64 --self-contained true \
    -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true
```

| Варіант | Байтів | |
|---|---|---|
| self-contained, 193 файли | 81 882 458 | 78,1 МіБ |
| single-file, типово | 75 320 292 | 71,8 МіБ |
| single-file + compression | 33 503 028 | **32,0 МіБ** |

Платою за стиснення є довший перший запуск: вміст розпаковується в памʼять.

### 2. `PublishTrimmed`

```bash
dotnet publish src/Cli -c Release -f net10.0 -r osx-arm64 --self-contained true \
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

## Коротко: мінімум команд

Усе з кореня `CrossApp`. Чотирьох команд достатньо, щоб зібрати, запустити,
опублікувати і запустити з публікації:

```bash
dotnet build
dotnet run --project src/Cli -f net10.0
dotnet publish src/Cli -c Release -f net10.0 -r osx-arm64 --self-contained true
./src/Cli/bin/Release/net10.0/osx-arm64/publish/Cli
```

Замініть `osx-arm64` на свою RID (`win-x64`, `linux-x64`) — див. `dotnet --info`.

Ще дві, якщо потрібно показати структуру рішення і результат multi-targeting:

```bash
dotnet sln list                 # два проєкти: Cli і Core
ls src/Core/bin/Debug           # два підкаталоги: net8.0 і net10.0
```


cd /Users/admin/LNU/lab_KPP/CrossApp && git switch main && dotnet build -f net10.0

dotnet run --project src/Cli -f net10.0

dotnet run --project src/Cli -f net10.0 -- data/sample.csv

неіснуючий
dotnet run --project src/Cli -f net10.0 -- data/nope.csv; echo "код виходу: $?"

додаткові
dotnet run --project src/Cli -f net10.0 -- data/sample.json

dotnet run --project src/Cli -f net10.0 -- README.md


dotnet build -f net8.0


різнорідні рядки за префіксом
dotnet run --project src/Cli -f net10.0 -- --mixed
Один файл, два типи: P — товар, C — клієнт. Один switch повертає два різні типи результату. 5 товарів, 5 клієнтів, 4 помилки.

cat -n data/mixed.csv


статистика імпорту
dotnet run --project src/Cli -f net10.0 | tail -1


в DomainDemo.cs рядок order.Status = OrderStatus.Draft;


dotnet build
dotnet build -f net8.0
dotnet run --project src/Cli -f net10.0 -- --domain


dotnet run --project src/Cli -f net10.0 -- --domain


/Users/admin/LNU/lab_KPP/CrossApp/src/Cli/bin/Debug/net10.0/data/orders.json

dotnet run --project src/Cli -f net10.0
rm -rf src/Cli/bin/Debug/net10.0/data
dotnet run --project src/Cli -f net10.0 -- --file
cat src/Cli/bin/Debug/net10.0/data/orders.json
dotnet list package



rm -rf src/Cli/bin/Debug/net10.0/logs
dotnet run --project src/Cli -f net10.0 -- --events

dotnet run --project src/Cli -f net10.0 -- --events --file --cache

dotnet run --project src/Cli -f net10.0

dotnet run --project src/Cli -f net10.0




dotnet build
Основні звіти
dotnet run --project src/Cli -f net10.0 -- report-top
dotnet run --project src/Cli -f net10.0 -- report-orders
dotnet run --project src/Cli -f net10.0 -- report-expensive
dotnet run --project src/Cli -f net10.0 -- report-groups
dotnet run --project src/Cli -f net10.0 -- report-months
dotnet run --project src/Cli -f net10.0 -- report-products
dotnet run --project src/Cli -f net10.0 -- report-sum
Відкладене виконання
dotnet run --project src/Cli -f net10.0 -- report-deferred
Порожнє джерело
dotnet run --project src/Cli -f net10.0 -- report-sum --empty
dotnet run --project src/Cli -f net10.0 -- report-expensive --empty
Звіти з файловим сховищем (прапорець --file не стає назвою звіту)
dotnet run --project src/Cli -f net10.0 -- report-groups --file
Додаткові завдання
dotnet run --project src/Cli -f net10.0 -- report-customers
dotnet run --project src/Cli -f net10.0 -- report-join
dotnet run --project src/Cli -f net10.0 -- report-query
dotnet run --project src/Cli -c Release -f net10.0 -- report-bench
Перелік усіх підкоманд
dotnet run --project src/Cli -f net10.0 -- report

 
відкладене DeferredScenario.cs.

EnumerableExtensions.cs -своє Топ-N

ReportService.cs, метод ByStatusQuery (from … group … into g … select), поруч із ByStatus






dotnet run --project src/Cli -f net10.0 -- import-orders
dotnet run --project src/Cli -f net10.0 -- import-orders --repeat
dotnet run --project src/Cli -f net10.0 -- import-orders nope.json




4. dotnet run --project src/Cli -f net10.0 -- import-orders --big=600000 --timeout=2000
dotnet run --project src/Cli -f net10.0 -- import-orders --big=600000

5. dotnet run --project src/Cli -f net10.0 -- import-orders data/orders-broken.json

6. dotnet test
dotnet test --logger "console;verbosity=normal"

1. dotnet run --project src/Cli -f net10.0 -- import-orders --stream

2. dotnet test --filter "FullyQualifiedName~WithTimeoutAsync"

3. dotnet run --project src/Cli -f net10.0 -- import-orders --big=200000

4. dotnet test --collect:"XPlat Code Coverage"



1)
dotnet run --project src/Cli -f net10.0 -- export
dotnet run --project src/Cli -f net10.0 -- export data/export-demo.json
dotnet run --project src/Cli -f net10.0 -- import

2)
src/Cli/AppConfiguration.cs — SetBasePath(AppContext.BaseDirectory).
src/Core/Configuration/AppOptions.cs — StorageOptions.ResolveFullPath: тут Path.Combine від базового каталогу і GetFullPath.
src/Cli/Samples.cs

3) git ls-files src/Cli

4) 
dotnet run --project src/Cli -f net10.0 -- config
CROSSAPP_DATA_PATH=/tmp/crossapp-demo dotnet run --project src/Cli -f net10.0 -- export
ls /tmp/crossapp-demo



5)  Secret читається з user-secrets або env; запуск з довільного cwd працює
Секрет з user-secrets (профіль Development):

dotnet user-secrets list --project src/Cli
dotnet run --project src/Cli -f net10.0 -- config
Очікувано: ApiKey: присутній, довжина N і шлях до secrets.json у рядку user-secrets. Перша команда показує значення ключа на екрані, тому на захисті краще показати тільки другу.

Секрет зі змінної середовища (у Production user-secrets не підключаються):

CROSSAPP_ENVIRONMENT=Production dotnet run --project src/Cli -f net10.0 -- config
CROSSAPP_ENVIRONMENT=Production CROSSAPP_Integration__ApiKey=demo-key dotnet run --project src/Cli -f net10.0 -- config
Перша команда виведе ApiKey: не заданий, друга — присутній, довжина 8.

Запуск з довільного каталогу. Спочатку з кореня:

dotnet run --project src/Cli -f net10.0 -- config
Потім з src/Cli:

cd src/Cli && dotnet run -f net10.0 -- config
Потім з /tmp, запуском самої dll:

cd /tmp && dotnet /Users/admin/LNU/lab_KPP/CrossApp/src/Cli/bin/Debug/net10.0/Cli.dll config


7) 
dotnet build
dotnet test
git log --oneline -1

