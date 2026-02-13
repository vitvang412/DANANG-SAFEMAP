-- =====================================================
-- IMPORTANT: Run this SQL script in MySQL Workbench
-- to add the google_id column to your accounts table
-- =====================================================

USE urban_security;

-- Add google_id column to accounts table
ALTER TABLE accounts ADD COLUMN google_id VARCHAR(100) DEFAULT NULL;

-- Verify the change
DESCRIBE accounts;

-- You should see google_id in the output