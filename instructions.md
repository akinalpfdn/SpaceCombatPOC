ADIM 1: WeaponSlotBar Ekle (Alt Orta - 4 Weapon Butonu)
1.1 Parent Container Oluştur
Hierarchy'de MobileControlsCanvas'a sağ tık
UI → Empty Object seç
Adını WeaponSlotBar yap
1.2 WeaponSlotBar Pozisyonu Ayarla
Inspector'da:


Rect Transform:
├── Anchor Preset: Alt ortadaki kareye tıkla (bottom-center)
├── Pivot: X=0.5, Y=0
├── Pos X: 0
├── Pos Y: 50 (alttan biraz yukarı)
├── Width: 500
└── Height: 100
1.3 Horizontal Layout Group Ekle
WeaponSlotBar seçiliyken Add Component
Horizontal Layout Group ara ve ekle
Ayarları:

Horizontal Layout Group:
├── Spacing: 15
├── Child Alignment: Middle Center
├── Child Force Expand Width: ☐ (kapalı)
└── Child Force Expand Height: ☐ (kapalı)
1.4 WeaponSlotBar Script Ekle
WeaponSlotBar seçiliyken Add Component
WeaponSlotBar ara ve ekle (SpaceCombat.UI.Mobile)
1.5 İlk Slot Butonunu Oluştur
WeaponSlotBar'a sağ tık → UI → Button - TextMeshPro
Adını Slot1 yap
Inspector'da Rect Transform:

Width: 100
Height: 80
Button component'ında:

Transition: Color Tint
Normal Color: (10, 22, 40, 200) → Koyu mavi
Highlighted: (0, 100, 120, 220)
Pressed: (0, 60, 80, 255)
Child olan Text (TMP) seç:

