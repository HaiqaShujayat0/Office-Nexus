-- SQL script to create UserBankAccounts table
-- Run this if migrations fail due to existing database

CREATE TABLE IF NOT EXISTS "UserBankAccounts" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_UserBankAccounts" PRIMARY KEY AUTOINCREMENT,
    "UserId" INTEGER NOT NULL,
    "BankName" TEXT NOT NULL,
    "AccountTitle" TEXT NOT NULL,
    "IBAN" TEXT NOT NULL,
    "AccountNumber" TEXT NULL,
    "BranchCode" TEXT NULL,
    "CNIC" TEXT NOT NULL,
    "CreatedAt" TEXT NOT NULL,
    "UpdatedAt" TEXT NULL,
    CONSTRAINT "FK_UserBankAccounts_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
);

CREATE UNIQUE INDEX IF NOT EXISTS "IX_UserBankAccounts_UserId" ON "UserBankAccounts" ("UserId");

