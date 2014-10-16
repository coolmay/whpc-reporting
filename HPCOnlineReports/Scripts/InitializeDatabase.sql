/* Cluster */
IF OBJECT_ID (N'dbo.Cluster', N'U') IS NOT NULL 
    DROP TABLE [dbo].[Cluster];
GO

CREATE TABLE [dbo].[Cluster] (
    [Id]          BIGINT        NOT NULL IDENTITY,
	[ClusterId]   NVARCHAR (64) NOT NULL,
    [ClusterName] NVARCHAR (64) NOT NULL,
    CONSTRAINT [PK_Cluster] PRIMARY KEY ([Id])
);
GO


/* MetricValueHistory */
IF OBJECT_ID (N'dbo.MetricValueHistory', N'U') IS NOT NULL 
    DROP TABLE [dbo].[MetricValueHistory];
GO

CREATE TABLE [dbo].[MetricValueHistory] (
    [Id]       BIGINT        NOT NULL IDENTITY,
	[ClusterId]   NVARCHAR (64) NOT NULL,
    [NodeName] NVARCHAR (64) NOT NULL,
    [Metric]   NVARCHAR (64) NOT NULL,
    [Counter]  NVARCHAR (64) NULL,
    [Time]     DATETIME      NOT NULL,
    [Value]    REAL          NOT NULL, 
    CONSTRAINT [PK_MetricValueHistory] PRIMARY KEY ([Id])
);
GO


/* Network */
IF OBJECT_ID (N'dbo.Network', N'U') IS NOT NULL 
    DROP TABLE [dbo].[Network];
GO

CREATE TABLE [dbo].[Network] (
    [Id]         BIGINT        NOT NULL IDENTITY,
	[ClusterId]   NVARCHAR (64) NOT NULL,
    [Name]       NVARCHAR (64) NOT NULL,
    [Type]       NVARCHAR (64) NOT NULL,
    [IpAddress]  NVARCHAR (64) NULL,
    [SubnetMask] NVARCHAR (64) NULL,
    CONSTRAINT [PK_Network] PRIMARY KEY ([Id])
);
GO


/* ChargeRate */
IF OBJECT_ID (N'dbo.ChargeRate', N'U') IS NOT NULL 
    DROP TABLE [dbo].[ChargeRate];
GO

CREATE TABLE [dbo].[ChargeRate] (
    [Id]        BIGINT         NOT NULL IDENTITY,
	[NodeSize]  NVARCHAR (32)  NOT NULL,
    [Rate]      REAL           NOT NULL,
    CONSTRAINT [PK_ChargeRate] PRIMARY KEY ([Id])
);
GO

INSERT INTO [dbo].[ChargeRate] ([NodeSize], [Rate]) VALUES ('Extra Small', 0.021);
INSERT INTO [dbo].[ChargeRate] ([NodeSize], [Rate]) VALUES ('Small', 0.091);
INSERT INTO [dbo].[ChargeRate] ([NodeSize], [Rate]) VALUES ('Medium', 0.182);
INSERT INTO [dbo].[ChargeRate] ([NodeSize], [Rate]) VALUES ('Large', 0.364);
INSERT INTO [dbo].[ChargeRate] ([NodeSize], [Rate]) VALUES ('Extra Large', 0.727);
INSERT INTO [dbo].[ChargeRate] ([NodeSize], [Rate]) VALUES ('Standard_A5', 0.303);
INSERT INTO [dbo].[ChargeRate] ([NodeSize], [Rate]) VALUES ('Standard_A6', 0.606);
INSERT INTO [dbo].[ChargeRate] ([NodeSize], [Rate]) VALUES ('Standard_A7', 1.211);
INSERT INTO [dbo].[ChargeRate] ([NodeSize], [Rate]) VALUES ('Standard_A8', 2.473);
INSERT INTO [dbo].[ChargeRate] ([NodeSize], [Rate]) VALUES ('Standard_A9', 4.945);
INSERT INTO [dbo].[ChargeRate] ([NodeSize], [Rate]) VALUES ('Standard_D1', 0.161);
INSERT INTO [dbo].[ChargeRate] ([NodeSize], [Rate]) VALUES ('Standard_D2', 0.321);
INSERT INTO [dbo].[ChargeRate] ([NodeSize], [Rate]) VALUES ('Standard_D3', 0.642);
INSERT INTO [dbo].[ChargeRate] ([NodeSize], [Rate]) VALUES ('Standard_D4', 1.284);
INSERT INTO [dbo].[ChargeRate] ([NodeSize], [Rate]) VALUES ('Standard_D11', 0.37);
INSERT INTO [dbo].[ChargeRate] ([NodeSize], [Rate]) VALUES ('Standard_D12', 0.739);
INSERT INTO [dbo].[ChargeRate] ([NodeSize], [Rate]) VALUES ('Standard_D13', 1.33);
INSERT INTO [dbo].[ChargeRate] ([NodeSize], [Rate]) VALUES ('Standard_D14', 2.394);
GO


