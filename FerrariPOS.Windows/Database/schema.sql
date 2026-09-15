-- FerrarisPOS C# / .NET 8
-- Esquema de referencia. La base real se crea en %APPDATA%\FerrarisPOS\FerrarisPOS.db.

-- products incluye wholesale_price para precio de mayoreo.
-- customers.credit_limit = 0 significa crédito infinito.
-- customer_accounts registra ventas a crédito y pagos de cuenta corriente.
-- customer_accounts.payment_method registra EFECTIVO, TARJETA o MIXTO.
-- cash_movements registra los pagos de cuenta corriente hechos en efectivo.

-- V73.1.49: los motivos de descuento se guardan en la venta y en cada item descontado.
-- La base real aplica estas columnas automaticamente desde Database.Initialize().

-- V73.1.52: categorías persistentes de productos.
CREATE TABLE IF NOT EXISTS product_categories(id INTEGER PRIMARY KEY AUTOINCREMENT,name TEXT NOT NULL COLLATE NOCASE UNIQUE,active INTEGER NOT NULL DEFAULT 1,created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,updated_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP);
CREATE INDEX IF NOT EXISTS idx_product_categories_name ON product_categories(name COLLATE NOCASE);
