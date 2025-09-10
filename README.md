# 🛰️ Blazor DDD Adres Sorgulama & Kota Uygulaması

Bu proje, Domain-Driven Design (DDD) prensipleri temel alınarak geliştirilmiş, .NET 9 üzerinde çalışan modern bir Blazor Server uygulamasıdır. Uygulama, kullanıcıların sisteme kaydolup giriş yaparak, kademeli bir form üzerinden adres (İl, İlçe, Mahalle, Cadde/Site) sorgulamasına olanak tanır. Her kullanıcının sorgu hakkı, merkezi olarak yönetilen ve yapılandırılabilir bir kota sistemi (örn: günlük 5, aylık 20) ile sınırlandırılmıştır.

Limitler aşıldığında, arayüz kullanıcıyı bilgilendirir ve API katmanı, endüstri standardı olan HTTP 429 (Too Many Requests) hata kodunu ve ilgili X-RateLimit-* başlıklarını döndürür.

[Türkçe](#türkçe) | [English](#english)

---

<a name="türkçe"></a>
##  Türkçe Açıklama

<details>
<summary>Detayları görmek için tıklayın</summary>

### 🤖 Örnek Kullanım / Demo

![Blazor Quota App Demo](images/blazor.gif)

---

### ✨ Temel Özellikler


* **🧠 Katmanlı Mimari:** Sorumlulukların net bir şekilde ayrıldığı, test edilebilir ve bakımı kolay bir kod tabanı için Domain-Driven Design prensipleri.
  
* **🛡️ Güvenli Kimlik Doğrulama:** Endüstri standardı olan ASP.NET Core Identity ile kullanıcı kaydı, girişi ve güvenliği.
  
* **⏱️ Dinamik Kota Kontrolü:** İstanbul (UTC+3) saat dilimine göre anlık olarak hesaplanan, appsettings.json'dan yapılandırılabilen günlük ve aylık kullanım limitleri.
  
* **⛓️ Kademeli Form:** İl seçildiğinde ilçelerin, ilçe seçildiğinde mahallelerin yüklendiği dinamik ve kullanıcı dostu bir arama arayüzü.
  
* **⚛️ Atomik Veritabanı İşlemleri:**  "Yarış Koşulu" (Race Condition) riskini ortadan kaldıran, EF Core Transaction tabanlı kota düşürme ve loglama işlemleri.
  
* **📡 Standart API Uç Noktaları:** Harici sistemler için belgelenmiş, GET /api/lookups/..., GET /api/usage ve POST /api/search uç noktaları.
  
* **📄 Akıllı Form Doğrulama:** Zorunlu alanları ve HasStreet gibi seçimlere bağlı koşullu gereksinimleri kontrol eden, kullanıcıyı yönlendiren temiz bir form doğrulama yapısı.

  

### 🛠️ Kullanılan Teknolojiler

* **Backend Framework:** .NET 9
* **Dil:** C#
* **Arayüz Teknolojisi:** Blazor Server
* **API Teknolojisi:** ASP.NET Core Minimal API
* **Veri Erişimi (ORM):** Entity Framework Core
* **Veritabanı:** SQLite
* **Kimlik Doğrulama:** ASP.NET Core Identity
* **API Dokümantasyonu:** Swagger (Swashbuckle)


### ⚠️ Dikkat Edilen Noktalar Ve Yapılan İyileştirmeler

* **Net Parametreler:** API endpoint'lerinin hangi parametreyi nereden aldığını ([FromQuery], [FromServices], [FromBody]) açıkça belirterek kodun okunabilirliği artırıldı.
* **Merkezi Yapılandırma:** Kota limitleri olan 5 ve 20 gibi statik değerler, kodun içinden çıkarılarak appsettings.json dosyasına taşındı. Bu sayede, gelecekte limitleri değiştirmek için kodu yeniden derlemeye gerek kalmaz.


### 🏛️ Mimarî ve Karar Gerekçeleri

* **Identity mi Basit User Tablosu mu?**
    * **Karar:** `ASP.NET Core Identity` kullanıldı.
    * **Neden:** Basit bir kullanıcı tablosu yerine Identity'nin tercih edilmesinin sebebi, şifreleme, token yönetimi, oturum güvenliği ve e-posta doğrulama gibi karmaşık işlemleri hazır, test edilmiş ve güvenli bir altyapıya devretmektir. Bu, geliştirme sürecini hızlandırır ve güvenlik risklerini minimize eder.

* **Repository Deseni mi Doğrudan DbContext mi?**
    * **Karar:** Doğrudan `DbContext` kullanıldı.
    * **Neden:** Projenin sorguları basit olduğu için Repository Deseni'nin getireceği ek soyutlama katmanı gereksiz karmaşıklık yaratacaktı. Doğrudan DbContext kullanımı, bu ölçekteki bir proje için daha pratik ve hızlı bir çözüm oldu.

* **Lookup Endpoint Stratejisi?**
    * **Karar:** Her bir lookup türü için ayrı (çoklu) endpointler oluşturuldu (`/api/lookups/provinces`, `/api/lookups/counties?provinceId=...`). Cache mekanizması bu görev kapsamında uygulanmadı.
    * **Neden:** Çoklu endpoint stratejisi, API'yi daha anlaşılır, REST prensiplerine uygun ve yönetilebilir kılar. Cache mekanizması ise, bu veriler sık değişmese de, 2 saatlik kısıtlı görev süresinde öncelik temel iş mantığına verildiği için eklenmemiştir, ancak production ortamı için potansiyel bir iyileştirmedir.

* **Transaction Kapsamı Nerede Başlıyor/Bitiyor?**
    * **Karar:** Transaction, `Application` katmanındaki `QuotaService.TryConsumeAndSearchAsync` metodunun en başında başlar ve metodun sonundaki `CommitAsync()` ile sona erer.
    * **Neden:** Bu kapsam, bir kullanıcının kota hakkını kontrol etme, hakkını düşürmek için `SearchLog` kaydı atma ve asıl aramayı gerçekleştirme adımlarının tümünü "ya hepsi ya hiçbiri" (atomik) prensibiyle çalışmasını garanti eder. Bu, veri tutarsızlığını önler.

* **Street/Site Birlikte Seçilirse Öncelik?**
    * **Karar:** Cadde (`Street`) önceliklidir.
    * **Neden:** Adres tariflerinde genellikle cadde bilgisi, site bilgisinden daha temel ve genel bir birimdir. Bu hiyerarşiden dolayı, `if (request.HasStreet) ... else if (request.HasSite) ...` yapısını kullanarak `Street`'e öncelik verdik.

* **Seed Stratejisi?**
    * **Karar:** Başlangıç verileri (`Seed Data`), EF Core migration dosyasının içine `InsertData` komutlarıyla eklendi.
    * **Neden:** Projemizin başlangıç verileri (iller, ilçeler vb.) sabit ve değişmez niteliktedir. Bu tür statik verileri doğrudan migration dosyasına `InsertData` komutlarıyla eklemek en basit ve en güvenilir yöntemdir. Bu sayede, projeyi alıp `dotnet ef database update` komutunu çalıştıran herkes, hem doğru veritabanı şemasına hem de çalışmak için gerekli olan başlangıç verilerine tek bir adımda sahip olur. `IHostedService` gibi ayrı bir servis, daha dinamik veya karmaşık "seeding" senaryoları için uygundur ve bu projenin kapsamı için gereksizdir.



### 📂 Proje Yapısı

```
QuotaApp/
├── 
├── Domain/           # Projenin çekirdeği. Veritabanı varlıklarını (Entities) ve temel iş kurallarını içerir.
│   └── Entities/
│
├── Application/      # İş mantığının ve kurallarının merkezidir. Gelen istekleri işler, veritabanı ile konuşur.
│   ├── DTOs/         # Katmanlar arası veri transferi için kullanılan nesneler.
│   ├── Services/     # IQuotaService ve QuotaService gibi ana iş mantığı servisleri.
│   └── Settings/     # appsettings.json'dan okunan yapılandırma nesneleri.
│
├── Infrastructure/   # Dış sistemlerle ve altyapıyla ilgili kodları barındırır.
│   ├── Migrations/   # EF Core veritabanı şema değişiklikleri.
│   └── ApplicationDbContext.cs # Veritabanı bağlantısı ve EF Core yapılandırması.
│
├── Presentation/     # Kullanıcıya sunulan katmandır.
│   ├── ApiEndpoints/ # Dış dünyaya açılan Minimal API uç noktaları.
│   └── Components/   # Blazor arayüzünü oluşturan .razor bileşenleri.
│
├── wwwroot/          # CSS, JavaScript gibi statik dosyalar.
├── app.db            # Çalışma zamanında oluşturulan SQLite veritabanı dosyası.
└── Program.cs        # Uygulamanın başlangıç noktası; servislerin ve middleware'lerin yapılandırıldığı yer.
```

### 🏁 Kurulum ve Çalıştırma

#### Adım 1: Projeyi ve Bağımlılıkları Kurma
```bash
# Projeyi klonlayın
git clone https://github.com/FatihSuicmez/DevBlazorQuotaApp.git
cd DevBlazorQuotaApp

# Gerekli .NET paketlerini yükleyin
dotnet restore
```

#### Adım 2: Veritabanını Oluşturma

Proje, Entity Framework Core "Code-First" yaklaşımını kullanır. Veritabanını ve tabloları oluşturmak için aşağıdaki komutu çalıştırmanız yeterlidir.

```bash
# Bu komut, proje ana dizininde app.db adında bir SQLite veritabanı dosyası oluşturacaktır.
dotnet ef database update
```


#### Adım 3: Uygulamayı Çalıştırma

Uygulamayı başlatmak için aşağıdaki komutu kullanın:

```bash
dotnet run
```

Terminalde belirtilen **`http://localhost:xxxx`** adresini bir web tarayıcısında açın.


#### Adım 4: Kullanım

* Sitenin sağ üst köşesindeki Register linki ile yeni bir kullanıcı hesabı oluşturun.

* Login linki ile sisteme giriş yapın.

* Ana sayfadaki sorgulama arayüzünü kullanarak limitler dahilinde testlerinizi yapabilirsiniz.


#### Adım 5: API Testi (Swagger)

Proje, geliştirme ortamında çalışırken tüm API endpoint'lerini listeleyen ve test etmeye olanak tanıyan bir Swagger arayüzü sunar.

* Uygulama çalışırken tarayıcınızda **`/swagger`** adresine gidin.
  
  **`(Örn: http://localhost:5227/swagger)`**
  * **Önemli Not:** API endpoint'leri yetkilendirme gerektirdiği için Swagger üzerinden test yapmadan önce uygulamanın ana arayüzündensisteme giriş yapmış olmanız gerekmektedir.
  Giriş yaptıktan sonra Swagger arayüzündeki "Try it out" özelliğini kullanarak istekleri başarılı bir şekilde gönderebilirsiniz.


</details>

---

<a name="english"></a>
## English Description

<details>
<summary>Click to see details</summary>

### 🤖 Sample Usage / Demo

![Blazor Quota App Demo](images/blazor.gif)

---

### ✨ Core Features

* **🧠 Layered Architecture:** Domain-Driven Design principles for a testable and maintainable codebase with a clear separation of concerns.

* **🛡️ Secure Authentication:** User registration, login, and security with the industry-standard ASP.NET Core Identity.

* **⏱️ Dynamic Quota Control:** Daily and monthly usage limits calculated in real-time based on the Istanbul (UTC+3) timezone, configurable from appsettings.json.

* **⛓️ Cascading Form:** A dynamic and user-friendly search interface where districts are loaded when a province is selected, and neighborhoods are loaded when a district is selected.

* **⚛️ Atomic Database Operations:** EF Core Transaction-based quota deduction and logging operations that eliminate the risk of "Race Conditions".

* **📡 Standard API Endpoints:** Documented GET /api/lookups/..., GET /api/usage, and POST /api/search endpoints for external systems.

* **📄 Smart Form Validation:** A clean form validation structure that checks for required fields and conditional requirements based on selections like HasStreet, guiding the user.


### 🛠️ Technologies Used

* **Backend Framework:** .NET 9

* **Language:** C#

* **UI Technology:** Blazor Server

* **API Technology:** ASP.NET Core Minimal API

* **Data Access (ORM):** Entity Framework Core

* **Database:** SQLite

* **Authentication:** ASP.NET Core Identity

* **API Documentation:** Swagger (Swashbuckle)
  

### ⚠️ Considerations and Improvements

* **Explicit Parameters:** Code readability was increased by explicitly specifying where API endpoints get their parameters from ([FromQuery], [FromServices], [FromBody]).

* **Centralized Configuration:** Static values like quota limits (5 and 20) were moved out of the code and into the appsettings.json file. This way, there is no need to recompile the code to change the limits in the future.


### 🏛️ Architecture and Design Rationale

* **ASP.NET Core Identity vs. Simple User Table**
    * **Decision:** `ASP.NET Core Identity` was used.
    * **Reason:** Instead of a simple user table, Identity was chosen to delegate complex operations such as encryption, token management, session security, and email verification to a ready, tested, and secure infrastructure. This accelerates the development process and minimizes security risks.

* **Repository Pattern vs. Direct DbContext Usage**
    * **Decision:** `DbContext` was used directly.
    * **Reason:** Given the simplicity of the project's queries, the additional abstraction layer from a Repository Pattern would have created unnecessary complexity. Direct `DbContext` usage was a more practical and faster solution for a project of this scale.

* **Lookup Endpoint Strategy**
    * **Decision:** Separate (multiple) endpoints were created for each lookup type (`/api/lookups/provinces`, `/api/lookups/counties?provinceId=...`). A caching mechanism was not implemented within the scope of this task.
    * **Reason:** The multiple-endpoint strategy makes the API more understandable, compliant with REST principles, and manageable. While caching would be a potential improvement for a production environment, priority was given to the core business logic within the limited 2-hour task timeframe, as this data does not change frequently.

* **Transaction Scope: Where Does It Start/End?**
    * **Decision:** The transaction starts at the beginning of the `QuotaService.TryConsumeAndSearchAsync` method in the `Application` layer and ends with `CommitAsync()` at the end of the method.
    * **Reason:** This scope ensures that all steps—checking a user's quota, creating a `SearchLog` to consume a credit, and performing the actual search—operate under the "all or nothing" (atomic) principle. This prevents data inconsistency.

* **Priority if Both Street and Site are Selected?**
    * **Decision:** `Street` is prioritized.
    * **Reason:** In address descriptions, street information is generally a more fundamental and general unit than site information. Due to this hierarchy, we prioritized `Street` by using an `if (request.HasStreet) ... else if (request.HasSite) ...` structure.
* **Seeding Strategy**
    * **Decision:** Initial data (`Seed Data`) was added directly into the EF Core migration file using `InsertData` commands.
    * **Reason:** The project's initial data (provinces, counties, etc.) is static and immutable. Adding this type of data directly into the migration file is the simplest and most reliable method. This way, anyone who runs the `dotnet ef database update` command gets both the correct database schema and the necessary initial data to run the application in a single step. A separate service like `IHostedService` is more suitable for dynamic or complex seeding scenarios and was unnecessary for the scope of this project.


### 📂 Project Structure

```
QuotaApp/
├── 
├── Domain/           # The core of the project. Contains database entities (Entities) and core business rules.
│   └── Entities/
│
├── Application/      # The center of business logic and rules. It processes incoming requests and communicates with the database.
│   ├── DTOs/         # Objects used for data transfer between layers.
│   ├── Services/     # Core business logic services like IQuotaService and QuotaService.
│   └── Settings/     # Configuration objects read from appsettings.json.
│
├── Infrastructure/   # Houses code related to external systems and infrastructure.
│   ├── Migrations/   # EF Core database schema changes.
│   └── ApplicationDbContext.cs # Database connection and EF Core configuration.
│
├── Presentation/     # The layer presented to the user.
│   ├── ApiEndpoints/ # Minimal API endpoints exposed to the outside world.
│   └── Components/   # The .razor components that make up the Blazor UI.
│
├── wwwroot/          # Static files like CSS, JavaScript.
├── app.db            # The SQLite database file created at runtime.
└── Program.cs        # The application's entry point; where services and middleware are configured.
```


### 🏁 Setup and Running

#### Step 1: Clone the Project and Install Dependencies

```bash
# Clone the project
git clone [https://github.com/FatihSuicmez/DevBlazorQuotaApp.git](https://github.com/FatihSuicmez/DevBlazorQuotaApp.git)
cd DevBlazorQuotaApp

# Install the required .NET packages
dotnet restore
```

#### Step 2: Create the Database

The project uses the Entity Framework Core "Code-First" approach. To create the database and tables, you just need to run the following command.

```bash
# This command will create an SQLite database file named app.db in the project's root directory.
dotnet ef database update
```

#### Step 3: Run the Application

To start the application, use the following command:

```bash
dotnet run
```

Open the **`http://localhost:xxxx`** address provided in the terminal in a web browser.

#### Step 4: Usage

* Create a new user account via the Register link in the top right corner of the site.

* Log in to the system with the Login link.

* You can perform your tests within the limits using the query interface on the main page.


#### Step 5: API Testing (Swagger)

The project provides a Swagger UI that lists all API endpoints and allows for testing them while running in the development environment.

While the application is running, navigate to the **`/swagger`** address in your browser.
**`(e.g., http://localhost:5227/swagger)`**
  * **Important Note:** Since the API endpoints require authorization, you must be logged into the system through the main application interface before testing via Swagger.
  After logging in, you can successfully send requests using the "Try it out" feature in the Swagger UI.

</details>

---
