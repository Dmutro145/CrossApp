# CrossApp
Наскрізний проєкт з крос-платформного програмування.

Предметна область:Замовлення. Сутності: Customer, Product, Order, OrderLine.
Призначення: оформлення замовлень клієнтів та підрахунок їх сум.
## Запуск
dotnet build

dotnet run --project src/Cli
## Середовище
.NET SDK 10.0.401, Windows 11 x64 (RID: win-x64)

## Порівняння розміру self-contained публікацій

| RID       | Розмір каталогу publish      |
| win-x64    | ≈ 76.8 MB (80 564 821 байт) |
| linux-x64 | ≈ 78.8 MB (82 621 721 байт) |

## Лабораторна робота 2 — Core + Cli

### Порівняння режимів публікації (лаб. 2)

| RID | Режим | Розмір publish | Потрібен runtime |
|-----|-------|-----------------|-------------------|
| win-x64 | self-contained | ≈76.85 МБ | ні |
| win-x64 | framework-dependent | ≈0.19 МБ | так (.NET 10) |

**Self-contained**
 включає копію .NET runtime — застосунок запускається на будь-якій машині без попередньо встановленого .NET, але каталог значно більший.
**Framework-dependent**
 містить лише код і залежності NuGet — розмір мінімальний, але потребує сумісного .NET 10 runtime на цільовій машині.

### Multi-targeting
Бібліотека Core успішно зібрана під два TFM (net8.0 і net10.0) — SDK 10.0.401 має необхідні targeting packs, додаткове встановлення не знадобилось.

## Лабораторна робота 3 — records, імпорт CSV

Формат `data/sample.csv`: роздільник `;`, перший рядок — заголовок (`id;name;price`),
кодування UTF-8, ціна з крапкою як десятковим роздільником (InvariantCulture).
У файлі навмисно є 3 пошкоджені рядки (12 — мало колонок, 13 — нечислова ціна,
14 — порожня назва): вони перевіряють, що імпорт не падає на поганих даних.
Extra
JSON importer (.csv / .json через switch expression)
Mixed CSV importer (P;..., C;...)
Import statistics (total / accepted / skipped / error %)

Запуск: `dotnet run --project src/Cli` (з кореня репозиторію)
або `dotnet run --project src/Cli -- шлях\до\файлу.csv`.

## Лабораторна 4: доменна модель

Сутності: `Order` та `OrderLine` у `src/Core/Domain`.
Records з лаби 3 (`ProductDto`, `CustomerDto`) лишаються DTO; додано `OrderDto` і `OrderLineDto`.

### Інваріанти
- Id замовлення не порожній (`Order.Create`, `ArgumentException`)
- Id клієнта не порожній (`Order.Create`, `ArgumentException`)
- Id товару в рядку не порожній (`OrderLine.Create`, `ArgumentException`)
- Назва товару в рядку не порожня (`OrderLine.Create`, `ArgumentException`)
- Ціна не від'ємна (`OrderLine.Create`, `ArgumentOutOfRangeException`)
- Кількість у рядку більша за нуль (`OrderLine.Create`, `ArgumentOutOfRangeException`)
- Не можна додати рядок до підтвердженого замовлення (`Order.AddLine`, `InvalidOperationException`)
- Не можна підтвердити порожнє замовлення (`Order.Confirm`, `InvalidOperationException`)
- Не можна підтвердити замовлення повторно (`Order.Confirm`, `InvalidOperationException`)
- `FromDto` відновлює сутність через `Create` / `AddLine` / `Confirm`, тобто з тими самими перевірками

### Запуск демонстрації
dotnet run --project src/Cli -- --domain

## Лабораторна 5: сервісний шар, два сховища, ручний DI

Інтерфейс: `IOrderStore` (`src/Core/Abstractions`) — `List`, `GetById`, `Add`, `Update`, `Remove`.
Реалізації (`src/Core/Storage`): `InMemoryOrderStore` (словник у пам'яті), `FileOrderStore` (кеш + JSON-файл).
Сервіс: `OrderService` (`src/Core/Services`) — `CreateOrder`, `AddLine`, `Confirm`, `All`, `Find`; залежить лише від `IOrderStore` (конструктор).

Залежності: `Cli → OrderService → IOrderStore → InMemoryOrderStore | FileOrderStore`.
Composition root — `src/Cli/Program.cs`: конкретні сховища створюються тільки там; DI-контейнер не підключено.

На диск іде `OrderDto` (додано поле `Status`, щоб зберігався і стан `Cancelled`).
Файл даних: `data/orders.json` поруч із виконуваним файлом (`src/Cli/bin/Debug/net10.0/data/`).

### Запуск
dotnet run --project src/Cli              # InMemoryOrderStore, дані зникають після виходу
dotnet run --project src/Cli -- --file    # FileOrderStore, дані накопичуються

Сценарій: створити замовлення, додати рядки, підтвердити, показати список, знайти за id, потім сценарій відмови (дубль id, неіснуючий id, повторне підтвердження).
Режими попередніх лабораторних: `-- --domain` (лаб. 4), `-- data\sample.csv` (лаб. 3).

## Лабораторна 6: події, Func, лямбди

Подія `OrderService.Changed` (`EventHandler<OrderChangedEventArgs>`) повідомляє, що замовлення змінилося. `Core` не пише в консоль і файл: як обробити подію, вирішує `Cli`.

Типи в `src/Core/Events`: `OrderChangeKind` (`OrderCreated`, `LineAdded`, `OrderConfirmed`), `OrderChangedEventArgs` (`OrderId`, `Kind`, `LinesCount`, `Total`, `At`).
Подію піднімає приватний `OnChanged` лише після збереження в сховищі, тому при винятку події немає.

Підписники в `Cli`: локальний метод `OnOrderChanged` (консоль і `logs/app.log`) та лямбда-лічильник. Є відписка `-=`. Пошук через `Func<Order, bool>`: `OrderService.Search`.

### Запуск
dotnet run --project src/Cli -- --events

Лог: `src/Cli/bin/Debug/net10.0/logs/app.log`.