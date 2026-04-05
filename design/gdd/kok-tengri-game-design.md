# Kök Tengri — Game Design Spec

**Status:** Approved
**Date:** 2026-03-26
**Author:** zbrave + Claude

---

## 1. Overview

Kök Tengri is a Türk mitolojisi temalı, pixel art, survivor-like mobil oyun. Oyuncu bir şaman/savaşçı olarak dalga dalga gelen mitolojik düşmanlara karşı hayatta kalır. Oyunun temel farkı **şaman büyü crafting sistemi**: run sırasında toplanan 5 temel element (Od, Sub, Yer, Yel, Temür) birleştirilerek benzersiz büyüler yaratılır. Kahraman + sınıf sistemi ile her run farklı bir deneyim sunar.

- **Platform:** Mobil (iOS + Android)
- **Motor:** Unity 2D (URP)
- **Dil:** C#
- **Monetizasyon:** Free-to-Play + IAP (kozmetik + kahraman, güç avantajı yok)
- **Görsel Stil:** Pixel art
- **Tema:** Türk mitolojisi (Tengri inancı, destanlar, mitolojik yaratıklar)
- **Hedef:** Solo geliştirici, MVP 3-4 ay

---

## 2. Player Fantasy

Sen Tengri'nin topraklarını koruyan son şamansın. Erlik Han'ın yeraltı orduları yeryüzüne yükseliyor. Elementlerin gücünü birleştir, ataların büyülerini yeniden keşfet ve karanlığı geri püskürt. Her run bir destan, her ölüm bir ders, her zafer bir efsane.

---

## 3. Core Loop

```
1. RUN BAŞLA → Kahraman + sınıf seç → arenaya gir
2. HAYATTA KAL → Dalga dalga düşmanlar, XP + element topla
3. SEVİYE ATLA → Element seç, elementleri birleştirerek büyü yarat/güçlendir
4. BOSS SAVAŞI → Her 5 dakikada mitolojik boss
5. RUN BİTİŞİ → Ölüm veya 30dk tamamla → Altın + Ruh Taşı kazan
6. META PROGRESSION → Kalıcı güçlendirmeler, yeni kahraman/sınıf aç
7. TEKRAR → Farklı build dene
```

- Tek run süresi: 15-30 dakika (mobil oturumlarına uygun)
- Tüm kahramanları açma: ~6-8 hafta (F2P), hemen (IAP)

---

## 4. Element & Büyü Crafting Sistemi

### 4.1 Temel Elementler (Türk Kozmolojisi)

| Element | Ad | Renk | Tema |
|---------|----|------|------|
| Ateş | Od | Kırmızı | Hasar, yanma, patlama |
| Su | Sub | Mavi | Yavaşlatma, donma, iyileşme |
| Toprak | Yer | Kahverengi | Savunma, zırh, itme |
| Hava | Yel | Beyaz | Hız, menzil, zincirleme |
| Demir | Temür | Gri | Keskin hasar, delme, kritik |

### 4.2 Crafting Kuralları

**İki ayrı alan:**
- **Element Envanteri** (max 3 slot): Toplanan ham elementler buraya gider. Büyü slotu **kaplamaz**.
- **Büyü Slotları** (max 6 slot): Oluşan büyüler buraya yerleşir.

**Crafting Akışı:**
1. Seviye atlayınca 3 rastgele element sunulur + 1 ücretsiz re-roll hakkı (3 yeni seçenek)
2. Seçilen element → Element Envanterine gider
3. Envanterde uyumlu çift varsa → otomatik büyü oluşur → Büyü Slotuna taşınır
4. Eğer seçilen element mevcut bir büyüyle eşleşiyorsa (aynı reçete) → büyü slotuna gitmez, mevcut büyü +1 seviye atlar
5. Envanter doluysa (3 element, hiçbiri eşleşmiyor) → sonraki level-up'ta ek seçenek: "Envanterdeki 1 elementi at"

