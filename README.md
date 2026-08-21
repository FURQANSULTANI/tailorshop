# 🧵 TopStitch Tailor — Customer Management System

---

## English

### What Do You Need To Install On A Fresh System?

#### Step 1 — Install the .NET 8 Runtime
1. Go to: https://dotnet.microsoft.com/download/dotnet/8.0
2. Under the "Windows" section, find "Run desktop apps"
   → Download ".NET Desktop Runtime 8.0.x" (x64)
3. Once downloaded, install it (Next > Next > Finish)

#### Step 2 — Run the App
1. Double-click `TailorShop.exe`
2. On first run, the database file is created automatically

---

### App Features

| Feature | Detail |
|---------|--------|
| Add Customer | Saves Name, Phone, Address |
| Measurements | Shirt, Shalwar Kameez, Pant, Coat/Sherwani |
| Search | Search by name or phone |
| Edit | Update existing measurements |
| Delete | Delete a customer record |
| Offline | No internet connection required |

### Measurements Saved

**Shirt:** Chest, Shoulder, Length, Sleeve, Neck

**Shalwar Kameez:** Kameez Length, Chest, Waist, Hip, Shalwar Length, Paincha

**Pant/Trouser:** Length, Waist, Hip, Thigh, Knee, Bottom

**Coat/Sherwani:** Length, Chest, Shoulder, Sleeve

---

### How To Build (For Developers)

```bash
# .NET 8 SDK must be installed
cd TailorShop
dotnet build
dotnet run
```

Or publish as a single .exe:
```bash
dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true
```

---

**Database:** `golden_tailor.db` — saved in the same folder as the app.
For backup, just copy this file! 💾

---
---

## Roman Urdu

### Fresh System Par Install Kya Karna Hoga?

#### Step 1 — .NET 8 Runtime Install Karein
1. Is link par jain: https://dotnet.microsoft.com/download/dotnet/8.0
2. "Windows" section mein "Run desktop apps" ke neeche
   → ".NET Desktop Runtime 8.0.x" download karein (x64)
3. Download hone ke baad install karein (Next > Next > Finish)

#### Step 2 — App Chalain
1. TailorShop.exe par double-click karein
2. Pehli baar chalane par automatically database file ban jaegi

---

### App Ki Features

| Feature | Detail |
|---------|--------|
| Customer Add | Naam, Phone, Address save hoga |
| Measurements | Shirt, Shalwar Kameez, Pant, Coat/Sherwani |
| Search | Naam ya phone se dhundh sakte hain |
| Edit | Purani measurements update kar sakte hain |
| Delete | Customer record mita sakte hain |
| Offline | Internet ki zaroorat nahi |

### Measurements Save Hoti Hain

**Shirt:** Chest, Shoulder, Length, Sleeve, Neck

**Shalwar Kameez:** Kameez Length, Chest, Waist, Hip, Shalwar Length, Paincha

**Pant/Trouser:** Length, Waist, Hip, Thigh, Knee, Bottom

**Coat/Sherwani:** Length, Chest, Shoulder, Sleeve

---

### Build Karne Ka Tarika (Developer Ke Liye)

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

**Database:** `golden_tailor.db` — same folder mein save hoti hai.
Backup ke liye sirf yeh file copy kar lein! 💾
