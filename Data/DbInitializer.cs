using Microsoft.EntityFrameworkCore;
using ThuChiCaNhan.Models;

namespace ThuChiCaNhan.Data;

public static class DbInitializer
{
    private static readonly (string Name, string Type)[] Defaults =
    [
        ("Ăn uống", "Chi tiêu"), ("Di chuyển", "Chi tiêu"), ("Học tập", "Chi tiêu"),
        ("Giải trí", "Chi tiêu"), ("Nhà cửa", "Chi tiêu"), ("Sức khỏe", "Chi tiêu"),
        ("Mua sắm", "Chi tiêu"), ("Khác", "Chi tiêu"),
        ("Lương", "Thu nhập"), ("Thưởng", "Thu nhập"), ("Đầu tư", "Thu nhập"),
        ("Thu nhập khác", "Thu nhập")
    ];

    public static async Task InitializeAsync(TrackerDbContext db)
    {
        await db.Database.EnsureCreatedAsync();
        await db.Database.ExecuteSqlRawAsync("""
            IF OBJECT_ID(N'dbo.Wallets', N'U') IS NULL
            BEGIN
                CREATE TABLE dbo.Wallets (
                    Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Wallets PRIMARY KEY,
                    Name nvarchar(80) NOT NULL,
                    Type nvarchar(40) NOT NULL
                );
                CREATE UNIQUE INDEX IX_Wallets_Name ON dbo.Wallets(Name);
            END;
            IF OBJECT_ID(N'dbo.Budgets', N'U') IS NULL
            BEGIN
                CREATE TABLE dbo.Budgets (
                    Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Budgets PRIMARY KEY,
                    CategoryId int NOT NULL,
                    Month datetime2 NOT NULL,
                    [Limit] decimal(18,2) NOT NULL,
                    CONSTRAINT FK_Budgets_Categories_CategoryId FOREIGN KEY (CategoryId) REFERENCES dbo.Categories(Id)
                );
                CREATE UNIQUE INDEX IX_Budgets_CategoryId_Month ON dbo.Budgets(CategoryId, Month);
            END;
            IF OBJECT_ID(N'dbo.RecurringTransactions', N'U') IS NULL
            BEGIN
                CREATE TABLE dbo.RecurringTransactions (
                    Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_RecurringTransactions PRIMARY KEY,
                    Amount decimal(18,2) NOT NULL,
                    Type nvarchar(max) NOT NULL,
                    Note nvarchar(max) NOT NULL,
                    CategoryId int NOT NULL,
                    WalletId int NOT NULL,
                    NextDueDate datetime2 NOT NULL,
                    DayOfMonth int NOT NULL,
                    IsActive bit NOT NULL,
                    CONSTRAINT FK_RecurringTransactions_Categories_CategoryId FOREIGN KEY (CategoryId) REFERENCES dbo.Categories(Id),
                    CONSTRAINT FK_RecurringTransactions_Wallets_WalletId FOREIGN KEY (WalletId) REFERENCES dbo.Wallets(Id)
                );
            END;

            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Wallets') AND name = N'IX_Wallets_Name')
                CREATE UNIQUE INDEX IX_Wallets_Name ON dbo.Wallets(Name);
            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Budgets') AND name = N'IX_Budgets_CategoryId_Month')
                CREATE UNIQUE INDEX IX_Budgets_CategoryId_Month ON dbo.Budgets(CategoryId, Month);
            """);

        await db.Database.ExecuteSqlRawAsync("""
            IF COL_LENGTH(N'dbo.Transactions', N'WalletId') IS NULL
                ALTER TABLE dbo.Transactions ADD WalletId int NULL;
            IF COL_LENGTH(N'dbo.Transactions', N'RecurringTransactionId') IS NULL
                ALTER TABLE dbo.Transactions ADD RecurringTransactionId int NULL;
            """);

        await db.Database.ExecuteSqlRawAsync("""
            IF NOT EXISTS (SELECT 1 FROM dbo.Wallets WHERE Name = N'Tiền mặt')
                INSERT INTO dbo.Wallets(Name, Type) VALUES (N'Tiền mặt', N'Tiền mặt');
            IF NOT EXISTS (SELECT 1 FROM dbo.Wallets WHERE Name = N'Tài khoản ngân hàng')
                INSERT INTO dbo.Wallets(Name, Type) VALUES (N'Tài khoản ngân hàng', N'Ngân hàng');
            IF NOT EXISTS (SELECT 1 FROM dbo.Wallets WHERE Name = N'MoMo')
                INSERT INTO dbo.Wallets(Name, Type) VALUES (N'MoMo', N'Ví điện tử');
            IF NOT EXISTS (SELECT 1 FROM dbo.Wallets WHERE Name = N'ZaloPay')
                INSERT INTO dbo.Wallets(Name, Type) VALUES (N'ZaloPay', N'Ví điện tử');

            DECLARE @DefaultWalletId int = (SELECT TOP (1) Id FROM dbo.Wallets WHERE Name = N'Tiền mặt');
            UPDATE dbo.Transactions SET WalletId = @DefaultWalletId WHERE WalletId IS NULL;
            IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Transactions') AND name = N'WalletId' AND is_nullable = 1)
                ALTER TABLE dbo.Transactions ALTER COLUMN WalletId int NOT NULL;
            IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE parent_object_id = OBJECT_ID(N'dbo.Transactions') AND name = N'FK_Transactions_Wallets_WalletId')
                ALTER TABLE dbo.Transactions ADD CONSTRAINT FK_Transactions_Wallets_WalletId FOREIGN KEY (WalletId) REFERENCES dbo.Wallets(Id);
            IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE parent_object_id = OBJECT_ID(N'dbo.Transactions') AND name = N'FK_Transactions_RecurringTransactions_RecurringTransactionId')
                ALTER TABLE dbo.Transactions ADD CONSTRAINT FK_Transactions_RecurringTransactions_RecurringTransactionId FOREIGN KEY (RecurringTransactionId) REFERENCES dbo.RecurringTransactions(Id) ON DELETE SET NULL;
            """);

        foreach (var (name, type) in Defaults)
        {
            if (!await db.Categories.AnyAsync(category => category.Name == name && category.Type == type))
                db.Categories.Add(new ExpenseCategory { Name = name, Type = type });
        }

        await db.SaveChangesAsync();
        await SampleDataSeeder.SeedIfEmptyAsync(db);
    }
}
