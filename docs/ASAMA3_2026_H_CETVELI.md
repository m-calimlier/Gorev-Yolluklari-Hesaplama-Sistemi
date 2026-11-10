# Aşama 3 — 2026 H Cetveli ve resmî PDF dosya doğrulaması

**Tarih:** 09.11.2026 | **Durum:** Test çalıştırılınca belirlenecek; geliştirici ortamında Windows/.NET derlemesi yapılmadı.

## Bu teslimde

- Kullanıcının tarayıcıdan kaydettiği `Kaynaklar/01_Mevzuat/6245_Harcirah_Kanunu.pdf` ve önceden indirilmiş `Kaynaklar/02_Butce_ve_Tarifeler/2026_H_Cetveli.pdf` dosyalarının varlığı, en az dosya boyutu, PDF imzası ve SHA-256 kayıt altına alınır.
- `docs/PROJE_DEVIR_NOTU_2026-11-09.md` varlığı uyarıyla kontrol edilir. Önceki belgeler hiçbir şekilde değiştirilmez.
- 2026 H Cetveli I-B için ayrı doğrulanmış **ön hesap** JSON'u ve onu kullanan C# seçici eklenir. Önceki `2026_H_Cetveli_Veri_Ozeti.json` **değiştirilmez**.
- H Cetveli I-A (üst düzey makamlar), özel statüler ve yurt dışı tarifeleri otomatik seçilmez.
- Eksik derece/ek gösterge, tanımsız personel sınıfı, 2025 veya 2027 tarihi hesaplamayı bloklar. `0` ek gösterge açıkça beyan edildiğinde geçerlidir. Kategori tutarları resmî cetvelle birebir denetlenir.
- C# testleri sentetik girdiler kullanır. Gerçek personel hakkı, memuriyet mahalli, özel durum, seyahat ve ödeme toplamı henüz onaylanmaz.

## 2026 H Cetveli I-B verileri

- Ek gösterge 8000 ve üstü: **890 TL** (`B_a`).
- Ek gösterge 6400–7999: **880 TL** (`B_b`).
- Ek gösterge 3600–6399: **870 TL** (`B_c`).
- Diğerleri, derece 1–4: **860 TL** (`B_d`).
- Diğerleri, derece 5–15: **850 TL** (`B_e`).

**Birincil resmî kaynak:** https://www.sbb.gov.tr/wp-content/uploads/2025/12/8-H-Cetveli_2026Butcesi.pdf

## Nasıl test edilir?

`powershell.exe -NoProfile -ExecutionPolicy Bypass -File "C:\Users\MuratPC\Desktop\Görev Yollukları Hesaplama Sistemi\03_Asama3_Tarife_Testleri.ps1"`

Script başarısızsa `TOPLAM` satırını başarılı varsaymayın. Sonuca göre düzeltme yapılmalıdır.

## Açık işler

1. 6245 resmî PDF'nin **yalnız PDF başlığı değil madde 15'in 7588/3 ile güncellendiği bölüm dahil güncel metni** belge içinde kontrol edilmeli. Bu teslim otomatik metin çıkarımı/yasal karşılaştırma yapmaz.
2. `Kaynak_Katalogu.json` dosyasındaki `BUT-003` Hakem Kurulu Kararı / Toplu Sözleşme yanlış adlandırması düzeltilmeli; henüz otomatik yapılmadı.
3. Seçilen tarife ile 39/45 ön hesapları arasına hukuki hak sahipliği ve doğrulama katmanı konulmalı; herhangi bir otomatik resmî bordro açık edilmemeli.
4. Kurum, kadro ve personel veri modeli; JSON kayıt şema sürümü ve gerçek geçmiş-yıl regresyonları kurulmalı.
5. PDF resmî Örnek 27/28 ve icmal çıktıları, gerçek örneklerle karşılaştırılmadan tamamlandı sayılmamalı.
