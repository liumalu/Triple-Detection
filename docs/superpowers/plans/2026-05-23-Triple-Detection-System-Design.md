# Triple Detection 系统设计与实现

## Context

用户需要构建一个完整的视觉检测系统，具有以下特点：
- **现有基础**：WPF + VisionMaster SDK 的 Phase 1 MVP（已实现方案加载、单次/连续运行、结果展示）
- **最终目标**：完整的生产管理系统，涵盖产品管理、任务流控、PLC/IO 集成、权限审计
- **技术选型**：WPF + SQLite（单机版），便于部署和迁移

## 系统架构

### 模块划分

```
┌─────────────────────────────────────────────────────────┐
│                     表现层 (WPF)                         │
├─────────────────────────────────────────────────────────┤
│  主检测页面 │ 产品管理 │ 任务管理 │ 系统配置 │ 审计查询  │
├─────────────────────────────────────────────────────────┤
│                   业务逻辑层 (Services)                  │
├──────────┬───────────┬──────────────┬──────────┬─────────┤
│ 产品服务 │  任务服务 │ PLC/IO服务   │ 用户服务 │ 配置服务 │
├──────────┴───────────┴──────────────┴──────────┴─────────┤
│                   数据访问层 (SQLite + EF Core)          │
├─────────────────────────────────────────────────────────┤
│              VisionMaster SDK (检测执行)                 │
└─────────────────────────────────────────────────────────┘
```

### 模块说明

| 模块 | 说明 | 优先级 |
|------|------|--------|
| **产品管理** | 产品定义（名称、规格、绑定的 .sol 方案文件路径） | P0 |
| **任务管理** | 创建任务（关联产品、检测参数）、审核流程、任务状态（待审/已审/执行中/已完成） | P0 |
| **主检测页面** | 选择已审核任务、执行检测、查看结果、图像存储 | P0 |
| **权限管控** | 用户角色（管理员/操作员）、菜单权限、登录认证 | P1 |
| **系统参数配置** | 全局参数（VM IP/端口、相机、PLC、图像存储路径） | P1 |
| **操作审计日志** | 用户操作记录、登录日志 | P1 |
| **检测记录统计** | 检测结果查询、数值统计、导出 | P1 |
| **PLC/IO 集成** | 剔除指令下发（产品完成后） | P2（后续） |

## 数据库设计

### 核心表结构

**通用字段**（所有表）：`CreateBy`, `UpdateBy`, `CreateAt`, `UpdateAt`, `IsDeleted`

```sql
-- 产品表
Products: Id, Code, Name, Description, SolFilePath,
          ValidType (0=年 1=月 2=日，默认年), ValidPeriod (默认1),
          CreateBy, UpdateBy, CreateAt, UpdateAt, IsDeleted

-- 任务表
Tasks: Id, Name, ProductId, Status (0=待审核 1=已审核 2=执行中 3=已完成),
       CreatedBy, ReviewedBy, ReviewedAt, CreateBy, UpdateBy, CreateAt, UpdatedAt, IsDeleted

-- 用户表
Users: Id, Username, PasswordHash, Role (Admin/Operator),
       CreateBy, UpdateBy, CreateAt, UpdateAt, IsDeleted

-- 操作日志表
AuditLogs: Id, UserId, Action, Details, IpAddress, CreateAt

-- 检测记录表
DetectionRecords: Id, TaskId, Result (OK/NG), Confidence, CharCount, CodeInfo,
                 ImagePath, DetectionTime, CreateBy, CreateAt

-- 系统配置表
SystemConfigs: Id, Category, Key, Value, Description, CreateBy, UpdateBy, CreateAt, UpdateAt

## Phase 1 实施计划

### Step 1: 搭建项目框架与数据库

1. 创建解决方案结构：`TripleDetection.sln`
2. 添加数据访问层项目 `TripleDetection.Data`（Entity Framework Core + SQLite）
3. 添加业务服务层项目 `TripleDetection.Services`
4. 重构现有 `TripleDetection.App` 为展示层
5. 创建 SQLite 数据库和初始化脚本
6. 配置依赖注入

### Step 2: 产品管理模块

1. 创建 `Product` 实体和 `ProductService`
2. 实现 `ProductRepository`（CRUD）
3. 创建 `ProductListViewModel` 和列表页面
4. 实现产品与 .sol 方案文件的绑定

### Step 3: 任务管理模块

1. 创建 `Task` 实体和 `TaskService`
2. 实现任务状态流转（待审核→已审核→执行）
3. 创建 `TaskListViewModel` 和任务管理页面
4. 实现审核功能（仅管理员可操作）

### Step 4: 主检测页面集成

1. 重构现有 `MainWindow` 绑定任务下拉
2. 集成 `DetectionService` 执行检测
3. 实现检测记录保存到数据库
4. 图像存储路径可配置

### Step 5: 权限管控

1. 用户登录/登出
2. 角色权限控制（管理员 vs 操作员）
3. 页面级权限控制

### Step 6: 系统参数配置

1. `SystemConfig` 实体和 `ConfigService`
2. 配置页面（VM IP/端口、图像存储路径等）
3. 配置持久化到数据库

### Step 7: 审计日志与检测统计

1. 操作日志记录（登录、操作）
2. 检测记录查询
3. 统计数据展示

---

**分步实施策略**：
- 先完成 Step 1-4（核心检测流程）
- 每步结束可单独测试运行
- Step 5-7 可在核心功能稳定后继续