**Büyü Kuralları:**
- Aynı elementten 2 tane = **Temel Büyü** (5 adet)
- Farklı 2 element = **Birleşik Büyü** (10 kombinasyon)
- Aynı büyünün reçetesini tekrar tamamlama = büyü seviye atlar (max seviye 5)
- Run başına max 6 aktif büyü slotu

**Seviye Atlama Ekranı UI:**
- Seçim ekranında her element seçeneğinin yanında **recipe tooltip** gösterilir
- Tooltip, envanterdeki elementlerle olası birleşimi gösterir: "Bu elementi seçersen → [Büyü Adı] oluşacak"
- Eğer birleşim yoksa: "Envantere eklenir" mesajı
- Mevcut büyüyü güçlendiriyorsa: "Alev Halkası Seviye 3 → 4" mesajı

### 4.3 Büyü Tablosu

| Kombinasyon | Büyü Adı | Etki |
|-------------|----------|------|
| Od + Od | Alev Halkası | Etrafında dönen ateş çemberi |
| Sub + Sub | Şifa Pınarı | Pasif HP yenileme |
| Yer + Yer | Kaya Kalkanı | Orbiting kalkan taşları |
| Yel + Yel | Rüzgar Koşusu | Hareket hızı boost + iz bırakan hasar |
| Temür + Temür | Demir Yağmuru | Rastgele düşen demir parçaları |
| Od + Temür | Kılıç Fırtınası | Yanan kılıçlar fırlatır |
| Sub + Yel | Buz Rüzgarı | Koni şeklinde dondurucu dalga |
| Yel + Temür | Ok Yağmuru | Rastgele düşen ok sağanağı |
| Od + Sub | Buhar Patlaması | Yakın alan hasar + görüş engeli |
| Yer + Temür | Deprem | Zemin çatlar, AoE hasar |
| Od + Yel | Ateş Kasırgası | Hareket eden alev girdabı |
| Yer + Sub | Bataklık | Yerdeki alan düşmanları yavaşlatır |
| Od + Yer | Lav Seli | Yerde ilerleyen lav izi |
| Sub + Temür | Buz Kılıcı | Donduran yakın menzil darbeler |
| Yer + Yel | Kum Fırtınası | Geniş alan yavaşlatma + hasar |

### 4.4 Başlangıç Elementi Mekaniği

Her kahramanın bir "Başlangıç Elementi" vardır. Bu şu şekilde çalışır:
- Run başladığında, kahraman otomatik olarak başlangıç elementinden 1 kopya alır
- İlk seviye atlayışta aynı elementi seçmek hemen bir Temel Büyü yaratır (2 tur beklemek yerine 1 tur)
- Başlangıç elementi, seviye atlama seçeneklerinde +15% daha sık görünür
- Umay (Sub + Yer): Run başında her iki elementten 1'er kopya alır — ilk seviye atlayışta ikisinden birini seçerek hemen büyü yaratabilir

### 4.5 XP & Seviye Atlama Sistemi

**XP Kaynakları (düşman tipine göre):**

| Düşman | XP |
|--------|----|
| Kara Kurtlar | 1 |
| Yek Uşakları | 3 |
| Albastılar | 5 |
| Çor'lar | 4 (bölünen yarılar: 2) |
| Demirci Cinleri | 8 |
| Göl Aynası | 6 (kopyalar: 0) |
| Elite (herhangi) | Normal × 3 |

**Seviye Atlama Formülü:**
```
XP_gerekli = 10 × seviye^1.4
```

**Beklenen Seviye İlerlemesi:**

| Dakika | Beklenen Seviye | Toplam Büyü Slotu Kullanımı |
|--------|----------------|----------------------------|
| 2 | 5 | 2-3 büyü |
| 5 | 10 | 4-5 büyü |
| 10 | 16 | 6 büyü (dolu), güçlendirmeye başla |
| 20 | 24 | 6 büyü, çoğu seviye 3-4 |
| 30 | 30-32 | 6 büyü, 2-3 tanesi max seviye |

### 4.6 Savaş Formülleri

**Oyuncu Temel Değerler:**
- Başlangıç HP: 100
- Başlangıç hareket hızı: 3.0 birim/sn

