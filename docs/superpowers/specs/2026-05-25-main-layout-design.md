# Triple Detection - Main Application Layout Design

## Context

The user wants to design the main application layout for the Triple Detection WPF application. Product management and task management pages are complete, but the overall application navigation, menu, and main detection page need proper layout planning.

**User Requirements:**
- Navigation rail on the left (collapsible to maximize main content area)
- Top header with: system logo (replaceable), system name, logged-in user display
- Detection page should use wizard/stepper layout for task selection and execution
- Navigation order: 仪表盘 (Dashboard) → 检测执行 (Detection) → 产品管理 (Product) → 任务管理 (Task) → 系统配置 (System)

## 1. Overall Layout Structure

```
┌────────────────────────────────────────────────────────────────┐
│ [Logo] Triple Detection           [User: Admin] [▼] [Logout] │ ← Header (48px height)
├─────┬──────────────────────────────────────────────────────────┤
│     │                                                          │
│  N  │                                                          │
│  A  │                     Main Content                         │
│  V  │                                                          │
│     │                                                          │
│  R  │                                                          │
│  A  │                                                          │
│  I  │                                                          │
│  L  │                                                          │
│     │                                                          │
│[▤]  │                                                          │
└─────┴──────────────────────────────────────────────────────────┘
```

**Dimensions:**
- Header: 48px height, full width
- Navigation Rail: 200px expanded / 48px collapsed
- Main Content: fills remaining space

---

## 2. Header Specification

**Layout (horizontal):**
```
[Logo 48x48] [System Name] ................. [User Avatar] [Username ▼] [Logout]
```

**Components:**
- **Logo**: 48x48 image, left-aligned, configurable path via App.config
- **System Name**: "Triple Detection" text, 16px bold, next to logo
- **User Menu**: Dropdown with username, role display, logout option
- **Logout Button**: Icon button, right-aligned

**Background**: #2D3748 (dark slate)
**Text Color**: #FFFFFF

---

## 3. Navigation Rail Specification

### 3.1 Expanded State (200px width)

```
┌────────────────────────────────────┐
│ [Logo]                      [▤]    │ ← collapse button
├────────────────────────────────────┤
│ ▌ [Icon]                          │
│   Dashboard                       │
├────────────────────────────────────┤
│   [Icon]                          │
│   Detection                       │
├────────────────────────────────────┤
│   [Icon]                          │
│   Products                        │
├────────────────────────────────────┤
│   [Icon]                          │
│   Tasks                           │
├────────────────────────────────────┤
│   [Icon]                          │
│   Settings                        │
└────────────────────────────────────┘
```

**Items:**
| Icon | Label (Chinese) | Label (English) | Route |
|------|-----------------|-----------------|-------|
| 📊 | 仪表盘 | Dashboard | /dashboard |
| 🔍 | 检测执行 | Detection | /detection |
| 📦 | 产品管理 | Products | /products |
| 📋 | 任务管理 | Tasks | /tasks |
| ⚙️ | 系统配置 | Settings | /settings |

**Active State:**
- Left accent bar: 4px width, #4FD1C5 (teal)
- Background: rgba(79, 209, 197, 0.1)
- Text color: #4FD1C5

**Hover State:**
- Background: rgba(255, 255, 255, 0.05)

### 3.2 Collapsed State (48px width)

```
┌────┐
│ [▤]│ ← expand button (top)
├────┤
│    │
│ [📊]│ ← icon only, tooltip on hover
├────┤
│ [🔍]│
├────┤
│ [📦]│
├────┤
│ [📋]│
├────┤
│ [⚙️]│
└────┘
```

**Transition:** Smooth width animation (200ms ease-in-out)

### 3.3 Toggle Behavior

- Click hamburger icon (top-right of rail) → collapse/expand
- State persisted in local settings
- Tooltip shown when collapsed: "Expand navigation"
- Expanded by default

---

## 4. Detection Page (Wizard Layout)

The detection page uses a 3-step wizard:

```
┌────────────────────────────────────────────────────────────────┐
│  ① 选择任务    ────▶    ② 执行检测    ────▶    ③ 结果查看     │
├────────────────────────────────────────────────────────────────┤
│                                                                │
│   Step 1: Select Task                                          │
│   ┌────────────────────────────────────────────────────────┐  │
│   │ [Task Dropdown ▼]                    [Start Detection] │  │
│   └────────────────────────────────────────────────────────┘  │
│                                                                │
│   Task Info:                                                   │
│   - Product: OCR Product A                                      │
│   - Batch: 2026052501                                          │
│   - Production Date: 2026-05-25                                │
│   - Valid Until: 2027-05-25                                    │
│                                                                │
├────────────────────────────────────────────────────────────────┤
│   [VmRenderControl - VisionMaster Display Area]               │
│                                                                │
│   ┌────────────────────────────────────────────────────────┐  │
│   │                                                        │  │
│   │                                                        │  │
│   │                    640 x 480                           │  │
│   │                                                        │  │
│   │                                                        │  │
│   └────────────────────────────────────────────────────────┘  │
│                                                                │
├────────────────────────────────────────────────────────────────┤
│   Results Panel                                               │
│   ┌──────────────┬──────────────┬──────────────┬───────────┐  │
│   │  Results    │   Count      │  Confidence  │   Time    │  │
│   │  OK: 12     │   15         │  98.5%       │   45ms    │  │
│   └──────────────┴──────────────┴──────────────┴───────────┘  │
│   [History] [Export]                                          │
└────────────────────────────────────────────────────────────────┘
```