/* AllocationHistory */
IF OBJECT_ID (N'dbo.AllocationHistory', N'U') IS NOT NULL 
    DROP TABLE [dbo].[AllocationHistory];
GO

CREATE TABLE [dbo].[AllocationHistory] (
    [Id]					 BIGINT          NOT NULL IDENTITY,
	[ClusterId]   NVARCHAR (64) NOT NULL,
    [AllocationHistoryId]    BIGINT          NOT NULL,
    [JobId]                  INT             NOT NULL,
    [RequeueId]              INT             NOT NULL,
    [NodeName]               NVARCHAR (64)   NOT NULL,
    [UniqueCoreId]           INT             NOT NULL,
    [StartTime]              DATETIME        NOT NULL,
    [endtime]                DATETIME        NULL,
    CONSTRAINT [PK_AllocationHistory] PRIMARY KEY ([Id])
);
GO


/* CustomProperty */
IF OBJECT_ID (N'dbo.CustomProperty', N'U') IS NOT NULL 
    DROP TABLE [dbo].[CustomProperty];
GO

CREATE TABLE [dbo].[CustomProperty] (
    [Id]                BIGINT            NOT NULL IDENTITY,
	[ClusterId]   NVARCHAR (64) NOT NULL,
    [JobId]             INT               NOT NULL,
    [RequeueId]         INT               NOT NULL,
    [PropertyName]      NVARCHAR (128)    NOT NULL,
    [PropertyValue]     NVARCHAR (128)    NOT NULL,
    CONSTRAINT [PK_CustomProperty] PRIMARY KEY ([Id])
);
GO


/* DailyNodeStat */
IF OBJECT_ID (N'dbo.DailyNodeStat', N'U') IS NOT NULL 
    DROP TABLE [dbo].[DailyNodeStat];
GO

CREATE TABLE [dbo].[DailyNodeStat] (
    [Id]                   BIGINT          NOT NULL IDENTITY,
	[ClusterId]   NVARCHAR (64) NOT NULL,
    [NodeName]             NVARCHAR (64)   NOT NULL,
    [Date]                 SMALLDATETIME   NOT NULL,
    [UtilizedTime]         BIGINT          NOT NULL,
    [CoreAvailableTime]    BIGINT          NOT NULL,
    [CoreTotalTime]        BIGINT          NOT NULL,
    CONSTRAINT [PK_DailyNodeStat] PRIMARY KEY ([Id])
);
GO


/* JobHistory */
IF OBJECT_ID (N'dbo.JobHistory', N'U') IS NOT NULL 
    DROP TABLE [dbo].[JobHistory];
GO

