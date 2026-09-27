IF DB_ID(N'ProductDb') IS NULL
    CREATE DATABASE [ProductDb];
GO

IF DB_ID(N'OrderDb') IS NULL
    CREATE DATABASE [OrderDb];
GO

IF DB_ID(N'InventoryDb') IS NULL
    CREATE DATABASE [InventoryDb];
GO

IF DB_ID(N'PaymentDb') IS NULL
    CREATE DATABASE [PaymentDb];
GO

IF DB_ID(N'IdentityDb') IS NULL
    CREATE DATABASE [IdentityDb];
GO