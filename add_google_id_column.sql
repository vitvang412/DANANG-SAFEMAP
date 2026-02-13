-- Run this SQL script to add google_id column to accounts table
-- You can run this in MySQL Workbench or any MySQL client

USE urban_security;

-- Add google_id column if it doesn't exist
ALTER TABLE accounts 
ADD COLUMN IF NOT EXISTS google_id VARCHAR(100) NULL;

-- Verify the column was added
DESCRIBE accounts;
