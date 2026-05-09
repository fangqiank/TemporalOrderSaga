# Temporal Order Saga

基于 Temporal.io 的订单 Saga 补偿事务模式实现，使用 .NET 10 + Temporalio SDK，包含完整的前后端购物体验。

## 架构图

![Temporal Order Saga Architecture](./TemporalOrderSaga-architecture.svg)

## 技术栈

- **.NET 10** + Temporalio SDK 1.14.0
- **ASP.NET Core** WebAPI + 静态前端
- **EF Core** + SQLite（商品 + 用户数据）
- **Temporal Server** 1.25.2 (docker-compose)
- **PostgreSQL 16** (Temporal 持久化存储)

## 项目结构

| 项目 | 说明 |
|------|------|
| `OrderSaga.Contracts` | 共享消息类型：OrderInput, OrderResult, CreateOrderRequest, OrderStatus 等 |
| `OrderSaga.Workflows` | Temporal Worker 宿主 + Saga Workflow + Activities |
| `Client` | WebAPI 购物服务 + SPA 前端（wwwroot） |

### Client 项目

```
Client/
├── Controllers/
│   ├── ProductsController.cs    # GET /api/products
│   ├── OrdersController.cs      # POST /api/orders, GET /api/orders/{id}
│   └── CustomersController.cs   # GET /api/customers
├── Data/
│   └── AppDbContext.cs           # EF Core + SQLite + 种子数据
├── Models/
│   ├── Product.cs                # 商品实体
│   └── Customer.cs               # 用户实体（含余额）
└── wwwroot/
    ├── index.html                # SPA 单页应用
    ├── css/style.css             # 深色主题样式
    └── js/app.js                 # 购物车 + 下单 + 订单追踪
```

## 用户系统

前端顶部提供用户选择器，每个用户有不同余额，支付时真实检查余额是否足够。

| 用户 | 余额 | 说明 |
|------|------|------|
| 张三 (土豪) | ¥10,000 | 随便买 |
| 李四 (小康) | ¥1,500 | 买小件没问题，大件会余额不足 |
| 王五 (吃土) | ¥0 | 任何订单都会支付失败 → 触发补偿 |

## Saga 流程

```
1. ReserveInventory  ─── 成功则注册补偿: ReleaseInventory
       │
2. AuthorizePayment  ─── 余额检查，成功则注册补偿: VoidPayment
       │
3. SendConfirmationEmail
       │
4. CompleteOrder
```

任一步骤失败，按 LIFO 顺序执行已注册的补偿操作（VoidPayment → ReleaseInventory）。

### 失败场景与补偿

| 失败场景 | 触发补偿 |
|---------|---------|
| 库存预留失败 (10% 随机) | 无（尚未执行任何操作） |
| 支付失败（余额不足） | 释放库存 |
| 确认邮件失败 | 取消支付 + 释放库存 |

### 订单状态追踪

| 状态 | 说明 |
|------|------|
| `Pending` | 等待处理 |
| `InventoryReserved` | 库存已预留 |
| `PaymentAuthorized` | 支付已授权 |
| `Completed` | 订单完成（绿色） |
| `Failed` | 订单失败（红色，补偿回滚） |

前端实时轮询工作流状态，区分展示成功/失败结果，失败时显示具体原因和补偿日志。

## 快速开始

```bash
# 1. 启动 Temporal 基础设施
docker-compose up -d

# 2. 等待服务就绪后启动 Worker
dotnet run --project OrderSaga.Workflows

# 3. 另开终端，启动 WebAPI + 前端
dotnet run --project Client
```

- 购物页面: `http://localhost:5283`
- Temporal gRPC: `localhost:7233`
- Temporal UI: http://localhost:8080

## 功能

- 用户选择器（3 个用户，不同余额）
- 余额实时显示 + 购物车余额不足警告
- 商品浏览（5 个分类，10 款商品）
- 购物车管理（添加、调整数量、localStorage 持久化）
- 一键下单（启动 Temporal Saga 工作流）
- 支付余额检查（替代随机失败，真实检查用户余额）
- 订单状态实时追踪（轮询 + 步骤指示器）
- 成功/失败结果区分展示
- 失败补偿日志可视化
- 一键重新下单

## 前置条件

- .NET 10 SDK
- Docker & Docker Compose
