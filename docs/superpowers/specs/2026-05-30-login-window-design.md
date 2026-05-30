# Login Window - Design Spec

> **For agentic workers:** Use superpowers:writing-plans to create implementation plan after this spec is approved.

**Goal:** Add a separate LoginWindow shown before MainWindow. User authenticates with username + password (hashed). App logo shown and configurable.

**Tech Stack:** WPF (.NET Framework 4.8), MVVM (Prism), existing Styles.xaml

---

## UI/UX Specification

### Window Behavior

- **Type:** Separate `Window` (not dialog, not integrated view)
- **Size:** 400×520 px, fixed (not resizable)
- **Position:** Center screen on launch
- **Flow:** LoginWindow shown first → authenticate → close LoginWindow → show MainWindow
- **Close button (X):** Closes app entirely (no MainWindow shown)
- **Startup:** Entry point changes from Bootstrapper → show LoginWindow → success → run Bootstrapper

### Visual Layout (top to bottom)

```
┌─────────────────────────────────────┐
│                                     │
│         [LOGO IMAGE]                │  ← 120×120 px, centered
│         SystemName                  │  ← from App.config "SystemName"
│         "欢迎登录"                   │  ← subtitle, gray text
│                                     │
│   ┌─────────────────────────────┐   │
│   │  👤  用户名                   │   │  ← TextBox with icon
│   └─────────────────────────────┘   │
│   ┌─────────────────────────────┐   │
│   │  🔒  密码          [👁]      │   │  ← PasswordBox with show/hide toggle
│   └─────────────────────────────┘   │
│                                     │
│        [ 登 录 ]                    │  ← Primary button, full width
│                                     │
│   ┌─────────────────────────────┐   │
│   │  错误提示信息                 │   │  ← Red text, hidden by default
│   └─────────────────────────────┘   │
│                                     │
│         Triple Detection v1.0        │  ← Footer, small gray text
└─────────────────────────────────────┘
```

### Colors & Typography

- **Background:** `#FFFFFF` (white)
- **Card/Input bg:** `#F5F5F5`
- **Primary accent:** `#4FD1C5` (teal, from existing Styles.xaml)
- **Error text:** `#E53E3E` (red)
- **Secondary text:** `#666666`
- **Font:** Segoe UI, system default
- **Title:** 20px bold, `#1A202C`
- **Subtitle:** 14px, `#666666`
- **Input labels:** 12px, `#666666`
- **Button:** 14px bold

### Components

**Logo display:**
- Image: `LoginLogoPath` from App.config (default: `Resources/logo.png`)
- Size: 120×120 px, centered
- Fallback: If image not found, show a text placeholder with system name initials
- The logo asset path should be easily replaceable

**Username TextBox:**
- Placeholder text: "请输入用户名"
- Icon: 👤 prefix (or small icon)
- Validation: red border if empty on submit

**Password PasswordBox:**
- Placeholder text: "请输入密码"
- Icon: 🔒 prefix
- Show/Hide toggle button (eye icon) on the right
- Validation: red border if empty on submit

**Login Button:**
- Full width within input area
- Teal background (`PrimaryBrush`)
- Text: "登 录"
- Loading state: text changes to "登录中..." with no double-click

**Error Message Area:**
- Hidden by default
- Shows on auth failure: "用户名或密码错误" (or "账号已被禁用" / "账号已被锁定")
- Red text, centered below button
- Clears when user starts typing again

### Interactions

| Action | Behavior |
|--------|----------|
| Click Login | Validate inputs → call UserService.Authenticate → success/failure handling |
| Press Enter (any field) | Same as click Login |
| Type in any field | Clear error message |
| Click show/hide eye | Toggle password visibility (PasswordBox ↔ TextBox) |
| Click X to close | App closes entirely |
| Auth success | Close LoginWindow, show MainWindow |
| Auth failure | Show error message, shake the form |

### Shake Animation on Failure

- Horizontal shake: 3 oscillations, 4px amplitude, 400ms duration
- Combined with error message display

---

## Functionality Specification

### Authentication Flow

1. User enters username + password, clicks Login (or presses Enter)
2. Validate both fields non-empty → show red border if empty
3. Call `UserService.Authenticate(username, password)`
   - If null → show error "用户名或密码错误", shake form
   - If user `IsEnabled == false` → show "账号已被禁用"
   - If user `IsLocked == true` → show "账号已被锁定"
