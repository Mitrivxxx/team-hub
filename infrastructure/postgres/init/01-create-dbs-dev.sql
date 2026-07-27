SELECT 'CREATE DATABASE auth_db' WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'auth_db')\gexec
SELECT 'CREATE DATABASE organization_db' WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'organization_db')\gexec