CREATE TABLE [dbo].[JobHistory] (
    [Id]						BIGINT				NOT NULL IDENTITY,
	[ClusterId]   NVARCHAR (64) NOT NULL,
    [JobHistoryId]              INT					NOT NULL,
    [JobId]						INT					NOT NULL,
    [RequeueId]					INT					NOT NULL,
    [Name]						NVARCHAR (128)		NULL,
    [State]						VARCHAR (8)			NULL,
    [EventTime]					DATETIME			NOT NULL,
    [SubmitTime]				DATETIME			NULL,
    [StartTime]					DATETIME			NULL,
    [Owner]						NVARCHAR (80)		NOT NULL,
    [Project]					NVARCHAR (80)		NOT NULL,
    [Service]					NVARCHAR (128)		NOT NULL,
    [Template]					NVARCHAR (50)		NOT NULL,
    [KernelCpuTime]             BIGINT				NOT NULL,
    [UserCpuTime]               BIGINT				NOT NULL,
    [MemoryUsed]                BIGINT				NOT NULL,
    [NumberOfCalls]             BIGINT				NOT NULL,
    [CallDuration]              BIGINT				NOT NULL,
    [CallsPerSecond]            BIGINT				NOT NULL,
    [RunTime]                   BIGINT				NOT NULL,
    [WaitTime]                  BIGINT				NOT NULL,
    [Priority]                  INT					NULL,
    [Type]						VARCHAR (7)			NULL,
    [Preemptable]				BIT					NULL,
    [NumberOfTasks]				INT					NOT NULL,
    [CanceledTasksCount]		INT					NOT NULL,
    [FailedTasksCount]			INT					NOT NULL,
    [FinishedTasksCount]		INT					NOT NULL,
	[License]					NVARCHAR (160)		NULL,
	[RunUntilCanceled]			BIT					NULL,
	[IsExclusive]				BIT					NULL,
	[FailOnTaskFailure]			BIT					NULL,
	[CanceledBy]				NVARCHAR (80)		NULL,
	[PoolName]					NVARCHAR (64)		NULL,
	[ParentJobIds]				NVARCHAR (4000)		NULL,
	[ChildJobIds]				NVARCHAR (4000)		NULL,
    CONSTRAINT [PK_JobHistory] PRIMARY KEY ([Id])
);
GO


/* NodeEventHistory */
IF OBJECT_ID (N'dbo.NodeEventHistory', N'U') IS NOT NULL 
    DROP TABLE [dbo].[NodeEventHistory];
GO

CREATE TABLE [dbo].[NodeEventHistory] (
    [Id]					BIGINT			NOT NULL IDENTITY,
	[ClusterId]   NVARCHAR (64) NOT NULL,
    [NodeEventHistoryId]    INT				NOT NULL,
    [NodeName]				NVARCHAR (64)   NOT NULL,
	[EventTime]				DATETIME		NOT NULL,
	[Event]					NVARCHAR (11)	NOT NULL,
    CONSTRAINT [PK_NodeEventHistory] PRIMARY KEY ([Id])
);
GO


/* NodeGroupMemberShip */
IF OBJECT_ID (N'dbo.NodeGroupMemberShip', N'U') IS NOT NULL 
    DROP TABLE [dbo].[NodeGroupMemberShip];
GO

CREATE TABLE [dbo].[NodeGroupMemberShip] (
    [Id]			BIGINT          NOT NULL IDENTITY,
	[ClusterId]   NVARCHAR (64) NOT NULL,
    [NodeId]		INT				NOT NULL,
    [NodeName]		NVARCHAR (64)   NOT NULL,
    [GroupName]		NVARCHAR (256)  NOT NULL,
    [GroupId]		INT			    NOT NULL,
    CONSTRAINT [PK_NodeGroupMemberShip] PRIMARY KEY ([Id])
);
GO


/* NodeGroup */
IF OBJECT_ID (N'dbo.NodeGroup', N'U') IS NOT NULL 
    DROP TABLE [dbo].[NodeGroup];
GO

CREATE TABLE [dbo].[NodeGroup] (
    [Id]			BIGINT          NOT NULL IDENTITY,
	[ClusterId]   NVARCHAR (64) NOT NULL,
    [GroupId]		INT		        NOT NULL,
    [GroupName]		NVARCHAR (256)	NOT NULL,
    CONSTRAINT [PK_NodeGroup] PRIMARY KEY ([Id])
);
GO


/* Node */
IF OBJECT_ID (N'dbo.Node', N'U') IS NOT NULL 
    DROP TABLE [dbo].[Node];
GO

CREATE TABLE [dbo].[Node] (
    [Id]					BIGINT          NOT NULL IDENTITY,
	[ClusterId]   NVARCHAR (64) NOT NULL,
    [NodeName]				NVARCHAR (64)   NOT NULL,
    [Processor]				NVARCHAR (400)  NULL,
    [NumberOfCores]			INT				NULL,
    [NumberOfSockets]		INT				NULL,
    [MemorySize]			INT				NULL,
    [Location]				NVARCHAR (400)  NULL,
    [Role]					NVARCHAR (31)   NOT NULL,
    [SubscribedCores]		INT				NULL,
    [SubscribedSockets]		INT				NULL,
	[AzureInstanceSize]     NVARCHAR (64)   NULL,
    CONSTRAINT [PK_Node] PRIMARY KEY ([Id])
);
GO







