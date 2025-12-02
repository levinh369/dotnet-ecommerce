-- Tạo Database (nếu chưa có)
-- CREATE DATABASE DoAn;
-- GO
-- USE DoAn;
-- GO

-- 1. Bảng Users (Người dùng)
CREATE TABLE [User] (
    UserId INT IDENTITY(1,1) PRIMARY KEY,
    Email NVARCHAR(255) NOT NULL,
    PasswordHash NVARCHAR(MAX) NOT NULL,
    FullName NVARCHAR(255),
    Phone NVARCHAR(20),
    Address NVARCHAR(MAX),
    IsEmailConfirmed BIT DEFAULT 0,
    ResetToken NVARCHAR(MAX),
    ResetTokenExpiry DATETIME2,
    Role NVARCHAR(50), -- Admin, Customer, etc.
    CreatedAt DATETIME2 DEFAULT GETDATE(),
    AvatarUrl NVARCHAR(MAX),
    DistrictId INT -- Khóa ngoại nếu có bảng District, tạm thời để INT
);
GO

-- 2. Bảng Categories (Danh mục) - Có đệ quy ParentId
CREATE TABLE Categories (
    CategoryId INT IDENTITY(1,1) PRIMARY KEY,
    CategoryName NVARCHAR(255) NOT NULL,
    ParentId INT NULL, -- Cho phép Null nếu là danh mục gốc
    FOREIGN KEY (ParentId) REFERENCES Categories(CategoryId)
);
GO

-- 3. Bảng Colors (Màu sắc)
CREATE TABLE Colors (
    ColorId INT IDENTITY(1,1) PRIMARY KEY,
    ColorName NVARCHAR(100),
    ColorCode NVARCHAR(50) -- Mã Hex ví dụ #FFFFFF
);
GO

-- 4. Bảng Sizes (Kích thước)
CREATE TABLE Sizes (
    SizeId INT IDENTITY(1,1) PRIMARY KEY,
    SizeName NVARCHAR(50),
    IsActive BIT DEFAULT 1
);
GO

-- 5. Bảng Products (Sản phẩm gốc)
CREATE TABLE Product (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(255) NOT NULL,
    Img NVARCHAR(MAX),
    IsActive BIT DEFAULT 1,
    Price DECIMAL(18,2),
    Description NVARCHAR(MAX),
    CreatedAt DATETIME2 DEFAULT GETDATE(),
    UpdatedAt DATETIME2,
    CategoryId INT,
    FOREIGN KEY (CategoryId) REFERENCES Categories(CategoryId)
);
GO

-- 6. Bảng ProductImages (Ảnh phụ của sản phẩm)
CREATE TABLE ProductImages (
    ProductImageId INT IDENTITY(1,1) PRIMARY KEY,
    ProductId INT NOT NULL,
    ImageUrl NVARCHAR(MAX),
    IsMain BIT DEFAULT 0,
    FOREIGN KEY (ProductId) REFERENCES Product(Id) ON DELETE CASCADE
);
GO

-- 7. Bảng ProductColorImages (Ảnh theo màu của sản phẩm)
CREATE TABLE ProductColorImages (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    ProductId INT NOT NULL,
    ColorId INT NOT NULL,
    ImageUrl NVARCHAR(MAX),
    FOREIGN KEY (ProductId) REFERENCES Product(Id),
    FOREIGN KEY (ColorId) REFERENCES Colors(ColorId)
);
GO

-- 8. Bảng ProductVariants (Biến thể sản phẩm: Size + Color + Stock)
CREATE TABLE ProductVariants (
    ProductVariantId INT IDENTITY(1,1) PRIMARY KEY,
    ProductId INT NOT NULL,
    SizeId INT NOT NULL,
    ColorId INT NOT NULL,
    StockQuantity INT DEFAULT 0,
    SKU NVARCHAR(100),
    ImageUrl NVARCHAR(MAX),
    IsActive BIT DEFAULT 1,
    Price DECIMAL(18,2), -- Giá riêng cho biến thể nếu có
    FOREIGN KEY (ProductId) REFERENCES Product(Id) ON DELETE CASCADE,
    FOREIGN KEY (SizeId) REFERENCES Sizes(SizeId),
    FOREIGN KEY (ColorId) REFERENCES Colors(ColorId)
);
GO

