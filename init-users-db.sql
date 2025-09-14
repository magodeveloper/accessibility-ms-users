-- Script de inicialización para MySQL - Microservicio Users
-- Este script se ejecuta automáticamente cuando se crea el contenedor

-- Crear usuario adicional para operaciones de solo lectura
CREATE USER IF NOT EXISTS 'readonly_user'@'%' IDENTIFIED BY 'ReadOnly2025UsersPass';
GRANT SELECT ON usersdb.* TO 'readonly_user'@'%';

-- Crear usuario para backup
CREATE USER IF NOT EXISTS 'backup_user'@'%' IDENTIFIED BY 'BackUp2025UsersPass';
GRANT SELECT, LOCK TABLES, SHOW VIEW ON usersdb.* TO 'backup_user'@'%';

-- Crear usuario para monitoreo
CREATE USER IF NOT EXISTS 'monitor_user'@'%' IDENTIFIED BY 'Monitor2025UsersPass';
GRANT PROCESS ON *.* TO 'monitor_user'@'%';

-- Reforzar permisos del usuario principal de la aplicación
GRANT ALL PRIVILEGES ON usersdb.* TO 'msuser'@'%';

FLUSH PRIVILEGES;

-- Crear tabla de logs de auditoría si no existe
USE usersdb;
CREATE TABLE IF NOT EXISTS audit_logs (
    id BIGINT AUTO_INCREMENT PRIMARY KEY,
    user_id VARCHAR(255),
    action VARCHAR(100),
    table_name VARCHAR(100),
    timestamp DATETIME(6) DEFAULT CURRENT_TIMESTAMP(6),
    ip_address VARCHAR(45),
    user_agent TEXT
);
