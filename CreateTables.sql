/****** Object:  Table [dbo].[Episodes]    Script Date: 02.11.2019 19:06:37 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Episodes](
	[Id] [int] NOT NULL,
	[ReleaseId] [int] NULL,
	[Title] [nvarchar](512) NULL,
	[Links] [nvarchar](max) NULL,
	[Created] [bigint] NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Releases]    Script Date: 02.11.2019 19:06:37 ******/
CREATE TABLE [dbo].[Releases](
	[Id] [int] NOT NULL,
	[Titles] [nvarchar](512) NULL,
	[Poster] [nvarchar](128) NULL,
	[LastModified] [bigint] NULL,
	[StatusCode] [tinyint] NULL,
	[Genres] [nvarchar](512) NULL,
	[Voicers] [nvarchar](512) NULL,
	[Year] [smallint] NULL,
	[Season] [nvarchar](20) NULL,
	[Description] [nvarchar](max) NULL,
	[Torrents] [nvarchar](max) NULL,
	[Rating] [int] NULL,
	[Code] [nvarchar](128) NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
ALTER TABLE [dbo].[Episodes]  WITH CHECK ADD  CONSTRAINT [FK_Episodes_ToReleases] FOREIGN KEY([ReleaseId])
REFERENCES [dbo].[Releases] ([Id])
GO
ALTER TABLE [dbo].[Episodes] CHECK CONSTRAINT [FK_Episodes_ToReleases]
GO
/****** Object:  Index [IX_Releases_Id_LastModified_Rating]    Script Date: 02.11.2019 19:15:57 ******/
CREATE NONCLUSTERED INDEX [IX_Releases_Id_LastModified_Rating] ON [dbo].[Releases]
(
	[Id] ASC,
	[LastModified] DESC,
	[Rating] DESC
)WITH (STATISTICS_NORECOMPUTE = OFF, DROP_EXISTING = OFF, ONLINE = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Episodes_ReleaseId]    Script Date: 02.11.2019 19:15:51 ******/
CREATE NONCLUSTERED INDEX [IX_Episodes_ReleaseId] ON [dbo].[Episodes]
(
	[ReleaseId] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, DROP_EXISTING = OFF, ONLINE = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Episodes_Created]    Script Date: 02.11.2019 19:16:46 ******/
CREATE NONCLUSTERED INDEX [IX_Episodes_Created] ON [dbo].[Episodes]
(
	[Created] DESC
)WITH (STATISTICS_NORECOMPUTE = OFF, DROP_EXISTING = OFF, ONLINE = OFF) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Torrents]    Script Date: 04.11.2019 21:41:05 ******/
CREATE TABLE [dbo].[Torrents](
	[Id] [int] NOT NULL,
	[ReleaseId] [int] NOT NULL,
	[Created] [bigint] NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

ALTER TABLE [dbo].[Torrents]  WITH CHECK ADD  CONSTRAINT [FK_Torrents_ToReleases] FOREIGN KEY([ReleaseId])
REFERENCES [dbo].[Releases] ([Id])
GO

ALTER TABLE [dbo].[Torrents] CHECK CONSTRAINT [FK_Torrents_ToReleases]
GO
/****** Object:  Index [IX_Torrents_Created]    Script Date: 04.11.2019 21:41:30 ******/
CREATE NONCLUSTERED INDEX [IX_Torrents_Created] ON [dbo].[Torrents]
(
	[Created] DESC
)WITH (STATISTICS_NORECOMPUTE = OFF, DROP_EXISTING = OFF, ONLINE = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Torrents_ReleaseId]    Script Date: 04.11.2019 21:41:35 ******/
CREATE NONCLUSTERED INDEX [IX_Torrents_ReleaseId] ON [dbo].[Torrents]
(
	[ReleaseId] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, DROP_EXISTING = OFF, ONLINE = OFF) ON [PRIMARY]
GO