**Büyü Hasarı:**
```
büyü_hasar = temel_hasar × (1 + 0.25 × (büyü_seviyesi - 1)) × element_çarpan × sınıf_bonus
```
- `temel_hasar`: Büyüye göre değişir (ScriptableObject'te tanımlanır, aralık: 5-25)
- `element_çarpan`: Zayıflık = 1.5×, Normal = 1.0×, Direnç = 0.6×
- `sınıf_bonus`: İlgili sınıf bonusu varsa 1.25×, yoksa 1.0×

**Düşman HP Ölçeklendirmesi:**
```
düşman_hp = temel_hp × (1 + 0.12 × dakika)
```
- Kara Kurt temel HP: 8
- Yek Uşağı temel HP: 25
- Albastı temel HP: 15
- Çor temel HP: 20 (yarılar: 10)
- Demirci Cin temel HP: 40
- Göl Aynası temel HP: 12 (kopyalar: 6)

**Düşman Hasarı (oyuncuya):**
```
düşman_hasar = temel_temas_hasar × (1 + 0.08 × dakika)
```
- Temas hasarı aralığı: 5-15 (düşman tipine göre)

### 4.7 Evrim Sistemi (POST-MVP)

> **Not:** Bu sistem MVP kapsamında değildir. İlk güncelleme ile eklenecektir.

- Bir büyü max seviyeye (5) ulaşınca + belirli bir ikinci büyü de max seviyede = **Evrim Büyüsü**
- Passive item sistemi yerine, iki max seviye büyünün birleşmesi olarak çalışır
- Örnek: Max Alev Halkası + Max Kılıç Fırtınası = **Ergenekon Ateşi** (ekranı kaplayan mega alev)
- Run başına 1-2 evrim gerçekçi → tekrar oynama motivasyonu
- Evrim olduğunda 2 büyü slotu 1'e düşer (6 → 5 slot) ama güç çok artar

---

## 5. Kahramanlar & Sınıf Sistemi

### 5.1 Kahramanlar

| Kahraman | İlham | Başlangıç Elementi | Açılma |
|----------|-------|---------------------|--------|
| **Korkut** | Dede Korkut'un genç çırağı | Yel (Hava) | Ücretsiz (başlangıç) |
| **Ay Kağan** | Oğuz Kağan destanı | Od (Ateş) | 5.000 altın |
| **Börte** | Kurt ana, Ergenekon | Temür (Demir) | 8.000 altın |
| **Ayzıt** | Güzellik/bereket tanrıçası | Sub (Su) | 12.000 altın |
| **Alp Er Tunga** | Efsanevi savaşçı kağan | Yer (Toprak) | 50 ruh taşı |
| **Umay** | Ana tanrıça, koruyucu | Sub + Yer | 100 ruh taşı |

MVP'de 4 kahraman (Korkut + Ay Kağan + Börte + Ayzıt). Alp Er Tunga ve Umay post-MVP güncellemesiyle.

### 5.2 Sınıflar

Sınıf bonusu zıt/tamamlayıcı elementler üzerinden tanımlanır — aynı element stack'lemek yerine farklı elementleri birleştirmek optimal olur.

| Sınıf | Element Bonusu | Etkilenen Büyüler | Pasif | Etkileşim |
|-------|---------------|-------------------|-------|-----------|
| **Kam** (Şaman) | Birleşik büyüler +15% etki | Tüm 10 birleşik büyü (farklı 2 element) | Büyü seçiminde 4. seçenek görür | Çeşitlilik ödüllendirir, ama bonus daha düşük |
| **Batur** (Savaşçı) | Sub veya Yer içeren büyüler +25% | Şifa Pınarı, Kaya Kalkanı, Buz Rüzgarı, Bataklık, Buhar Patlaması, Deprem, Lav Seli, Buz Kılıcı, Kum Fırtınası | Yakın hasara %10 azaltma | Agresif karakterlere savunma derinliği |
| **Mergen** (Okçu) | Od veya Temür içeren büyüler +25% | Alev Halkası, Demir Yağmuru, Kılıç Fırtınası, Ok Yağmuru, Buhar Patlaması, Deprem, Ateş Kasırgası, Lav Seli, Buz Kılıcı | Hareket hızı +10% | Destek karakterlere saldırı gücü |
| **Otacı** (Şifacı) | Yel veya Od içeren büyüler +25% | Alev Halkası, Rüzgar Koşusu, Kılıç Fırtınası, Buz Rüzgarı, Ok Yağmuru, Buhar Patlaması, Ateş Kasırgası, Lav Seli, Kum Fırtınası | Her 30sn HP yenileme | Savunmacı karakterlere ofansif seçenek |

Başlangıçta 2 sınıf açık (Kam, Batur). Diğerleri meta-progression ile açılır.

**Bonus kuralı:** Bir büyünün reçetesinde belirtilen elementlerden en az biri varsa, sınıf bonusu uygulanır. Kam bonusu sadece birleşik büyülere (iki farklı element) uygulanır, temel büyülere (aynı element ×2) uygulanmaz.

### 5.3 Tasarım Kuralı

Kahraman + sınıf kombinasyonu zıt güçler verdiğinde optimal → oyuncu farklı combo'ları denemeye teşvik edilir:
- Börte (Demir) + Batur = Su/Toprak büyüleri güçlenir → agresife savunma derinliği
- Korkut (Hava) + Mergen = Ateş/Demir büyüleri güçlenir → desteğe saldırı gücü
- Ayzıt (Su) + Otacı = Hava/Ateş büyüleri güçlenir → iyileştiriciye ofans
- Ay Kağan (Ateş) + Batur = Su/Toprak büyüleri güçlenir → ateşe su/toprak dengesi

---

## 6. Düşmanlar & Boss Sistemi

### 6.1 Normal Düşmanlar

| Düşman | İlham | Davranış | İlk Görülme | Zayıflık | Direnç |
|--------|-------|----------|-------------|----------|--------|
| Kara Kurtlar | Kötü ruhların kurt formu | Sürü halinde koşar, hızlı ama zayıf | Dk 0 | Od | Yel |
| Yek Uşakları | Yeraltı kötü ruhları | Yavaş, dayanıklı, grupça iter | Dk 2 | Yel | Temür |
| Albastılar | Kötü dişi ruhlar | Menzilli saldırı, kaçarak ateş | Dk 5 | Temür | Od |
| Çor'lar | Bozulmuş toprak ruhları | Öldüğünde ikiye bölünür | Dk 8 | Temür | Sub |
| Demirci Cinleri | Yeraltı demircileri | Zırhlı, sadece büyüyle hasar alır | Dk 12 | Sub | Yer |
| Göl Aynası | Su perileri | Klonlanır, sahte kopyalar üretir | Dk 18 | Od | Sub |

Her düşman tipi belirli elementlere karşı zayıf/güçlü → "her element işe yarar" dengesi.

### 6.2 Boss'lar (Her 5 Dakikada)

| Dakika | Boss | İlham | Mekanik |
|--------|------|-------|---------|
| 5:00 | **Tepegöz** | Tek gözlü dev | Yavaş döner, ön tarafından (göz yönü) gelen hasara 2× alır. Oyuncu doğru yay içinde pozisyon almalı |
| 10:00 | **Yer Tanrısı** | Toprak ruhu | Zemine çatlaklar açar, güvenli alan daralır |
| 15:00 | **Erlik Han'ın Elçisi** | Yeraltı habercisi | Büyüleri geçici kilitler, fiziksel kaçınma gerekir |
| 20:00 | **Boz Ejderha** | Türk ejderha miti | Uçar, dalış saldırısı, ateş nefesi |
| 25:00 | **Erlik Han** | Yeraltı tanrısı | Final boss, tüm önceki boss mekaniklerini karıştırır |

### 6.3 Boss Tasarım Kuralları

- Her boss bir oyuncu becerisini test eder (konumlanma, zamanlama, build uyumu)
- Boss'lar element zayıflığı olmaz — her build ile yenilebilir, farklı zorlukta
- İlk yenişte "Destan Sayfası" verir (koleksiyon + lore)

### 6.4 Elite Düşmanlar (Dk 10+)

- Normal düşmanların güçlü versiyonları, altın aura ile belirgin
- Daha fazla XP + garanti element drop
- Risk/ödül kararı: kovalamak mı, kaçmak mı?

---

## 7. Meta-Progression & Ekonomi

### 7.1 Para Birimleri

| Para Birimi | Kazanım | Harcama |
|-------------|---------|---------|
| **Altın** | Her run sonu, elite düşmanlardan | Kalıcı güçlendirmeler, ilk kahramanlar |
| **Ruh Taşı** (Premium) | Boss ilk yeniş, başarımlar, nadir drop, IAP | Premium kahramanlar, kozmetikler |

### 7.2 Kalıcı Güçlendirmeler (Altın)

| Yükseltme | Max Seviye | Etki | Temel Maliyet | Maliyet Formülü | Max'a Toplam |
|-----------|-----------|------|---------------|-----------------|-------------|
| Sağlık | 20 | +5% HP / seviye | 200 | 200 × seviye | 42.000 |
| Güç | 20 | +3% tüm hasar | 200 | 200 × seviye | 42.000 |
| Hız | 10 | +2% hareket hızı | 300 | 300 × seviye^1.3 | 7.600 |
| Mıknatıs | 10 | +5% toplama menzili | 150 | 150 × seviye | 8.250 |
| Şans | 10 | +2% nadir element bulma | 400 | 400 × seviye^1.3 | 10.100 |
| Element Affinite | 5×5 | Belirli element büyüleri +4% | 500 | 500 × seviye^1.5 | 27.900 |

**Toplam altın tüm yükseltmeleri max'lamak için:** ~138.000 altın

### 7.2.1 Ekonomi Sanity Check

Ortalama bir run (20dk hayatta kalma, 600 kill, 3 boss):
```
Altın = (20 × 10) + (600 × 0.5) + (3 × 100) = 200 + 300 + 300 = 800 altın/run
```

İyi bir run (30dk, 1200 kill, 5 boss):
```
Altın = (30 × 10) + (1200 × 0.5) + (5 × 100) = 300 + 600 + 500 = 1.400 altın/run
```

Günde 2 run ortalamasıyla (~1.100 altın/gün):
- İlk kahraman açma (5.000): ~5 gün
- İkinci kahraman (8.000): ~7 gün daha
- Üçüncü kahraman (12.000): ~11 gün daha
- Tüm yükseltmeler max: ~125 gün (~4 ay)
- **Tüm kahramanlar + tüm yükseltmeler: ~6-8 hafta kahraman, ~4 ay tam max**

### 7.3 Destan Koleksiyonu

- Her boss ilk yenildiğinde "Destan Sayfası" açılır
- Her kahraman + sınıf combo'suyla run bitirme → "Destan Damgası"
- Koleksiyonu doldurmak küçük ama kalıcı bonuslar verir
- Tamamlamacı oyuncular için uzun vadeli hedef

### 7.4 IAP Mağazası

| Ürün | Fiyat | İçerik |
|------|-------|--------|
| Küçük Ruh Taşı | ~$0.99 | 50 ruh taşı |
| Orta Ruh Taşı | ~$4.99 | 300 ruh taşı |
| Büyük Ruh Taşı | ~$9.99 | 700 ruh taşı |
| Şaman Paketi (tek seferlik) | ~$2.99 | 1 kahraman + 200 ruh taşı |
| Kağan Paketi (tek seferlik) | ~$9.99 | 2 kahraman + 500 ruh taşı + kozmetik |
| Sezon Geçişi | ~$4.99/ay | Günlük ruh taşı + özel kozmetik yolu |

### 7.5 Adil Denge Kuralı

- Ruh taşıyla satın alınan hiçbir şey güç avantajı vermez
- Kahramanlar farklı deneyim sunar ama birbirinden "güçlü" değildir
- Tüm kahramanlar altınla da açılabilir (sadece daha uzun sürer)
- Kozmetikler: alternatif pixel art skin'ler, özel ölüm efektleri, büyü renk varyasyonları

### 7.6 Run Sonu Ödül Formülü

```
Altın = (hayatta_kalınan_dakika × 10) + (öldürülen_düşman × 0.5) + (boss_yenilen × 100)
Ruh Taşı = boss_ilk_yeniş (5 adet) + başarım (değişken) + nadir_drop (~%2 şans)
```

---

## 8. Teknik Mimari

### 8.1 Motor & Dil

- **Motor:** Unity 2D (URP)
- **Dil:** C#
- **Input:** Unity New Input System (joystick + touch)
- **Rendering:** URP 2D (pixel art + ışık efektleri)

### 8.2 Proje Yapısı

```
src/
├── Core/                  # DI tabanlı çekirdek
│   ├── GameManager.cs     # Run durumu, dalga yönetimi
│   ├── EventBus.cs        # Gevşek bağlı event sistemi
│   └── ObjectPool.cs      # Düşman/mermi havuzu
├── Gameplay/
│   ├── Player/            # Hareket, sağlık, element toplama
│   ├── Enemies/           # Spawner, düşman tipleri, davranışlar
│   ├── Spells/            # Büyü sistemi, element crafting
│   ├── Boss/              # Boss AI, faz sistemi
│   └── Waves/             # Dalga konfigürasyonu (data-driven)
├── Meta/
│   ├── Progression/       # Kalıcı yükseltmeler, kahraman açma
│   ├── Economy/           # Altın/ruh taşı yönetimi
│   ├── Collection/        # Destan sayfaları, başarımlar
│   └── SaveSystem/        # Yerel kayıt + cloud save
├── UI/
│   ├── HUD/               # Run içi: HP, element, büyü slotları
│   ├── Menus/             # Ana menü, kahraman seçimi, mağaza
│   └── Popups/            # Seviye atlama, run sonu, IAP
└── Data/
    ├── ScriptableObjects/ # Tüm balance değerleri SO olarak
    ├── Enemies/           # Düşman tanımları
    └── Spells/            # Büyü tanımları, element matrisi
```

### 8.3 Kritik Teknik Kararlar

| Karar | Seçim | Neden |
|-------|-------|-------|
| Düşman yönetimi | Object Pooling | Yüzlerce düşman → GC spike önleme |
| Balance değerleri | ScriptableObjects | Kod değiştirmeden değer ayarlama |
| Event sistemi | EventBus pattern | Gevşek bağlantı, test edilebilirlik |
| Kayıt | JSON + şifreleme | Hile önleme, cloud backup hazırlığı |
| Input | New Input System | Joystick + touch, mobilde esnek |
| Rendering | URP 2D | Pixel art için yeterli, performanslı |

### 8.4 Performans Hedefleri

| Metrik | Hedef |
|--------|-------|
| FPS | 60 sabit (orta segment telefon) |
| Ekrandaki max düşman | 300+ |
| Bellek | < 512 MB |
| APK boyutu | < 150 MB |
| Pil tüketimi | 30dk run = max %10 pil |

---

## 9. MVP Kapsam (3-4 Ay)

| Ay | Hedef |
|----|-------|
| Ay 1 | Core loop: hareket, düşman spawn, element toplama, temel büyü sistemi |
| Ay 2 | Büyü crafting, 4 düşman tipi, 3 boss (Tepegöz, Yer Tanrısı, Erlik Han'ın Elçisi), seviye atlama UI |
| Ay 3 | Meta-progression, 4 kahraman (Korkut + 3), 2 sınıf (Kam, Batur), ana menü, run sonu ekranı |
| Ay 4 | Polish, balans, IAP entegrasyonu, mobil optimizasyon, analytics/crash reporting, soft launch |

**MVP'den ertelenen öğeler (post-launch güncelleme):**
- Boz Ejderha ve Erlik Han boss'ları (4. ve 5. boss)
- Alp Er Tunga ve Umay kahramanları
- Mergen ve Otacı sınıfları
- Evrim sistemi (Section 4.7)
- Göl Aynası ve Demirci Cinleri düşman tipleri (son 2)
- Cloud save
- Sezon Geçişi IAP
- Daily Quests sistemi (retention için kritik — post-MVP 1. öncelik)

---

## 10. Edge Cases

- **Element envanter dolu (3/3) + yeni element:** Sonraki level-up'ta ek seçenek sunulur: "Envanterdeki 1 elementi at." Oyuncu isterse re-roll kullanarak farklı element arayabilir
- **Büyü slotları dolu (6/6) + yeni büyü oluşacak:** Büyü oluşmaz, element envanterde kalır. Oyuncuya bilgi: "Büyü slotların dolu." Mevcut büyüyü güçlendirme seçeneği sunulur (aynı reçeteli element çifti = +1 seviye)
- **Slotlar dolu + sunulan hiçbir element mevcut büyüyü güçlendiremez:** 1 kez re-roll hakkı (zaten her level-up'ta var). Hala eşleşme yoksa küçük stat boost sunulur (+3% HP, +2% hız, veya +2% hasar — oyuncu seçer)
- **Tüm boss'lar yenildi ama 30dk dolmadı:** Sonsuz dalga modu, artan zorluk, bonus altın. Erlik Han yenildiğinde özel "Destan Tamamlandı" mesajı gösterilir ama run devam eder
- **Erlik Han (final boss) yenildikten sonra:** "Kahraman Modu" aktif — düşman spawn hızı ve HP'si %50 artar, ekstra altın çarpanı 1.5×. Run 30dk'da biter veya oyuncu ölür
- **Oyuncu AFK kalırsa:** 10sn hareketsizlik → otomatik yavaş hareket (mobilde pil koruma)
- **İnternet bağlantısı yok:** Tüm core gameplay offline çalışır, IAP ve cloud save bağlantı gerektirir
- **Başlangıç elementi + envanter etkileşimi:** Başlangıç elementi run başında envantere otomatik eklenir (1/3 slot dolu başlar). Umay'da 2/3 slot dolu başlar

---

## 11. Dependencies

- Unity 2D (URP) — 2022.3 LTS veya üzeri
- Unity New Input System
- Unity IAP package
- TextMeshPro (UI)
- Pixel art asset pipeline (Aseprite → Unity)
- Cloud save: Unity Cloud Save veya custom backend (post-MVP)

---

## 12. Tuning Knobs

| Parametre | Varsayılan | Aralık | Dosya |
|-----------|-----------|--------|-------|
| Run süresi | 30 dk | 15-45 dk | WaveConfig.asset |
| Boss spawn aralığı | 5 dk | 3-10 dk | WaveConfig.asset |
| Element seçenek sayısı | 3 | 2-5 | LevelUpConfig.asset |
| Max büyü slotu | 6 | 4-8 | PlayerConfig.asset |
| Max büyü seviyesi | 5 | 3-7 | SpellConfig.asset |
| Düşman spawn hızı artışı | +10%/dk | +5-20%/dk | WaveConfig.asset |
| Altın çarpanı | 1.0x | 0.5-2.0x | EconomyConfig.asset |
| XP eğrisi | Üstel | Doğrusal/Üstel | ProgressionConfig.asset |

---

## 13. Acceptance Criteria

- [ ] Oyuncu arenada hareket edebilir, düşmanlar spawn olur ve hasar alır/verir
- [ ] Element toplama ve büyü crafting çalışır (15 büyü kombinasyonu)
- [ ] Seviye atlama ekranında element seçimi yapılabilir
- [ ] 3 boss (Tepegöz, Yer Tanrısı, Erlik Han'ın Elçisi) her 5dk'da spawn olur, benzersiz mekanikleri çalışır
- [ ] Run sonu ekranı altın/ruh taşı gösterir ve kalıcı olarak kaydeder
- [ ] Meta-progression: güçlendirmeler satın alınabilir ve run'a etki eder
- [ ] 4 kahraman (Korkut, Ay Kağan, Börte, Ayzıt) ve 2 sınıf (Kam, Batur) seçilebilir
- [ ] 60 FPS, 300+ düşman ekranda, orta segment mobilde
- [ ] IAP mağazası çalışır (test modunda)
- [ ] Oyun tamamen offline çalışır (IAP hariç)
