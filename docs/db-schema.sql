-- Schema of the Comments app (implemented in PostgreSQL via EF Core migrations).
-- Written in MySQL syntax so it can be opened in MySQL Workbench.

CREATE TABLE Users (
                       Id         INT           NOT NULL AUTO_INCREMENT,
                       UserName   VARCHAR(50)   NOT NULL,
                       Email      VARCHAR(254)  NOT NULL,
                       HomePage   VARCHAR(2048) NULL,
                       CreatedAt  DATETIME(6)   NOT NULL,
                       PRIMARY KEY (Id),
                       UNIQUE KEY IX_Users_Email_UserName (Email, UserName),
                       KEY IX_Users_UserName (UserName)
);

CREATE TABLE Comments (
                          Id         INT           NOT NULL AUTO_INCREMENT,
                          UserId     INT           NOT NULL,
                          ParentId   INT           NULL,
                          Text       TEXT          NOT NULL,
                          IpAddress  VARCHAR(45)   NOT NULL,
                          UserAgent  VARCHAR(512)  NULL,
                          CreatedAt  DATETIME(6)   NOT NULL,
                          PRIMARY KEY (Id),
                          KEY IX_Comments_ParentId_CreatedAt (ParentId, CreatedAt),
                          KEY IX_Comments_UserId (UserId),
                          CONSTRAINT FK_Comments_Users_UserId FOREIGN KEY (UserId) REFERENCES Users (Id) ON DELETE RESTRICT,
                          CONSTRAINT FK_Comments_Comments_ParentId FOREIGN KEY (ParentId) REFERENCES Comments (Id) ON DELETE CASCADE
);

CREATE TABLE Attachments (
                             Id                INT          NOT NULL AUTO_INCREMENT,
                             CommentId         INT          NOT NULL,
                             OriginalFileName  VARCHAR(255) NOT NULL,
                             StoredFileName    VARCHAR(100) NOT NULL,
                             ContentType       VARCHAR(100) NOT NULL,
                             Kind              VARCHAR(20)  NOT NULL,
                             Status            VARCHAR(20)  NOT NULL,
                             SizeBytes         BIGINT       NOT NULL,
                             Width             INT          NULL,
                             Height            INT          NULL,
                             PRIMARY KEY (Id),
                             UNIQUE KEY IX_Attachments_CommentId (CommentId),
                             CONSTRAINT FK_Attachments_Comments_CommentId FOREIGN KEY (CommentId) REFERENCES Comments (Id) ON DELETE CASCADE
);