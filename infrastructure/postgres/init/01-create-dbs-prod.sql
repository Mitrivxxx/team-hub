SELECT 'CREATE DATABASE authdb' WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'authdb')\gexec
SELECT 'CREATE DATABASE organizationdb' WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'organizationdb')\gexec
SELECT 'CREATE DATABASE notificationdb' WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'notificationdb')\gexec
SELECT 'CREATE DATABASE chatdb' WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'chatdb')\gexec
