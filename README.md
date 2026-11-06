# NewsPortal

Türkçe haber portalı. Proje; `backend` altında ASP.NET Core .NET 8 katmanlı API çözümü ve `frontend` altında Next.js uygulaması olarak yapılandırılmıştır.

## Gereksinimler

- .NET 8 SDK
- Node.js 20+
- SQL Server (yerel geliştirmede varsayılan bağlantı `localhost` kullanır)

## Geliştirme

Backend: `dotnet run --project backend/NewsPortal.API`

Frontend: `cd frontend`, `npm install`, `npm run dev`

API sağlık adresleri: `/health` ve `/api/health`. Swagger geliştirme ortamında `/swagger` adresindedir.

## İlk çalışan özellikler

- Public kategoriler: `/kategori/gundem`, `/kategori/ekonomi`, `/kategori/spor`, `/kategori/dunya`
- Yönetim ekranı: `/admin`; RSS inceleme: `/admin/rss`
- Hava durumu API'si: `GET /api/public/weather?city=Malatya`
- Döviz API'si: `GET /api/public/exchange-rates`
- Onaylanmış RSS haberleri: `GET /api/public/news?category=gundem`
- RSS yönetimi ve tarama uçları yalnızca `Development` ortamında açılır.

Hava durumu Open-Meteo, günlük referans döviz kurları Frankfurter üzerinden gelir; ikisi için de API anahtarı gerekmez. Geliştirme ortamında TRT Haber'in resmi RSS akışları otomatik taranır. Yeni kaynaklar `/admin/rss` ekranından eklenebilir. RSS ile gelen kayıtlar önce `PendingReview` olur; editör onaylamadan kategori sayfalarında görünmez. Kaynak bağlantısı korunur ve tam haber kopyalanmaz.

RSS kaynakları ve çekilen kayıtlar SQL Server'daki `RssFeedSources` ve `RssNewsItems` tablolarında kalıcı tutulur. Kullanıcı/rol yetkilendirmesi henüz eklenmediği için yönetim uçlarını production ortamına açmayın.

Migration'ları uygulamak için SQL Server'ı başlatıp `dotnet ef database update --project backend/NewsPortal.Infrastructure --startup-project backend/NewsPortal.API` komutunu çalıştırın. RSS tablolarını oluşturan migration `AddRssTables` adını taşır.

Frontend API adresini `NEXT_PUBLIC_API_BASE_URL` ile yapılandırın. Gizli anahtarlar kaynak koda eklenmemelidir.