Text: "1"
Font Size: 36
Alignment: Center
Color: Cyan (#00D4FF)
1.6 WeaponSlotButton Script Ekle
Slot1 seçiliyken Add Component
WeaponSlotButton ara ve ekle
Inspector'da:

WeaponSlotButton:
├── Slot Index: 0
├── Slot Number Text: (Text TMP child'ını sürükle)
└── Background: (Slot1'in kendi Image'ını sürükle)
1.7 Slot1'i 3 Kere Kopyala
Slot1'e sağ tık → Duplicate (3 kere)
İsimleri değiştir: Slot2, Slot3, Slot4
Her birinin WeaponSlotButton script'inde:
Slot2 → Slot Index: 1, Text: "2"
Slot3 → Slot Index: 2, Text: "3"
Slot4 → Slot Index: 3, Text: "4"
1.8 WeaponSlotBar Referanslarını Bağla
WeaponSlotBar objesini seç
Inspector'da WeaponSlotBar script'inde:

Slots (Array):
├── Element 0: Slot1
├── Element 1: Slot2
├── Element 2: Slot3
└── Element 3: Slot4
ADIM 2: MobileHealthBar Ekle (Sol Üst)
2.1 Parent Container Oluştur
MobileControlsCanvas'a sağ tık → UI → Empty Object
Adını StatusBars yap
2.2 StatusBars Pozisyonu
Inspector'da:


Rect Transform:
├── Anchor Preset: Sol üst (top-left)
├── Pivot: X=0, Y=1
├── Pos X: 20
├── Pos Y: -20
├── Width: 250
└── Height: 80
2.3 Vertical Layout Group Ekle
Add Component → Vertical Layout Group

Spacing: 8
Child Alignment: Upper Left
Child Force Expand: Width ☑, Height ☐
2.4 Health Bar Oluştur
StatusBars'a sağ tık → UI → Image
Adını HealthBarBG yap
Rect Transform:

Width: 200
Height: 25
Image component:

Color: (30, 30, 30, 200) → Koyu gri arka plan
2.5 Health Bar Fill Ekle
HealthBarBG'ye sağ tık → UI → Image
Adını HealthFill yap
Rect Transform - Stretch anchor (her yöne yapışık):

Anchor: Stretch-Stretch (sağ alttaki preset)
Left: 2, Right: 2, Top: 2, Bottom: 2
Image component:

Color: (255, 68, 68, 255) → Kırmızı (#FF4444)
Image Type: Filled
Fill Method: Horizontal
Fill Origin: Left
Fill Amount: 1 (kod kontrol edecek)
2.6 Health Text Ekle (Opsiyonel)
HealthBarBG'ye sağ tık → UI → Text - TextMeshPro
Adını HealthText yap
Rect Transform: Stretch (parent'a sığ)
Text ayarları:

Text: "10,000 / 10,000"
Font Size: 14
Alignment: Center
Color: White
2.7 Shield Bar Oluştur (Health Bar'ı Kopyala)
HealthBarBG'ye sağ tık → Duplicate
Adını ShieldBarBG yap
Child HealthFill'in adını ShieldFill yap
Child HealthText'in adını ShieldText yap
ShieldFill Image color: (68, 136, 255, 255) → Mavi (#4488FF)
ShieldText: "10,000 / 10,000"
2.8 MobileHealthBar Script Ekle
StatusBars objesini seç
Add Component → MobileHealthBar
Referansları bağla:

MobileHealthBar:
├── Health Fill: HealthFill (Image)
├── Health Text: HealthText (TMP)
├── Shield Fill: ShieldFill (Image)
└── Shield Text: ShieldText (TMP)
ADIM 3: MobileInputManager'a WeaponSlotBar Bağla
Şu an MobileInputManager sadece Joystick ve AttackButton'ı yönetiyor. WeaponSlotBar'ı da bağlamak için:

Sahnede MobileInputManager objesini bul
Inspector'da script'i gör
Şu anki referanslar zaten var:
Joystick ✓
Attack Button ✓
WeaponSlotBar'ı da initialize etmek için MobileInputManager.cs'e ekleme yapmam gerekiyor
Bekle, ben bunu ekleyeyim:


ADIM 3 (Devamı): MobileInputManager'a Referans Bağla
Sahnede MobileInputManager objesini bul
Inspector'da MobileInputManager script'ini gör
Yeni eklenen Weapon Slot Bar alanına WeaponSlotBar objesini sürükle

MobileInputManager (Script):
├── Joystick: JoystickBase ✓ (zaten var)
├── Ship Movement: (boş bırak, otomatik bulunur)
├── Mobile Controls Canvas: MobileControlsCanvas ✓
├── Attack Button: AttackButton ✓
├── Weapon Slot Bar: WeaponSlotBar ← YENİ EKLE
└── Enable In Editor: ☑
ÖZET - Final Hierarchy

MobileControlsCanvas
├── JoystickBase (VirtualJoystick)
│   └── JoystickHandle
├── AttackButton (AttackButton script)
├── WeaponSlotBar (WeaponSlotBar script + Horizontal Layout)
│   ├── Slot1 (WeaponSlotButton, index=0)
│   ├── Slot2 (WeaponSlotButton, index=1)
│   ├── Slot3 (WeaponSlotButton, index=2)
│   └── Slot4 (WeaponSlotButton, index=3)
└── StatusBars (MobileHealthBar script + Vertical Layout)
    ├── HealthBarBG
    │   ├── HealthFill (Image, Filled)
    │   └── HealthText (TMP)
    └── ShieldBarBG
        ├── ShieldFill (Image, Filled)
        └── ShieldText (TMP)

(Ayrı obje - sahnede herhangi bir yerde)
MobileInputManager
├── Joystick → JoystickBase
├── Attack Button → AttackButton
├── Weapon Slot Bar → WeaponSlotBar
└── Mobile Controls Canvas → MobileControlsCanvas
Test
Oyunu başlat
Sol alt: Joystick ile hareket et
Sağ alt: Attack butonu ile saldır
Alt orta: 1-2-3-4 butonlarına tıkla, weapon değişmeli
Sol üst: HP/Shield bar'lar görünmeli ve hasar alınca güncelenmeli
