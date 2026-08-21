# 🧵 Tailor Shop — Customer Management System

## Fresh System Par Install Kya Karna Hoga?

### Step 1 — .NET 8 Runtime Install Karein
1. Is link par jain: https://dotnet.microsoft.com/download/dotnet/8.0
2. "Windows" section mein "Run desktop apps" ke neeche
   → ".NET Desktop Runtime 8.0.x" download karein (x64)
3. Download hone ke baad install karein (Next > Next > Finish)

### Step 2 — App Chalain
1. TailorShop.exe par double-click karein
2. Pehli baar chalane par automatically database file ban jaegi

---

## App Ki Features

| Feature | Detail |
|---------|--------|
| Customer Add | Naam, Phone, Address save hoga |
| Measurements | Shirt, Shalwar Kameez, Pant, Coat/Sherwani |
| Search | Naam ya phone se dhundh sakte hain |
| Edit | Purani measurements update kar sakte hain |
| Delete | Customer record mita sakte hain |
| Offline | Internet ki zaroorat nahi |

## Measurements Save Hoti Hain

**Shirt:** Chest, Shoulder, Length, Sleeve, Neck

**Shalwar Kameez:** Kameez Length, Chest, Waist, Hip, Shalwar Length, Paincha

**Pant/Trouser:** Length, Waist, Hip, Thigh, Knee, Bottom

**Coat/Sherwani:** Length, Chest, Shoulder, Sleeve

---

## Build Karne Ka Tarika (Developer Ke Liye)

```bash
# .NET 8 SDK install ho
cd TailorShop
dotnet build
dotnet run
```

Ya publish (single .exe):
```bash
dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true
```

---

**Database:** `tailor_shop.db` — same folder mein save hoti hai.
Backup ke liye sirf yeh file copy kar lein! 💾
