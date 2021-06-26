CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" TEXT NOT NULL CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY,
    "ProductVersion" TEXT NOT NULL
);

BEGIN TRANSACTION;

-- CREATE TABLE "Albums" (
    -- "Id" INTEGER NOT NULL CONSTRAINT "PK_Albums" PRIMARY KEY AUTOINCREMENT,
    -- "Name" TEXT NOT NULL
-- );

-- CREATE TABLE "Artists" (
    -- "Id" INTEGER NOT NULL CONSTRAINT "PK_Artists" PRIMARY KEY AUTOINCREMENT,
    -- "Name" TEXT NOT NULL
-- );

-- CREATE TABLE "Users" (
    -- "Id" INTEGER NOT NULL CONSTRAINT "PK_Users" PRIMARY KEY AUTOINCREMENT,
    -- "Name" TEXT NOT NULL
-- );

-- CREATE TABLE "Tracks" (
    -- "Id" INTEGER NOT NULL CONSTRAINT "PK_Tracks" PRIMARY KEY AUTOINCREMENT,
    -- "ArtistId" INTEGER NOT NULL,
    -- "AlbumId" INTEGER NOT NULL,
    -- "Name" TEXT NOT NULL,
    -- CONSTRAINT "FK_Tracks_Albums_AlbumId" FOREIGN KEY ("AlbumId") REFERENCES "Albums" ("Id") ON DELETE CASCADE,
    -- CONSTRAINT "FK_Tracks_Artists_ArtistId" FOREIGN KEY ("ArtistId") REFERENCES "Artists" ("Id") ON DELETE CASCADE
-- );

-- CREATE TABLE "Scrobbles" (
    -- "Id" INTEGER NOT NULL CONSTRAINT "PK_Scrobbles" PRIMARY KEY AUTOINCREMENT,
    -- "Timestamp" INTEGER NOT NULL,
    -- "UserId" INTEGER NOT NULL,
    -- "TrackId" INTEGER NOT NULL,
    -- CONSTRAINT "FK_Scrobbles_Tracks_TrackId" FOREIGN KEY ("TrackId") REFERENCES "Tracks" ("Id") ON DELETE RESTRICT,
    -- CONSTRAINT "FK_Scrobbles_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE RESTRICT
-- );

-- CREATE UNIQUE INDEX "IX_Albums_Name" ON "Albums" ("Name");

-- CREATE UNIQUE INDEX "IX_Artists_Name" ON "Artists" ("Name");

-- CREATE INDEX "IX_Scrobbles_Timestamp" ON "Scrobbles" ("Timestamp");

-- CREATE INDEX "IX_Scrobbles_TrackId" ON "Scrobbles" ("TrackId");

-- CREATE UNIQUE INDEX "IX_Scrobbles_UserId_TrackId_Timestamp" ON "Scrobbles" ("UserId", "TrackId", "Timestamp");

-- CREATE INDEX "IX_Tracks_AlbumId" ON "Tracks" ("AlbumId");

-- CREATE INDEX "IX_Tracks_ArtistId" ON "Tracks" ("ArtistId");

-- CREATE UNIQUE INDEX "IX_Tracks_ArtistId_AlbumId_Name" ON "Tracks" ("ArtistId", "AlbumId", "Name");

-- CREATE INDEX "IX_Tracks_Name" ON "Tracks" ("Name");

-- CREATE UNIQUE INDEX "IX_Users_Name" ON "Users" ("Name");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20210626170820_InitialCreate', '5.0.4');

COMMIT;