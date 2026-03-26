CREATE TABLE currencies (
	id UUID PRIMARY KEY
		    DEFAULT gen_random_uuid(),
    code CHAR(3) NOT NULL
				 UNIQUE,
    name VARCHAR(50) NOT NULL,
    symbol VARCHAR(5) NOT NULL
);

INSERT INTO currencies (code, name, symbol) VALUES
	('RUB', 'Российский рубль', '₽'),
	('USD', 'Доллар США', '$');

CREATE TABLE users (
	id UUID PRIMARY KEY
		    DEFAULT gen_random_uuid(),
	login VARCHAR(100) NOT NULL
					   UNIQUE,
	password_hash VARCHAR(255) NOT NULL,
	email VARCHAR(255) NOT NULL
					   UNIQUE,
	currency_id UUID REFERENCES currencies(id)
					 ON DELETE SET NULL,
	created_at TIMESTAMP NOT NULL
						 DEFAULT NOW(),
    last_sync TIMESTAMP
);

CREATE INDEX idx_users_login ON users(login);
CREATE INDEX idx_users_email ON users(email);

INSERT INTO users (id, login, password_hash, email, currency_id, created_at, last_sync)
VALUES 
    (
        'a0eebc99-9c0b-4ef8-bb6d-6bb9bd380a11',
        'testuser',
        '$2a$10$NkqJZ2N3y6bF9xQ1y4v6xO0wX3m5n7p8q9r0s1t2u3v4w5x6y7z8A9B0C',
        'test@example.com',
        (SELECT id FROM currencies WHERE code = 'RUB'),
        NOW(),
        NULL
    ),
    (
        'b0eebc99-9c0b-4ef8-bb6d-6bb9bd380a22',
        'alice',
        '$2a$10$NkqJZ2N3y6bF9xQ1y4v6xO0wX3m5n7p8q9r0s1t2u3v4w5x6y7z8A9B0C',
        'alice@example.com',
        (SELECT id FROM currencies WHERE code = 'USD'),
        NOW(),
        NULL
    );

CREATE TABLE categories (
	id UUID PRIMARY KEY
			DEFAULT gen_random_uuid(),
	user_id UUID REFERENCES users(id)
				 ON DELETE CASCADE,
	name VARCHAR(100) NOT NULL,
	type VARCHAR(10) NOT NULL,
	CONSTRAINT categories_type_check
		CHECK (type IN ('income',
						'expense'))
);

CREATE INDEX idx_categories_user_id ON categories(user_id);
CREATE INDEX idx_categories_type ON categories(type);

INSERT INTO categories (id, user_id, name, type)
VALUES
    (gen_random_uuid(), NULL, 'Зарплата', 'income'),
    (gen_random_uuid(), NULL, 'Подработка', 'income'),
    (gen_random_uuid(), NULL, 'Подарки', 'income'),
    (gen_random_uuid(), NULL, 'Возврат долга', 'income');

INSERT INTO categories (id, user_id, name, type)
VALUES
    (gen_random_uuid(), NULL, 'Продукты', 'expense'),
    (gen_random_uuid(), NULL, 'Транспорт', 'expense'),
    (gen_random_uuid(), NULL, 'Коммунальные услуги', 'expense'),
    (gen_random_uuid(), NULL, 'Связь и интернет', 'expense'),
    (gen_random_uuid(), NULL, 'Развлечения', 'expense'),
    (gen_random_uuid(), NULL, 'Здоровье', 'expense'),
    (gen_random_uuid(), NULL, 'Одежда', 'expense'),
    (gen_random_uuid(), NULL, 'Другое', 'expense');

INSERT INTO categories (id, user_id, name, type)
VALUES 
    (gen_random_uuid(), (SELECT id FROM users WHERE login = 'testuser'), 'Такси', 'expense'),
    (gen_random_uuid(), (SELECT id FROM users WHERE login = 'testuser'), 'Фриланс', 'income'),
    (gen_random_uuid(), (SELECT id FROM users WHERE login = 'alice'), 'Кофе', 'expense');

CREATE TABLE transactions (
	id UUID PRIMARY KEY
			DEFAULT gen_random_uuid(),
	user_id UUID NOT NULL
				 REFERENCES users(id)
				 ON DELETE CASCADE,
	type VARCHAR(10) NOT NULL,
	date DATE NOT NULL
			  DEFAULT NOW(),
	total_amount DECIMAL(12, 2) NOT NULL,
	currency_id UUID REFERENCES currencies(id)
				     ON DELETE SET NULL,
	comment TEXT,
	category_id UUID REFERENCES categories(id)
					 ON DELETE SET NULL,
	is_deleted BOOLEAN NOT NULL
					   DEFAULT FALSE,
	created_at TIMESTAMP NOT NULL
						 DEFAULT NOW(),
	updated_at TIMESTAMP NOT NULL
						 DEFAULT NOW(),
	sync_status VARCHAR(20) NOT NULL
							DEFAULT 'synced',
	CONSTRAINT transactions_sync_status_check
		CHECK (sync_status IN ('synced',
							   'pending',
							   'conflict'))
);

CREATE INDEX idx_transactions_user_id ON transactions(user_id);
CREATE INDEX idx_transactions_date ON transactions(date);
CREATE INDEX idx_transactions_is_deleted ON transactions(is_deleted);

CREATE TABLE transaction_items (
	id UUID PRIMARY KEY
			DEFAULT gen_random_uuid(),
	transaction_id UUID NOT NULL
						REFERENCES transactions(id)
						ON DELETE CASCADE,
	name VARCHAR(255) NOT NULL,
	quantity DECIMAL(10, 3) NOT NULL
							DEFAULT 1,
	unit_price DECIMAL(12, 3) NOT NULL,
	total_price DECIMAL(12, 2) NOT NULL,
	created_at TIMESTAMP NOT NULL
						 DEFAULT NOW(),
	updated_at TIMESTAMP NOT NULL
						 DEFAULT NOW(),
	sync_status VARCHAR(20) NOT NULL
							DEFAULT 'synced',
	is_deleted BOOLEAN NOT NULL
					   DEFAULT FALSE,
	CONSTRAINT transaction_items_sync_status_check
		CHECK (sync_status IN ('synced',
							   'pending',
							   'conflict'))
);

CREATE INDEX idx_transaction_items_transaction_id
	ON transaction_items(transaction_id);