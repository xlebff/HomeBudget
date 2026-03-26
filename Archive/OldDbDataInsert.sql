DO $$
DECLARE
    v_user_id UUID;
    v_rub_id UUID;
    v_usd_id UUID;
    v_cat_food UUID;
    v_cat_transport UUID;
    v_cat_taxi UUID;
    v_cat_salary UUID;
    v_cat_freelance UUID;
    v_cat_health UUID;
    v_transaction_id UUID;
BEGIN
    -- ID пользователя testuser
    v_user_id := (SELECT id FROM users WHERE login = 'testuser');
    v_rub_id := (SELECT id FROM currencies WHERE code = 'RUB');
    v_usd_id := (SELECT id FROM currencies WHERE code = 'USD');
    
    -- Категории для testuser
    v_cat_food := (SELECT id FROM categories WHERE name = 'Продукты' AND type = 'expense' AND categories.user_id IS NULL);
    v_cat_transport := (SELECT id FROM categories WHERE name = 'Транспорт' AND type = 'expense' AND categories.user_id IS NULL);
    v_cat_taxi := (SELECT id FROM categories WHERE name = 'Такси' AND type = 'expense' AND categories.user_id = v_user_id);
    v_cat_salary := (SELECT id FROM categories WHERE name = 'Зарплата' AND type = 'income' AND categories.user_id IS NULL);
    v_cat_freelance := (SELECT id FROM categories WHERE name = 'Фриланс' AND type = 'income' AND categories.user_id = v_user_id);
    
    -- 1. Расход в магазине (testuser)
    INSERT INTO transactions (id, user_id, type, date, total_amount, currency_id, comment, category_id, created_at, updated_at, sync_status)
    VALUES (gen_random_uuid(), v_user_id, 'expense', '2025-03-15', 1250.50, v_rub_id, 'Покупка в Пятёрочке', v_cat_food, NOW(), NOW(), 'synced')
    RETURNING id INTO v_transaction_id;

    INSERT INTO transaction_items (id, transaction_id, name, quantity, unit_price, total_price, created_at, updated_at, sync_status)
    VALUES 
        (gen_random_uuid(), v_transaction_id, 'Молоко', 2, 80.00, 160.00, NOW(), NOW(), 'synced'),
        (gen_random_uuid(), v_transaction_id, 'Хлеб', 1, 45.00, 45.00, NOW(), NOW(), 'synced'),
        (gen_random_uuid(), v_transaction_id, 'Сыр', 1, 350.00, 350.00, NOW(), NOW(), 'synced'),
        (gen_random_uuid(), v_transaction_id, 'Колбаса', 1, 695.50, 695.50, NOW(), NOW(), 'synced');

    -- 2. Расход на такси (testuser)
    INSERT INTO transactions (id, user_id, type, date, total_amount, currency_id, comment, category_id, created_at, updated_at, sync_status)
    VALUES (gen_random_uuid(), v_user_id, 'expense', '2025-03-16', 320.00, v_rub_id, 'Поездка в аэропорт', v_cat_taxi, NOW(), NOW(), 'synced')
    RETURNING id INTO v_transaction_id;

    INSERT INTO transaction_items (id, transaction_id, name, quantity, unit_price, total_price, created_at, updated_at, sync_status)
    VALUES (gen_random_uuid(), v_transaction_id, 'Такси (Яндекс)', 1, 320.00, 320.00, NOW(), NOW(), 'synced');

    -- 3. Доход – зарплата (testuser)
    INSERT INTO transactions (id, user_id, type, date, total_amount, currency_id, comment, category_id, created_at, updated_at, sync_status)
    VALUES (gen_random_uuid(), v_user_id, 'income', '2025-03-01', 50000.00, v_rub_id, 'Зарплата за февраль', v_cat_salary, NOW(), NOW(), 'synced')
    RETURNING id INTO v_transaction_id;

    INSERT INTO transaction_items (id, transaction_id, name, quantity, unit_price, total_price, created_at, updated_at, sync_status)
    VALUES (gen_random_uuid(), v_transaction_id, 'Оклад', 1, 50000.00, 50000.00, NOW(), NOW(), 'synced');

    -- 4. Доход – фриланс (testuser)
    INSERT INTO transactions (id, user_id, type, date, total_amount, currency_id, comment, category_id, created_at, updated_at, sync_status)
    VALUES (gen_random_uuid(), v_user_id, 'income', '2025-03-10', 15000.00, v_rub_id, 'Разработка сайта', v_cat_freelance, NOW(), NOW(), 'synced')
    RETURNING id INTO v_transaction_id;

    INSERT INTO transaction_items (id, transaction_id, name, quantity, unit_price, total_price, created_at, updated_at, sync_status)
    VALUES (gen_random_uuid(), v_transaction_id, 'Вёрстка', 1, 15000.00, 15000.00, NOW(), NOW(), 'synced');

    -- 5. Расход – транспорт (проездной) – мягко удалённая (testuser)
    INSERT INTO transactions (id, user_id, type, date, total_amount, currency_id, comment, category_id, is_deleted, created_at, updated_at, sync_status)
    VALUES (gen_random_uuid(), v_user_id, 'expense', '2025-03-05', 2000.00, v_rub_id, 'Тройка', v_cat_transport, TRUE, NOW(), NOW(), 'synced')
    RETURNING id INTO v_transaction_id;

    INSERT INTO transaction_items (id, transaction_id, name, quantity, unit_price, total_price, created_at, updated_at, sync_status)
    VALUES (gen_random_uuid(), v_transaction_id, 'Проездной', 1, 2000.00, 2000.00, NOW(), NOW(), 'synced');

    -- 6. Расход в долларах (alice)
    v_user_id := (SELECT id FROM users WHERE login = 'alice');
    v_cat_health := (SELECT id FROM categories WHERE name = 'Здоровье' AND type = 'expense' AND categories.user_id IS NULL);
    
    INSERT INTO transactions (id, user_id, type, date, total_amount, currency_id, comment, category_id, created_at, updated_at, sync_status)
    VALUES (gen_random_uuid(), v_user_id, 'expense', '2025-03-18', 55.00, v_usd_id, 'Аптека', v_cat_health, NOW(), NOW(), 'synced')
    RETURNING id INTO v_transaction_id;

    INSERT INTO transaction_items (id, transaction_id, name, quantity, unit_price, total_price, created_at, updated_at, sync_status)
    VALUES (gen_random_uuid(), v_transaction_id, 'Витамины', 1, 35.00, 35.00, NOW(), NOW(), 'synced'),
           (gen_random_uuid(), v_transaction_id, 'Маска', 2, 10.00, 20.00, NOW(), NOW(), 'synced');
END $$;