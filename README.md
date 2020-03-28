### MiniCommunication(迷你通讯)

![输入图片说明](https://images.gitee.com/uploads/images/2020/0328/225736_8f061161_7379074.png "0.png")

#### 简介

基于.net core 3.1和WPF开发的聊天软件

#### 安装说明

执行MiniCommunication.sql中的命令创建数据库及表

[编译以下项目]

核心库
MiniSocket.Transmitting，MiniSocket.Server，MiniSocket.Client

终端程序
MiniComm.Server，MiniComm.Client

#### 配置文件

将server-config.xml和client-config.xml文件分别copy到Server和Client终端的根目录，并进行相应的配置(可以通过启动终端自动创建)

#### 使用说明

Server终端：通过"help"命令查看帮助

#### 更新

**2020/3/28** 
MiniCommunication beta 1.0