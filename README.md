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
      Abstractions/         контракти (тиждень 5)
        IOrderStore.cs      List / GetById / Add / Update / Remove
      Storage/              реалізації сховищ (тиждень 5)
        InMemoryOrderStore.cs   словник у пам'яті, початкові дані через конструктор
        FileOrderStore.cs       кеш + JSON-файл, запис після кожної зміни
        CachingOrderStore.cs    декоратор над будь-яким IOrderStore (додаткове завдання)
      Events/               дані доменних подій (тиждень 6)
        OrderChangeKind.cs  Created / LineAdded / Confirmed / Cancelled
        OrderChangedEventArgs.cs  OrderId, Kind, LinesCount, Total, Delta, At
      Services/
        OrderService.cs     CreateOrder / AddLine / Confirm / Cancel / Find / Search + event Changed
      SampleData.cs         10 товарів і 16 замовлень для демо і тижня 7
    Cli/                    консольний застосунок (net8.0;net10.0)
      Cli.csproj            містить ProjectReference на Core
      Program.cs            composition root + вибір режиму
      DomainDemo.cs         сценарії «успіх» і «порушення інваріантів» (тиждень 4)
      OrderScenario.cs      сценарій сервісу і сценарій відмови (тиждень 5)
      EventScenario.cs      підписники, відписка, відмова без події (тиждень 6, --events)
  data/
    sample.csv              13 рядків даних, з них 3 навмисно пошкоджені
    sample.json             той самий домен у JSON (додаткове завдання)
    mixed.csv               товари і клієнти в одному файлі (додаткове завдання)
    catalog.csv             каталог для перевірки інваріантів Product (тиждень 4)
```

Напрямок залежності односторонній: **Cli → Core**. Core не знає про Cli, тому
його згодом можна буде підключити до проєктів `Api` (тиждень 10) і Blazor-клієнта
(тиждень 12). Правило «жодної бізнес-логіки в Program.cs» діє з цієї лабораторної.

Файл сховища `data/orders.json` з'являється не в корені репозиторію, а поруч зі
зібраним застосунком (`src/Cli/bin/Debug/net10.0/data/`), бо шлях будується від
`AppContext.BaseDirectory`. У git він не потрапляє — `bin/` ігнорується.
Так само поруч зі збіркою створюється журнал подій `logs/app.log` (тиждень 6).

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
| Початкові дані | `IEnumerable<Order>? seed` у конструкторі (`SampleData.Orders()`) | те, що є у файлі |
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
Замовлень на старті: 16

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
Усього: 17
```

Повторний запуск знову починає з 16 зразків: створене замовлення зникло разом із процесом.

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
    string line = $"{e.At:HH:mm:ss} {e.Kind,-9} {e.OrderId} {e.Delta,10:+0.00;-0.00} " +
                  $"→ сума {e.Total:F2} грн, рядків {e.LinesCount}";
    Console.WriteLine(line);
    File.AppendAllText(logPath, line + Environment.NewLine);
}

service.Changed += OnOrderChanged;          // 1: журнал (локальна функція)
service.Changed += (_, _) => changes++;     // 2: лічильник (лямбда із замиканням)
```

Шлях журналу будує `Program.PrepareLogPath()`:
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
  O-007 клієнт C-002 від 2026-09-13 — чернетка, рядків 1, сума 1799.00 грн
  O-010 клієнт C-004 від 2026-09-19 — чернетка, рядків 0, сума 0.00 грн
  O-015 клієнт C-003 від 2026-09-29 — скасоване, рядків 0, сума 0.00 грн
Знайдено: 3
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