4. On success:
   - Call `SessionManager.SetCurrentUser(user)` to set session
   - Close LoginWindow
   - Show MainWindow

### Password Hashing

**Note:** UserService currently does plain text comparison. This spec requires hashed passwords, but existing users in DB have plain text passwords. Migration strategy:

- **Phase 1 (this implementation):** Support both hashed AND plain text for backward compatibility
  - On login attempt, first try hashed comparison
  - If fails, try plain text (for existing users)
  - When user changes password, save as hashed
- **Phase 2:** Run migration to rehash all passwords (separate task)
- **Algorithm:** SHA256 with salt

```csharp
// Password hashing (new passwords, and migration)
string salt = GenerateSalt(16);
string hash = ComputeSha256(salt + password);
StoreInDb(salt, hash);

// Login check
string storedSalt = GetSalt(username);
string storedHash = GetHash(username);
if (storedHash == ComputeSha256(storedSalt + enteredPassword)) → success
else if (storedHash == enteredPassword) → legacy plain text → migrate
```

### LoginWindow ViewModel (LoginViewModel)

```
Properties:
  - Username: string (bindable)
  - Password: string (bindable, not stored)
  - ErrorMessage: string (bindable)
  - IsLoading: bool (bindable)
  - LogoPath: string (from App.config)

Commands:
  - LoginCommand: executes login flow
  - TogglePasswordVisibilityCommand: toggles show/hide

Services:
  - IUserService: authentication
  - ISessionManager: set current user session
```

---

## Architecture

### Files to Create

| File | Purpose |
|------|---------|
| `TripleDetection.App/Views/LoginWindow.xaml` | Login UI |
| `TripleDetection.App/Views/LoginWindow.xaml.cs` | Code-behind |
| `TripleDetection.App/ViewModels/LoginViewModel.cs` | Login VM |
| `TripleDetection.App/Resources/PasswordHelper.cs` | Password show/hide logic |
| `TripleDetection.App/Services/PasswordHashService.cs` | SHA256 + salt hashing |

### Files to Modify

| File | Change |
|------|--------|
| `TripleDetection.App/App.xaml.cs` | Show LoginWindow before Bootstrapper runs |
| `TripleDetection.App/App.config` | Add `LoginLogoPath` key (optional, fallback to existing SystemLogoPath) |
| `TripleDetection.Services/UserService.cs` | Add hashed password support (dual-mode: hash + legacy plain text) |
| `TripleDetection.App/Bootstrapper.cs` | Allow deferred shell creation (for login flow) |

### Startup Flow Change

**Before:**
```
App.Startup → Bootstrapper.Run() → MainWindow.Show() → Dashboard
```

**After:**
```
App.Startup → Show LoginWindow
  → Authenticate
  → Success: Close LoginWindow → Bootstrapper.Run() → MainWindow.Show() → Dashboard
  → Failure: Stay on LoginWindow
  → Close (X): App exits
```

### DI Registration (in Bootstrapper)

- `LoginViewModel` registered as transient
- `PasswordHashService` registered as singleton (or static helper)
- `LoginWindow` registered for navigation resolution

---

## Edge Cases

| Scenario | Handling |
|----------|----------|
| Empty username field | Red border + no API call |
| Empty password field | Red border + no API call |
| User not found | Error: "用户名或密码错误" |
| Wrong password | Error: "用户名或密码错误" |
| User disabled | Error: "账号已被禁用" |
| User locked | Error: "账号已被锁定" |
| DB connection failure | Error: "数据库连接失败，请稍后重试" |
| Logo image not found | Show text placeholder (system initials) |

---

## Logo Asset Configuration

In `App.config`:
```xml
<add key="LoginLogoPath" value="Resources/logo.png" />
```

- Relative to application base directory
- Supports PNG, JPG
- If key absent, fall back to `SystemLogoPath`

---

## Design Self-Review

1. **Spec coverage:** Login window UI, auth flow, password hashing, startup flow, logo config — all covered
2. **Placeholder scan:** No TBD/TODO — all decisions made
3. **Internal consistency:** Startup flow diagram matches implementation notes
4. **Ambiguity check:** Logo size, shake animation, fallback behavior — all explicit
5. **Scope:** Focused on login window only — no broader auth refactoring (that can be separate task)