-- 9. Bảng Vouchers (Mã giảm giá)
CREATE TABLE Vouchers (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Code NVARCHAR(50) NOT NULL,
    Name NVARCHAR(255),
    Description NVARCHAR(MAX),
    Type NVARCHAR(50), -- Percent, Amount
    DiscountType NVARCHAR(50),
    Status NVARCHAR(50),
    DiscountValue DECIMAL(18,2),
    MinOrderValue DECIMAL(18,2),
    UsageLimit INT,
    UsageCount INT DEFAULT 0,
    StartDate DATETIME2,
    EndDate DATETIME2,
    CreatedAt DATETIME2 DEFAULT GETDATE(),
    MaxPerUser INT,
    PointCost INT -- Điểm cần để đổi voucher
);
GO

-- 10. Bảng UserVouchers (Voucher người dùng sở hữu)
CREATE TABLE UserVouchers (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT NOT NULL,
    VoucherId INT NOT NULL,
    Claimed BIT DEFAULT 1,
    ClaimedAt DATETIME2 DEFAULT GETDATE(),
    Used BIT DEFAULT 0,
    UsedAt DATETIME2,
    ExpiredAt DATETIME2,
    FOREIGN KEY (UserId) REFERENCES [User](UserId),
    FOREIGN KEY (VoucherId) REFERENCES Vouchers(Id)
);
GO

-- 11. Bảng CartModels (Giỏ hàng)
CREATE TABLE CartModels (
    CartId INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT NOT NULL,
    CreatedDate DATETIME2 DEFAULT GETDATE(),
    FOREIGN KEY (UserId) REFERENCES [User](UserId) ON DELETE CASCADE
);
GO

-- 12. Bảng CartItemModels (Chi tiết giỏ hàng)
CREATE TABLE CartItemModels (
    CartItemId INT IDENTITY(1,1) PRIMARY KEY,
    CartId INT NOT NULL,
    ProductVariantId INT NOT NULL,
    Quantity INT DEFAULT 1,
    FOREIGN KEY (CartId) REFERENCES CartModels(CartId) ON DELETE CASCADE,
    FOREIGN KEY (ProductVariantId) REFERENCES ProductVariants(ProductVariantId)
);
GO

-- 13. Bảng Orders (Đơn hàng)
CREATE TABLE Orders (
    OrderId INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT NOT NULL,
    Email NVARCHAR(255),
    Phone NVARCHAR(20),
    OrderDate DATETIME2 DEFAULT GETDATE(),
    ShippedDate DATETIME2,
    ShippingAddress NVARCHAR(MAX),
    TotalAmount DECIMAL(18,2),
    Status NVARCHAR(50), -- Pending, Processing, Shipped...
    CreatedAt DATETIME2 DEFAULT GETDATE(),
    UpdatedAt DATETIME2,
    PaymentStatus NVARCHAR(50),
    DiscountValue DECIMAL(18,2),
    FinalAmount DECIMAL(18,2),
    VoucherId INT NULL,
    FOREIGN KEY (UserId) REFERENCES [User](UserId),
    FOREIGN KEY (VoucherId) REFERENCES Vouchers(Id)
);
GO

-- 14. Bảng OrderDetails (Chi tiết đơn hàng)
CREATE TABLE OrderDetails (
    OrderDetailId INT IDENTITY(1,1) PRIMARY KEY,
    OrderId INT NOT NULL,
    ProductVariantId INT NOT NULL,
    Quantity INT NOT NULL,
    -- Có thể thêm cột Price tại thời điểm mua để lưu lịch sử giá
    FOREIGN KEY (OrderId) REFERENCES Orders(OrderId) ON DELETE CASCADE,
    FOREIGN KEY (ProductVariantId) REFERENCES ProductVariants(ProductVariantId)
);
GO

