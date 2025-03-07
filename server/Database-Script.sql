create type role as enum ('USER', 'ADMIN');

alter type role owner to postgres;

create table companys
(
    id   serial
        constraint companys_pk
            primary key,
    name varchar
        constraint companys_pk_2
            unique
);

alter table companys
    owner to postgres;

create table users
(
    id       serial
        constraint users_pk
            primary key,
    username varchar
        constraint users_pk_2
            unique,
    password varchar,
    role     role,
    email    varchar
        constraint users_pk_3
            unique,
    company  integer not null
        constraint users_companys_id_fk
            references companys
);

alter table users
    owner to postgres;

create view user_with_company(user_id, username, password, email, role, company_id, company_name) as
SELECT u.id   AS user_id,
       u.username,
       u.password,
       u.email,
       u.role,
       c.id   AS company_id,
       c.name AS company_name
FROM users u
         JOIN companys c ON u.company = c.id;

alter table user_with_company
    owner to postgres;

INSERT INTO public.companys (id, name) VALUES (1, 'Demo AB');
INSERT INTO public.companys (id, name) VALUES (24, 'Test AB');

INSERT INTO public.users (username, password, role, email, company) VALUES ( 'Master', 'abc123', 'ADMIN', 'm@email.com', 1);
INSERT INTO public.users (username, password, role, email, company) VALUES ( 'no92one', 'abc123', 'USER', 'no@email.com', 1);
INSERT INTO public.users (username, password, role, email, company) VALUES ( 'Testare', 'abc123', 'ADMIN', 'test@gmail.com', 24);