**Wizard Steps:**
1. **选择任务** (Select Task): Dropdown to select task, display task details
2. **执行检测** (Execute Detection): VisionMaster display, run/pause controls
3. **结果查看** (View Results): Statistics, history list, export options

**Controls:**
- [Start] - Begin detection (changes to [Pause] while running)
- [Stop] - Stop detection
- [Reset] - Clear results

---

## 5. Other Pages Layout

### 5.1 Dashboard Page

```
┌────────────────────────────────────────────────────────────────┐
│   仪表盘                                                        │
├────────────────────────────────────────────────────────────────┤
│   ┌─────────────┐ ┌─────────────┐ ┌─────────────┐ ┌─────────┐│
│   │ Today's OK  │ │ Today's NG  │ │ Total Tasks │ │ Pending ││
│   │    156      │ │      3      │ │     42      │ │    8    ││
│   └─────────────┘ └─────────────┘ └─────────────┘ └─────────┘│
│                                                                │
│   Recent Detections                                            │
│   ┌─────────────────────────────────────────────────────────┐ │
│   │ Time       │ Task      │ Result │ Confidence │          │ │
│   │ 12:30:15   │ Task-001  │ OK     │ 99.2%      │          │ │
│   │ 12:29:45   │ Task-002  │ NG     │ 87.5%      │          │ │
│   └─────────────────────────────────────────────────────────┘ │
│                                                                │
│   Quick Actions                                                │
│   [New Detection] [Manage Products] [View Tasks]              │
└────────────────────────────────────────────────────────────────┘
```

### 5.2 Product/Task List Pages

Standard list + edit panel layout (existing implementation preserved).

### 5.3 Settings Page

```
┌────────────────────────────────────────────────────────────────┐
│   系统配置                                                        │
├────────────────────────────────────────────────────────────────┤
│   ┌───────────────┬────────────────────────────────────────┐   │
│   │ VM Settings   │                                        │   │
│   │ Camera        │   [Form fields for selected category]  │   │
│   │ PLC           │                                        │   │
│   │ Image Storage │                                        │   │
│   │ User Admin    │                                        │   │
│   └───────────────┴────────────────────────────────────────┘   │
└────────────────────────────────────────────────────────────────┘
```

---

## 6. Component States

### Navigation Item States

| State | Background | Text Color | Left Bar |
|-------|------------|------------|----------|
| Default | Transparent | #A0AEC0 | None |
| Hover | rgba(255,255,255,0.05) | #FFFFFF | None |
| Active | rgba(79,209,197,0.1) | #4FD1C5 | 4px #4FD1C5 |
| Disabled | Transparent | #718096 | None |

### Button States

| State | Background | Text Color |
|-------|------------|------------|
| Default | #4FD1C5 | #1A202C |
| Hover | #38B2AC | #1A202C |
| Active/Pressed | #2C7A7B | #FFFFFF |
| Disabled | #A0AEC0 | #718096 |

---

## 7. Technical Implementation

### Files to Modify/Create

| File | Action | Description |
|------|--------|-------------|
| `MainWindow.xaml` | Modify | Replace current layout with header + nav rail + content |
| `MainWindow.xaml.cs` | Modify | Add navigation logic, collapse/expand handlers |
| `ViewModels/MainViewModel.cs` | Modify | Add navigation state, current page tracking |
| `Views/DashboardView.xaml/.cs` | Create | Dashboard page |
| `Views/DetectionView.xaml/.cs` | Create | Detection wizard page |
| `Views/SettingsView.xaml/.cs` | Create | Settings page |
| `App.config` | Modify | Add logo path configuration |
| `Resources/Styles.xaml` | Create | Shared styles for navigation, buttons |

### Key Components

1. **NavigationRail**: Custom UserControl with collapse/expand animation
2. **HeaderBar**: Top bar with logo, user info, logout
3. **WizardControl**: Step indicator + content area for detection page
4. **PageContentArea**: ContentControl that switches between pages

### Configuration (App.config)

```xml
<appSettings>
  <add key="SystemLogoPath" value="Resources/logo.png" />
  <add key="SystemName" value="Triple Detection" />
  <add key="NavRailExpanded" value="true" />
</appSettings>
```

---

## 8. Verification

1. **Build**: Project compiles without errors
2. **Navigation**: Click each nav item switches page correctly
3. **Collapse**: Click hamburger → rail animates to 48px, content expands
4. **Expand**: Click expand button → rail animates back to 200px
5. **Logo**: Configurable via App.config, loads from specified path
6. **User**: Username displayed, logout works
7. **Detection**: Wizard steps navigate correctly, VM display shows
8. **Responsive**: Window resize handles gracefully (min 1024x768)

---

## Status

- [x] User approved overall structure
- [x] Navigation rail: 200px expanded / 48px collapsed, expanded by default
- [x] Header: Logo (48x48) + name + user menu + logout
- [x] Detection page: Wizard/stepper layout with 3 steps
- [x] Design document written
- [ ] User reviews written spec
- [ ] Implementation plan created