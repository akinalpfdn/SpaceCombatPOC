# Mobile Debug Changes - 2026-02-05 (Updated)

Bu dosya Android'de çalışmayan rotation ve audio sorunlarını düzeltmek için yapılan değişiklikleri içerir.

---

## 1. ShipMovement.cs - Rigidbody.MoveRotation Kullanımı (YENİ!)

**Dosya:** `Assets/Scripts/Movement/ShipMovement.cs`

**Problem:**
- `transform.rotation` kullanılıyordu ama Rigidbody bunu physics update'de override ediyordu
- Android'de sadece tilt değişiyordu, Y rotation hiç değişmiyordu

**Çözüm:**
- `transform.rotation` yerine `_rigidbody.MoveRotation()` kullanıldı
- `transform.rotation.eulerAngles.y` yerine `_rigidbody.rotation.eulerAngles.y` okunuyor

**Değiştirilen metodlar:**
1. `RotateTowards()` - Targeting modu için
2. `CalculateAndApplyRotation()` - Hareket bazlı rotation için
3. `SmoothTilt()` - Tilt smoothing için

**Örnek değişiklik:**
```csharp
// ÖNCE:
float currentY = transform.rotation.eulerAngles.y;
transform.rotation = Quaternion.Euler(0, newY, _currentTilt);

// SONRA:
float currentY = _rigidbody.rotation.eulerAngles.y;
Quaternion newRotation = Quaternion.Euler(0, newY, tiltToApply);
_rigidbody.MoveRotation(newRotation);
```

---

## 2. ShipMovement.cs - ApplyMovement Metodu

**Problem:** Targeting modunda bile `SmoothTilt()` çağrılıyor ve rotation'a müdahale ediyor

**Değişiklik (satır ~152-172):**
```csharp
// BEFORE:
if (_autoRotateEnabled && _rotateToMovement && _currentSpeed > _rotationThreshold)
{
    CalculateAndApplyRotation(moveDir2D);
}
else if (_enableBanking)
{
    _targetTilt = 0;
    SmoothTilt();  // Bu targeting modunda da çalışıyordu!
}

// AFTER:
if (_autoRotateEnabled)
{
    if (_rotateToMovement && _currentSpeed > _rotationThreshold)
    {
        CalculateAndApplyRotation(moveDir2D);
    }
    else if (_enableBanking)
    {
        _targetTilt = 0;
        SmoothTilt();
    }
}
// When auto-rotate is disabled, don't touch rotation at all
```

---

## 3. AudioManager.cs - PlaySFXClip Metodu

**Problem:** Android'de AudioClip'ler yüklenmemiş olabiliyor

**Değişiklik (satır ~111-155):**
```csharp
public void PlaySFXClip(AudioClip clip, ...)
{
    if (clip == null) return;

    // Android: Ensure clip is loaded
    if (clip.loadState == AudioDataLoadState.Unloaded)
    {
        clip.LoadAudioData();
    }

    if (clip.loadState != AudioDataLoadState.Loaded)
    {
        return;  // Skip if not ready
    }

    // ... rest of method ...

    // Use PlayOneShot - more reliable on mobile
    source.PlayOneShot(clip, finalVolume);
}
```

---

## Eğer Çalışmazsa Kontrol Edilecekler

1. **TargetSelector.cs satır 91:** Eğer `if (Mouse.current == null && Keyboard.current == null) return;` varsa, Android'de tüm Update atlıyor olabilir

2. **Script Execution Order:** MobileInputManager ve TargetSelector'ın çalışma sırası önemli olabilir

3. **Rigidbody constraints:** Y rotation freeze edilmiş olabilir

4. **Inspector değerleri:**
   - ShipConfig'de rotationSpeed > 0 mı?
   - AudioManager'da SoundLibrary atanmış mı?

---

## Revert Etmek İçin

Git ile revert:
```bash
git checkout -- Assets/Scripts/Movement/ShipMovement.cs
git checkout -- Assets/Scripts/Audio/AudioManager.cs
```