-- 15. Bảng Reviews (Đánh giá sản phẩm)
CREATE TABLE Reviews (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    ProductVariantId INT NOT NULL, -- Theo sơ đồ nối vào Variant
    UserId INT NOT NULL,
    Comment NVARCHAR(MAX),
    Rating INT CHECK (Rating >= 1 AND Rating <= 5),
    CreatedAt DATETIME2 DEFAULT GETDATE(),
    OrderDetailId INT NULL, -- Ràng buộc chỉ mua mới được review
    SellerReply NVARCHAR(MAX),
    SellerReplyAt DATETIME2,
    IsVisible BIT DEFAULT 1,
    FOREIGN KEY (ProductVariantId) REFERENCES ProductVariants(ProductVariantId),
    FOREIGN KEY (UserId) REFERENCES [User](UserId),
    FOREIGN KEY (OrderDetailId) REFERENCES OrderDetails(OrderDetailId)
);
GO

-- 16. Bảng Conversations (Cuộc trò chuyện - Chat)
CREATE TABLE Conversations (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    BuyerId INT NOT NULL, -- Người mua
    Title NVARCHAR(255),
    CreatedAt DATETIME2 DEFAULT GETDATE(),
    FOREIGN KEY (BuyerId) REFERENCES [User](UserId)
);
GO

-- 17. Bảng Messages (Tin nhắn)
CREATE TABLE Messages (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    ConversationId INT NOT NULL,
    SenderId INT NOT NULL, -- Có thể là User hoặc Admin
    Content NVARCHAR(MAX),
    ReplyToMessageId INT NULL, -- Trả lời tin nhắn nào
    CreatedAt DATETIME2 DEFAULT GETDATE(),
    IsRead BIT DEFAULT 0,
    ProductId INT NULL, -- Chat về sản phẩm nào (context)
    FOREIGN KEY (ConversationId) REFERENCES Conversations(Id) ON DELETE CASCADE,
    FOREIGN KEY (SenderId) REFERENCES [User](UserId),
    FOREIGN KEY (ProductId) REFERENCES Product(Id)
);
GO

-- 18. Bảng OrderNotification (Thông báo đơn hàng)
CREATE TABLE OrderNotification (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    OrderId INT NOT NULL,
    IsRead BIT DEFAULT 0,
    CreatedAt DATETIME2 DEFAULT GETDATE(),
    Message NVARCHAR(MAX),
    Title NVARCHAR(255),
    UserId INT NOT NULL,
    Type NVARCHAR(50),
    Url NVARCHAR(MAX), -- Link click vào
    FOREIGN KEY (OrderId) REFERENCES Orders(OrderId),
    FOREIGN KEY (UserId) REFERENCES [User](UserId)
);
GO

-- 19. Bảng UserPoints (Điểm tích lũy tổng)
CREATE TABLE UserPoints (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT NOT NULL,
    TotalPoints INT DEFAULT 0,
    LifetimePoints INT DEFAULT 0, -- Tổng điểm đã từng tích
    FOREIGN KEY (UserId) REFERENCES [User](UserId) ON DELETE CASCADE
);
GO

-- 20. Bảng PointHistories (Lịch sử điểm)
CREATE TABLE PointHistories (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT NOT NULL,
    PointsChange INT NOT NULL, -- Có thể âm hoặc dương
    Reason NVARCHAR(255),
    ReferenceId INT NULL, -- ID đơn hàng hoặc sự kiện liên quan
    CreatedAt DATETIME2 DEFAULT GETDATE(),
    FOREIGN KEY (UserId) REFERENCES [User](UserId)
);
GO