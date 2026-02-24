-- Script tạo bảng Tin tức cho dự án DaNangSafeMap
CREATE TABLE IF NOT EXISTS Articles (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    Title VARCHAR(255) NOT NULL,
    Summary VARCHAR(500),         -- Tóm tắt ngắn hiện ở trang chủ
    Content TEXT NOT NULL,         -- Nội dung chi tiết bài viết
    ImageUrl VARCHAR(255),        -- Lưu đường dẫn ảnh minh họa
    AuthorId INT NOT NULL,        -- Khóa ngoại liên kết với bảng Users
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    Status INT DEFAULT 1,         -- 1: Hiển thị, 0: Ẩn/Bản nháp
    FOREIGN KEY (AuthorId) REFERENCES Users(Id) ON DELETE CASCADE
);
ALTER TABLE articles 
ADD CONSTRAINT fk_article_user
FOREIGN KEY (AuthorId) REFERENCES users(Id) 
ON DELETE CASCADE 
ON UPDATE CASCADE;
ALTER TABLE articles 
ADD COLUMN CategoryId INT NOT NULL DEFAULT 1 AFTER ImageUrl;