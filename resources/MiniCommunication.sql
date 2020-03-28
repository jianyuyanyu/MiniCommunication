CREATE DATABASE MiniCommunication
ON PRIMARY
(
NAME='MiniCommunication',
FILENAME='C:\MiniCommunication.mdf',
SIZE=10MB,
MAXSIZE=UNLIMITED,
FILEGROWTH=30MB
)
LOG ON
(
NAME='MiniCommunication_Log',
FILENAME='C:\MiniCommunication_Log.ldf',
SIZE=10MB,
MAXSIZE=UNLIMITED,
FILEGROWTH=30MB
);

CREATE TABLE User_Info
(
u_id INT PRIMARY KEY IDENTITY(0,1),
u_name NVARCHAR(50) UNIQUE NOT NULL,
u_pwd NVARCHAR(50) NOT NULL,
u_nickname NVARCHAR(50) NOT NULL,
u_gender NVARCHAR(5) CHECK(u_gender='男' OR u_gender='女') NOT NULL,
u_age INT CHECK(u_age>=0) NOT NULL,
u_head_icon NVARCHAR(50) NOT NULL,
u_state INT DEFAULT(0) NOT NULL,
signup_time DATETIME DEFAULT(GETDATE()) NOT NULL,
offline_time DATETIME DEFAULT(GETDATE()) NOT NULL
);
CREATE TABLE User_Friend_Info
(
u_name NVARCHAR(50) FOREIGN KEY(u_name) REFERENCES User_Info(u_name),
f_name NVARCHAR(50) FOREIGN KEY(f_name) REFERENCES User_Info(u_name),
PRIMARY KEY(u_name,f_name)
);