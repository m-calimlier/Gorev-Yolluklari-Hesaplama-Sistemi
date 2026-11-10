# Resmî kaynaklar — başlangıç durumu (9 Kasım 2026)

Bu paketteki `.url` dosyaları kaynakların Windows kısayollarıdır. **Orijinal PDF/XLSX dosyaları paketin içinde değildir.** Bu ortamda kaynak sunucularına doğrudan dosya indirme erişimi bulunmadığı için gerçekte indirilmemiş dosyaları varmış gibi sunmuyoruz.

1. Proje ana dizininde `01_Kaynaklari_Indir.ps1` dosyasını PowerShell ile çalıştırın.
2. İndirici `Kaynak_Katalogu.json` kayıtlarını okuyup PDF ve XLSX dosyalarını ilgili klasöre yazar.
3. Uzantı/imza denetimi yapar; hatalı HTML/engel sayfalarını PDF sanıp kaydetmez.
4. Başarılı dosyalara SHA-256 hesaplar; sonuçları `Indirme_Raporu_....csv` olarak kayıt altına alır.
5. Ağ veya kurum erişimi engeliyse `.url` kısayollarından tarayıcıyla açıp elle kaydedin.

## Hukuki kullanım sınırı

Bunlar yazılım geliştirmeye temel teşkil eden çalışma kaynaklarıdır. Eski bir mevzuat derlemesi güncel mevzuatın yerine geçmez. Mevzuatın son değişiklikleri, tebliğler, fiili görev tarihi, personel statüsü ve kurum uygulaması doğrulanmadan ödeme yetkisi ya da nihai bordro üretilmemelidir.

`Mevzuat_Kural_Matrisi.csv`: kurallar ve test hazırlığı — **henüz uygulanmış hesaplama motoru değildir**.
`2026_H_Cetveli_Veri_Ozeti.json`: H Cetvelinde görülen memur ve hizmetli kategorileri — otomatik hesapta henüz kullanılmaz.
